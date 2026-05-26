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
    private static GL gl;
    private static IWindow window;
    private static IKeyboard keyboard;
    private static IMouse mouse;

    private static float movementSpeed = 5;
    private static float mouseSensitivity = 0.1f;
    private static Vector2 lastMousePosition;
    private static bool firstMouseMovement = true;

    private static Shader shader;
    private static Camera camera;
    private static TextureAtlas atlas;
    private static BlockRenderer blockRenderer;
    private static Chunk chunk1;
    private static Chunk chunk2;

    private static double tickTimer = 0.0;
    private static byte ticksPerSecond = 20;
    private static int tick = 0;
    private static double timePerTick = 0.0;

    public static void Main(string[] args)
    {
        timePerTick = 1.0 / ticksPerSecond;

        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(1280, 720);
        options.Title = "Voxel Engine";

        window = Window.Create(options);

        window.Load += OnLoad;

        window.Render += (double dt) =>
        {
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            blockRenderer.Begin(tick, atlas.Texture, camera);

            blockRenderer.Render(chunk1.WorldPosition, chunk1.Mesh);
            blockRenderer.Render(chunk2.WorldPosition, chunk2.Mesh);

            blockRenderer.End();
        };

        window.Update += OnUpdate;

        window.Closing += () =>
        {
            chunk1.Mesh?.Dispose();
            chunk2.Mesh?.Dispose();
        };

        window.Resize += (size) =>
        {
            gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);
            camera.AspectRatio = (float)size.X / size.Y;
        };

        window.Run();
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

        HandleKeyboard(dt);
        HandleMouse();
    }

    private static void OnLoad()
    {
        gl = window.CreateOpenGL();

        gl.ClearColor(Color.CornflowerBlue);

        gl.Enable(EnableCap.CullFace);
        gl.CullFace(TriangleFace.Back);
        
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        gl.Enable(EnableCap.DepthTest);
        gl.DepthFunc(DepthFunction.Lequal);

        IInputContext input = window.CreateInput();
        keyboard = input.Keyboards[0];
        mouse = input.Mice[0];
        mouse.Cursor.CursorMode = CursorMode.Raw;

        keyboard.KeyDown += (kb, key, code) =>
        {
            if (key == Key.Escape) window.Close();
        };

        float aspectRatio = (float)window.Size.X / window.Size.Y;
        camera = new Camera(aspectRatio);
        camera.Position = new Vector3(0.0f, 2.0f, 5.0f);

        atlas = new TextureAtlas(gl);
        atlas.Add("./assets/textures/block/up.png");
        atlas.Add("./assets/textures/block/down.png");
        atlas.Add("./assets/textures/block/north.png");
        atlas.Add("./assets/textures/block/south.png");
        atlas.Add("./assets/textures/block/east.png");
        atlas.Add("./assets/textures/block/west.png");
        atlas.Add("./assets/textures/block/up2.png");
        atlas.Add("./assets/textures/block/top_left_overlay.png");
        atlas.Add("./assets/textures/block/animated.png");
        atlas.Stitch();

        shader = Shader.CreateShader(gl, "./assets/shaders/shader.vert", "./assets/shaders/shader.frag");
        blockRenderer = new BlockRenderer(gl, shader);

        Block.UP_BLOCK.GenerateStatesAndModels(atlas);
        Block.ANIMATED_BLOCK.GenerateStatesAndModels(atlas);
        Block.LAYERED_BLOCK.GenerateStatesAndModels(atlas);
        Block.CROSS_BLOCK.GenerateStatesAndModels(atlas);
        BlockModelBakery.ClearJsonCaches();

        chunk1 = new Chunk(0, 0, 0);
        chunk2 = new Chunk(1, 1, 1);
        chunk1.BuildMesh(gl);
        chunk2.BuildMesh(gl);
    }

    private static void HandleKeyboard(double deltaTime)
    {
        float speed = movementSpeed * (float)deltaTime;

        Vector3 newPosition = camera.Position;

        // Standard WASD movement controls
        if (keyboard.IsKeyPressed(Key.W))
            newPosition += camera.Forward * speed;

        if (keyboard.IsKeyPressed(Key.S))
            newPosition -= camera.Forward * speed;

        if (keyboard.IsKeyPressed(Key.A))
            newPosition -= camera.Right * speed;

        if (keyboard.IsKeyPressed(Key.D))
            newPosition += camera.Right * speed;

        // Optional: Vertical flying controls for dev exploration
        if (keyboard.IsKeyPressed(Key.Space))
            newPosition += Vector3.UnitY * speed; // Move straight Up

        if (keyboard.IsKeyPressed(Key.ShiftLeft))
            newPosition -= Vector3.UnitY * speed; // Move straight Down

        // Close window instantly on Escape
        if (keyboard.IsKeyPressed(Key.Escape))
            window.Close();

        camera.Position = newPosition;
    }

    private static void HandleMouse()
    {
        Vector2 currentPosition = mouse.Position;

        if (firstMouseMovement)
        {
            lastMousePosition = currentPosition;
            firstMouseMovement = false;
            return;
        }

        // Calculate how far the mouse traveled since last frame
        float xOffset = currentPosition.X - lastMousePosition.X;
        float yOffset = lastMousePosition.Y - currentPosition.Y; // Inverted because screen Y coordinates go down

        lastMousePosition = currentPosition;

        // Apply sensitivity adjustments
        xOffset *= mouseSensitivity;
        yOffset *= mouseSensitivity;

        // Pass the delta changes directly into your camera wrapper
        camera.ModifyOrientation(xOffset, yOffset);
    }
}
