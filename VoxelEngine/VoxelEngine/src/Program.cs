using System.Drawing;
using System.Numerics;
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

    private static double tickTimer = 0.0;
    private static byte ticksPerSecond = 20;
    private static int tick = 0;
    private static double timePerTick = 1.0 / ticksPerSecond;

    private static TextureAtlas atlas;
    private static Shader shader;
    private static BlockRenderer blockRenderer;

    private static World world;
    private static Player player;

    public static void Main(string[] args)
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(2000, 1200);
        options.Title = "Voxel Engine";

        Window = new Window(options);

        Window.OnLoad += OnLoad;
        Window.OnClosing += () => world?.Dispose();
        Window.OnResize += (size) => player.Camera.UpdateAspectRatio(size);
        Window.OnRender += OnRender;
        Window.OnUpdate += OnUpdate;

        Window.OnKeyDown += (kb, key, code) =>
        {
            if (key == Key.Escape)
            {
                Window.Close();
                return;
            }
            player?.OnKeyDown(key);
        };
        Window.OnKeyUp += (kb, key, code) =>
        {
            player?.OnKeyUp(key);
        };

        Window.OnMouseMove += (ms, pos) =>
        {
            player?.OnMouseMove(pos);
        };
        Window.Run();
    }

    private static void OnUpdate(double dt)
    {
        tickTimer += dt;

        while (tickTimer >= timePerTick)
        {
            tickTimer -= timePerTick;

            if (tick == int.MaxValue)
                tick = 0;
            else
                tick++;
        }

        player.HandleUpdate(dt);
        world.HandleUpdate(Window.Gl, dt, player.Position);
    }
    private static void OnRender(double dt)
    {
        Window.Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        blockRenderer.Begin(tick, atlas.Texture, player.Camera);

        foreach (var chunk in world.LoadedChunks.Values)
        {
            if (chunk.Mesh != null && chunk.Mesh.IndexCount > 0)
                blockRenderer.Render(chunk.WorldPosition, chunk.Mesh);
        }

        blockRenderer.End();
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

        world = new World(0, player, Window.Gl);
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
