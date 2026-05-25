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
}
