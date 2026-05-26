using System.Numerics;
using VoxelEngine.src.rendering.textures;
using static VoxelEngine.src.models.ModelData;
using static VoxelEngine.src.models.StateData;

namespace VoxelEngine.src.models;

public static class BlockModelBakery
{
    //private static readonly Vector3[] baseFacePositions = new Vector3[]
    //{
    //    new(0, 0, 0), new(1, 0, 0), new(1, 1, 0), new(0, 1, 0), // Back
    //    new(0, 0, 1), new(1, 0, 1), new(1, 1, 1), new(0, 1, 1)  // Front
    //};
    //private static readonly Dictionary<BlockFace, int[]> faceBuildOrder = new()
    //{
    //    { BlockFace.Up, new[] { 7, 6, 2, 3 } }, // Top    / Up    / (+Y)
    //    { BlockFace.Down, new[] { 0, 1, 5, 4 } }, // Bottom / Down  / (-Y)
    //    { BlockFace.North, new[] { 1, 0, 3, 2 } }, // Back   / North / (-Z)
    //    { BlockFace.South, new[] { 4, 5, 6, 7 } }, // Front  / South / (+Z)
    //    { BlockFace.East, new[] { 5, 1, 2, 6 } }, // Right  / East  / (+X)
    //    { BlockFace.West, new[] { 0, 4, 7, 3 } }, // Left   / West  / (-X)
    //};
    //private static readonly Vector2[] baseUVs = new Vector2[]
    //{
    //    new Vector2(0, 0),
    //    new Vector2(1, 0),
    //    new Vector2(1, 1),
    //    new Vector2(0, 1),
    //};

    public static Dictionary<string, BlockModel> CachedModels = new();

    public static void CreateModels(TextureAtlas atlas)
    {
        ModelData cube = new ModelData
        {
            Elements = new List<ModelElement>()
            {
                new ModelElement
                {
                    From = [0,0,0], To = [16,16,16],
                    Faces = new()
                    {
                        { BlockFace.Up, new() { Texture = "#up" } },
                        { BlockFace.Down, new() { Texture = "#down" } },
                        { BlockFace.North, new() { Texture = "#north" } },
                        { BlockFace.South, new() { Texture = "#south" } },
                        { BlockFace.East, new() { Texture = "#east" } },
                        { BlockFace.West, new() { Texture = "#west" } },
                    }
                }
            }
        };
        ModelData cube_all = new ModelData
        {
            Parent = "block/cube",
            Textures = new()
            {
                { "up", "#all" },
                { "down", "#all" },
                { "north", "#all" },
                { "south", "#all" },
                { "east", "#all" },
                { "west", "#all" },
            }
        };
        ModelData cross = new ModelData
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
                        { BlockFace.North, new() { UVs = [0,0,16,16], Texture = "#cross" } },
                        { BlockFace.South, new() { UVs = [0,0,16,16], Texture = "#cross"  } },
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
                        { BlockFace.East, new() { UVs = [0,0,16,16], Texture = "#cross"  } },
                        { BlockFace.West, new() { UVs = [0,0,16,16], Texture = "#cross"  } },
                    }
                }
            }
        };

        ModelData up_block = new ModelData
        {
            Parent = "block/cube_all",
            Textures = new()
            {
                { "all", "block/up" }
            }
        };
        ModelData cross_block = new ModelData
        {
            Parent = "block/cross",
            Textures = new()
            {
                { "cross", "block/north" }
            }
        };
        ModelData layered_block = new ModelData
        {
            Elements = new List<ModelElement>()
            {
                new ModelData.ModelElement
                {
                    From = [0,0,0], To = [16,16,16],
                    Faces = new()
                    {
                        { BlockFace.Up, new() { UVs = [0,0,16,16], Texture = "block/up" } },
                        { BlockFace.Down, new() { UVs = [0,0,16,16], Texture = "block/down" } },
                        { BlockFace.North, new() { UVs = [0,0,16,16], Texture = "block/north" } },
                        { BlockFace.South, new() { UVs = [0,0,16,16], Texture = "block/south" } },
                        { BlockFace.East, new() { UVs = [0,0,16,16], Texture = "block/east" } },
                        { BlockFace.West, new() { UVs = [0,0,16,16], Texture = "block/west" } },
                    }
                },
                new ModelElement
                {
                    From = [0,0,0], To = [16,16,16],
                    Faces = new()
                    {
                        { BlockFace.East, new() { UVs = [0,0,16,16], Texture = "block/top_left_overlay", TintIndex = 1  } },
                        { BlockFace.West, new() { UVs = [0,0,16,16], Texture = "block/top_left_overlay", TintIndex = 1  } },
                        { BlockFace.North, new() { UVs = [0,0,16,16], Texture = "block/top_left_overlay", TintIndex = 1  } },
                        { BlockFace.South, new() { UVs = [0,0,16,16], Texture = "block/top_left_overlay", TintIndex = 1  } },
                    }
                }
            }
        };
        ModelData animated_block = new ModelData
        {
            Parent = "block/cube_all",
            Textures = new()
            {
                { "all", "block/animated" }
            }
        };

        Dictionary<string, ModelData> models = new()
        {
            { "block/cube", cube },
            { "block/cube_all", cube_all },
            { "block/cross", cross },
            { "block/up_block", up_block },
            { "block/cross_block", cross_block },
            { "block/layered_block", layered_block },
            { "block/animated_block", animated_block },
        };

        StateData full_state = new StateData
        {
            Variants = new()
            {
                {"", new()
                    {
                        Model = "block/up_block"
                    }
                },
                {"axis=z", new()
                    {
                        Model = "block/up_block",
                        X = 90
                    }
                },
                {"axis=x", new()
                    {
                        Model = "block/up_block",
                        X = 90,
                        Y = 90
                    }
                }
            }
        };
        StateData cross_state = new StateData
        {
            Variants = new()
            {
                {"", new()
                    {
                        Model = "block/cross_block"
                    }
                }
            }
        };
        StateData layered_state = new StateData
        {
            Variants = new()
            {
                {"", new()
                    {
                        Model = "block/layered_block"
                    }
                }
            }
        };
        StateData animated_state = new StateData
        {
            Variants = new()
            {
                {"", new()
                    {
                        Model = "block/animated_block"
                    }
                }
            }
        };

        List<ModelData> modelChain = new(8);
        foreach (var state in new StateData[] { full_state, cross_state, layered_state, animated_state })
        {
            foreach (var (key, variant) in state.Variants)
            {
                string identifier = $"block/{variant.Model}{key}";

                LoadModelChain(variant.Model, models, modelChain);
                Dictionary<string, string> textureMap = ResolveAndBuildTextureMap(modelChain);
                List<ModelElement>? activeElements = null;
                for (int i = 0; i < modelChain.Count; i++)
                {
                    if (modelChain[i].Elements != null && modelChain[i].Elements.Count > 0)
                    {
                        activeElements = modelChain[i].Elements;
                        break;
                    }
                }
                if (activeElements == null) continue;

                BlockModel model = new BlockModel(variant.Model, key);
                foreach (var element in activeElements)
                {
                    GenerateElement(variant, element, model, atlas, textureMap);
                }

                CachedModels.Add(identifier, model);
            }
        }
    }

    private static void LoadModelChain(string startModel, Dictionary<string, ModelData> models, List<ModelData> outChain)
    {
        outChain.Clear();
        string? currentModel = startModel;

        int depthSafety = 0;

        while (!string.IsNullOrEmpty(currentModel) && models.TryGetValue(currentModel, out ModelData data))
        {
            if (++depthSafety > 32)
            {
                Console.WriteLine($"[Error] Circular inheritance chain detected inside: {startModel}");
                break;
            }

            outChain.Add(data);
            currentModel = data.Parent;
        }
    }

    private static Dictionary<string, string> ResolveAndBuildTextureMap(List<ModelData> modelParentChain)
    {
        Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase);

        for (int i = modelParentChain.Count - 1; i >= 0; i--)
        {
            var textDict = modelParentChain[i].Textures;
            if (textDict == null) continue;

            foreach (var (key, val) in textDict)
            {
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(val))
                    map[key] = val;
            }
        }

        foreach (var key in map.Keys.ToList())
        {
            string value = map[key];
            if (value.Length > 1 && value[0] == '#')
            {
                string targetKey = value.Substring(1);

                HashSet<string> resolutionDepth = new();
                while (map.TryGetValue(targetKey, out string? resolved) && resolved.StartsWith('#'))
                {
                    if (!resolutionDepth.Add(targetKey)) break;
                    targetKey = resolved.Substring(1);
                }

                if (map.TryGetValue(targetKey, out string? finalValue))
                {
                    map[key] = finalValue;
                }
            }
        }

        return map;
    }

    private static void GenerateElement(
        StateVariant variant, ModelElement element, BlockModel model,
        TextureAtlas atlas, Dictionary<string, string> textureMap)
    {
        ModelElement.ElementRotation? rot = element.Rotation;
        foreach (var (face, data) in element.Faces)
        {
            string texKey = data.Texture[0] == '#' ? data.Texture.Substring(1) : data.Texture;
            BakedQuad quad = GenerateQuad(face, element.From, element.To, data.UVs, data.Rotation, textureMap.GetValueOrDefault(texKey, texKey), data.TintIndex, rot, variant, atlas);

            ApplyModelRotation(new Vector3(0.5f, 0.5f, 0.5f), variant.X, variant.Y, ref quad);

            BlockFace worldFace = face;

            int xSteps = (variant.X % 360) / 90;
            for (int i = 0; i < xSteps; i++) worldFace = worldFace.RotateX90();

            int ySteps = (variant.Y % 360) / 90;
            for (int i = 0; i < ySteps; i++) worldFace = worldFace.RotateY90();

            model.AddFace(worldFace, quad);
        }
    }

    public static BakedQuad GenerateQuad(BlockFace face,
        float[] from, float[] to, float[] uvs, int faceRotation, string tex, int tintInd,
        ModelElement.ElementRotation? elementRotation, StateVariant state, TextureAtlas atlas)
    {
        Vector3 min = new Vector3(from[0], from[1], from[2]) / 16f;
        Vector3 max = new Vector3(to[0], to[1], to[2]) / 16f;

        // Calculates rotated vertices
        CalculateVertices(face, min, max, out Vector3 v0, out Vector3 v1, out Vector3 v2, out Vector3 v3);
        RotateVertices(elementRotation, ref v0, ref v1, ref v2, ref v3);

        // Calculates rotated UVs
        // CalculateUVs(face, faceRotation, uvs, v0, v1, v2, v3,
        //     out Vector2 t0, out Vector2 t1, out Vector2 t2, out Vector2 t3);
        // RotateUVs(faceRotation, ref t0, ref t1, ref t2, ref t3);

        Vector4 animData = atlas.GetAnimationData(tex);

        return new BakedQuad([v0, v1, v2, v3], animData, tintInd);
    }

    private static void CalculateUVs(BlockFace face, int faceRotation, float[] uvs,
        Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
        out Vector2 uv0, out Vector2 uv1, out Vector2 uv2, out Vector2 uv3)
    {
        if (uvs == null || uvs.Length != 4)
        {
            switch (face)
            {
                case BlockFace.Up: uv0 = new(v0.X, 1f - v0.Z); uv1 = new(v1.X, 1f - v1.Z); uv2 = new(v2.X, 1f - v2.Z); uv3 = new(v3.X, 1f - v3.Z); break;
                case BlockFace.Down: uv0 = new(v0.X, v0.Z); uv1 = new(v1.X, v1.Z); uv2 = new(v2.X, v2.Z); uv3 = new(v3.X, v3.Z); break;
                case BlockFace.North: uv0 = new(1f - v0.X, v0.Y); uv1 = new(1f - v1.X, v1.Y); uv2 = new(1f - v2.X, v2.Y); uv3 = new(1f - v3.X, v3.Y); break;
                case BlockFace.South: uv0 = new(v0.X, v0.Y); uv1 = new(v1.X, v1.Y); uv2 = new(v2.X, v2.Y); uv3 = new(v3.X, v3.Y); break;
                case BlockFace.West: uv0 = new(v0.Z, v0.Y); uv1 = new(v1.Z, v1.Y); uv2 = new(v2.Z, v2.Y); uv3 = new(v3.Z, v3.Y); break;
                case BlockFace.East: uv0 = new(1f - v0.Z, v0.Y); uv1 = new(1f - v1.Z, v1.Y); uv2 = new(1f - v2.Z, v2.Y); uv3 = new(1f - v3.Z, v3.Y); break;
                default: throw new ArgumentOutOfRangeException(nameof(face));
            }
        }
        else
        {
            uv0 = new(uvs[0] / 16f, 1f - uvs[3] / 16f);
            uv1 = new(uvs[2] / 16f, 1f - uvs[3] / 16f);
            uv2 = new(uvs[2] / 16f, 1f - uvs[1] / 16f);
            uv3 = new(uvs[0] / 16f, 1f - uvs[1] / 16f);
        }
    }

    private static void RotateUVs(int rotation, ref Vector2 t0, ref Vector2 t1, ref Vector2 t2, ref Vector2 t3)
    {
        Vector2 originalT0 = t0, originalT1 = t1, originalT2 = t2, originalT3 = t3;
        int uvIndexOffset = rotation / 90;
        t0 = (uvIndexOffset % 4) switch { 0 => originalT0, 1 => originalT1, 2 => originalT2, _ => originalT3 };
        t1 = ((uvIndexOffset + 1) % 4) switch { 0 => originalT0, 1 => originalT1, 2 => originalT2, _ => originalT3 };
        t2 = ((uvIndexOffset + 2) % 4) switch { 0 => originalT0, 1 => originalT1, 2 => originalT2, _ => originalT3 };
        t3 = ((uvIndexOffset + 3) % 4) switch { 0 => originalT0, 1 => originalT1, 2 => originalT2, _ => originalT3 };
    }

    private static void CalculateVertices(BlockFace face, Vector3 min, Vector3 max,
        out Vector3 v0, out Vector3 v1, out Vector3 v2, out Vector3 v3)
    {
        switch (face)
        {
            case BlockFace.Up: v0 = new(min.X, max.Y, max.Z); v1 = new(max.X, max.Y, max.Z); v2 = new(max.X, max.Y, min.Z); v3 = new(min.X, max.Y, min.Z); break;
            case BlockFace.Down: v0 = new(min.X, min.Y, min.Z); v1 = new(max.X, min.Y, min.Z); v2 = new(max.X, min.Y, max.Z); v3 = new(min.X, min.Y, max.Z); break;
            case BlockFace.North: v0 = new(max.X, min.Y, min.Z); v1 = new(min.X, min.Y, min.Z); v2 = new(min.X, max.Y, min.Z); v3 = new(max.X, max.Y, min.Z); break;
            case BlockFace.South: v0 = new(min.X, min.Y, max.Z); v1 = new(max.X, min.Y, max.Z); v2 = new(max.X, max.Y, max.Z); v3 = new(min.X, max.Y, max.Z); break;
            case BlockFace.East: v0 = new(max.X, min.Y, max.Z); v1 = new(max.X, min.Y, min.Z); v2 = new(max.X, max.Y, min.Z); v3 = new(max.X, max.Y, max.Z); break;
            case BlockFace.West: v0 = new(min.X, min.Y, min.Z); v1 = new(min.X, min.Y, max.Z); v2 = new(min.X, max.Y, max.Z); v3 = new(min.X, max.Y, min.Z); break;
            default: throw new ArgumentOutOfRangeException(nameof(face));
        }
    }

    private static void RotateVertices(ModelElement.ElementRotation? rotation,
        ref Vector3 v0, ref Vector3 v1, ref Vector3 v2, ref Vector3 v3)
    {
        if (rotation != null)
        {
            var rot = rotation.Value;
            RotateVertex(rot, ref v0);
            RotateVertex(rot, ref v1);
            RotateVertex(rot, ref v2);
            RotateVertex(rot, ref v3);
        }
    }

    private static void RotateVertex(ModelElement.ElementRotation rotation, ref Vector3 vertex)
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
                _ => new Vector3(pos.X * scale, pos.Y, pos.Z * scale)
            };
        }

        Vector3 axisVector = axis switch
        {
            'x' => Vector3.UnitX,
            'z' => Vector3.UnitZ,
            _ => Vector3.UnitY
        };

        Quaternion q = Quaternion.CreateFromAxisAngle(axisVector, radians);

        vertex = Vector3.Transform(pos, q) + origin;
    }

    private static void ApplyModelRotation(Vector3 center, float rotX, float rotY, ref BakedQuad quad)
    {
        if (rotX == 0 && rotY == 0) return;

        float radX = rotX * (MathF.PI / 180f);
        float radY = rotY * (MathF.PI / 180f);
        Quaternion blockRot = Quaternion.CreateFromAxisAngle(-Vector3.UnitY, radY)
                            * Quaternion.CreateFromAxisAngle(Vector3.UnitX, radX);

        for (int i = 0; i < 4; i++)
        {
            quad.Positions[i] = Vector3.Transform(quad.Positions[i] - center, blockRot) + center;
        }
    }
}
