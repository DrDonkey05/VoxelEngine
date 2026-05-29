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
    // State trackers for continuous holding inside Program.cs
    private static bool isLeftMouseDown = false;
    private static bool isRightMouseDown = false;

    // Cooldown pacing metrics (in seconds)
    private static double interactionTimer = 0.0;
    private const double BREAK_COOLDOWN = 0.2;
    private const double PLACE_COOLDOWN = 0.2;

    // Track the last acted-upon block position to avoid double-placing/breaking on the exact same spot in a single frame
    private static Vector3 lastInteractedBlockPos = new Vector3(float.MaxValue);

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
        Window.OnMouseDown += (mouse, button) => {
            if (button == MouseButton.Left)
            {
                interactionTimer = BREAK_COOLDOWN;
                isLeftMouseDown = true;
                TriggerInteraction(); // Instant action!
            }
            if (button == MouseButton.Right)
            {
                interactionTimer = PLACE_COOLDOWN;
                isRightMouseDown = true;
                TriggerInteraction(); // Instant action!
            }
        };

        Window.OnMouseUp += (mouse, button) => {
            if (button == MouseButton.Left) isLeftMouseDown = false;
            if (button == MouseButton.Right) isRightMouseDown = false;

            // Clear tracking when buttons are released
            if (!isLeftMouseDown && !isRightMouseDown)
            {
                lastInteractedBlockPos = new Vector3(float.MaxValue);
            }
        };

        Window.Run();
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
        player?.HandleUpdate(dt);

        if (player != null)
        {
            gameInstance?.UpdateSharedPlayerPosition(player.Position);
        }

        // Process continuous auto-repeat if a button remains held down
        if (isLeftMouseDown || isRightMouseDown)
        {
            interactionTimer += dt;
            double targetCooldown = isLeftMouseDown ? BREAK_COOLDOWN : PLACE_COOLDOWN;

            if (interactionTimer >= targetCooldown)
            {
                TriggerInteraction();
                interactionTimer = 0.0; // Clean, standard reset
            }
        }
    }

    private static void TriggerInteraction()
    {
        if (gameInstance?.World == null || player == null) return;

        var rayResult = PerformVoxelRaycast(player.Camera, 6.0f);
        if (!rayResult.Hit) return;

        if (isLeftMouseDown)
        {
            gameInstance.EnqueueInteraction(rayResult.BlockPos, Block.AIR.DefaultState.Id, InteractionType.Break);
        }
        else if (isRightMouseDown)
        {
            Vector3 placePos = rayResult.BlockPos + rayResult.HitNormal;

            if (placePos != lastInteractedBlockPos)
            {
                gameInstance.EnqueueInteraction(placePos, Block.STONE.DefaultState.Id, InteractionType.Place);
                lastInteractedBlockPos = placePos;

                Console.WriteLine($"Placed block at {placePos.X}, {placePos.Y}, {placePos.Z}");
            }
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