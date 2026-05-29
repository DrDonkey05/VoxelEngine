namespace VoxelEngine.src.world.terrain;

public record class NoiseSettings(
    int seed, float scale, int octaves, float persistance, float lacuranity,
    int baseHeight, int heightVariance)
{

}
