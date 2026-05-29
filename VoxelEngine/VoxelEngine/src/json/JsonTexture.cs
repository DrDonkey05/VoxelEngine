using System.Text.Json.Serialization;

namespace VoxelEngine.src.json;

public class JsonTexture
{
    [JsonPropertyName("tickdelay")]
    public int TickDelay { get; set; }
}
