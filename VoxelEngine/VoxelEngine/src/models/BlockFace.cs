using System.Numerics;

namespace VoxelEngine.src.models;

public enum BlockFace
{
    Up,     // +Y
    Down,   // -Y
    North,  // -Z
    South,  // +Z
    East,   // +X
    West    // -X
}

public static class BlockFaceExt
{
    public static readonly BlockFace[] Faces = new BlockFace[] {
        BlockFace.Up, BlockFace.Down, BlockFace.North, BlockFace.South, BlockFace.East, BlockFace.West
    }; 
    
    public static BlockFace RotateX90(this BlockFace face) => face switch
    {
        BlockFace.Up => BlockFace.South,
        BlockFace.South => BlockFace.Down,
        BlockFace.Down => BlockFace.North,
        BlockFace.North => BlockFace.Up,
        _ => face // East and West stay on their axis but rotate in place
    };

    public static BlockFace RotateY90(this BlockFace face) => face switch
    {
        BlockFace.North => BlockFace.East,
        BlockFace.East => BlockFace.South,
        BlockFace.South => BlockFace.West,
        BlockFace.West => BlockFace.North,
        _ => face // Up and Down stay on their axis
    };

    public static BlockFace RotateZ90(this BlockFace face) => face switch
    {
        BlockFace.Up => BlockFace.West,
        BlockFace.West => BlockFace.Down,
        BlockFace.East => BlockFace.Up,
        BlockFace.Down => BlockFace.East,
        _ => face // North and South stay on their axis
    };

    public static string ToLower(this BlockFace face)
    {
        return face switch
        {
            BlockFace.Up => "up",
            BlockFace.Down => "south",
            BlockFace.North => "north",
            BlockFace.South => "south",
            BlockFace.East => "east",
            BlockFace.West => "west",
            _ => "up"
        };
    }

    public static BlockFace? FromString(string str)
    {
        return str switch
        {
            "up" => BlockFace.Up,
            "down" => BlockFace.Down,
            "north" => BlockFace.North,
            "south" => BlockFace.South,
            "east" => BlockFace.East,
            "west" => BlockFace.West,
            _ => null
        };
    }

    public static Vector3 Normal(this BlockFace face)
    {
        return face switch
        {
            BlockFace.Up => Vector3.UnitY,
            BlockFace.Down => -Vector3.UnitY,
            BlockFace.North => -Vector3.UnitZ,
            BlockFace.South => Vector3.UnitZ,
            BlockFace.East => Vector3.UnitX,
            BlockFace.West => -Vector3.UnitX,
            _ => throw new Exception(nameof(face))
        };
    }
}
