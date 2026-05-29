using System.Numerics;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace VoxelEngine.src;

public class Window
{
    public GL Gl { get; private set; }
    public float AspectRatio { get; private set; }
    private IWindow window;
    private IKeyboard keyboard;
    public IMouse Mouse { get; private set; }

    public event Action OnLoad;
    public event Action<double> OnRender;
    public event Action<Vector2> OnResize;
    public event Action<double> OnUpdate;
    public event Action OnClosing;

    public event Action<IKeyboard, Key, int> OnKeyDown;
    public event Action<IKeyboard, Key, int> OnKeyUp;
    public event Action<IMouse, Vector2> OnMouseMove;
    public event Action<IMouse, MouseButton> OnMouseDown;
    public event Action<IMouse, MouseButton> OnMouseUp;

    public Window(WindowOptions options)
    {
        window = Silk.NET.Windowing.Window.Create(options);

        window.Load += () =>
        {
            Gl = window.CreateOpenGL();
            AspectRatio = (float)window.Size.X / window.Size.Y;
            IInputContext input = window.CreateInput();
            keyboard = input.Keyboards[0];
            Mouse = input.Mice[0];
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp += OnKeyUp;
            Mouse.MouseMove += OnMouseMove;
            Mouse.MouseDown += OnMouseDown;
            Mouse.MouseUp += OnMouseUp;

            OnLoad?.Invoke();
        };
        window.Render += (dt) =>
        {
            OnRender?.Invoke(dt);
        };
        window.Update += (dt) =>
        {
            OnUpdate?.Invoke(dt);
        };
        window.Resize += (size) =>
        {
            Gl?.Viewport(0, 0, (uint)size.X, (uint)size.Y);
            OnResize?.Invoke(new Vector2(size.X, size.Y));
        };
        window.Closing += OnClosing;
    }

    public void Run()
    {
        window?.Run();
    }
    public void Close()
    {
        window?.Close();
    }
}
