using Silk.NET.OpenGL;
using VoxelEngine.src.json;
using VoxelEngine.src.models;
using VoxelEngine.src.rendering.textures;

namespace VoxelEngine.src.world;

public class Block
{
    public static Block UP_BLOCK = new Block("up_block");
    public static Block CROSS_BLOCK = new Block("cross_block");
    public static Block ANIMATED_BLOCK = new Block("animated_block");
    public static Block LAYERED_BLOCK = new Block("layered_block");

    public string Id { get; }
    public BlockState DefaultState { get; private set; }

    public readonly Dictionary<string, BlockState> StatesByProperties = new();

    public Block(string id)
    {
        Id = id;
    }

    public IEnumerable<BlockState> GetAllStates()
    {
        return StatesByProperties.Values;
    }

    public void RegisterStates(List<BlockState> states, string defaultCondition = "")
    {
        foreach (var state in states)
        {
            string key = state.GetPropertyKey();
            StatesByProperties.Add(key, state);
        }

        DefaultState = StatesByProperties.TryGetValue(defaultCondition, out var d)
            ? d
            : StatesByProperties.Values.First();
    }

    public BlockState GetState(Dictionary<string, string> properties)
    {
        string key = BlockState.GetPropertyKey(properties);
        if (StatesByProperties.TryGetValue(key, out var state))
        {
            return state;
        }
        return DefaultState; // Safe fallback
    }

    public void GenerateStatesAndModels(TextureAtlas atlas)
    {
        var stateList = new List<BlockState>();
        JsonState? jsonState = JsonLoader.Load<JsonState>($"./assets/blockstates/{this.Id}.json");

        if (jsonState == null)
            return;

        BlockModelBakery.CreateModel(jsonState, atlas);

        foreach (var variant in jsonState.Variants)
        {
            string variantKey = variant.Key; // e.g., "axis=z" or ""
            var variantData = variant.Value;

            // Parse "axis=z" into a clean Dictionary<string, string>
            var properties = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(variantKey))
            {
                string[] pairs = variantKey.Split(',');
                foreach (string pair in pairs)
                {
                    string[] split = pair.Split('=');
                    if (split.Length == 2) properties[split[0]] = split[1];
                }
            }

            var newState = new BlockState(
                this,
                variantData.Model,
                $"{variantData.Model}{variantKey}",
                properties
            );

            stateList.Add(newState);
        }

        RegisterStates(stateList, defaultCondition: "");
    }
}
