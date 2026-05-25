using System.Numerics;
using System.Reflection;
using VoxelEngine.src.rendering;
using static VoxelEngine.src.models.ModelData;

namespace VoxelEngine.src.models;

public class BlockModel
{
    public Dictionary<BlockFace, BakedQuad> Faces { get; private set; } = new();
    public string Name { get; private set; }
    public string Variant { get; private set; }

    public BlockModel(string name, string variant)
    {
        Variant = variant;
        Name = name;
    }

    public string GetIdentifier()
    {
        return $"{Name}{Variant}";
    }

    public void AddFace(BlockFace face, BakedQuad bakedQuad)
    {
        Faces[face] = bakedQuad;
    }
}
