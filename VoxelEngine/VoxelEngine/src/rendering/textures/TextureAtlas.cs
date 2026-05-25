using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using StbImageSharp;

namespace VoxelEngine.src.rendering.textures;

public class TextureAtlas : IDisposable
{
    private readonly GL gl;

    public Dictionary<string, AtlasSlot> Slots { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Texture2D? Texture { get; private set; }
    public int AtlasWidth { get; private set; }
    public int AtlasHeight { get; private set; }
    public byte[]? AtlasPixelData { get; private set; }

    private readonly List<PendingSprite> pendingSprites = new();

    public TextureAtlas(GL gl)
    {
        this.gl = gl;
    }

    public void Add(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"File not found at: {path}");
            return;
        }

        using Stream stream = File.OpenRead(path);
        StbImage.stbi_set_flip_vertically_on_load(0);
        ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        if (image.Width <= 0 || image.Height <= 0)
        {
            Console.WriteLine($"Image dimensions invalid at: {path}");
            return;
        }

        string baseResourceKey = Path.GetFileNameWithoutExtension(path);
        string? parentDir = Path.GetFileName(Path.GetDirectoryName(path));

        if (!string.IsNullOrEmpty(parentDir) && parentDir != "textures")
        {
            baseResourceKey = $"{parentDir}/{baseResourceKey}";
        }

        pendingSprites.Add(new PendingSprite(baseResourceKey, image.Width, image.Height, image.Data));
    }

    public void Stitch()
    {
        if (pendingSprites.Count == 0) return;

        pendingSprites.Sort((a, b) => b.Height.CompareTo(a.Height));

        int totalArea = 0;
        foreach (var s in pendingSprites) totalArea += s.Width * s.Height;

        int currentAtlasSize = 64;
        while (currentAtlasSize * currentAtlasSize < totalArea * 1.15f)
        {
            currentAtlasSize *= 2;
        }

        while (true)
        {
            Slots.Clear();
            int shelfX = 0;
            int shelfY = 0;
            int shelfHeight = 0;
            bool fitsAll = true;

            foreach (var sprite in pendingSprites)
            {
                if (shelfX + sprite.Width > currentAtlasSize)
                {
                    shelfX = 0;
                    shelfY += shelfHeight;
                    shelfHeight = 0;
                }

                if (shelfY + sprite.Height > currentAtlasSize)
                {
                    currentAtlasSize *= 2;
                    fitsAll = false;
                    break;
                }

                Slots[sprite.Key] = new AtlasSlot(shelfX, shelfY, sprite.Width, sprite.Height);
                shelfX += sprite.Width;
                if (sprite.Height > shelfHeight) shelfHeight = sprite.Height;
            }

            if (fitsAll) break;
        }

        AtlasWidth = currentAtlasSize;
        AtlasHeight = currentAtlasSize;
        AtlasPixelData = new byte[AtlasWidth * AtlasHeight * 4];

        Span<byte> atlasSpan = AtlasPixelData.AsSpan();
        int atlasStride = AtlasWidth * 4;

        foreach (var sprite in pendingSprites)
        {
            var loc = Slots[sprite.Key];
            ReadOnlySpan<byte> spriteSpan = sprite.Data.AsSpan();
            int spriteStride = sprite.Width * 4;

            for (int row = 0; row < sprite.Height; row++)
            {
                int sourceOffset = row * spriteStride;
                int destOffset = ((loc.Y + row) * atlasStride) + (loc.X * 4);

                spriteSpan.Slice(sourceOffset, spriteStride)
                          .CopyTo(atlasSpan.Slice(destOffset, spriteStride));
            }
        }

        Texture?.Dispose();
        Texture = new Texture2D(gl, AtlasPixelData, AtlasWidth, AtlasHeight);
        pendingSprites.Clear();

        DumpAtlas(AtlasPixelData, AtlasWidth, AtlasHeight);
    }

    public Vector2 Get(Vector2 local, AtlasSlot slot)
    {
        float u = (slot.X + (local.X * slot.Width)) / AtlasWidth;
        float v = (slot.Y + ((1.0f - local.Y) * slot.Height)) / AtlasHeight;
        return new Vector2(u, v);
    }

    public Vector2 Get(Vector2 local, string texName)
    {
        if (Slots.TryGetValue(texName, out var slot))
        {
            return Get(local, slot);
        }
        return Vector2.Zero;
    }

    public static void DumpAtlas(byte[] data, int width, int height)
    {
        string path = "../../../debug_atlas.tga";
        using var stream = File.OpenWrite(path);
        using var writer = new BinaryWriter(stream);

        // Write TGA header variables directly
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((byte)2); // Uncompressed true-color
        writer.Write((short)0); writer.Write((short)0); writer.Write((byte)0);
        writer.Write((short)0); writer.Write((short)0);
        writer.Write((short)width);
        writer.Write((short)height);
        writer.Write((byte)32); // 32 bits per pixel (RGBA)
        writer.Write((byte)0x20); // Top-left origin flag

        // Conversion from RGBA to BGRA in memory via 32-bit vector casting
        byte[] outputData = new byte[data.Length];
        Span<uint> sourcePixels = MemoryMarshal.Cast<byte, uint>(data.AsSpan());
        Span<uint> destPixels = MemoryMarshal.Cast<byte, uint>(outputData.AsSpan());

        for (int i = 0; i < sourcePixels.Length; i++)
        {
            uint rgba = sourcePixels[i];
            // Swap red (bit 0-7) and blue (bit 16-23) channels
            destPixels[i] = (rgba & 0xFF00FF00) | ((rgba & 0x00FF0000) >> 16) | ((rgba & 0x000000FF) << 16);
        }

        writer.Write(outputData);
    }

    public void Dispose()
    {
        Texture?.Dispose();
        GC.SuppressFinalize(this);
    }

    public readonly record struct AtlasSlot(int X, int Y, int Width, int Height);
    private readonly record struct PendingSprite(string Key, int Width, int Height, byte[] Data);
}