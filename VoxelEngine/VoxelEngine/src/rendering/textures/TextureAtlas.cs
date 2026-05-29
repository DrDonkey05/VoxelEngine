using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using StbImageSharp;
using VoxelEngine.src.json;

namespace VoxelEngine.src.rendering.textures;

public class TextureAtlas : IDisposable
{
    private readonly GL gl;

    // Every texture maps to a Slot. Static textures simply have a FrameCount of 1.
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

        if (image.Width <= 0 || image.Height <= 0) return;

        string baseResourceKey = Path.GetFileNameWithoutExtension(path);
        string? parentDir = Path.GetFileName(Path.GetDirectoryName(path));
        if (!string.IsNullOrEmpty(parentDir) && parentDir != "textures")
        {
            baseResourceKey = $"{parentDir}/{baseResourceKey}";
        }

        // Animated textures are vertical filmstrips. 
        // Frame width is the full width of the file; height per frame equals the width.
        int frameSize = image.Width;
        int frameCount = image.Height / frameSize;
        int bytesPerFrame = frameSize * frameSize * 4;

        List<byte[]> frames = new List<byte[]>(frameCount);
        for (int f = 0; f < frameCount; f++)
        {
            byte[] frameData = new byte[bytesPerFrame];
            Array.Copy(image.Data, f * bytesPerFrame, frameData, 0, bytesPerFrame);
            frames.Add(frameData);
        }
        int frameTickDelay = -1;
        if (File.Exists($"{path}.json"))
        {
            JsonTexture meta = JsonLoader.Load<JsonTexture>($"{path}.json");
            frameTickDelay = meta.TickDelay;
        }

        pendingSprites.Add(new PendingSprite(baseResourceKey, frameSize, frames, frameTickDelay));
    }

    public void Stitch()
    {
        if (pendingSprites.Count == 0) return;

        // Sort by image frame resolutions to pack high-res textures optimally
        pendingSprites.Sort((a, b) => b.FrameSize.CompareTo(a.FrameSize));

        // Total layout space calculation: we pack ALL frames horizontally as a filmstrip row!
        int totalArea = 0;
        foreach (var s in pendingSprites)
        {
            totalArea += (s.FrameSize * s.Frames.Count) * s.FrameSize;
        }

        int currentAtlasSize = 64;
        while (currentAtlasSize * currentAtlasSize < totalArea * 1.20f) currentAtlasSize *= 2;

        while (true)
        {
            Slots.Clear();
            int shelfX = 0;
            int shelfY = 0;
            int shelfHeight = 0;
            bool fitsAll = true;

            foreach (var sprite in pendingSprites)
            {
                // Calculate the full horizontal length needed to layout all frames side-by-side
                int totalStripWidth = sprite.FrameSize * sprite.Frames.Count;

                if (shelfX + totalStripWidth > currentAtlasSize)
                {
                    shelfX = 0;
                    shelfY += shelfHeight;
                    shelfHeight = 0;
                }

                if (shelfY + sprite.FrameSize > currentAtlasSize)
                {
                    currentAtlasSize *= 2;
                    fitsAll = false;
                    break;
                }

                // Store structural layout boundary tracking information
                Slots[sprite.Key] = new AtlasSlot(
                    new Vector2i(shelfX, shelfY),
                    sprite.FrameSize,
                    sprite.FrameSize,
                    sprite.Frames.Count,
                    sprite.FrameTickDelay
                );

                shelfX += totalStripWidth;
                if (sprite.FrameSize > shelfHeight) shelfHeight = sprite.FrameSize;
            }

            if (fitsAll) break;
        }

        AtlasWidth = currentAtlasSize;
        AtlasHeight = currentAtlasSize;
        AtlasPixelData = new byte[AtlasWidth * AtlasHeight * 4];
        Span<byte> atlasSpan = AtlasPixelData.AsSpan();
        int atlasStride = AtlasWidth * 4;

        // Copy frame buffer data channels sequentially onto the flat 2D target matrix
        foreach (var sprite in pendingSprites)
        {
            var slot = Slots[sprite.Key];
            int spriteStride = sprite.FrameSize * 4;

            for (int f = 0; f < sprite.Frames.Count; f++)
            {
                // Offset each frame index horizontally to generate the horizontal layout strip
                int frameOffsetX = slot.Origin.X + (f * slot.FrameWidth);
                ReadOnlySpan<byte> frameSpan = sprite.Frames[f].AsSpan();

                for (int row = 0; row < sprite.FrameSize; row++)
                {
                    int sourceOffset = row * spriteStride;
                    int destOffset = ((slot.Origin.Y + row) * atlasStride) + (frameOffsetX * 4);

                    frameSpan.Slice(sourceOffset, spriteStride)
                             .CopyTo(atlasSpan.Slice(destOffset, spriteStride));
                }
            }
        }

        Texture?.Dispose();
        Texture = new Texture2D(gl, AtlasPixelData, AtlasWidth, AtlasHeight);
        pendingSprites.Clear();

        DumpAtlas(AtlasPixelData, AtlasWidth, AtlasHeight);
    }

    /// <summary>
    /// Returns the data required by the vertex shader:
    /// X, Y = Min normalized UV coordinates of frame 0.
    /// Z = Width of a single frame in UV coordinates.
    /// W = Total frame count of the animation.
    /// </summary>
    public Vector4 GetAnimationDataOld(string texName)
    {
        if (Slots.TryGetValue(texName, out var slot))
        {
            float minU = (float)slot.Origin.X / AtlasWidth;
            float minV = (float)slot.Origin.Y / AtlasHeight;
            float frameWidthUV = (float)slot.FrameWidth / AtlasWidth;

            return new Vector4(minU, minV, frameWidthUV, slot.FrameCount);
        }
        return Vector4.Zero;
    }
    public Vector4 GetAnimationData(string texName)
    {
        if (Slots.TryGetValue(texName, out var slot))
        {
            float minU = (float)slot.Origin.X / AtlasWidth;
            float minV = (float)slot.Origin.Y / AtlasHeight;
            float frameWidthUV = (float)slot.FrameWidth / AtlasWidth;

            // Bit-pack two 16-bit integers into one 32-bit integer:
            // Lower 16 bits = FrameCount
            // Upper 16 bits = FrameTickDelay
            int packedData = (slot.FrameTickDelay << 16) | (slot.FrameCount & 0xFFFF);
            // Bit-reinterpret the int bits as a float so OpenGL passes it raw 
            // without altering the underlying bits.
            float wChannel = BitConverter.Int32BitsToSingle(packedData);

            return new Vector4(minU, minV, frameWidthUV, wChannel);
        }
        return Vector4.Zero;
    }

    public static void DumpAtlas(byte[] data, int width, int height)
    {
        string path = "../../../debug_atlas.tga";
        using var stream = File.OpenWrite(path);
        using var writer = new BinaryWriter(stream);

        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((byte)2);
        writer.Write((short)0); writer.Write((short)0); writer.Write((byte)0);
        writer.Write((short)0); writer.Write((short)0);
        writer.Write((short)width);
        writer.Write((short)height);
        writer.Write((byte)32);
        writer.Write((byte)0x20);

        byte[] outputData = new byte[data.Length];
        Span<uint> sourcePixels = MemoryMarshal.Cast<byte, uint>(data.AsSpan());
        Span<uint> destPixels = MemoryMarshal.Cast<byte, uint>(outputData.AsSpan());

        for (int i = 0; i < sourcePixels.Length; i++)
        {
            uint rgba = sourcePixels[i];
            destPixels[i] = (rgba & 0xFF00FF00) | ((rgba & 0x00FF0000) >> 16) | ((rgba & 0x000000FF) << 16);
        }

        writer.Write(outputData);
    }

    public void Dispose()
    {
        Texture?.Dispose();
        GC.SuppressFinalize(this);
    }

    public readonly record struct Vector2i(int X, int Y);
    public readonly record struct AtlasSlot(
        Vector2i Origin, int FrameWidth, int FrameHeight, int FrameCount, int FrameTickDelay);
    private readonly record struct PendingSprite(string Key, int FrameSize, List<byte[]> Frames, int FrameTickDelay);
}