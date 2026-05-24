using System.Numerics;
using System.Runtime.InteropServices;

namespace VoxelEngine.src.rendering;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Vertex
{
    public Vector3 Position { get; }
    public Vector2 TexCoords { get; }

    public Vertex(Vector3 position, Vector2 texCoords)
    {
        this.Position = position;
        this.TexCoords = texCoords;
    }
}
