using System;
using System.Numerics;

namespace VoxelEngine.src;

public class Camera
{
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Vector3 Forward { get; private set; } = -Vector3.UnitZ;
    public Vector3 Up { get; private set; } = Vector3.UnitY;
    public Vector3 Right { get; private set; } = Vector3.UnitX;

    public float Yaw { get; set; } = -90.0f;
    public float Pitch { get; set; } = 0.0f;

    public float Fov { get; set; } = 60.0f;
    public float AspectRatio { get; set; }

    public Matrix4x4 ViewMatrix => GetViewMatrix();
    public Matrix4x4 ProjectionMatrix { get; private set; }

    public Camera(float aspectRatio)
    {
        this.AspectRatio = aspectRatio;
        UpdateCameraVectors();
        CreateProjectionMatrix();
    }

    private Camera() { }

    public Camera Clone()
    {
        return new Camera()
        {
            Position = this.Position,
            Forward = this.Forward,
            Up = this.Up,
            Right = this.Right,
            Yaw = this.Yaw,
            Pitch = this.Pitch,
            Fov = this.Fov,
            AspectRatio = this.AspectRatio,
            ProjectionMatrix = this.ProjectionMatrix
        };
    }

    public void ModifyOrientation(float xOffset, float yOffset, bool limitPitch = true)
    {
        Yaw += xOffset;
        Pitch += yOffset;

        if (limitPitch)
        {
            Pitch = Math.Clamp(Pitch, -89.9f, 89.9f);
        }

        UpdateCameraVectors();
    }

    private void UpdateCameraVectors()
    {
        float yawRad = MathF.PI / 180.0f * Yaw;
        float pitchRad = MathF.PI / 180.0f * Pitch;

        Vector3 front;
        front.X = MathF.Cos(yawRad) * MathF.Cos(pitchRad);
        front.Y = MathF.Sin(pitchRad);
        front.Z = MathF.Sin(yawRad) * MathF.Cos(pitchRad);

        Forward = Vector3.Normalize(front);
        Right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
        Up = Vector3.Normalize(Vector3.Cross(Right, Forward));
    }

    public void UpdateAspectRatio(float aspectRatio)
    {
        AspectRatio = aspectRatio;
        CreateProjectionMatrix();
    }

    public void UpdateAspectRatio(Vector2 size)
    {
        AspectRatio = size.X / size.Y;
        CreateProjectionMatrix();
    }

    private Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(Position, Position + Forward, Up);
    }

    private void CreateProjectionMatrix()
    {
        float fovRad = MathF.PI / 180.0f * Fov;
        ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(fovRad, AspectRatio, 0.1f, 1000.0f);
    }
}