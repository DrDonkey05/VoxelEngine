using System.Numerics;
using VoxelEngine.src.rendering;

namespace VoxelEngine.src.models;

public class BlockModel
{
    public Dictionary<BlockFace, BakedQuad> Faces { get; private set; } = new();

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

    public static BlockModel CreateModel()
    {
        float[] from = { 5f, 5f, 5f };
        float[] to = { 11f, 11f, 11f };

        BlockModel model = new BlockModel();
        Dictionary<BlockFace, bool> facesToDo = new()
        {
            {BlockFace.Up, true},
            {BlockFace.Down, true},
            {BlockFace.North, true},
            {BlockFace.South, true},
            {BlockFace.East, true},
            {BlockFace.West, true},
        };
        foreach (var (face, todo) in facesToDo)
        {
            if (!todo)
                continue;
            model.AddFace(face, GenerateQuad(face, from, to, new float[] {8f, 5f, 14f, 11f}));
        }
        return model;
    }
    public static BakedQuad GenerateQuad(BlockFace face, float[] from, float[] to, float[] uvs)
    {
        Vector3 minV = new Vector3(from[0] / 16f, from[1] / 16f, from[2] / 16f);
        Vector3 maxV = new Vector3(to[0] / 16f, to[1] / 16f, to[2] / 16f);

        Vector3[] vertices = face switch
        {
            BlockFace.Up => [new(minV.X, maxV.Y, maxV.Z), new(maxV.X, maxV.Y, maxV.Z), new(maxV.X, maxV.Y, minV.Z), new(minV.X, maxV.Y, minV.Z)],
            BlockFace.Down => [new(minV.X, minV.Y, minV.Z), new(maxV.X, minV.Y, minV.Z), new(maxV.X, minV.Y, maxV.Z), new(minV.X, minV.Y, maxV.Z)],
            BlockFace.North => [new(maxV.X, minV.Y, minV.Z), new(minV.X, minV.Y, minV.Z), new(minV.X, maxV.Y, minV.Z), new(maxV.X, maxV.Y, minV.Z)],
            BlockFace.South => [new(minV.X, minV.Y, maxV.Z), new(maxV.X, minV.Y, maxV.Z), new(maxV.X, maxV.Y, maxV.Z), new(minV.X, maxV.Y, maxV.Z)],
            BlockFace.East => [new(maxV.X, minV.Y, maxV.Z), new(maxV.X, minV.Y, minV.Z), new(maxV.X, maxV.Y, minV.Z), new(maxV.X, maxV.Y, maxV.Z)],
            BlockFace.West => [new(minV.X, minV.Y, minV.Z), new(minV.X, minV.Y, maxV.Z), new(minV.X, maxV.Y, maxV.Z), new(minV.X, maxV.Y, minV.Z)],
            _ => throw new ArgumentOutOfRangeException(nameof(face))
        };

        Vector2[] faceUvs; 
        if (uvs == null || uvs.Length != 4)
        {
            faceUvs = face switch
            {
                BlockFace.Up => [
                    new(vertices[0].X, 1f - vertices[0].Z),
                new(vertices[1].X, 1f - vertices[1].Z),
                new(vertices[2].X, 1f - vertices[2].Z),
                new(vertices[3].X, 1f - vertices[3].Z)
                ],
                BlockFace.Down => [
                    new(vertices[0].X, vertices[0].Z),
                new(vertices[1].X, vertices[1].Z),
                new(vertices[2].X, vertices[2].Z),
                new(vertices[3].X, vertices[3].Z)
                ],
                BlockFace.North => [
                    new(1f - vertices[0].X, vertices[0].Y),
                new(1f - vertices[1].X, vertices[1].Y),
                new(1f - vertices[2].X, vertices[2].Y),
                new(1f - vertices[3].X, vertices[3].Y)
                ],
                BlockFace.South => [
                    new(vertices[0].X, vertices[0].Y),
                new(vertices[1].X, vertices[1].Y),
                new(vertices[2].X, vertices[2].Y),
                new(vertices[3].X, vertices[3].Y)
                ],
                BlockFace.West => [
                    new(vertices[0].Z, vertices[0].Y),
                new(vertices[1].Z, vertices[1].Y),
                new(vertices[2].Z, vertices[2].Y),
                new(vertices[3].Z, vertices[3].Y)
                ],
                BlockFace.East => [
                    new(1f - vertices[0].Z, vertices[0].Y),
                new(1f - vertices[1].Z, vertices[1].Y),
                new(1f - vertices[2].Z, vertices[2].Y),
                new(1f - vertices[3].Z, vertices[3].Y)
                ],
                _ => throw new ArgumentOutOfRangeException(nameof(face))
            };
        }
        else
        {
            faceUvs = [
                new(uvs[0] / 16f, 1f - uvs[3] / 16f),
                new(uvs[2] / 16f, 1f - uvs[3] / 16f),
                new(uvs[2] / 16f, 1f - uvs[1] / 16f),
                new(uvs[0] / 16f, 1f - uvs[1] / 16f)
            ];
        }

        void SimulateTextureAtlasLookup()
        {
            // Only works because I am hardcoding for the rots.png texture atlas
            Vector2 factor = face switch
            {
                BlockFace.Up => new Vector2(0, 1),
                BlockFace.Down => new Vector2(1, 1),
                BlockFace.East => new Vector2(0, 0),
                BlockFace.West => new Vector2(2, 1),
                BlockFace.North => new Vector2(1, 0),
                BlockFace.South => new Vector2(2, 0),
                _ => throw new ArgumentOutOfRangeException(nameof(face))
            };
            faceUvs[0] = new Vector2(faceUvs[0].X / 3f + 1 / 3f * factor.X, faceUvs[0].Y / 2f + 1 / 2f * factor.Y);
            faceUvs[1] = new Vector2(faceUvs[1].X / 3f + 1 / 3f * factor.X, faceUvs[1].Y / 2f + 1 / 2f * factor.Y);
            faceUvs[2] = new Vector2(faceUvs[2].X / 3f + 1 / 3f * factor.X, faceUvs[2].Y / 2f + 1 / 2f * factor.Y);
            faceUvs[3] = new Vector2(faceUvs[3].X / 3f + 1 / 3f * factor.X, faceUvs[3].Y / 2f + 1 / 2f * factor.Y);
        }
        SimulateTextureAtlasLookup();

        return new BakedQuad(vertices, faceUvs);
    }
}
