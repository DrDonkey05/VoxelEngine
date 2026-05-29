using System.Text.Json;
using System.Text.Json.Serialization;

namespace VoxelEngine.src.json;

public class JsonState
{

    [JsonPropertyName("parts")]
    public List<StatePart> Parts { get; set; } = new();

    public class StateVariant
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;
        [JsonPropertyName("x")]
        public int X { get; set; } = 0;
        [JsonPropertyName("y")]
        public int Y { get; set; } = 0;
        [JsonPropertyName("uvlock")]
        public bool UVLock { get; set; } = false;
    }

    public class StatePart
    {
        [JsonPropertyName("apply")]
        public required StateVariant Variant { get; set; }

        [JsonPropertyName("when")]
        [JsonConverter(typeof(StateWhenConverter))]
        public IStateWhen? When { get; set; }

        public interface IStateWhen
        {
            bool Matches(Dictionary<string, string> stateProperties);
        }

        public class StateWhenConverter : JsonConverter<IStateWhen>
        {
            public override IStateWhen? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException("Expected JSON Object token block for 'when' definition parsing.");

                using JsonDocument doc = JsonDocument.ParseValue(ref reader);
                return ParseWhenElement(doc.RootElement);
            }

            private static IStateWhen? ParseWhenElement(JsonElement element)
            {
                if (element.ValueKind != JsonValueKind.Object) return null;

                if (element.TryGetProperty("AND", out JsonElement andElement))
                    return ParseLogicalGroup("AND", andElement);

                if (element.TryGetProperty("OR", out JsonElement orElement))
                    return ParseLogicalGroup("OR", orElement);

                var simple = new SimpleCondition();
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    string rawVal = property.Value.ValueKind == JsonValueKind.String
                        ? (property.Value.GetString() ?? string.Empty)
                        : property.Value.GetRawText().Trim('"');

                    string[] splitValues = rawVal.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    simple.Conditions[property.Name] = splitValues;
                }

                return simple;
            }

            private static LogicalGroupCondition ParseLogicalGroup(string op, JsonElement arrayElement)
            {
                var group = new LogicalGroupCondition { Operator = op };
                if (arrayElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement element in arrayElement.EnumerateArray())
                    {
                        var subCond = ParseWhenElement(element);
                        if (subCond != null) group.SubConditions.Add(subCond);
                    }
                }
                return group;
            }

            public override void Write(Utf8JsonWriter writer, IStateWhen value, JsonSerializerOptions options)
            {
                throw new NotImplementedException("Asset configuration loading engine is strictly read-only.");
            }
        }

        public class LogicalGroupCondition : IStateWhen
        {
            public string Operator { get; init; } = "AND";
            public List<IStateWhen> SubConditions { get; init; } = [];

            public bool Matches(Dictionary<string, string> stateProperties)
            {
                if (Operator.Equals("OR", StringComparison.OrdinalIgnoreCase))
                {
                    for (int i = 0; i < SubConditions.Count; i++)
                    {
                        if (SubConditions[i].Matches(stateProperties)) return true;
                    }
                    return false;
                }
                else
                {
                    for (int i = 0; i < SubConditions.Count; i++)
                    {
                        if (!SubConditions[i].Matches(stateProperties)) return false;
                    }
                    return true;
                }
            }
        }

        public class SimpleCondition : IStateWhen
        {
            public Dictionary<string, string[]> Conditions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

            public bool Matches(Dictionary<string, string> stateProperties)
            {
                foreach (var (conditionKey, allowedOptions) in Conditions)
                {
                    if (!stateProperties.TryGetValue(conditionKey, out string? stateValue))
                        return false;

                    bool matchedAny = false;
                    for (int i = 0; i < allowedOptions.Length; i++)
                    {
                        if (stateValue.Equals(allowedOptions[i], StringComparison.OrdinalIgnoreCase))
                        {
                            matchedAny = true;
                            break;
                        }
                    }

                    if (!matchedAny) return false;
                }

                return true;
            }
        }
    }
}