using System.Numerics;
using ServerProj.src.noise;
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

    public Chunk(int x, int y, int z, NoiseSettings noiseSettings)
    {
        Position = new Vector3(x, y, z);
        WorldPosition = Position * SIZE;
        IsDirty = false;

        GenerateChunkData(noiseSettings);
    }

    private void GenerateChunkData(NoiseSettings noiseSettings)
    {
        int before = DateTime.Now.Millisecond;


        int chunkMinY = (int)WorldPosition.Y;
        int chunkMaxY = chunkMinY + SIZE;

        for (int x = 0; x < SIZE; x++)
        {
            int worldX = (int)WorldPosition.X + x;

            for (int z = 0; z < SIZE; z++)
            {
                int worldZ = (int)WorldPosition.Z + z;

                double noiseValue = Noise.Get2D(noiseSettings, worldX, worldZ);
                int surfaceHeight = (int)(noiseValue * noiseSettings.heightVariance) + noiseSettings.baseHeight;

                if (surfaceHeight < chunkMinY)
                {
                    FillLocalColumn(x, z, 0, SIZE, Block.AIR.DefaultState.Id);
                    continue;
                }
                if (surfaceHeight - 2 >= chunkMaxY)
                {
                    FillLocalColumn(x, z, 0, SIZE, Block.STONE.DefaultState.Id);
                    continue;
                }

                for (int y = 0; y < SIZE; y++)
                {
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

                    this.blocks[LocalCoordToIndex(x, y, z)] = block.DefaultState.Id;
                }
            }
        }
        int after = DateTime.Now.Millisecond;

        Console.WriteLine($"Generated chunk in {after - before} ms");
    }

    private void FillLocalColumn(int localX, int localZ, int localMinY, int localMaxY, int stateId)
    {
        for (int y = localMinY; y < localMaxY; y++)
        {
            this.blocks[LocalCoordToIndex(localX, y, localZ)] = stateId;
        }
    }

    public BlockState? GetBlock(int x, int y, int z)
    {
        if (CheckInBounds(x, y, z))
            return BlockState.ById[blocks[LocalCoordToIndex(x, y, z)]];
        return null;
    }

    public void BuildMesh(GL gl, World world)
    {
        List<Vertex> vertices = new();
        List<uint> indices = new();
        uint offset = 0;

        for (int x = 0; x < SIZE; x++)
        {
            for (int z = 0; z < SIZE; z++)
            {
                for (int y = 0; y < SIZE; y++)
                {
                    int index = LocalCoordToIndex(x, y, z);
                    int blockId = blocks[index];

                    if (blockId != Block.AIR.DefaultState.Id)
                    {
                        BlockState currentState = BlockState.ById[blockId];
                        BlockModel model = ModelBakery.CachedModels[currentState.ModelVariant];

                        foreach (var (face, quads) in model.Faces)
                        {
                            foreach (var quad in quads)
                            {
                                if (quad.CullFace != null)
                                {
                                    Vector3 normal = quad.CullFace.Value.Normal();

                                    int dx = x + (int)normal.X + (int)WorldPosition.X;
                                    int dy = y + (int)normal.Y + (int)WorldPosition.Y;
                                    int dz = z + (int)normal.Z + (int)WorldPosition.Z;

                                    BlockState? neighborState = world.GetBlock(dx, dy, dz);

                                    if (neighborState != null && neighborState != Block.AIR.DefaultState)
                                        continue;
                                }

                                Vector3 blockPos = new Vector3(x, y, z);
                                for (int i = 0; i < 4; i++)
                                {
                                    vertices.Add(new Vertex(
                                        quad.Positions[i] + blockPos,
                                        quad.UVs[i],
                                        new Vector3(quad.AnimData.X, quad.AnimData.Y, quad.AnimData.Z),
                                        BitConverter.SingleToInt32Bits(quad.AnimData.W),
                                        quad.Tint == 0 ? TINT : Vector3.One
                                    ));
                                }

                                indices.Add(offset + 0);
                                indices.Add(offset + 1);
                                indices.Add(offset + 2);
                                indices.Add(offset + 2);
                                indices.Add(offset + 3);
                                indices.Add(offset + 0);

                                offset += 4;
                            }
                        }
                    }
                }
            }
        }

        Mesh?.Dispose();
        Mesh = new Mesh(gl, vertices.ToArray(), indices.ToArray());
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