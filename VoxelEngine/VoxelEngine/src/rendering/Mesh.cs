using System.Numerics;
using Silk.NET.OpenGL;

namespace VoxelEngine.src.rendering;

public unsafe class Mesh : IDisposable
{
    private readonly GL gl;
    private readonly uint vao;
    private readonly uint vbo;
    private readonly uint ebo;
    public uint IndexCount { get; private set; }

    public Mesh(GL gl, MeshData meshData)
    {
        this.gl = gl;
        this.IndexCount = (uint)meshData.Indices.Length;

        vao = gl.GenVertexArray();
        vbo = gl.GenBuffer();
        ebo = gl.GenBuffer();

        gl.BindVertexArray(vao);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        fixed (void* v = meshData.Vertices)
        {
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(meshData.Vertices.Length * sizeof(Vector3)), v, BufferUsageARB.StaticDraw);
        }

        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        fixed (void* i = meshData.Indices)
        {
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(meshData.Indices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);
        }

        uint stride = (uint)sizeof(Vector3);
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
        gl.EnableVertexAttribArray(0);

        gl.BindVertexArray(0);
    }

    public void Bind()
    {
        gl.BindVertexArray(vao);
    }
    public void Dispose()
    {
        gl.DeleteVertexArray(vao);
        gl.DeleteBuffer(vbo);
        gl.DeleteBuffer(ebo);
    }
}
