using VoxelEngine.src.models;

namespace VoxelEngine.src.rendering;

public class MeshBuilder
{
    public static void BuildMesh(BlockModel model, List<Vertex> vertices, List<uint> indices)
    {
        uint offset = 0;

        foreach (var (face, quad) in model.Faces)
        {
            for (int i = 0; i < 4; i++)
            {
                vertices.Add(new Vertex(
                    quad.Positions[i],
                    quad.TexCoords[i]
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
