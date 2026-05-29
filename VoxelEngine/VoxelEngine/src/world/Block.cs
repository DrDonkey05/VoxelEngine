using Silk.NET.OpenGL;
using VoxelEngine.src.json;
using VoxelEngine.src.models;
using VoxelEngine.src.rendering.textures;

namespace VoxelEngine.src.world;

public class Block
{
    public static Block AIR = new Block("air");
    public static Block DIRT = new Block("dirt");
    public static Block STONE = new Block("stone");
    public static Block GRASS_BLOCK = new Block("grass_block");

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

    public void RegisterStates(List<BlockState> states)
    {
        foreach (var state in states)
        {
            string key = state.GetPropertyKey();
            StatesByProperties.Add(key, state);
        }

        DefaultState = StatesByProperties.TryGetValue(states[0].ModelVariant, out var d)
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

        HashSet<string> createdModelKeys = new();
        if (jsonState.Parts != null && jsonState.Parts.Count > 0)
        {
            // Gather all property conditions present inside the JSON rules
            var uniquePropertyDomain = DiscoverMultipartProperties(jsonState.Parts);

            // Compute every possible state combination permutation array layout
            var listAllPossiblePermutations = GenerateCombinations(uniquePropertyDomain);

            List<JsonState.StateVariant> variantsOfPartList = new();
            foreach (var propertySet in listAllPossiblePermutations)
            {
                string variantName = BlockState.GetPropertyKey(propertySet);
                // Check which separate multi-part element segments match this current active state configuration
                foreach (var part in jsonState.Parts)
                {
                    if (part.When == null || part.When.Matches(propertySet))
                    {
                        // Compose unique variant descriptors per sub-component to ensure bakery caches align
                        // $"{part.Variant.Model}_x{part.Variant.X}_y{part.Variant.Y}";
                        variantsOfPartList.Add(part.Variant);
                    }
                }
                if (variantsOfPartList.Count <= 0)
                    continue;

                var newState = new BlockState(this, this.Id, variantName, propertySet);
                ModelBakery.CreateModel(variantsOfPartList, newState.Model, newState.ModelVariant, atlas);
                variantsOfPartList.Clear();
                stateList.Add(newState);
            }
        }

        RegisterStates(stateList);
    }

    // Helper to parse strings like "axis=z,facing=north" into structural Dictionaries
    private static Dictionary<string, string> ParsePropertyKey(string key)
    {
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(key)) return properties;

        string[] pairs = key.Split(',');
        foreach (string pair in pairs)
        {
            string[] split = pair.Split('=');
            if (split.Length == 2) properties[split[0]] = split[1];
        }
        return properties;
    }

    // Unpacks all possible states a multipart block can experience based on its JSON files values
    private static Dictionary<string, HashSet<string>> DiscoverMultipartProperties(List<JsonState.StatePart> parts)
    {
        var domain = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in parts)
        {
            if (part.When == null) continue;

            // We gather keys from our compiled structural objects
            GatherPropertiesFromCondition(part.When, domain);
        }

        return domain;
    }

    private static void GatherPropertiesFromCondition(JsonState.StatePart.IStateWhen condition, Dictionary<string, HashSet<string>> domain)
    {
        if (condition is JsonState.StatePart.SimpleCondition simple)
        {
            foreach (var pair in simple.Conditions)
            {
                if (!domain.ContainsKey(pair.Key))
                {
                    domain[pair.Key] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    domain[pair.Key].Add("none");
                }

                // Include all split pipe values into the possibilities list (e.g., "side", "up")
                foreach (var val in pair.Value)
                {
                    domain[pair.Key].Add(val);
                }
            }
        }
        else if (condition is JsonState.StatePart.LogicalGroupCondition group)
        {
            foreach (var sub in group.SubConditions)
            {
                GatherPropertiesFromCondition(sub, domain);
            }
        }
    }

    // Recursive permutation generator math to expand property tables out cleanly
    private static List<Dictionary<string, string>> GenerateCombinations(Dictionary<string, HashSet<string>> domain)
    {
        var results = new List<Dictionary<string, string>>();
        var keys = domain.Keys.ToList();

        // If the block has zero properties (no conditions parsed anywhere)
        if (keys.Count == 0)
        {
            results.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            return results;
        }

        GenerateCombinationsRecursive(0, keys, domain, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), results);
        return results;
    }

    private static void GenerateCombinationsRecursive(
        int index,
        List<string> keys,
        Dictionary<string, HashSet<string>> domain,
        Dictionary<string, string> current,
        List<Dictionary<string, string>> results)
    {
        if (index == keys.Count)
        {
            // Add a clean snapshot copy of the verified state configuration
            results.Add(new Dictionary<string, string>(current, StringComparer.OrdinalIgnoreCase));
            return;
        }

        string currentKey = keys[index];
        foreach (string val in domain[currentKey])
        {
            current[currentKey] = val;

            GenerateCombinationsRecursive(index + 1, keys, domain, current, results);

            // FIX: Backtracking step. Clear out this key-value choice before 
            // looping around to evaluate alternative brother branches!
            current.Remove(currentKey);
        }
    }
}
