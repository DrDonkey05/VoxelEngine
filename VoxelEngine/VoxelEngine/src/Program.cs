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
        Window.OnMouseDown += (mouse, button) => player?.OnMouseDown(button, gameInstance);

        Window.OnMouseUp += (mouse, button) => player?.OnMouseUp(button);

        Window.Run();
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

    // --- RUNS AT MAX FPS ---
    private static void OnUpdate(double dt)
    {
        player?.HandleUpdate(gameInstance, dt);

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
        atlas.Add("./assets/textures/block/oak_shelf.png");
        atlas.Add("./assets/textures/block/redstone_dust_dot.png");
        atlas.Add("./assets/textures/block/redstone_dust_line0.png");
        atlas.Add("./assets/textures/block/redstone_dust_line1.png");
        atlas.Add("./assets/textures/block/redstone_dust_overlay.png");
        atlas.Add("./assets/textures/block/cobblestone.png");
        atlas.Stitch();
    }

    private static void LoadBlocks()
    {
        Block.AIR.GenerateStatesAndModels(atlas);
        Block.STONE.GenerateStatesAndModels(atlas);
        Block.DIRT.GenerateStatesAndModels(atlas);
        Block.GRASS_BLOCK.GenerateStatesAndModels(atlas);
        Block.OAK_SHELF.GenerateStatesAndModels(atlas);
        Block.REDSTONE_WIRE.GenerateStatesAndModels(atlas);
        Block.COBBLESTONE_WALL.GenerateStatesAndModels(atlas);
        ModelBakery.ClearJsonCaches();
    }

    private static void LoadRenderer()
    {
        shader = Shader.CreateShader(Window.Gl, "./assets/shaders/shader.vert", "./assets/shaders/shader.frag");
        blockRenderer = new BlockRenderer(Window.Gl, shader);
    }
}