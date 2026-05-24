using System.Numerics;
using System.Reflection;
using VoxelEngine.src.rendering;
using static VoxelEngine.src.models.ModelData;

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

        ModelData full_block = new ModelData
        {
            Elements = new List<ModelData.ModelElement>()
            {
                new ModelData.ModelElement
                {
                    From = [0,0,0], To = [16,16,16],
                    Faces = new()
                    {
                        { BlockFace.Up, new() { UVs = [0,0,16,16], Rotation=90 } },
                        { BlockFace.Down, new() { UVs = [0,0,16,16], Rotation=90 } },
                        { BlockFace.North, new() { UVs = [0,0,16,16], Rotation=90 } },
                        { BlockFace.South, new() { UVs = [0,0,16,16], Rotation=90 } },
                        { BlockFace.East, new() { UVs = [0,0,16,16], Rotation=90 } },
                        { BlockFace.West, new() { UVs = [0,0,16,16], Rotation=90 } },
                    }
                }
            }
        };
        ModelData cross_block = new ModelData
        {
            Elements = new List<ModelElement>()
            {
                new ModelElement
                {
                    Rotation = new ModelElement.ElementRotation()
                    { 
                        Angle = 45, Axis = "y", Origin = [8,8,8], Rescale = true 
                    },
                    From = [0.8f,0,8], To = [15.2f,16,8],
                    Faces = new()
                    {
                        { BlockFace.North, new() { UVs = [0,0,16,16] } },
                        { BlockFace.South, new() { UVs = [0,0,16,16] } },
                    }
                },
                new ModelElement
                {
                    Rotation = new ModelElement.ElementRotation() 
                    { 
                        Angle = 45, Axis = "y", Origin = [8,8,8], Rescale = true 
                    },
                    From = [8,0,0.8f], To = [8,16,15.2f],
                    Faces = new()
                    {
                        { BlockFace.East, new() { UVs = [0,0,16,16] } },
                        { BlockFace.West, new() { UVs = [0,0,16,16] } },
                    }
                }
            }
        };

        BlockModel model = new BlockModel();
        // cross_block
        // full_block
        ModelData toModel = full_block;

        foreach (var element in toModel.Elements)
        {
            GenerateElement(model, element);
        }
        return model;
    }
    private static void GenerateElement(BlockModel model, ModelElement element)
    {
        ModelElement.ElementRotation? rot = element.Rotation;
        foreach (var (face, data) in element.Faces)
        {
            model.AddFace(face, GenerateQuad(face, element.From, element.To, data.UVs, data.Rotation, rot));
        }
    }
    public static BakedQuad GenerateQuad(BlockFace face, float[] from, float[] to, float[] uvs, int faceRotation, ModelElement.ElementRotation? elementRotation)
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

        Vector2[] defaultFaceUVs;
        if (uvs == null || uvs.Length != 4)
        {
            defaultFaceUVs = face switch
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
            defaultFaceUVs = [
                new(uvs[0] / 16f, 1f - uvs[3] / 16f),
                new(uvs[2] / 16f, 1f - uvs[3] / 16f),
                new(uvs[2] / 16f, 1f - uvs[1] / 16f),
                new(uvs[0] / 16f, 1f - uvs[1] / 16f)
            ];
        }

        int uvIndexOffset = faceRotation / 90;
        Vector2[] faceUvs = [
            defaultFaceUVs[(uvIndexOffset + 0) % 4],
            defaultFaceUVs[(uvIndexOffset + 1) % 4],
            defaultFaceUVs[(uvIndexOffset + 2) % 4],
            defaultFaceUVs[(uvIndexOffset + 3) % 4],
        ];

        if (elementRotation != null)
        {
            for (int i = 0; i < 4; i++)
            {
                vertices[i] = RotateElementVertex(vertices[i], elementRotation.Value);
            }
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

    private static Vector3 RotateElementVertex(Vector3 vertex, ModelElement.ElementRotation rotation)
    {
        Vector3 origin = new Vector3(rotation.Origin[0], rotation.Origin[1], rotation.Origin[2]) / 16f;
        Vector3 pos = vertex - origin;

        char axis = string.IsNullOrEmpty(rotation.Axis) ? 'y' : char.ToLowerInvariant(rotation.Axis[0]);

        float radians = rotation.Angle * (MathF.PI / 180f);
        if (rotation.Rescale && rotation.Angle != 0f)
        {
            float scale = 1f / MathF.Cos(radians);

            pos = axis switch
            {
                'x' => new Vector3(pos.X, pos.Y * scale, pos.Z * scale),
                'z' => new Vector3(pos.X * scale, pos.Y * scale, pos.Z),
                _ => new Vector3(pos.X * scale, pos.Y, pos.Z * scale) // 'y' is default
            };
        }

        Vector3 axisVector = axis switch
        {
            'x' => Vector3.UnitX,
            'z' => Vector3.UnitZ,
            _ => Vector3.UnitY
        };

        Quaternion q = Quaternion.CreateFromAxisAngle(axisVector, radians);

        return Vector3.Transform(pos, q) + origin;
    }
}
