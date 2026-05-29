using System.Numerics;
using Silk.NET.OpenGL;
using VoxelEngine.src.rendering.textures;

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

    public void Begin(int tick, Texture2D texture, Camera camera)
    {
        shader.Use();
        shader.SetUniform("uGlobalFrameTicker", tick);

        shader.SetUniform("uView", camera.ViewMatrix);
        shader.SetUniform("uProjection", camera.ProjectionMatrix);

        texture.Bind();
        shader.SetUniform("uTexture", 0);
    }

    public unsafe void Render(Vector3 position, Mesh mesh)
    {
        Matrix4x4 model = Matrix4x4.CreateTranslation(position);
        shader.SetUniform("uModel", model);

        mesh.Bind();

        gl.DrawElements(PrimitiveType.Triangles, mesh.IndexCount, DrawElementsType.UnsignedInt, (void*)0);
    }

    public void End()
    {
        gl.BindVertexArray(0);
    }
}
