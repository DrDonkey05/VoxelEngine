using System.Numerics;
using System.Runtime.InteropServices;

namespace VoxelEngine.src.rendering;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Vertex
{
    public Vector3 Position { get; }
    public Vector2 TexCoords { get; }
    public Vector3 Color { get; }

    public Vertex(Vector3 position, Vector2 texCoords, Vector3 color)
    {
        this.Position = position;
        this.TexCoords = texCoords;
        this.Color = color;
    }
}
