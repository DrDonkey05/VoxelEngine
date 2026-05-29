using System.Numerics;
using Silk.NET.OpenGL;
using VoxelEngine.src.models;
using VoxelEngine.src.world.terrain;

namespace VoxelEngine.src.world;

public class World : IDisposable
{
    public Dictionary<Vector3, Chunk> LoadedChunks { get; } = new();
    private readonly List<Vector3> chunksToRemove = new();

    private Player player;
    private int renderDistanceRadius = 3;
    private NoiseSettings noiseSettings;
    private bool isDisposed;

    private Vector3 lastPlayerChunkPos = new Vector3(float.MaxValue);

    public World(int seed, Player player, GL gl)
    {
        this.player = player;
        noiseSettings = new NoiseSettings(seed, 0.01f, 3, 0.5f, 2f, 8, 16);

        Vector3 initialPos = Vector3.Zero;
        ForceStreamAndMeshEntireWorld(gl, initialPos);
        lastPlayerChunkPos = initialPos;
    }

    public void HandleUpdate(GL gl, double dt, Vector3 playerWorldPos)
    {
        Vector3 playerChunkPos = GetChunkPosFromBlockPos(
            (int)MathF.Floor(playerWorldPos.X),
            (int)MathF.Floor(playerWorldPos.Y),
            (int)MathF.Floor(playerWorldPos.Z)
        );

        if (playerChunkPos != lastPlayerChunkPos)
        {
            ForceStreamAndMeshEntireWorld(gl, playerChunkPos);
            lastPlayerChunkPos = playerChunkPos;
        }

        foreach (var chunk in LoadedChunks.Values)
        {
            if (chunk.IsDirty)
            {
                chunk.BuildMesh(gl, this);
                chunk.IsDirty = false;
            }
        }
    }

    private void ForceStreamAndMeshEntireWorld(GL gl, Vector3 playerChunkPos)
    {
        int pX = (int)playerChunkPos.X;
        int pY = (int)playerChunkPos.Y;
        int pZ = (int)playerChunkPos.Z;

        // Generate new chunks
        List<Chunk> newlyGeneratedChunks = new List<Chunk>();
        for (int x = -renderDistanceRadius; x <= renderDistanceRadius; x++)
        {
            for (int y = -renderDistanceRadius; y <= renderDistanceRadius; y++)
            {
                for (int z = -renderDistanceRadius; z <= renderDistanceRadius; z++)
                {
                    Vector3 targetChunkPos = new Vector3(pX + x, pY + y, pZ + z);

                    if (!LoadedChunks.ContainsKey(targetChunkPos))
                    {
                        Chunk chunk = new Chunk((int)targetChunkPos.X, (int)targetChunkPos.Y, (int)targetChunkPos.Z, noiseSettings);
                        LoadedChunks.Add(chunk.Position, chunk);
                        newlyGeneratedChunks.Add(chunk);
                    }
                }
            }
        }

        // Update neighbours to be dirty
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

        // Build new chunk meshes
        foreach (var chunk in newlyGeneratedChunks)
        {
            chunk.BuildMesh(gl, this);
            chunk.IsDirty = false;
        }

        // Check chunks to remove
        chunksToRemove.Clear();
        foreach (var chunkPos in LoadedChunks.Keys)
        {
            float distanceX = MathF.Abs(chunkPos.X - playerChunkPos.X);
            float distanceY = MathF.Abs(chunkPos.Y - playerChunkPos.Y);
            float distanceZ = MathF.Abs(chunkPos.Z - playerChunkPos.Z);

            if (distanceX > renderDistanceRadius ||
                distanceY > renderDistanceRadius ||
                distanceZ > renderDistanceRadius)
            {
                chunksToRemove.Add(chunkPos);
            }
        }


        // Remove a chunk
        for (int i = 0; i < chunksToRemove.Count; i++)
        {
            Vector3 pos = chunksToRemove[i];
            if (LoadedChunks.TryGetValue(pos, out Chunk chunk))
            {
                chunk.Mesh?.Dispose();
                LoadedChunks.Remove(pos);
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