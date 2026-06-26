using System.Text.Json;
using System.Text.Json.Serialization;
using Pixagen.Rendering;

namespace Pixagen.Game.Features.ScenesFeature.Serialization;

public sealed class SceneAssetStore
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public SceneDefinition Load(string path)
    {
        using FileStream stream = File.OpenRead(path);
        SceneDefinition? scene = JsonSerializer.Deserialize<SceneDefinition>(stream, Options);
        return scene ?? throw new InvalidOperationException($"Scene file '{path}' is empty or invalid.");
    }

    public async Task<SceneDefinition> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using FileStream stream = File.OpenRead(path);
        SceneDefinition? scene = await JsonSerializer
            .DeserializeAsync<SceneDefinition>(stream, Options, cancellationToken)
            .ConfigureAwait(false);
        return scene ?? throw new InvalidOperationException($"Scene file '{path}' is empty or invalid.");
    }

    public void Save(string path, SceneDefinition scene)
    {
        string fullPath = Path.GetFullPath(path);
        string directory = Path.GetDirectoryName(fullPath) ?? AppContext.BaseDirectory;
        Directory.CreateDirectory(directory);

        string tempPath = Path.Combine(
            directory,
            $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (FileStream stream = File.Create(tempPath))
            {
                JsonSerializer.Serialize(stream, scene, Options);
            }

            File.Move(tempPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IncludeFields = true,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new PixelColorJsonConverter());
        options.Converters.Add(new FixJsonConverter());
        options.Converters.Add(new Vector3JsonConverter());
        options.Converters.Add(new QuaternionJsonConverter());
        options.Converters.Add(new TransformJsonConverter());
        options.Converters.Add(new MaterialJsonConverter());
        options.Converters.Add(new SceneComponentJsonConverter());
        return options;
    }
}
