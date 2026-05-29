using System.Numerics;
using System.Runtime.InteropServices;

namespace VoxelEngine.src.models;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BakedQuad
{
    public Vector3[] Positions { get; }
    public Vector2[] UVs { get; }
    public Vector4 AnimData { get; }
    public int Tint { get; }
    public BlockFace? CullFace { get; }

    public BakedQuad(Vector3[] positions, Vector2[] uvs, Vector4 animData, int tint, BlockFace? cullFace)
    {
        this.Positions = positions;
        this.UVs = uvs;
        this.AnimData = animData;
        this.Tint = tint;
        this.CullFace = cullFace;
    }
}
