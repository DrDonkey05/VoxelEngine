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
}
