using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using Silk.NET.OpenGL;
using VoxelEngine.src.models;
using VoxelEngine.src.world.terrain;

namespace VoxelEngine.src.world;

public class World : IDisposable
{
    // Rule 2: Background threads read/write here, so we use Concurrent collections
    public ConcurrentDictionary<Vector3, Chunk> LoadedChunks { get; } = new();

    // Dedicated cache list read ONLY by the Render Thread (No collection dictionary locking)
    public List<Chunk> ChunksToRender { get; } = new();

    // Safe multi-threaded pipelines communicating back down to the main GPU layer
    private readonly ConcurrentQueue<Chunk> chunksReadyToUpload = new();
    private readonly ConcurrentQueue<Vector3> chunksToRemove = new();

    private Player player;
    private int renderDistanceRadius = 5;
    private NoiseSettings noiseSettings;
    private bool isDisposed;

    private Vector3 lastPlayerChunkPos = new Vector3(float.MaxValue);

    public World(int seed, Player player)
    {
        this.player = player;
        noiseSettings = new NoiseSettings(seed, 0.01f, 3, 0.5f, 2f, 8, 64);
    }

    public (bool Hit, Vector3 BlockPos, Vector3 HitNormal) PerformVoxelRaycast(Vector3 origin, Vector3 direction, float maxDistance)
    {
        Vector3 rayOrigin = origin;
        Vector3 rayDirection = Vector3.Normalize(direction);

        float step = 0.05f;
        Vector3 currentPos = rayOrigin;
        Vector3 previousBlockPos = new Vector3(MathF.Floor(rayOrigin.X), MathF.Floor(rayOrigin.Y), MathF.Floor(rayOrigin.Z));

        for (float distance = 0; distance < maxDistance; distance += step)
        {
            currentPos += rayDirection * step;

            int bx = (int)MathF.Floor(currentPos.X);
            int by = (int)MathF.Floor(currentPos.Y);
            int bz = (int)MathF.Floor(currentPos.Z);
            Vector3 currentBlockPos = new Vector3(bx, by, bz);

            if (currentBlockPos != previousBlockPos)
            {
                var blockState = GetBlock(bx, by, bz);
                if (blockState != null && blockState.Id != Block.AIR.DefaultState.Id)
                {
                    Vector3 hitNormal = previousBlockPos - currentBlockPos;
                    return (true, currentBlockPos, hitNormal);
                }
                previousBlockPos = currentBlockPos;
            }
        }

        return (false, Vector3.Zero, Vector3.Zero);
    }

    // =========================================================================
    // 1. GAME THREAD PIPELINE (Fixed 20 TPS, No OpenGL Allowed)
    // =========================================================================
    public void HandleGameTick(double dt, Vector3 playerWorldPos)
    {
        Vector3 playerChunkPos = GetChunkPosFromBlockPos(
            (int)MathF.Floor(playerWorldPos.X),
            (int)MathF.Floor(playerWorldPos.Y),
            (int)MathF.Floor(playerWorldPos.Z)
        );

        if (playerChunkPos != lastPlayerChunkPos)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            StreamWorldAroundPlayer(playerChunkPos); 
            stopwatch.Stop();
            lastPlayerChunkPos = playerChunkPos;
            Console.WriteLine($"Chunk updates took {stopwatch.ElapsedMilliseconds} ms");
        }

        // Handle structural dirty flags (e.g. background tasks or neighborhood updates)
        // Take a fast snapshot copy map to prevent thread access collisions during loops
        var worldSnapshot = new Dictionary<Vector3, Chunk>(LoadedChunks);
        foreach (var chunk in LoadedChunks.Values)
        {
            if (chunk.IsDirty)
            {
                // Computes structural data on background CPU thread without blocking frames!
                chunk.CompileVertexData(worldSnapshot);
                chunksReadyToUpload.Enqueue(chunk);
                chunk.IsDirty = false;
            }
        }
    }
    private void StreamWorldAroundPlayer(Vector3 playerChunkPos)
    {
        int pX = (int)playerChunkPos.X;
        int pY = (int)playerChunkPos.Y;
        int pZ = (int)playerChunkPos.Z;

        List<Chunk> newlyGeneratedChunks = new List<Chunk>();

        // =========================================================================
        // 1. STRUCTURAL PHASE: SPIRAL OUTWARD GENERATION
        // =========================================================================
        // We iterate shell by shell (radius r) starting from the center (0) out to our render distance limit
        for (int r = 0; r <= renderDistanceRadius; r++)
        {
            // For a radius of 0, we just check the single player column
            if (r == 0)
            {
                ProcessVerticalChunkColumn(pX, pZ, pY, newlyGeneratedChunks);
                continue;
            }

            // Top Row: Left to Right
            for (int x = -r; x <= r; x++)
                ProcessVerticalChunkColumn(pX + x, pZ - r, pY, newlyGeneratedChunks);

            // Right Column: Top to Bottom (skip corners already hit)
            for (int z = -r + 1; z <= r; z++)
                ProcessVerticalChunkColumn(pX + r, pZ + z, pY, newlyGeneratedChunks);

            // Bottom Row: Right to Left (skip corners already hit)
            for (int x = r - 1; x >= -r; x--)
                ProcessVerticalChunkColumn(pX + x, pZ + r, pY, newlyGeneratedChunks);

            // Left Column: Bottom to Top (skip corners already hit)
            for (int z = r - 1; z >= -r + 1; z--)
                ProcessVerticalChunkColumn(pX - r, pZ + z, pY, newlyGeneratedChunks);
        }

        // =========================================================================
        // 2. PROPAGATION PHASE: FLAGGING NEIGHBORS
        // =========================================================================
        foreach (var chunk in newlyGeneratedChunks)
        {
            foreach (var face in BlockFaceExt.Faces)
            {
                Vector3 neighborPos = chunk.Position + face.Normal();
                if (LoadedChunks.TryGetValue(neighborPos, out Chunk neighbor))
                {
                    neighbor.IsDirty = true;
                }
            }
        }

        // =========================================================================
        // 3. MATHEMATICAL MESHING PHASE
        // =========================================================================
        var worldSnapshot = new Dictionary<Vector3, Chunk>(LoadedChunks);
        foreach (var chunk in newlyGeneratedChunks)
        {
            chunk.CompileVertexData(worldSnapshot);
            chunksReadyToUpload.Enqueue(chunk);
        }

        // =========================================================================
        // 4. CLEANUP PHASE: UNLOAD OUT OF RANGE
        // =========================================================================
        foreach (var chunkPos in LoadedChunks.Keys)
        {
            float distanceX = MathF.Abs(chunkPos.X - playerChunkPos.X);
            float distanceY = MathF.Abs(chunkPos.Y - playerChunkPos.Y);
            float distanceZ = MathF.Abs(chunkPos.Z - playerChunkPos.Z);

            if (distanceX > renderDistanceRadius ||
                distanceY > renderDistanceRadius ||
                distanceZ > renderDistanceRadius)
            {
                chunksToRemove.Enqueue(chunkPos);
            }
        }
    }

    /// <summary>
    /// Helper function to populate chunks vertically at a given (X, Z) coordinate map point.
    /// Generates from the player's level outward to capture immediate ground elements fast.
    /// </summary>
    private void ProcessVerticalChunkColumn(int cx, int cz, int centerY, List<Chunk> generationList)
    {
        for (int y = -renderDistanceRadius; y <= renderDistanceRadius; y++)
        {
            Vector3 targetChunkPos = new Vector3(cx, centerY + y, cz);

            if (!LoadedChunks.ContainsKey(targetChunkPos))
            {
                Chunk chunk = new Chunk((int)targetChunkPos.X, (int)targetChunkPos.Y, (int)targetChunkPos.Z, noiseSettings);
                if (LoadedChunks.TryAdd(chunk.Position, chunk))
                {
                    generationList.Add(chunk);
                }
            }
        }
    }
    // =========================================================================
    // 2. RENDER THREAD PIPELINE (Main Thread Unlocked FPS, OpenGL Allowed)
    // =========================================================================
    public void HandleGpuUploads(GL gl)
    {
        bool renderListChanged = false;

        // Process newly meshed chunks waiting for graphics assignment
        while (chunksReadyToUpload.TryDequeue(out Chunk chunk))
        {
            // Rule 1: We carry out the actual hardware API interaction strictly here
            chunk.UploadMeshToGPU(gl);
            renderListChanged = true;
        }

        // Process removals safely inside the GL viewport instance loop context
        while (chunksToRemove.TryDequeue(out Vector3 posToRemove))
        {
            if (LoadedChunks.TryRemove(posToRemove, out Chunk chunk))
            {
                chunk.Mesh?.Dispose(); // Free GPU VRAM allocations cleanly
                renderListChanged = true;
            }
        }

        // Rule 3: Rebuild a fast array cache only when a chunk actually gets loaded/unloaded
        if (renderListChanged)
        {
            ChunksToRender.Clear();
            foreach (var chunk in LoadedChunks.Values)
            {
                if (chunk.Mesh != null && chunk.Mesh.IndexCount > 0)
                {
                    ChunksToRender.Add(chunk);
                }
            }
        }
    }

    public BlockState? GetBlock(int x, int y, int z)
    {
        Vector3 chunkPos = GetChunkPosFromBlockPos(x, y, z);

        if (LoadedChunks.TryGetValue(chunkPos, out Chunk chunk))
        {
            int localX = x - (int)chunkPos.X * Chunk.SIZE;
            int localY = y - (int)chunkPos.Y * Chunk.SIZE;
            int localZ = z - (int)chunkPos.Z * Chunk.SIZE;

            return chunk.GetBlock(localX, localY, localZ);
        }
        return null;
    }
    public void SetBlock(int x, int y, int z, ushort blockStateId)
    {
        Vector3 chunkPos = GetChunkPosFromBlockPos(x, y, z);

        if (LoadedChunks.TryGetValue(chunkPos, out Chunk chunk))
        {
            int localX = x - (int)chunkPos.X * Chunk.SIZE;
            int localY = y - (int)chunkPos.Y * Chunk.SIZE;
            int localZ = z - (int)chunkPos.Z * Chunk.SIZE;

            chunk.SetBlock(localX, localY, localZ, blockStateId);
            chunk.IsDirty = true;

            // --- UPDATE NEIGHBOR CHUNKS IF MODIFICATION LIES ON AN EDGE ---
            if (localX == 0) MarkNeighborDirty(chunkPos + new Vector3(-1, 0, 0));
            if (localX == Chunk.SIZE - 1) MarkNeighborDirty(chunkPos + new Vector3(1, 0, 0));
            if (localY == 0) MarkNeighborDirty(chunkPos + new Vector3(0, -1, 0));
            if (localY == Chunk.SIZE - 1) MarkNeighborDirty(chunkPos + new Vector3(0, 1, 0));
            if (localZ == 0) MarkNeighborDirty(chunkPos + new Vector3(0, 0, -1));
            if (localZ == Chunk.SIZE - 1) MarkNeighborDirty(chunkPos + new Vector3(0, 0, 1));
        }
    }

    private void MarkNeighborDirty(Vector3 neighborChunkPos)
    {
        if (LoadedChunks.TryGetValue(neighborChunkPos, out Chunk neighbor))
        {
            neighbor.IsDirty = true;
        }
    }

    public Vector3 GetChunkPosFromBlockPos(int x, int y, int z)
    {
        return new Vector3(
            MathF.Floor((float)x / Chunk.SIZE),
            MathF.Floor((float)y / Chunk.SIZE),
            MathF.Floor((float)z / Chunk.SIZE)
        );
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            foreach (var chunk in LoadedChunks.Values)
            {
                chunk?.Mesh?.Dispose();
            }
            LoadedChunks.Clear();
            isDisposed = true;
        }
        GC.SuppressFinalize(this);
    }
}