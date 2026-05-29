using System;
using System.Collections.Concurrent;
using System.Numerics;
using System.Threading;
using VoxelEngine.src.world;

namespace VoxelEngine.src;

public enum InteractionType { Break, Place }

public struct BlockInteractionRequest
{
    public Vector3 TargetBlockPos { get; init; }
    public ushort BlockStateId { get; init; }
    public InteractionType Type { get; init; }
}

public class Game
{
    private byte ticksPerSecond = 20;
    private double timePerTick;
    private bool isRunning;

    public World World { get; private set; }
    
    // Thread-safe atomic container to receive the player's position from the render thread
    private Vector3 sharedPlayerPosition = Vector3.Zero;
    private readonly ConcurrentQueue<BlockInteractionRequest> interactionQueue = new();
    private readonly object positionLock = new();

    public Game(float initialAspectRatio)
    {
        timePerTick = 1.0 / ticksPerSecond;
    }

    public void Initialize()
    {
        // World no longer depends directly on a local player instance for loop updates
        World = new World(0, null);
    }

    /// <summary>
    /// Called by the Render Thread to safely pass the high-FPS player location down to the simulation.
    /// </summary>
    public void EnqueueInteraction(Vector3 blockPos, ushort stateId, InteractionType type)
    {
        interactionQueue.Enqueue(new BlockInteractionRequest 
        { 
            TargetBlockPos = blockPos, 
            BlockStateId = stateId, 
            Type = type
        });
    }
    public void UpdateSharedPlayerPosition(Vector3 position)
    {
        lock (positionLock)
        {
            sharedPlayerPosition = position;
        }
    }

    public void Start()
    {
        isRunning = true;
        double tickTimer = 0.0;
        long lastTimeTicks = DateTime.UtcNow.Ticks;

        while (isRunning)
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            double elapsedSeconds = (nowTicks - lastTimeTicks) / 10_000_000.0;
            lastTimeTicks = nowTicks;

            tickTimer += elapsedSeconds;

            while (tickTimer >= timePerTick)
            {
                tickTimer -= timePerTick;

                // Process all pending block additions/removals before running simulation checks
                while (interactionQueue.TryDequeue(out var request))
                {
                    World.SetBlock(
                        (int)request.TargetBlockPos.X,
                        (int)request.TargetBlockPos.Y,
                        (int)request.TargetBlockPos.Z,
                        request.BlockStateId
                    );
                }

                // Grab the freshest position computed by the render thread
                Vector3 currentTargetPos;
                lock (positionLock)
                {
                    currentTargetPos = sharedPlayerPosition;
                }

                // Tick structural simulation features (chunk generation, data populating)
                World.HandleGameTick(timePerTick, currentTargetPos);
            }

            Thread.Sleep(1);
        }
    }

    public void Stop()
    {
        isRunning = false;
    }

    public void Shutdown()
    {
        World?.Dispose();
    }
}