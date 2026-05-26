using System.Numerics;
using System.Reflection;
using VoxelEngine.src.rendering;
using static VoxelEngine.src.models.ModelData;

namespace VoxelEngine.src.models;

public class BlockModel
{
    public Dictionary<BlockFace, List<BakedQuad>> Faces { get; private set; } = new();
    public string Name { get; private set; }
    public string Variant { get; private set; }

    public BlockModel(string name, string variant)
    {
        Variant = variant;
        Name = name;

        Faces = new()
        {
            {BlockFace.Up, new List<BakedQuad>() },
            {BlockFace.Down, new List<BakedQuad>() },
            {BlockFace.North, new List<BakedQuad>() },
            {BlockFace.South, new List<BakedQuad>() },
            {BlockFace.East, new List<BakedQuad>() },
            {BlockFace.West, new List<BakedQuad>() },
        };
    }

    public string GetIdentifier()
    {
        return $"{Name}{Variant}";
    }

    public void AddFace(BlockFace face, BakedQuad bakedQuad)
    {
        Faces[face].Add(bakedQuad);
    }
}
