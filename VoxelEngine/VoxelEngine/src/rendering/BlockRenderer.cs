using System.Numerics;
using Silk.NET.Input;
using Silk.NET.OpenGL;

namespace VoxelEngine.src.rendering;

public class BlockRenderer
{
    private Shader shader;
    private GL gl;

    public BlockRenderer(GL gl, Shader shader)
    {
        this.gl = gl;
        this.shader = shader;
    }

    public unsafe void Render(Mesh mesh, Camera camera)
    {
        Matrix4x4 model = Matrix4x4.CreateTranslation(Vector3.Zero);

        shader.Use();
        shader.SetUniform("uView", camera.ViewMatrix);
        shader.SetUniform("uProjection", camera.ProjectionMatrix);
        shader.SetUniform("uModel", model);

        mesh.Bind();

        gl.DrawElements(PrimitiveType.Triangles, mesh.IndexCount, DrawElementsType.UnsignedInt, (void*)0);

        gl.BindVertexArray(0);
    }
}
