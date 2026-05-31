using System.Numerics;
using Silk.NET.Input;
using VoxelEngine.src.world;

namespace VoxelEngine.src;

public class Player
{
    private static Vector3 cameraPosOffset = new Vector3(0, 1.6f, 0);

    public Camera Camera { get; }
    public Vector3 Position { get; private set; }

    private bool[] inputs = new bool[6];
    private Vector2 lastMousePosition;
    private bool firstMouseMovement = true; 
    private bool isLeftMouseDown = false;
    private bool isRightMouseDown = false;
    private double interactionTimer = 0.0;
    private const double BREAK_COOLDOWN = 0.2;
    private const double PLACE_COOLDOWN = 0.2;
    private Vector3 lastInteractedBlockPos = new Vector3(float.MaxValue);

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
    public void HandleUpdate(Game gameInstance, double deltaTime)
    {
        float movementSpeed = 5f;
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

        if (isLeftMouseDown || isRightMouseDown)
        {
            interactionTimer += deltaTime;
            double targetCooldown = isLeftMouseDown ? BREAK_COOLDOWN : PLACE_COOLDOWN;

            if (interactionTimer >= targetCooldown)
            {
                TriggerInteraction(gameInstance);
                interactionTimer = 0.0;
            }
        }
    }

    public void OnMouseDown(MouseButton button, Game gameInstance)
    {
        if (button == MouseButton.Left)
        {
            interactionTimer = BREAK_COOLDOWN;
            isLeftMouseDown = true;
            TriggerInteraction(gameInstance);
        }
        if (button == MouseButton.Right)
        {
            interactionTimer = PLACE_COOLDOWN;
            isRightMouseDown = true;
            TriggerInteraction(gameInstance);
        }
    }

    public void OnMouseUp(MouseButton button)
    {
        if (button == MouseButton.Left) isLeftMouseDown = false;
        if (button == MouseButton.Right) isRightMouseDown = false;

        if (!isLeftMouseDown && !isRightMouseDown)
        {
            lastInteractedBlockPos = new Vector3(float.MaxValue);
        }
    }

    private void TriggerInteraction(Game gameInstance)
    {
        if (gameInstance?.World == null) return;

        var rayResult = gameInstance.World.PerformVoxelRaycast(Camera.Position, Camera.Forward, 6.0f);
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
