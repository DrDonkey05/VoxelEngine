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
        int size = 0;
        // VERTEX POS
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)size);
        gl.EnableVertexAttribArray(0);
        size += 3 * sizeof(float);

        // TEX BOUNDS
        gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (void*)size);
        gl.EnableVertexAttribArray(1);
        size += 3 * sizeof(float);

        // PACKED ANIM DATA
        gl.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, stride, (void*)size);
        gl.EnableVertexAttribArray(2);
        size += 1 * sizeof(int);

        // COLOR
        gl.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, stride, (void*)size);
        gl.EnableVertexAttribArray(3);
        size += 3 * sizeof(float);

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
