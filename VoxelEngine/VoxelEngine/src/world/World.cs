using System;
using System.Collections.Generic;
using System.Numerics;
using ServerProj.src.noise;
using Silk.NET.OpenGL;
using VoxelEngine.src.models;

namespace VoxelEngine.src.world;

public class World : IDisposable
{
    public Dictionary<Vector3, Chunk> LoadedChunks { get; } = new();
    private readonly List<Chunk> chunksToMesh = new();
    private readonly List<Vector3> chunksToRemove = new();

    private Player player;
    private int renderDistanceRadius = 3;
    private Perlin terrainGenerator;
    private bool isDisposed;

    private Vector3 lastPlayerChunkPos = new Vector3(float.MaxValue);
    private int sortTicker = 0;

    public World(int seed, Player player, GL gl)
    {
        this.player = player;
        terrainGenerator = new Perlin(seed);
        GenerateSpawnChunks();
    }

    public void HandleUpdate(GL gl, double dt, Vector3 playerWorldPos)
    {
        Vector3 playerChunkPos = GetChunkPosFromBlockPos(
            (int)MathF.Floor(playerWorldPos.X),
            (int)MathF.Floor(playerWorldPos.Y),
            (int)MathF.Floor(playerWorldPos.Z)
        );

        if (playerChunkPos != lastPlayerChunkPos)
        {
            UpdateStreaming(playerChunkPos);
            lastPlayerChunkPos = playerChunkPos;
        }

        if (chunksToMesh.Count > 0)
        {
            sortTicker++;
            if (sortTicker >= 10)
            {
                chunksToMesh.Sort((a, b) =>
                    Vector3.DistanceSquared(a.Position, playerChunkPos)
                    .CompareTo(Vector3.DistanceSquared(b.Position, playerChunkPos))
                );
                sortTicker = 0;
            }

            Chunk meshingChunk = null;
            int targetIndex = -1;

            for (int i = 0; i < chunksToMesh.Count; i++)
            {
                if (AreNeighborsLoadedOrBoundary(chunksToMesh[i].Position, playerChunkPos))
                {
                    meshingChunk = chunksToMesh[i];
                    targetIndex = i;
                    break;
                }
            }

            if (meshingChunk != null)
            {
                chunksToMesh.RemoveAt(targetIndex);
                meshingChunk.BuildMesh(gl, this);
                meshingChunk.IsDirty = false;
            }
        }

        foreach (var chunk in LoadedChunks.Values)
        {
            if (chunk.IsDirty && AreNeighborsLoadedOrBoundary(chunk.Position, playerChunkPos))
            {
                chunk.BuildMesh(gl, this);
                chunk.IsDirty = false;
                break;
            }
        }
    }

    private bool AreNeighborsLoadedOrBoundary(Vector3 chunkPos, Vector3 playerChunkPos)
    {
        Vector3[] neighbors = new Vector3[]
        {
            chunkPos + Vector3.UnitX, chunkPos - Vector3.UnitX,
            chunkPos + Vector3.UnitY, chunkPos - Vector3.UnitY,
            chunkPos + Vector3.UnitZ, chunkPos - Vector3.UnitZ
        };

        for (int i = 0; i < neighbors.Length; i++)
        {
            Vector3 n = neighbors[i];

            if (!LoadedChunks.ContainsKey(n))
            {
                float dx = MathF.Abs(n.X - playerChunkPos.X);
                float dy = MathF.Abs(n.Y - playerChunkPos.Y);
                float dz = MathF.Abs(n.Z - playerChunkPos.Z);

                if (dx <= renderDistanceRadius && dy <= renderDistanceRadius && dz <= renderDistanceRadius)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private void UpdateStreaming(Vector3 playerChunkPos)
    {
        int pX = (int)playerChunkPos.X;
        int pY = (int)playerChunkPos.Y;
        int pZ = (int)playerChunkPos.Z;

        // --- PASS 1: LOAD NEW CHUNKS ---
        for (int x = -renderDistanceRadius; x <= renderDistanceRadius; x++)
        {
            for (int y = -renderDistanceRadius; y <= renderDistanceRadius; y++)
            {
                for (int z = -renderDistanceRadius; z <= renderDistanceRadius; z++)
                {
                    Vector3 targetChunkPos = new Vector3(pX + x, pY + y, pZ + z);

                    if (!LoadedChunks.ContainsKey(targetChunkPos))
                    {
                        GenerateChunk((int)targetChunkPos.X, (int)targetChunkPos.Y, (int)targetChunkPos.Z);
                    }
                }
            }
        }

        // --- PASS 2: UNLOAD DISTANT CHUNKS ---
        chunksToRemove.Clear();
        foreach (var chunkPos in LoadedChunks.Keys)
        {
            float distanceX = MathF.Abs(chunkPos.X - playerChunkPos.X);
            float distanceY = MathF.Abs(chunkPos.Y - playerChunkPos.Y);
            float distanceZ = MathF.Abs(chunkPos.Z - playerChunkPos.Z);

            if (distanceX > renderDistanceRadius ||
                distanceY > renderDistanceRadius ||
                distanceZ > renderDistanceRadius)
            {
                chunksToRemove.Add(chunkPos);
            }
        }

        for (int i = 0; i < chunksToRemove.Count; i++)
        {
            Vector3 pos = chunksToRemove[i];
            if (LoadedChunks.TryGetValue(pos, out Chunk chunk))
            {
                chunk.Mesh?.Dispose();
                LoadedChunks.Remove(pos);
                chunksToMesh.Remove(chunk);
            }
        }
    }

    public BlockState? GetBlock(int x, int y, int z)
    {
        Vector3 chunkPos = GetChunkPosFromBlockPos(x, y, z);

        if (LoadedChunks.TryGetValue(chunkPos, out Chunk chunk))
        {
            int localX = x - (int)chunkPos.X * Chunk.SIZE;
            int localY = y - (int)chunkPos.Y * Chunk.SIZE;
            int localZ = z - (int)chunkPos.Z * Chunk.SIZE;

            return chunk.GetBlock(localX, localY, localZ);
        }
        return null;
    }

    public Vector3 GetChunkPosFromBlockPos(int x, int y, int z)
    {
        return new Vector3(
            MathF.Floor((float)x / Chunk.SIZE),
            MathF.Floor((float)y / Chunk.SIZE),
            MathF.Floor((float)z / Chunk.SIZE)
        );
    }

    private void GenerateSpawnChunks()
    {
        UpdateStreaming(Vector3.Zero);
        lastPlayerChunkPos = Vector3.Zero;
    }

    private Chunk GenerateChunk(int x, int y, int z)
    {
        Chunk chunk = new Chunk(x, y, z, terrainGenerator);
        LoadedChunks.Add(chunk.Position, chunk);

        Vector3[] directions = new Vector3[]
        {
            new(1, 0, 0), new(-1, 0, 0),
            new(0, 1, 0), new(0, -1, 0),
            new(0, 0, 1), new(0, 0, -1)
        };

        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 neighborPos = chunk.Position + directions[i];
            if (LoadedChunks.TryGetValue(neighborPos, out Chunk neighbor))
            {
                neighbor.IsDirty = true;
            }
        }

        chunksToMesh.Add(chunk);
        return chunk;
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            foreach (var chunk in LoadedChunks.Values)
            {
                chunk?.Mesh?.Dispose();
            }
            LoadedChunks.Clear();
            chunksToMesh.Clear();
            isDisposed = true;
        }
        GC.SuppressFinalize(this);
    }
}