using System;
using System.Drawing;
using System.Numerics;
using System.Threading;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using VoxelEngine.src.models;
using VoxelEngine.src.rendering;
using VoxelEngine.src.rendering.textures;
using VoxelEngine.src.world;
using Shader = VoxelEngine.src.rendering.Shader;

namespace VoxelEngine.src;

public class Program
{
    public static Window Window { get; private set; }

    private static Thread gameThread;
    private static Game gameInstance;

    private static TextureAtlas atlas;
    private static Shader shader;
    private static BlockRenderer blockRenderer;

    // The Main Render Thread owns the Player and Camera entirely now!
    private static Player player;

    public static void Main(string[] args)
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(2000, 1200);
        options.Title = "Unlocked Input Voxel Engine";

        Window = new Window(options);

        Window.OnLoad += OnLoad;
        Window.OnClosing += OnClosing;
        Window.OnResize += (size) => player?.Camera?.UpdateAspectRatio(size);
        Window.OnRender += OnRender;
        Window.OnUpdate += OnUpdate; // We bring back OnUpdate for high-speed tracking!

        Window.OnKeyDown += (kb, key, code) =>
        {
            if (key == Key.Escape)
            {
                Window.Close();
                return;
            }
            player?.OnKeyDown(key);
        };
        Window.OnKeyUp += (kb, key, code) => player?.OnKeyUp(key);
        Window.OnMouseMove += (ms, pos) => player?.OnMouseMove(pos);
        Window.OnMouseDown += OnMouseDown;

        Window.Run();
    }

    private static void OnMouseDown(IMouse mouse, MouseButton button)
    {
        if (player == null || gameInstance?.World == null) return;

        // Run a high-precision raycast through our unlocked, live camera context
        // Raycast distance limit: 5-6 blocks out (standard player reach)
        var rayResult = PerformVoxelRaycast(player.Camera, 6.0f);

        if (rayResult.Hit)
        {
            if (button == MouseButton.Left)
            {
                // Break block -> change target position to AIR
                gameInstance.EnqueueInteraction(rayResult.BlockPos, Block.AIR.DefaultState.Id, InteractionType.Break);
            }
            else if (button == MouseButton.Right)
            {
                // Place block -> change the block right adjacent to the face we hit (e.g., STONE or DIRT)
                Vector3 placePos = rayResult.BlockPos + rayResult.HitNormal;
                gameInstance.EnqueueInteraction(placePos, Block.STONE.DefaultState.Id, InteractionType.Place);
            }
        }
    }

    // Simple Voxel Raycast (DDA-lite / Sampling approach)
    private static (bool Hit, Vector3 BlockPos, Vector3 HitNormal) PerformVoxelRaycast(Camera camera, float maxDistance)
    {
        Vector3 rayOrigin = camera.Position;
        Vector3 rayDirection = Vector3.Normalize(camera.Forward);

        float step = 0.05f; // Small stepping increments for precision
        Vector3 currentPos = rayOrigin;
        Vector3 previousBlockPos = new Vector3(MathF.Floor(rayOrigin.X), MathF.Floor(rayOrigin.Y), MathF.Floor(rayOrigin.Z));

        for (float distance = 0; distance < maxDistance; distance += step)
        {
            currentPos += rayDirection * step;

            int bx = (int)MathF.Floor(currentPos.X);
            int by = (int)MathF.Floor(currentPos.Y);
            int bz = (int)MathF.Floor(currentPos.Z);
            Vector3 currentBlockPos = new Vector3(bx, by, bz);

            if (currentBlockPos != previousBlockPos)
            {
                var blockState = gameInstance.World.GetBlock(bx, by, bz);
                // If we hit a block that isn't AIR or outside of generation limits
                if (blockState != null && blockState.Id != Block.AIR.DefaultState.Id)
                {
                    // Calculate surface normal based on where we entered the voxel bounding box
                    Vector3 hitNormal = previousBlockPos - currentBlockPos;
                    return (true, currentBlockPos, hitNormal);
                }
                previousBlockPos = currentBlockPos;
            }
        }

        return (false, Vector3.Zero, Vector3.Zero);
    }

    private static void OnLoad()
    {
        Window.Gl.ClearColor(Color.CornflowerBlue);
        Window.Gl.Enable(EnableCap.CullFace);
        Window.Gl.CullFace(TriangleFace.Back);
        Window.Gl.Enable(EnableCap.Blend);
        Window.Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        Window.Gl.Enable(EnableCap.DepthTest);
        Window.Gl.DepthFunc(DepthFunction.Lequal);

        LoadTextureAtlas();
        LoadBlocks();
        LoadRenderer();

        Camera camera = new Camera(Window.AspectRatio);
        player = new Player(camera, Vector3.UnitY * 32);

        Window.Mouse.Cursor.CursorMode = CursorMode.Raw;

        // Start the background game systems thread
        gameInstance = new Game(Window.AspectRatio);
        gameInstance.Initialize();

        gameThread = new Thread(gameInstance.Start);
        gameThread.Name = "GameSimulationThread";
        gameThread.IsBackground = true;
        gameThread.Start();
    }

    // --- RUNS AT MAX FPS (e.g., 144+ updates per second) ---
    private static void OnUpdate(double dt)
    {
        // 1. Calculate input mechanics instantly using precise frame delta times
        player?.HandleUpdate(dt);

        // 2. Safely push the updated position down to the world generation thread
        if (player != null)
        {
            gameInstance?.UpdateSharedPlayerPosition(player.Position);
        }
    }

    // --- RUNS AT MAX FPS ---
    private static void OnRender(double dt)
    {
        Window.Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Upload any waiting meshes that the background thread built
        gameInstance.World?.HandleGpuUploads(Window.Gl);

        // Render directly using the live, unlocked player camera state!
        blockRenderer.Begin(0, atlas.Texture, player.Camera);

        var renderList = gameInstance.World?.ChunksToRender;
        if (renderList != null)
        {
            for (int i = 0; i < renderList.Count; i++)
            {
                Chunk chunk = renderList[i];
                blockRenderer.Render(chunk.WorldPosition, chunk.Mesh);
            }
        }

        blockRenderer.End();
    }

    private static void OnClosing()
    {
        gameInstance?.Stop();
        gameThread?.Join(500);
        gameInstance?.Shutdown();
    }

    private static void LoadTextureAtlas()
    {
        atlas = new TextureAtlas(Window.Gl);
        atlas.Add("./assets/textures/block/stone.png");
        atlas.Add("./assets/textures/block/dirt.png");
        atlas.Add("./assets/textures/block/grass_block_top.png");
        atlas.Add("./assets/textures/block/grass_block_side.png");
        atlas.Add("./assets/textures/block/grass_block_side_overlay.png");
        atlas.Stitch();
    }

    private static void LoadBlocks()
    {
        Block.AIR.GenerateStatesAndModels(atlas);
        Block.STONE.GenerateStatesAndModels(atlas);
        Block.DIRT.GenerateStatesAndModels(atlas);
        Block.GRASS_BLOCK.GenerateStatesAndModels(atlas);
        ModelBakery.ClearJsonCaches();
    }

    private static void LoadRenderer()
    {
        shader = Shader.CreateShader(Window.Gl, "./assets/shaders/shader.vert", "./assets/shaders/shader.frag");
        blockRenderer = new BlockRenderer(Window.Gl, shader);
    }
}