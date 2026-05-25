using Silk.NET.OpenGL;
using StbImageSharp;

namespace VoxelEngine.src.rendering.textures;

public class Texture2D : IDisposable
{
    private readonly GL gl;
    private readonly uint handle;

    public int Width { get; }
    public int Height { get; }

    public Texture2D(GL gl, byte[] pixelData, int width, int height)
    {
        this.gl = gl;
        this.Width = width;
        this.Height = height;

        handle = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, handle);

        unsafe
        {
            fixed (byte* ptr = pixelData)
            {
                gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    InternalFormat.Rgba,
                    (uint)Width,
                    (uint)Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    ptr
                );
            }
        }

        // Apply pixel-perfect filtering parameters (Nearest neighbor for sharp voxels!)
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        // Wrap modes to prevent texture bleeding on voxel edges
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        gl.BindTexture(TextureTarget.Texture2D, 0);
    }
    public void Bind(TextureUnit unit = TextureUnit.Texture0)
    {
        gl.ActiveTexture(unit);
        gl.BindTexture(TextureTarget.Texture2D, handle);
    }

    public void Dispose()
    {
        gl.DeleteTexture(handle);
    }

    private static Texture2D defaultTexture = null;
    public static Texture2D LoadTexture(GL gl, string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Failed to find texture at: {path}");
            return defaultTexture;
        }

        using Stream stream = File.OpenRead(path);
        StbImage.stbi_set_flip_vertically_on_load(1);
        ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        if (image.Width <= 0 || image.Height <= 0)
        {
            Console.WriteLine($"Invalid texture dimensions at: {path}");
            return defaultTexture;
        }

        return new Texture2D(gl, image.Data, image.Width, image.Height);
    }
}
