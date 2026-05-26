using System.Numerics;
using Silk.NET.OpenGL;
using VoxelEngine.src.models;
using VoxelEngine.src.rendering;

namespace VoxelEngine.src.world;

public class Chunk
{
    public Mesh Mesh { get; set; }

    private const int SIZE = 16;
    private readonly Vector3 TINT = new Vector3(0.65f, 1.0f, 0.45f);

    private bool[] blocks = new bool[SIZE * SIZE * SIZE];
    private Vector3 position;

    public Chunk(int x, int y, int z)
    {
        position = new Vector3(x, y, z);

        Array.Fill(blocks, true);
    }

    public void BuildMesh(GL gl)
    {
        List<Vertex> vertices = new();
        List<uint> indices = new();

        string stone = "block/animated_block";
        string dirt = "block/up_block";
        string grass = "block/layered_block";
        string feature = "block/cross_block";

        // TODO: This is basically terrain generation and mesh generation in one lol
        //       Split them later. Instead of array.fill in the constructor,
        //       run through a chunk generation process

        uint offset = 0;
        for (int x = 0; x < SIZE; x++)
        {
            for (int z = 0; z < SIZE; z++)
            {
                for (int y = 0; y < SIZE; y++)
                {
                    int index = LocalCoordToIndex(x, y, z);

                    Block block;
                    if (y < SIZE - 4)
                        block = Block.ANIMATED_BLOCK;
                    else if (y < SIZE - 2)
                        block = Block.UP_BLOCK;
                    else if (y < SIZE - 1)
                        block = Block.LAYERED_BLOCK;
                    else
                        block = Block.CROSS_BLOCK;

                    string key = block.DefaultState.ModelVariant;
                    BlockModel model = BlockModelBakery.CachedModels[key];

                    if (blocks[index])
                    {
                        foreach (var (face, quads) in model.Faces)
                        {
                            foreach (var quad in quads)
                            {
                                if (quad.CullFace != null)
                                {
                                    Vector3 normal = quad.CullFace.Value.Normal();
                                    int dx = x + (int)normal.X, dy = y + (int)normal.Y, dz = z + (int)normal.Z;

                                    if (CheckInBounds(dx, dy, dz))
                                    {
                                        int checkIndex = LocalCoordToIndex(dx, dy, dz);

                                        if (blocks[checkIndex])
                                            continue;
                                    }
                                }

                                Vector3 blockPos = new Vector3(x, y, z);
                                for (int i = 0; i < 4; i++)
                                {
                                    vertices.Add(new Vertex(
                                        quad.Positions[i] + blockPos,
                                        new Vector3(quad.AnimData.X, quad.AnimData.Y, quad.AnimData.Z),
                                        BitConverter.SingleToInt32Bits(quad.AnimData.W),
                                        quad.Tint == 1 ? TINT : Vector3.One
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

    public Vector3 WorldPosition => position * SIZE;
}
