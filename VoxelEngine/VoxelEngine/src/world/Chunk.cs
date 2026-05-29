using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using Silk.NET.OpenGL;
using VoxelEngine.src.models;
using VoxelEngine.src.rendering;
using VoxelEngine.src.world.terrain;

namespace VoxelEngine.src.world;

public class Chunk
{
    public Mesh Mesh { get; set; }
    public bool IsDirty { get; set; }

    public const int SIZE = 16;
    private readonly Vector3 TINT = new Vector3(0f, 0.73f, 0.12f);

    private int[] blocks = new int[SIZE * SIZE * SIZE];
    public Vector3 Position { get; }
    public Vector3 WorldPosition { get; }

    // Thread-safe temporary storage for raw generated mesh structures
    private Vertex[] pendingVertices;
    private uint[] pendingIndices;
    private readonly object dataLock = new();

    private bool isEmpty;

    public Chunk(int x, int y, int z, NoiseSettings noiseSettings)
    {
        Position = new Vector3(x, y, z);
        WorldPosition = Position * SIZE;
        IsDirty = false;

        GenerateChunkData(noiseSettings);
    }

    private void GenerateChunkData(NoiseSettings noiseSettings)
    {
        isEmpty = true;
        int chunkMinY = (int)WorldPosition.Y;
        int chunkMaxY = chunkMinY + SIZE;

        for (int x = 0; x < SIZE; x++)
        {
            int xOffset = x * SIZE * SIZE;
            int worldX = x + (int)WorldPosition.X;
            for (int z = 0; z < SIZE; z++)
            {
                int xzOffset = xOffset + (z * SIZE);
                int worldZ = z + (int)WorldPosition.Z;

                double noiseValue = Noise.Get2D(noiseSettings, worldX, worldZ);
                int surfaceHeight = (int)(noiseValue * noiseSettings.heightVariance) + noiseSettings.baseHeight;

                if (surfaceHeight < chunkMinY)
                {
                    FillLocalColumn(xzOffset, 0, SIZE, Block.AIR.DefaultState.Id);
                    continue;
                }
                if (surfaceHeight - 2 >= chunkMaxY)
                {
                    FillLocalColumn(xzOffset, 0, SIZE, Block.STONE.DefaultState.Id);
                    continue;
                }

                for (int y = 0; y < SIZE; y++)
                {
                    int index = xzOffset + y;
                    int blockId = blocks[index];

                    int worldY = chunkMinY + y;
                    Block block;

                    if (worldY < surfaceHeight - 2)
                        block = Block.STONE;
                    else if (worldY < surfaceHeight)
                        block = Block.DIRT;
                    else if (worldY == surfaceHeight)
                        block = Block.GRASS_BLOCK;
                    else
                        block = Block.AIR;

                    if (block != Block.AIR)
                        isEmpty = false;

                    this.blocks[index] = block.DefaultState.Id;
                }
            }
        }
    }

    private void FillLocalColumn(int xzOffset, int localMinY, int localMaxY, int stateId)
    {
        for (int y = localMinY; y < localMaxY; y++)
        {
            this.blocks[xzOffset + y] = stateId;
        }
    }

    public BlockState? GetBlock(int x, int y, int z)
    {
        if (CheckInBounds(x, y, z))
            return BlockState.ById[blocks[LocalCoordToIndex(x, y, z)]];
        return null;
    }
    public void SetBlock(int localX, int localY, int localZ, int blockStateId)
    {
        if (CheckInBounds(localX, localY, localZ))
        {
            blocks[LocalCoordToIndex(localX, localY, localZ)] = blockStateId;
        }
    }

    // =========================================================================
    // PHASE 1: GAME THREAD (Pure Math & CPU Calculations, No OpenGL)
    // =========================================================================
    public void CompileVertexData(Dictionary<Vector3, Chunk> worldSnapshot)
    {
        if (isEmpty) return; // Your optimization skip flag!

        // 1. Rent maximum-capacity blocks from the shared global pool
        // No allocation occurs here; we are just borrowing existing heap memory blocks
        Vertex[] workingVertices = ArrayPool<Vertex>.Shared.Rent(24576);
        uint[] workingIndices = ArrayPool<uint>.Shared.Rent(36864);

        int vertexCount = 0;
        int indexCount = 0;
        uint offset = 0;

        // --- Pre-fetch your neighbor chunks here (Your optimization step) ---
        worldSnapshot.TryGetValue(Position + new Vector3(-1, 0, 0), out Chunk leftChunk);
        worldSnapshot.TryGetValue(Position + new Vector3(1, 0, 0), out Chunk rightChunk);
        worldSnapshot.TryGetValue(Position + new Vector3(0, -1, 0), out Chunk bottomChunk);
        worldSnapshot.TryGetValue(Position + new Vector3(0, 1, 0), out Chunk topChunk);
        worldSnapshot.TryGetValue(Position + new Vector3(0, 0, -1), out Chunk backChunk);
        worldSnapshot.TryGetValue(Position + new Vector3(0, 0, 1), out Chunk frontChunk);

        // 2. Run your 3D loops 
        for (int x = 0; x < SIZE; x++)
        {
            int xOffset = x * SIZE * SIZE;
            for (int z = 0; z < SIZE; z++)
            {
                int xzOffset = xOffset + (z * SIZE);
                for (int y = 0; y < SIZE; y++)
                {
                    int index = xzOffset + y;
                    int blockId = blocks[index];

                    if (blockId == Block.AIR.DefaultState.Id) continue;

                    BlockState currentState = BlockState.ById[blockId];
                    BlockModel model = ModelBakery.CachedModels[currentState.ModelVariant];

                    foreach (var (face, quads) in model.Faces)
                    {
                        foreach (var quad in quads)
                        {
                            if (quad.CullFace != null)
                            {
                                Vector3 normal = quad.CullFace.Value.Normal();

                                int nx = x + (int)normal.X;
                                int ny = y + (int)normal.Y;
                                int nz = z + (int)normal.Z;

                                BlockState neighborState;

                                // If inside the current chunk bounds, read directly from local blocks array
                                if (nx >= 0 && nx < SIZE && ny >= 0 && ny < SIZE && nz >= 0 && nz < SIZE)
                                {
                                    neighborState = BlockState.ById[blocks[nx * SIZE * SIZE + nz * SIZE + ny]];
                                }
                                else
                                {
                                    // Use your pre-fetched neighbor pointers!
                                    Chunk neighborChunk = null;
                                    if (nx < 0) { neighborChunk = leftChunk; nx = SIZE - 1; }
                                    else if (nx >= SIZE) { neighborChunk = rightChunk; nx = 0; }
                                    else if (ny < 0) { neighborChunk = bottomChunk; ny = SIZE - 1; }
                                    else if (ny >= SIZE) { neighborChunk = topChunk; ny = 0; }
                                    else if (nz < 0) { neighborChunk = backChunk; nz = SIZE - 1; }
                                    else if (nz >= SIZE) { neighborChunk = frontChunk; nz = 0; }

                                    neighborState = neighborChunk?.GetBlock(nx, ny, nz) ?? Block.AIR.DefaultState;
                                }

                                if (neighborState != Block.AIR.DefaultState) continue;
                            }

                            Vector3 blockPos = new Vector3(x, y, z);

                            // Append 4 vertices directly into our rented array
                            for (int i = 0; i < 4; i++)
                            {
                                workingVertices[vertexCount++] = new Vertex(
                                    quad.Positions[i] + blockPos,
                                    quad.UVs[i],
                                    new Vector3(quad.AnimData.X, quad.AnimData.Y, quad.AnimData.Z),
                                    BitConverter.SingleToInt32Bits(quad.AnimData.W),
                                    quad.Tint == 0 ? TINT : Vector3.One
                                );
                            }

                            // Append 6 indices directly into our rented array
                            workingIndices[indexCount++] = offset + 0;
                            workingIndices[indexCount++] = offset + 1;
                            workingIndices[indexCount++] = offset + 2;
                            workingIndices[indexCount++] = offset + 2;
                            workingIndices[indexCount++] = offset + 3;
                            workingIndices[indexCount++] = offset + 0;

                            offset += 4;
                        }
                    }
                }
            }
        }

        // 3. Clone ONLY the used slice out to the final staging arrays
        // This runs lightning fast because it copies raw memory contiguous blocks
        lock (dataLock)
        {
            pendingVertices = new Vertex[vertexCount];
            Array.Copy(workingVertices, 0, pendingVertices, 0, vertexCount);

            pendingIndices = new uint[indexCount];
            Array.Copy(workingIndices, 0, pendingIndices, 0, indexCount);
        }

        // 4. Return the arrays back to the pool immediately so other chunks can use them
        ArrayPool<Vertex>.Shared.Return(workingVertices);
        ArrayPool<uint>.Shared.Return(workingIndices);
    }

    // =========================================================================
    // PHASE 2: RENDER THREAD (OpenGL Pipeline Upload execution)
    // =========================================================================
    public void UploadMeshToGPU(GL gl)
    {
        Vertex[] verticesToUpload;
        uint[] indicesToUpload;

        lock (dataLock)
        {
            if (pendingVertices == null || pendingIndices == null)
                return; // Nothing new to send down

            verticesToUpload = pendingVertices;
            indicesToUpload = pendingIndices;

            pendingVertices = null;
            pendingIndices = null;
        }

        // Safely dispose old GPU allocations inside the context thread boundary
        Mesh?.Dispose();

        // Create active hardware bindings 
        Mesh = new Mesh(gl, verticesToUpload, indicesToUpload);
    }

    private int LocalCoordToIndex(int x, int y, int z)
    {
        return x * SIZE * SIZE + z * SIZE + y;
    }

    private bool CheckInBounds(int x, int y, int z)
    {
        return !(x < 0 || x >= SIZE || y < 0 || y >= SIZE || z < 0 || z >= SIZE);
    }
}