using System.Numerics;

namespace VoxelEngine.src.rendering;

public record MeshData(Vector3[] Vertices, uint[] Indices)
{
    private static readonly Vector3[] baseFacePositions = new Vector3[]
    {
        new(0, 0, 0), new(1, 0, 0), new(1, 1, 0), new(0, 1, 0), // Back
        new(0, 0, 1), new(1, 0, 1), new(1, 1, 1), new(0, 1, 1)  // Front
    };
    private static readonly int[][] faceBuildOrder = new int[][]
    {
        new[] { 7, 6, 2, 3 }, // Top    / Up    / (+Y)
        new[] { 0, 1, 5, 4 }, // Bottom / Down  / (-Y)
        new[] { 1, 0, 3, 2 }, // Back   / North / (-Z)
        new[] { 4, 5, 6, 7 }, // Front  / South / (+Z)
        new[] { 5, 1, 2, 6 }, // Right  / East  / (+X)
        new[] { 0, 4, 7, 3 }, // Left   / West  / (-X)
    };

    public static MeshData CreateCube()
    {
        Vector3[] vertices = new Vector3[24];

        for (int f = 0; f < faceBuildOrder.Length; f++)
        {
            int[] face = faceBuildOrder[f];
            for (int i = 0; i < face.Length; i++)
            {
                int ind = face[i];
                vertices[f * 4 + i] = baseFacePositions[ind];
            }
        }

        uint[] indices = new uint[36]
        {
            0, 1, 2, 2, 3, 0,
            4, 5, 6, 6, 7, 4,
            8, 9, 10, 10, 11, 8,
            12, 13, 14, 14, 15, 12,
            16, 17, 18, 18, 19, 16,
            20, 21, 22, 22, 23, 20,
        };

        return new MeshData(vertices, indices);
    }
}
