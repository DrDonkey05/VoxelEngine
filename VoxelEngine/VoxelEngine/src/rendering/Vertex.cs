using System.Numerics;
using System.Runtime.InteropServices;

namespace VoxelEngine.src.rendering;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Vertex
{
    public Vector3 Position { get; }
    public Vector3 TextureBounds { get; }
    public int PackedAnimData { get; }
    public Vector3 Color { get; }

    public Vertex(Vector3 position, Vector3 textureBounds, int packedAnimData, Vector3 color)
    {
        this.Position = position;
        this.TextureBounds = textureBounds;
        this.PackedAnimData = packedAnimData;
        this.Color = color;
    }
}
