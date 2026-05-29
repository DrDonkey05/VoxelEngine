namespace ServerProj.src.noise;

public static class Perlin
{
    #region 2D Noise
    public static double GetOctaveNoise2D(int seed, double x, double y, int octaves, double persistence, double lacunarity)
    {
        double total = 0;
        double frequency = 1;
        double amplitude = 1;
        double maxValue = 0;

        for (int i = 0; i < octaves; i++)
        {
            total += Noise2D(seed, x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return total / maxValue;
    }

    public static double Noise2D(int seed, double x, double y)
    {
        // We just pass a constant for Z to the 3D noise function
        // 0.0 is fine, but adding a small offset can prevent "zero-point" artifacts
        return Noise3D(seed, x, y, 0.0);
    }
    #endregion

    #region 3D Noise
    public static double GetOctaveNoise3D(int seed, double x, double y, double z, int octaves, double persistence, double lacunarity)
    {
        double total = 0;
        double frequency = 1;
        double amplitude = 1;
        double maxValue = 0;

        for (int i = 0; i < octaves; i++)
        {
            total += Noise3D(seed, x * frequency, y * frequency, z * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return total / maxValue;
    }

    public static double Noise3D(int seed, double x, double y, double z)
    {
        int ix = (int)Math.Floor(x);
        int iy = (int)Math.Floor(y);
        int iz = (int)Math.Floor(z);

        double fx = x - ix;
        double fy = y - iy;
        double fz = z - iz;

        double u = Fade(fx);
        double v = Fade(fy);
        double w = Fade(fz);

        int h000 = Hash(ix, iy, iz, seed);
        int h100 = Hash(ix + 1, iy, iz, seed);
        int h010 = Hash(ix, iy + 1, iz, seed);
        int h110 = Hash(ix + 1, iy + 1, iz, seed);
        int h001 = Hash(ix, iy, iz + 1, seed);
        int h101 = Hash(ix + 1, iy, iz + 1, seed);
        int h011 = Hash(ix, iy + 1, iz + 1, seed);
        int h111 = Hash(ix + 1, iy + 1, iz + 1, seed);

        return Lerp(w,
            Lerp(v, Lerp(u, Grad(h000, fx, fy, fz), Grad(h100, fx - 1, fy, fz)),
                    Lerp(u, Grad(h010, fx, fy - 1, fz), Grad(h110, fx - 1, fy - 1, fz))),
            Lerp(v, Lerp(u, Grad(h001, fx, fy, fz - 1), Grad(h101, fx - 1, fy, fz - 1)),
                    Lerp(u, Grad(h011, fx, fy - 1, fz - 1), Grad(h111, fx - 1, fy - 1, fz - 1)))
        );
    }
    #endregion

    #region Noise Helpers
    private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);
    private static double Lerp(double t, double a, double b) => a + t * (b - a);

    private static double Grad(int hash, double x, double y, double z)
    {
        int h = hash & 15;
        double u = h < 8 ? x : y;
        double v = h < 4 ? y : h == 12 || h == 14 ? x : z;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
    private static int Hash(int x, int y, int z, int seed)
    {
        uint h = (uint)(seed ^ x ^ (y * 397) ^ (z * 769));
        h ^= 0xdeadbeef;
        h ^= h >> 16;
        h *= 0x85ebca6b;
        h ^= h >> 13;
        h *= 0xc2b2ae35;
        h ^= h >> 16;
        return (int)(h & 255);
    }
    #endregion
}
