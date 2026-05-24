using System.Numerics;
using VoxelEngine.src.rendering;

namespace VoxelEngine.src.models;

public class BlockModel
{
    public Dictionary<BlockFace, BakedQuad> Faces { get; private set; } = new();

    private BlockModel()
    {

    }

    public void AddFace(BlockFace face, BakedQuad bakedQuad)
    {
        Faces[face] = bakedQuad;
    }


    private static readonly Vector3[] baseFacePositions = new Vector3[]
    {
        new(0, 0, 0), new(1, 0, 0), new(1, 1, 0), new(0, 1, 0), // Back
        new(0, 0, 1), new(1, 0, 1), new(1, 1, 1), new(0, 1, 1)  // Front
    };
    private static readonly Dictionary<BlockFace, int[]> faceBuildOrder = new()
    {
        { BlockFace.Up, new[] { 7, 6, 2, 3 } }, // Top    / Up    / (+Y)
        { BlockFace.Down, new[] { 0, 1, 5, 4 } }, // Bottom / Down  / (-Y)
        { BlockFace.North, new[] { 1, 0, 3, 2 } }, // Back   / North / (-Z)
        { BlockFace.South, new[] { 4, 5, 6, 7 } }, // Front  / South / (+Z)
        { BlockFace.East, new[] { 5, 1, 2, 6 } }, // Right  / East  / (+X)
        { BlockFace.West, new[] { 0, 4, 7, 3 } }, // Left   / West  / (-X)
    };
    private static readonly Vector2[] baseUVs = new Vector2[]
    {
        new Vector2(0, 0),
        new Vector2(1, 0),
        new Vector2(1, 1),
        new Vector2(0, 1),
    };

    public static BlockModel Create()
    {
        BlockModel model = new BlockModel();
        foreach (var face in BlockFaceExt.Faces)
        {
            model.AddFace(face, BakeFace(face));
        }
        return model;
    }

    public static BakedQuad BakeFace(BlockFace face)
    {
        Dictionary<BlockFace, Vector2[]> TEMP_UV_MAP = new()
        {
            {BlockFace.Up, new Vector2[] {new Vector2(0,1/2f), new Vector2(1/3f, 1/2f), new Vector2(1/3f, 1f), new Vector2(0, 1f)} },
            {BlockFace.Down, new Vector2[] {new Vector2(1/3f,1/2f), new Vector2(2/3f, 1/2f), new Vector2(2/3f, 1f), new Vector2(1/3f, 1f)} },
            {BlockFace.North, new Vector2[] {new Vector2(1/3f,0), new Vector2(2/3f, 0), new Vector2(2/3f, 1/2f), new Vector2(1/3f, 1/2f)} },
            {BlockFace.South, new Vector2[] {new Vector2(2/3f, 0), new Vector2(1f,0), new Vector2(1, 1/2f), new Vector2(2/3f, 1/2f)} },
            {BlockFace.East, new Vector2[] {new Vector2(2/3f,1/2f), new Vector2(1f, 1/2f), new Vector2(1, 1f), new Vector2(2/3f, 1f)} },
            {BlockFace.West, new Vector2[] {new Vector2(0,0), new Vector2(1/3f, 0), new Vector2(1/3f, 1/2f), new Vector2(0, 1/2f)} },
        };

        int[] faceOrder = faceBuildOrder[face];
        Vector3[] positions = new Vector3[faceOrder.Length];
        Vector2[] texCoords = new Vector2[faceOrder.Length];
        for (int i = 0; i < faceOrder.Length; i++)
        {
            positions[i] = baseFacePositions[faceOrder[i]];
            texCoords[i] = TEMP_UV_MAP[face][i];
        }

        return new BakedQuad(positions, texCoords);
    }
}
