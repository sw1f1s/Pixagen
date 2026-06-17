using System.Globalization;
using Pixagen.Game.Features.ResourceFeature.Runtime;

namespace Pixagen.Game.Features.ResourceFeature.Textures;

internal sealed class TextureResourceStore
{
    private readonly object _sync = new();
    private readonly Dictionary<string, TextureAsset> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, LoadOperation> _loading = new(StringComparer.OrdinalIgnoreCase);
    private int _version;

    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _cache.Count;
            }
        }
    }

    public long Bytes
    {
        get
        {
            lock (_sync)
            {
                long bytes = 0;
                foreach (TextureAsset texture in _cache.Values)
                {
                    bytes += texture.MipPixelCount * 4L;
                }

                return bytes;
            }
        }
    }

    public TextureAsset Load(string asset)
    {
        return LoadTracked(asset).Resource;
    }

    public ResourceLoadResult<TextureAsset> LoadTracked(string asset)
    {
        string id = ResourcePathResolver.NormalizeAssetId(asset, ".ppm");
        Task<ResourceLoadResult<TextureAsset>>? loading = null;
        lock (_sync)
        {
            if (_cache.TryGetValue(id, out TextureAsset? texture))
            {
                return new ResourceLoadResult<TextureAsset>(texture, false);
            }

            if (_loading.TryGetValue(id, out LoadOperation? operation))
            {
                loading = operation.LoadTask;
            }
        }

        if (loading is not null)
        {
            return new ResourceLoadResult<TextureAsset>(loading.GetAwaiter().GetResult().Resource, false);
        }

        TextureAsset loaded = LoadPpm(id);
        lock (_sync)
        {
            if (_cache.TryGetValue(id, out TextureAsset? texture))
            {
                return new ResourceLoadResult<TextureAsset>(texture, false);
            }

            _cache[id] = loaded;
            return new ResourceLoadResult<TextureAsset>(loaded, true);
        }
    }

    public ValueTask<TextureAsset> LoadAsync(string asset, CancellationToken cancellationToken = default)
    {
        ValueTask<ResourceLoadResult<TextureAsset>> loadTask = LoadTrackedAsync(asset, cancellationToken);
        if (loadTask.IsCompletedSuccessfully)
        {
            return new ValueTask<TextureAsset>(loadTask.GetAwaiter().GetResult().Resource);
        }

        return CompleteLoadAsync(loadTask);
    }

    public ValueTask<ResourceLoadResult<TextureAsset>> LoadTrackedAsync(
        string asset,
        CancellationToken cancellationToken = default)
    {
        string id = ResourcePathResolver.NormalizeAssetId(asset, ".ppm");
        lock (_sync)
        {
            if (_cache.TryGetValue(id, out TextureAsset? texture))
            {
                return new ValueTask<ResourceLoadResult<TextureAsset>>(new ResourceLoadResult<TextureAsset>(texture, false));
            }

            if (_loading.TryGetValue(id, out LoadOperation? operation))
            {
                return AwaitExistingLoadAsync(operation.LoadTask);
            }

            var newOperation = new LoadOperation(_version);
            newOperation.LoadTask = LoadAndCacheAsync(id, newOperation, cancellationToken);
            _loading[id] = newOperation;
            return new ValueTask<ResourceLoadResult<TextureAsset>>(newOperation.LoadTask);
        }
    }

    private async Task<ResourceLoadResult<TextureAsset>> LoadAndCacheAsync(
        string id,
        LoadOperation operation,
        CancellationToken cancellationToken)
    {
        try
        {
            TextureAsset loaded = await LoadPpmAsync(id, cancellationToken).ConfigureAwait(false);
            lock (_sync)
            {
                if (_loading.TryGetValue(id, out LoadOperation? currentOperation) &&
                    ReferenceEquals(currentOperation, operation))
                {
                    _loading.Remove(id);
                }

                if (operation.Version != _version)
                {
                    return new ResourceLoadResult<TextureAsset>(loaded, false);
                }

                if (_cache.TryGetValue(id, out TextureAsset? texture))
                {
                    return new ResourceLoadResult<TextureAsset>(texture, false);
                }

                _cache[id] = loaded;
                return new ResourceLoadResult<TextureAsset>(loaded, true);
            }
        }
        catch
        {
            lock (_sync)
            {
                if (_loading.TryGetValue(id, out LoadOperation? currentOperation) &&
                    ReferenceEquals(currentOperation, operation))
                {
                    _loading.Remove(id);
                }
            }

            throw;
        }
    }

    private static async ValueTask<TextureAsset> CompleteLoadAsync(
        ValueTask<ResourceLoadResult<TextureAsset>> loadTask)
    {
        ResourceLoadResult<TextureAsset> result = await loadTask.ConfigureAwait(false);
        return result.Resource;
    }

    private static async ValueTask<ResourceLoadResult<TextureAsset>> AwaitExistingLoadAsync(
        Task<ResourceLoadResult<TextureAsset>> loadTask)
    {
        ResourceLoadResult<TextureAsset> result = await loadTask.ConfigureAwait(false);
        return new ResourceLoadResult<TextureAsset>(result.Resource, false);
    }

    public bool IsLoaded(string asset)
    {
        string id = ResourcePathResolver.NormalizeAssetId(asset, ".ppm");
        lock (_sync)
        {
            return _cache.ContainsKey(id);
        }
    }

    public bool Unload(string asset)
    {
        string id = ResourcePathResolver.NormalizeAssetId(asset, ".ppm");
        lock (_sync)
        {
            _version++;
            _loading.Remove(id);
            return _cache.Remove(id);
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            _version++;
            _loading.Clear();
            _cache.Clear();
        }
    }

    private static TextureAsset LoadPpm(string id)
    {
        string path = ResourcePathResolver.ResolveTexturePath(id);
        return ParsePpm(id, path, File.ReadAllText(path));
    }

    private static async Task<TextureAsset> LoadPpmAsync(string id, CancellationToken cancellationToken)
    {
        string path = ResourcePathResolver.ResolveTexturePath(id);
        string text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return ParsePpm(id, path, text);
    }

    private static TextureAsset ParsePpm(string id, string path, string text)
    {
        string[] tokens = Tokenize(text);
        int index = 0;

        string magic = ReadToken(tokens, ref index, path);
        if (!string.Equals(magic, "P3", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Texture '{id}' uses unsupported PPM format '{magic}'. Only ASCII P3 is supported.");
        }

        int width = ReadInt(tokens, ref index, path);
        int height = ReadInt(tokens, ref index, path);
        int maxValue = ReadInt(tokens, ref index, path);
        if (maxValue <= 0)
        {
            throw new InvalidOperationException($"Texture '{id}' has invalid PPM max value '{maxValue}'.");
        }

        var pixels = new TexturePixel[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            byte r = ScaleChannel(ReadInt(tokens, ref index, path), maxValue);
            byte g = ScaleChannel(ReadInt(tokens, ref index, path), maxValue);
            byte b = ScaleChannel(ReadInt(tokens, ref index, path), maxValue);
            pixels[i] = new TexturePixel(r, g, b, 255);
        }

        return new TextureAsset(id, width, height, pixels);
    }

    private static string[] Tokenize(string text)
    {
        var tokens = new List<string>();
        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } line)
        {
            int commentIndex = line.IndexOf('#', StringComparison.Ordinal);
            if (commentIndex >= 0)
            {
                line = line[..commentIndex];
            }

            tokens.AddRange(line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return tokens.ToArray();
    }

    private static string ReadToken(string[] tokens, ref int index, string path)
    {
        if ((uint)index >= (uint)tokens.Length)
        {
            throw new InvalidOperationException($"Unexpected end of texture file '{path}'.");
        }

        return tokens[index++];
    }

    private static int ReadInt(string[] tokens, ref int index, string path)
    {
        string token = ReadToken(tokens, ref index, path);
        return int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : throw new InvalidOperationException($"Invalid integer token '{token}' in texture file '{path}'.");
    }

    private static byte ScaleChannel(int value, int maxValue)
    {
        return (byte)Math.Clamp((int)MathF.Round(value * 255f / maxValue), 0, 255);
    }

    private sealed class LoadOperation
    {
        public LoadOperation(int version)
        {
            Version = version;
        }

        public int Version { get; }
        public Task<ResourceLoadResult<TextureAsset>> LoadTask { get; set; } = null!;
    }
}
