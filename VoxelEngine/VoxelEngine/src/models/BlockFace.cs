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
}
