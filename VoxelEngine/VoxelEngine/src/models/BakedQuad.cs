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
    public Vector2[] TexCoords { get; }
    public int Tint { get; }

    public BakedQuad(Vector3[] positions, Vector2[] texCoords, int tint)
    {
        this.Positions = positions;
        this.TexCoords = texCoords;
        this.Tint = tint;
    }
}
