using System.Text.Json.Serialization;

namespace VoxelEngine.src.json;

public class JsonModel
{
    [JsonPropertyName("parent")]
    public string Parent { get; set; } = string.Empty;
    [JsonPropertyName("elements")]
    public List<JsonModel.Element> Elements { get; set; } = new();
    [JsonPropertyName("textures")]
    public Dictionary<string, string> Textures { get; set; } = new();

    public class Element
    {
        [JsonPropertyName("from")]
        public float[] From { get; set; } = new float[0];
        [JsonPropertyName("to")]
        public float[] To { get; set; } = new float[0];
        [JsonPropertyName("faces")]
        public Dictionary<string, Element.ElementFace> Faces { get; set; } = new();
        [JsonPropertyName("rotation")]
        public ElementRotation? Rotation { get; set; } = null;

        public class ElementFace
        {
            [JsonPropertyName("uv")]
            public float[] UV { get; set; } = new float[0];
            [JsonPropertyName("texture")]
            public string Texture { get; set; } = string.Empty;
            [JsonPropertyName("tintindex")]
            public int TintIndex { get; set; } = -1;
            [JsonPropertyName("cullface")]
            public string CullFace { get; set; } = string.Empty;
        }
        public class ElementRotation
        {
            [JsonPropertyName("angle")]
            public float Angle { get; set; } = 0.0f;
            [JsonPropertyName("axis")]
            public string Axis { get; set; } = string.Empty;
            [JsonPropertyName("origin")]
            public float[] Origin { get; set; } = new float[0];
            [JsonPropertyName("rescale")]
            public bool Rescale { get; set; } = true;
        }
    }
}
