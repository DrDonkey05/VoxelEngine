using VoxelEngine.src.world;

namespace VoxelEngine.src.models;

public class BlockState
{
    private static ushort globalBlockStatesCounter = 0;
    public static Dictionary<int, BlockState> ById = new();

    public ushort Id { get; }
    public Block Owner { get; }

    // Fallback references for standard block states
    public string Model { get; private set; } = string.Empty;
    public string ModelVariant { get; private set; } = string.Empty;

    public Dictionary<string, string> Properties { get; }


    public BlockState(Block owner, string model, string variant, Dictionary<string, string> properties)
    {
        Id = globalBlockStatesCounter++;
        Owner = owner;
        Model = model;
        ModelVariant = $"{model}{variant}";
        Properties = properties;

        ById.Add(Id, this);
    }

    public string GetPropertyKey()
    {
        return GetPropertyKey(Properties);
    }

    public static string GetPropertyKey(Dictionary<string, string> properties)
    {
        if (properties == null || properties.Count == 0) return "";

        var sortedPairs = properties.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}");
        return string.Join(",", sortedPairs);
    }
}