using System.Numerics;
using VoxelEngine.src.json;
using VoxelEngine.src.rendering.textures;

namespace VoxelEngine.src.models;

public static class ModelBakery
{
    public static Dictionary<string, BlockModel> CachedModels = new();
    private static Dictionary<string, JsonModel> fetchedModels = new();

    public static void ClearJsonCaches() => fetchedModels.Clear();

    public static void CreateModel(List<JsonState.StateVariant> variantParts, string modelName, string variantKey, TextureAtlas atlas)
    {
        if (variantKey == "cobblestone_walleast=none,north=none,south=none,up=none,west=tall")
        {

        }
        List<JsonModel> modelChain = new(8);
        string identifier = $"{variantKey}";

        BlockModel model = new BlockModel(modelName, variantKey);

        foreach (var variant in variantParts)
        {
            LoadModelChain(variant.Model, fetchedModels, modelChain);
            Dictionary<string, string> textureMap = ResolveAndBuildTextureMap(modelChain);
            List<JsonModel.Element>? activeElements = null;
            for (int i = 0; i < modelChain.Count; i++)
            {
                if (modelChain[i].Elements != null && modelChain[i].Elements.Count > 0)
                {
                    activeElements = modelChain[i].Elements;
                    break;
                }
            }
            if (activeElements == null) continue;

            foreach (var element in activeElements)
            {
                GenerateElement(variant, element, model, atlas, textureMap);
            }
        }

        if (!CachedModels.ContainsKey(identifier))
        {
            CachedModels.Add(identifier, model);
        }
    }

    private static void GenerateElement(
        JsonState.StateVariant variant, JsonModel.Element element, BlockModel model,
        TextureAtlas atlas, Dictionary<string, string> textureMap)
    {
        foreach (var (face, data) in element.Faces)
        {
            string texKey = data.Texture[0] == '#' ? data.Texture.Substring(1) : data.Texture;

            BlockFace? blockFace = BlockFaceExt.FromString(face);
            if (blockFace == null) throw new Exception($"{face} is not a valid Block Face");
            BlockFace? cullFace = BlockFaceExt.FromString(data.CullFace);

            BlockFace rotatedFace = blockFace.Value;
            int xSteps = (variant.X % 360) / 90;
            for (int i = 0; i < xSteps; i++)
            {
                rotatedFace = rotatedFace.RotateX90();
                if (cullFace != null) cullFace = cullFace.Value.RotateX90();
            }

            int ySteps = (variant.Y % 360) / 90;
            for (int i = 0; i < ySteps; i++)
            {
                rotatedFace = rotatedFace.RotateY90();
                if (cullFace != null) cullFace = cullFace.Value.RotateY90();
            }

            Vector3 from = new Vector3(element.From[0], element.From[1], element.From[2]) / 16f;
            Vector3 to = new Vector3(element.To[0], element.To[1], element.To[2]) / 16f;

            Vector4? uv = null;
            if (data.UV != null && data.UV.Length == 4)
                uv = new Vector4(data.UV[0], data.UV[1], data.UV[2], data.UV[3]);

            Vector3[] positions = new Vector3[4];
            Vector2[] uvs = new Vector2[4];

            ApplyModelChanges(variant.X, variant.Y, variant.UVLock, from, to, uv, blockFace.Value, rotatedFace, ref positions, ref uvs);

            if (element.Rotation != null)
            {
                Vector3 origin = new Vector3(element.Rotation.Origin[0], element.Rotation.Origin[1], element.Rotation.Origin[2]);
                ApplyElementChanges(element.Rotation.Angle, element.Rotation.Axis, origin, element.Rotation.Rescale, ref positions);
            }

            if (data.Rotation != 0)
            {
                ApplyFaceChanges(data.Rotation, ref uvs);
            }

            Vector4 animData = atlas.GetAnimationData(textureMap.GetValueOrDefault(texKey, texKey));
            BakedQuad quad = new BakedQuad(positions, uvs, animData, data.TintIndex, cullFace);

            model.AddFace(rotatedFace, quad);
        }
    }

    public static void ApplyFaceChanges(int rotation, ref Vector2[] uvs)
    {
        int steps = ((rotation % 360) + 360) % 360 / 90;
        if (steps == 0) return;

        Vector2 t0 = uvs[0], t1 = uvs[1], t2 = uvs[2], t3 = uvs[3];
        switch (steps)
        {
            case 1: uvs[0] = t1; uvs[1] = t2; uvs[2] = t3; uvs[3] = t0; break; // 90 Deg CW
            case 2: uvs[0] = t2; uvs[1] = t3; uvs[2] = t0; uvs[3] = t1; break; // 180 Deg
            case 3: uvs[0] = t3; uvs[1] = t0; uvs[2] = t1; uvs[3] = t2; break; // 270 Deg CW
        }
    }

    private static void ApplyElementChanges(float angle, string axis, Vector3 origin, bool rescale, ref Vector3[] positions)
    {
        if (angle == 0) return;

        Vector3 scaledOrigin = origin / 16f;
        char axisChar = string.IsNullOrEmpty(axis) ? 'y' : char.ToLowerInvariant(axis[0]);
        float radians = angle * (MathF.PI / 180f);

        Vector3 p0 = positions[0] - scaledOrigin,
                p1 = positions[1] - scaledOrigin,
                p2 = positions[2] - scaledOrigin,
                p3 = positions[3] - scaledOrigin;

        if (rescale)
        {
            float scale = 1f / MathF.Cos(radians);
            switch (axisChar)
            {
                case 'x':
                    p0.Y *= scale; p0.Z *= scale; p1.Y *= scale; p1.Z *= scale;
                    p2.Y *= scale; p2.Z *= scale; p3.Y *= scale; p3.Z *= scale;
                    break;
                case 'z':
                    p0.X *= scale; p0.Y *= scale; p1.X *= scale; p1.Y *= scale;
                    p2.X *= scale; p2.Y *= scale; p3.X *= scale; p3.Y *= scale;
                    break;
                default:
                    p0.X *= scale; p0.Z *= scale; p1.X *= scale; p1.Z *= scale;
                    p2.X *= scale; p2.Z *= scale; p3.X *= scale; p3.Z *= scale;
                    break;
            }
        }

        Vector3 axisVector = axisChar switch { 'x' => Vector3.UnitX, 'z' => Vector3.UnitZ, _ => Vector3.UnitY };
        Quaternion q = Quaternion.CreateFromAxisAngle(axisVector, radians);

        positions[0] = Vector3.Transform(p0, q) + scaledOrigin;
        positions[1] = Vector3.Transform(p1, q) + scaledOrigin;
        positions[2] = Vector3.Transform(p2, q) + scaledOrigin;
        positions[3] = Vector3.Transform(p3, q) + scaledOrigin;
    }

    private static void ApplyModelChanges(
    int rotX, int rotY, bool uvLock, Vector3 min, Vector3 max, Vector4? uv, BlockFace originalFace, BlockFace rotatedFace,
    ref Vector3[] positions, ref Vector2[] uvs)
    {
        switch (originalFace)
        {
            case BlockFace.Up:      positions[0] = new(min.X, max.Y, max.Z); positions[1] = new(max.X, max.Y, max.Z); positions[2] = new(max.X, max.Y, min.Z); positions[3] = new(min.X, max.Y, min.Z); break;
            case BlockFace.Down:    positions[0] = new(min.X, min.Y, min.Z); positions[1] = new(max.X, min.Y, min.Z); positions[2] = new(max.X, min.Y, max.Z); positions[3] = new(min.X, min.Y, max.Z); break;
            case BlockFace.North:   positions[0] = new(max.X, min.Y, min.Z); positions[1] = new(min.X, min.Y, min.Z); positions[2] = new(min.X, max.Y, min.Z); positions[3] = new(max.X, max.Y, min.Z); break;
            case BlockFace.South:   positions[0] = new(min.X, min.Y, max.Z); positions[1] = new(max.X, min.Y, max.Z); positions[2] = new(max.X, max.Y, max.Z); positions[3] = new(min.X, max.Y, max.Z); break;
            case BlockFace.East:    positions[0] = new(max.X, min.Y, max.Z); positions[1] = new(max.X, min.Y, min.Z); positions[2] = new(max.X, max.Y, min.Z); positions[3] = new(max.X, max.Y, max.Z); break;
            case BlockFace.West:    positions[0] = new(min.X, min.Y, min.Z); positions[1] = new(min.X, min.Y, max.Z); positions[2] = new(min.X, max.Y, max.Z); positions[3] = new(min.X, max.Y, min.Z); break;
            default: throw new ArgumentOutOfRangeException(nameof(originalFace));
        }

        if (uv == null)
        {
            float uMin, uMax, vMin, vMax;
            switch (originalFace)
            {
                case BlockFace.Up: uMin = min.X; uMax = max.X; vMin = min.Z; vMax = max.Z; break;
                case BlockFace.Down: uMin = min.X; uMax = max.X; vMin = min.Z; vMax = max.Z; break;
                case BlockFace.North: uMin = min.X; uMax = max.X; vMin = min.Y; vMax = max.Y; break;
                case BlockFace.South: uMin = min.X; uMax = max.X; vMin = min.Y; vMax = max.Y; break;
                case BlockFace.East: uMin = min.Z; uMax = max.Z; vMin = min.Y; vMax = max.Y; break;
                case BlockFace.West: uMin = min.Z; uMax = max.Z; vMin = min.Y; vMax = max.Y; break;
                default: throw new ArgumentOutOfRangeException();
            }

            float yAdj = 1f - vMax + 0f - vMin;
            uvs[0] = new Vector2(uMin, vMin + yAdj); uvs[1] = new Vector2(uMax, vMin + yAdj);
            uvs[2] = new Vector2(uMax, vMax + yAdj); uvs[3] = new Vector2(uMin, vMax + yAdj);
        }
        else
        {
            uvs[0] = new(uv.Value.X / 16f, 1f - uv.Value.W / 16f);
            uvs[1] = new(uv.Value.Z / 16f, 1f - uv.Value.W / 16f);
            uvs[2] = new(uv.Value.Z / 16f, 1f - uv.Value.Y / 16f);
            uvs[3] = new(uv.Value.X / 16f, 1f - uv.Value.Y / 16f);

            if (uvLock)
            {
                if (rotX != 0) ApplyFaceChanges(360 - rotX, ref uvs);
                if (rotY != 0) ApplyFaceChanges(360 - rotY, ref uvs);
            }
        }

        if (uvLock)
        {
            if (rotX != 0 && (originalFace == BlockFace.East || originalFace == BlockFace.West))
                ApplyFaceChanges(360 - rotX, ref uvs);
            if (rotY != 0 && (originalFace == BlockFace.Up || originalFace == BlockFace.Down))
                ApplyFaceChanges(360 - rotY, ref uvs);
        }

        if (rotX != 0 || rotY != 0)
        {
            Vector3 center = new Vector3(0.5f, 0.5f, 0.5f);
            float radX = rotX * (MathF.PI / 180f);
            float radY = rotY * (MathF.PI / 180f);

            Quaternion blockRot = Quaternion.CreateFromAxisAngle(-Vector3.UnitY, radY)
                                * Quaternion.CreateFromAxisAngle(Vector3.UnitX, radX);

            positions[0] = Vector3.Transform(positions[0] - center, blockRot) + center;
            positions[1] = Vector3.Transform(positions[1] - center, blockRot) + center;
            positions[2] = Vector3.Transform(positions[2] - center, blockRot) + center;
            positions[3] = Vector3.Transform(positions[3] - center, blockRot) + center;

            if (uvLock)
            {
                if (originalFace == BlockFace.Up || originalFace == BlockFace.Down)
                {
                    int turns = (rotY / 90) % 4;
                    if (turns == 1)
                    {
                        Vector2[] newUVs = new Vector2[4];
                        newUVs[3] = new Vector2(uvs[0].Y - 0.5f, -1 * (uvs[0].X - 0.5f)) + new Vector2(0.5f, 0.5f);
                        newUVs[0] = new Vector2(uvs[1].Y - 0.5f, -1 * (uvs[1].X - 0.5f)) + new Vector2(0.5f, 0.5f);
                        newUVs[1] = new Vector2(uvs[2].Y - 0.5f, -1 * (uvs[2].X - 0.5f)) + new Vector2(0.5f, 0.5f);
                        newUVs[2] = new Vector2(uvs[3].Y - 0.5f, -1 * (uvs[3].X - 0.5f)) + new Vector2(0.5f, 0.5f);
                        uvs = newUVs;

                    }
                    else if (turns == 2)
                    {
                        Vector2[] newUVs = new Vector2[4];
                        newUVs[2] = new Vector2(-1 * (uvs[0].X - 0.5f), -1 * (uvs[0].Y - 0.5f)) + new Vector2(0.5f, 0.5f);
                        newUVs[3] = new Vector2(-1 * (uvs[1].X - 0.5f), -1 * (uvs[1].Y - 0.5f)) + new Vector2(0.5f, 0.5f);
                        newUVs[0] = new Vector2(-1 * (uvs[2].X - 0.5f), -1 * (uvs[2].Y - 0.5f)) + new Vector2(0.5f, 0.5f);
                        newUVs[1] = new Vector2(-1 * (uvs[3].X - 0.5f), -1 * (uvs[3].Y - 0.5f)) + new Vector2(0.5f, 0.5f);
                        uvs = newUVs;
                    }
                    else if (turns == 3)
                    {
                        Vector2[] newUVs = new Vector2[4];
                        newUVs[1] = new Vector2(-1 * (uvs[0].Y - 0.5f), uvs[0].X - 0.5f) + new Vector2(0.5f, 0.5f);
                        newUVs[2] = new Vector2(-1 * (uvs[1].Y - 0.5f), uvs[1].X - 0.5f) + new Vector2(0.5f, 0.5f);
                        newUVs[3] = new Vector2(-1 * (uvs[2].Y - 0.5f), uvs[2].X - 0.5f) + new Vector2(0.5f, 0.5f);
                        newUVs[0] = new Vector2(-1 * (uvs[3].Y - 0.5f), uvs[3].X - 0.5f) + new Vector2(0.5f, 0.5f);
                        uvs = newUVs;
                    }
                }
            }
        }
    }

    private static void LoadModelChain(string startModel, Dictionary<string, JsonModel> models, List<JsonModel> outChain)
    {
        outChain.Clear();
        string? currentModel = startModel;
        int depthSafety = 0;
        JsonModel? model;
        while (!string.IsNullOrEmpty(currentModel) &&
             (models.TryGetValue(currentModel, out model) ||
             (model = JsonLoader.Load<JsonModel>($"./assets/models/{currentModel}.json")) != null))
        {
            if (++depthSafety > 32) break;
            outChain.Add(model);
            currentModel = model.Parent;
        }
    }

    private static Dictionary<string, string> ResolveAndBuildTextureMap(List<JsonModel> modelParentChain)
    {
        Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase);
        for (int i = modelParentChain.Count - 1; i >= 0; i--)
        {
            var textDict = modelParentChain[i].Textures;
            if (textDict == null) continue;
            foreach (var (key, val) in textDict)
            {
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(val)) map[key] = val;
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
                if (map.TryGetValue(targetKey, out string? finalValue)) map[key] = finalValue;
            }
        }
        return map;
    }
}