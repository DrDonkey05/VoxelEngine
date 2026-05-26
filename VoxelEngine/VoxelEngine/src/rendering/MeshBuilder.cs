using System.Numerics;
using VoxelEngine.src.models;

namespace VoxelEngine.src.rendering;

public class MeshBuilder
{
    public static void BuildMesh(BlockModel model, List<Vertex> vertices, List<uint> indices)
    {
        uint offset = 0;

        Vector3 tintColor = new Vector3(0.65f, 1.0f, 0.45f);

        foreach (var (face, quads) in model.Faces)
        {
            foreach (var quad in quads)
            {
                for (int i = 0; i < 4; i++)
                {
                    vertices.Add(new Vertex(
                        quad.Positions[i],
                        new Vector3(quad.AnimData.X, quad.AnimData.Y, quad.AnimData.Z),
                        BitConverter.SingleToInt32Bits(quad.AnimData.W),
                        quad.Tint == 1 ? tintColor : Vector3.One
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
