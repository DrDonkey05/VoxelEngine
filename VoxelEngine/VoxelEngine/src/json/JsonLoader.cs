using System.IO;
using System.Text.Json;

namespace VoxelEngine.src.json;

public static class JsonLoader
{
    public static T? Load<T>(string filePath)
    {
        if (!File.Exists(filePath))
            return default(T);

        try
        {
            string jsonText = File.ReadAllText(filePath);

            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
            };

            return JsonSerializer.Deserialize<T>(jsonText, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JSON ERROR] Failed to load model at {filePath}: {ex.Message}");
            return default(T);
        }
    }
}
