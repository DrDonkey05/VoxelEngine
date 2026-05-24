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

    public Mesh(GL gl, Vertex[] vertices, uint[] indices)
    {
        this.gl = gl;
        this.IndexCount = (uint)indices.Length;

        vao = gl.GenVertexArray();
        vbo = gl.GenBuffer();
        ebo = gl.GenBuffer();

        gl.BindVertexArray(vao);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        fixed (void* v = vertices)
        {
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(Vertex)), v, BufferUsageARB.StaticDraw);
        }

        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        fixed (void* i = indices)
        {
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);
        }

        uint stride = (uint)sizeof(Vertex);
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)sizeof(Vector3));
        gl.EnableVertexAttribArray(1);

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
