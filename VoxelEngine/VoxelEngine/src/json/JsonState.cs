using System.Text.Json.Serialization;

namespace VoxelEngine.src.json;

public class JsonState
{
    [JsonPropertyName("variants")]
    public Dictionary<string, StateVariant> Variants { get; set; } = new();

    public class StateVariant
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;
        [JsonPropertyName("x")]
        public int X { get; set; } = 0;
        [JsonPropertyName("y")]
        public int Y { get; set; } = 0;
    }
}
