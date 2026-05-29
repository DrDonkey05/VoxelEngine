using ServerProj.src.noise;

namespace VoxelEngine.src.world.terrain;

public static class Noise
{
    public static double Get2D(NoiseSettings settings, int x, int y)
    {
        return Perlin.GetOctaveNoise2D(settings.seed, x * settings.scale, y * settings.scale, settings.octaves, settings.persistance, settings.lacuranity);
    }
    public static double Get3D(NoiseSettings settings, int x, int y, int z)
    {
        return Perlin.GetOctaveNoise3D(settings.seed, x * settings.scale, y * settings.scale, z * settings.scale, settings.octaves, settings.persistance, settings.lacuranity);
    }
}
