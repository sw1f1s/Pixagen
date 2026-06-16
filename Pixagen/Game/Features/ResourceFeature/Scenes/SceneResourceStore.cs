using Pixagen.Game.Features.ResourceFeature.Runtime;
using Pixagen.Game.Features.ScenesFeature.Serialization;

namespace Pixagen.Game.Features.ResourceFeature.Scenes;

internal sealed class SceneResourceStore
{
    private const string DefaultSceneCacheKey = "default";

    private readonly object _sync = new();
    private readonly SceneAssetStore _assetStore = new();
    private readonly Dictionary<string, SceneDefinition> _cache = new(StringComparer.OrdinalIgnoreCase);
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

    public SceneDefinition Load(string path)
    {
        string key = ResourcePathResolver.NormalizeScenePath(path);
        Task<SceneDefinition>? loading = null;
        lock (_sync)
        {
            if (_cache.TryGetValue(key, out SceneDefinition? scene))
            {
                return scene;
            }

            if (_loading.TryGetValue(key, out LoadOperation? operation))
            {
                loading = operation.LoadTask;
            }
        }

        if (loading is not null)
        {
            return loading.GetAwaiter().GetResult();
        }

        SceneDefinition loaded = _assetStore.Load(key);
        lock (_sync)
        {
            if (_cache.TryGetValue(key, out SceneDefinition? scene))
            {
                return scene;
            }

            _cache[key] = loaded;
            return loaded;
        }
    }

    public ValueTask<SceneDefinition> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        string key = ResourcePathResolver.NormalizeScenePath(path);
        lock (_sync)
        {
            if (_cache.TryGetValue(key, out SceneDefinition? scene))
            {
                return new ValueTask<SceneDefinition>(scene);
            }

            if (_loading.TryGetValue(key, out LoadOperation? operation))
            {
                return new ValueTask<SceneDefinition>(operation.LoadTask);
            }

            var newOperation = new LoadOperation(_version);
            newOperation.LoadTask = LoadAndCacheAsync(key, newOperation, cancellationToken);
            _loading[key] = newOperation;
            return new ValueTask<SceneDefinition>(newOperation.LoadTask);
        }
    }

    private async Task<SceneDefinition> LoadAndCacheAsync(
        string key,
        LoadOperation operation,
        CancellationToken cancellationToken)
    {
        try
        {
            SceneDefinition loaded = await _assetStore.LoadAsync(key, cancellationToken).ConfigureAwait(false);
            lock (_sync)
            {
                if (_loading.TryGetValue(key, out LoadOperation? currentOperation) &&
                    ReferenceEquals(currentOperation, operation))
                {
                    _loading.Remove(key);
                }

                if (operation.Version != _version)
                {
                    return loaded;
                }

                if (_cache.TryGetValue(key, out SceneDefinition? scene))
                {
                    return scene;
                }

                _cache[key] = loaded;
                return loaded;
            }
        }
        catch
        {
            lock (_sync)
            {
                if (_loading.TryGetValue(key, out LoadOperation? currentOperation) &&
                    ReferenceEquals(currentOperation, operation))
                {
                    _loading.Remove(key);
                }
            }

            throw;
        }
    }

    public SceneDefinition LoadDefault()
    {
        string defaultPath = ResourcePathResolver.ResolveDefaultScenePath();
        if (File.Exists(defaultPath))
        {
            return Load(defaultPath);
        }

        lock (_sync)
        {
            if (_cache.TryGetValue(DefaultSceneCacheKey, out SceneDefinition? scene))
            {
                return scene;
            }

            SceneDefinition created = DefaultSceneFactory.Create();
            _cache[DefaultSceneCacheKey] = created;
            return created;
        }
    }

    public ValueTask<SceneDefinition> LoadDefaultAsync(CancellationToken cancellationToken = default)
    {
        string defaultPath = ResourcePathResolver.ResolveDefaultScenePath();
        if (File.Exists(defaultPath))
        {
            return LoadAsync(defaultPath, cancellationToken);
        }

        lock (_sync)
        {
            if (_cache.TryGetValue(DefaultSceneCacheKey, out SceneDefinition? scene))
            {
                return new ValueTask<SceneDefinition>(scene);
            }

            SceneDefinition created = DefaultSceneFactory.Create();
            _cache[DefaultSceneCacheKey] = created;
            return new ValueTask<SceneDefinition>(created);
        }
    }

    public void Save(string path, SceneDefinition scene)
    {
        string key = ResourcePathResolver.NormalizeScenePath(path);
        _assetStore.Save(key, scene);
        lock (_sync)
        {
            _version++;
            _loading.Remove(key);
            _cache[key] = scene;
        }
    }

    public void SaveDefault(string path)
    {
        Save(path, LoadDefault());
    }

    public bool IsLoaded(string path)
    {
        string key = ResourcePathResolver.NormalizeScenePath(path);
        lock (_sync)
        {
            return _cache.ContainsKey(key);
        }
    }

    public bool Unload(string path)
    {
        string key = ResourcePathResolver.NormalizeScenePath(path);
        lock (_sync)
        {
            _version++;
            _loading.Remove(key);
            return _cache.Remove(key);
        }
    }

    public bool UnloadDefault()
    {
        lock (_sync)
        {
            _version++;
            _loading.Remove(DefaultSceneCacheKey);
            return _cache.Remove(DefaultSceneCacheKey);
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

    private sealed class LoadOperation
    {
        public LoadOperation(int version)
        {
            Version = version;
        }

        public int Version { get; }
        public Task<SceneDefinition> LoadTask { get; set; } = null!;
    }
}
