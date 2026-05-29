namespace ServerProj.src.noise;

public class Perlin
{
    private readonly int[] _p = new int[512];

    public Perlin(int seed)
    {
        var random = new Random(seed);
        var permutation = Enumerable.Range(0, 256).OrderBy(x => random.Next()).ToArray();

        for (int i = 0; i < 512; i++)
            _p[i] = permutation[i % 256];
    }

    #region 2D Noise
    public double GetOctaveNoise2D(double x, double y, int octaves, double persistence, double lacunarity)
    {
        double total = 0;
        double frequency = 1;
        double amplitude = 1;
        double maxValue = 0;

        for (int i = 0; i < octaves; i++)
        {
            total += Noise2D(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return total / maxValue;
    }

    public double Noise2D(double x, double y)
    {
        // We just pass a constant for Z to the 3D noise function
        // 0.0 is fine, but adding a small offset can prevent "zero-point" artifacts
        return Noise3D(x, y, 0.0);
    }
    #endregion

    #region 3D Noise
    public double GetOctaveNoise3D(double x, double y, double z, int octaves, double persistence, double lacunarity)
    {
        double total = 0;
        double frequency = 1;
        double amplitude = 1;
        double maxValue = 0;

        for (int i = 0; i < octaves; i++)
        {
            total += Noise3D(x * frequency, y * frequency, z * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return total / maxValue;
    }

    public double Noise3D(double x, double y, double z)
    {
        int X = (int)Math.Floor(x) & 255;
        int Y = (int)Math.Floor(y) & 255;
        int Z = (int)Math.Floor(z) & 255;

        x -= Math.Floor(x);
        y -= Math.Floor(y);
        z -= Math.Floor(z);

        double u = Fade(x);
        double v = Fade(y);
        double w = Fade(z);

        int a = _p[X] + Y;
        int aa = _p[a] + Z;
        int ab = _p[a + 1] + Z;
        int b = _p[X + 1] + Y;
        int ba = _p[b] + Z;
        int bb = _p[b + 1] + Z;

        return Lerp(w, Lerp(v, Lerp(u, Grad(_p[aa], x, y, z), Grad(_p[ba], x - 1, y, z)),
                               Lerp(u, Grad(_p[ab], x, y - 1, z), Grad(_p[bb], x - 1, y - 1, z))),
                       Lerp(v, Lerp(u, Grad(_p[aa + 1], x, y, z - 1), Grad(_p[ba + 1], x - 1, y, z - 1)),
                               Lerp(u, Grad(_p[ab + 1], x, y - 1, z - 1), Grad(_p[bb + 1], x - 1, y - 1, z - 1))));
    }
    #endregion

    #region Noise Helpers
    private double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);
    private double Lerp(double t, double a, double b) => a + t * (b - a);

    private double Grad(int hash, double x, double y, double z)
    {
        int h = hash & 15;
        double u = h < 8 ? x : y;
        double v = h < 4 ? y : h == 12 || h == 14 ? x : z;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
    #endregion
}
