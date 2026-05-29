using System.Numerics;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace VoxelEngine.src;

public class Player
{
    private static Vector3 cameraPosOffset = new Vector3(0, 1.6f, 0);

    public Camera Camera { get; }
    public Vector3 Position { get; private set; }

    private bool[] inputs = new bool[6];
    private Vector2 lastMousePosition;
    private bool firstMouseMovement = true;

    public Player(Camera camera, Vector3 position)
    {
        Camera = camera;

        UpdatePosition(position);
    }

    private void UpdatePosition(Vector3 newPosition)
    {
        Position = newPosition;
        Camera.Position = Position + cameraPosOffset;
    }
    public void HandleUpdate(double deltaTime)
    {
        float movementSpeed = 25f;
        float speed = movementSpeed * (float)deltaTime;

        Vector3 newPosition = Position;
        if (inputs[0])
            newPosition += Camera.Forward * speed;

        if (inputs[1])
            newPosition -= Camera.Forward * speed;

        if (inputs[2])
            newPosition -= Camera.Right * speed;

        if (inputs[3])
            newPosition += Camera.Right * speed;

        // Optional: Vertical flying controls for dev exploration
        if (inputs[4])
            newPosition += Vector3.UnitY * speed; // Move straight Up

        if (inputs[5])
            newPosition -= Vector3.UnitY * speed; // Move straight Down

        UpdatePosition(newPosition);
    }

    public void OnMouseMove(Vector2 newMousePosition)
    {
        float mouseSensitivity = 0.1f;
        Vector2 currentPosition = newMousePosition;

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
        Camera.ModifyOrientation(xOffset, yOffset);
    }
    public void OnKeyDown(Key key)
    {
        SetKey(key, true);
    }
    public void OnKeyUp(Key key)
    {
        SetKey(key, false);
    }
    private void SetKey(Key key, bool value)
    {
        switch (key)
        {
            case Key.W:
                inputs[0] = value;
                break;
            case Key.S:
                inputs[1] = value;
                break;
            case Key.A:
                inputs[2] = value;
                break;
            case Key.D:
                inputs[3] = value;
                break;
            case Key.Space:
                inputs[4] = value;
                break;
            case Key.ShiftLeft:
                inputs[5] = value;
                break;
        }
    }
}
