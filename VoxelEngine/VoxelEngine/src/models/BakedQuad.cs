using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VoxelEngine.src.models;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BakedQuad
{
    public Vector3[] Positions { get; }
    public Vector4 AnimData { get; }
    public int Tint { get; }
    public BlockFace? CullFace { get; }

    public BakedQuad(Vector3[] positions, Vector4 animData, int tint, BlockFace? cullFace)
    {
        this.Positions = positions;
        this.AnimData = animData;
        this.Tint = tint;
        this.CullFace = cullFace;
    }
}
