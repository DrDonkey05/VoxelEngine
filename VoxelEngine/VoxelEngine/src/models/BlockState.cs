using System.IO;
using System.Text.Json;
using VoxelEngine.src.json;
using VoxelEngine.src.world;

namespace VoxelEngine.src.models;

public class BlockState
{
    private static int globalBlockStatesCounter = 0;
    public static Dictionary<int, BlockState> ById = new();

    public int Id { get; }
    public Block Owner { get; }
    public string Model { get; }
    public string ModelVariant { get; }

    public Dictionary<string, string> Properties { get; }

    public BlockState(Block owner, string model, string modelVariant, Dictionary<string, string> properties)
    {
        Id = globalBlockStatesCounter++;
        Owner = owner;
        Model = model;
        ModelVariant = modelVariant;
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
