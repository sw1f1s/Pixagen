using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.RenderFeature.Meshes;
using Pixagen.Game.Features.RenderFeature.Textures;
using Pixagen.Game.Features.ResourceFeature.Meshes;
using Pixagen.Game.Features.ResourceFeature.Scenes;
using Pixagen.Game.Features.ResourceFeature.Textures;
using Pixagen.Game.Features.ScenesFeature.Serialization;

namespace Pixagen.Game.Features.ResourceFeature.Runtime;

public sealed class ResourceManager : IDisposeInject, IDisposable
{
    private readonly object _sync = new();
    private readonly MeshResourceStore _meshes = new();
    private readonly TextureResourceStore _textures = new();
    private readonly SceneResourceStore _scenes = new();
    private readonly Dictionary<string, SceneResourceScope> _sceneScopes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _meshSceneReferences = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _textureSceneReferences = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public MeshAsset LoadMesh(string asset)
    {
        ThrowIfDisposed();
        return _meshes.Load(asset);
    }

    public ValueTask<MeshAsset> LoadMeshAsync(string asset, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _meshes.LoadAsync(asset, cancellationToken);
    }

    public MeshAsset GetMesh(string asset)
    {
        return LoadMesh(asset);
    }

    public bool IsMeshLoaded(string asset)
    {
        ThrowIfDisposed();
        return _meshes.IsLoaded(asset);
    }

    public bool UnloadMesh(string asset)
    {
        ThrowIfDisposed();
        return _meshes.Unload(asset);
    }

    public TextureAsset LoadTexture(string asset)
    {
        ThrowIfDisposed();
        return _textures.Load(asset);
    }

    public ValueTask<TextureAsset> LoadTextureAsync(string asset, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _textures.LoadAsync(asset, cancellationToken);
    }

    public TextureAsset GetTexture(string asset)
    {
        return LoadTexture(asset);
    }

    public bool IsTextureLoaded(string asset)
    {
        ThrowIfDisposed();
        return _textures.IsLoaded(asset);
    }

    public bool UnloadTexture(string asset)
    {
        ThrowIfDisposed();
        return _textures.Unload(asset);
    }

    public SceneDefinition LoadScene(string path)
    {
        ThrowIfDisposed();
        return _scenes.Load(path);
    }

    public ValueTask<SceneDefinition> LoadSceneAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _scenes.LoadAsync(path, cancellationToken);
    }

    public SceneDefinition LoadDefaultScene()
    {
        ThrowIfDisposed();
        return _scenes.LoadDefault();
    }

    public ValueTask<SceneDefinition> LoadDefaultSceneAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _scenes.LoadDefaultAsync(cancellationToken);
    }

    public SceneDefinition LoadStartupScene(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? LoadDefaultScene()
            : LoadScene(path);
    }

    public ValueTask<SceneDefinition> LoadStartupSceneAsync(
        string? path,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(path)
            ? LoadDefaultSceneAsync(cancellationToken)
            : LoadSceneAsync(path, cancellationToken);
    }

    public SceneResourceScope LoadSceneWithResources(string path)
    {
        ThrowIfDisposed();
        string scenePath = ResourcePathResolver.NormalizeScenePath(path);
        return LoadSceneResources(LoadScene(scenePath), scenePath, false);
    }

    public async ValueTask<SceneResourceScope> LoadSceneWithResourcesAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        string scenePath = ResourcePathResolver.NormalizeScenePath(path);
        SceneDefinition scene = await LoadSceneAsync(scenePath, cancellationToken).ConfigureAwait(false);
        return await LoadSceneResourcesAsync(scene, scenePath, false, cancellationToken).ConfigureAwait(false);
    }

    public SceneResourceScope LoadDefaultSceneWithResources()
    {
        ThrowIfDisposed();
        string defaultPath = ResourcePathResolver.ResolveDefaultScenePath();
        bool hasDefaultFile = File.Exists(defaultPath);
        SceneDefinition scene = LoadDefaultScene();
        return LoadSceneResources(scene, hasDefaultFile ? defaultPath : null, true);
    }

    public async ValueTask<SceneResourceScope> LoadDefaultSceneWithResourcesAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        string defaultPath = ResourcePathResolver.ResolveDefaultScenePath();
        bool hasDefaultFile = File.Exists(defaultPath);
        SceneDefinition scene = await LoadDefaultSceneAsync(cancellationToken).ConfigureAwait(false);
        return await LoadSceneResourcesAsync(
                scene,
                hasDefaultFile ? defaultPath : null,
                true,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public SceneResourceScope LoadStartupSceneWithResources(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? LoadDefaultSceneWithResources()
            : LoadSceneWithResources(path);
    }

    public ValueTask<SceneResourceScope> LoadStartupSceneWithResourcesAsync(
        string? path,
        CancellationToken cancellationToken = default)
    {
        return string.IsNullOrWhiteSpace(path)
            ? LoadDefaultSceneWithResourcesAsync(cancellationToken)
            : LoadSceneWithResourcesAsync(path, cancellationToken);
    }

    public SceneResourceScope LoadSceneResources(SceneDefinition scene)
    {
        return LoadSceneResources(scene, null, false);
    }

    public async ValueTask<SceneResourceScope> LoadSceneResourcesAsync(
        SceneDefinition scene,
        CancellationToken cancellationToken = default)
    {
        return await LoadSceneResourcesAsync(scene, null, false, cancellationToken).ConfigureAwait(false);
    }

    private SceneResourceScope LoadSceneResources(
        SceneDefinition scene,
        string? scenePath,
        bool isDefaultScene)
    {
        ThrowIfDisposed();
        SceneResourceScope scope = CreateSceneResourceScope(scene, scenePath, isDefaultScene);
        EnsureSceneScopeIsFree(scope.SceneId);

        foreach (string asset in scope.MeshAssets)
        {
            _meshes.Load(asset);
        }

        foreach (string asset in scope.TextureAssets)
        {
            _textures.Load(asset);
        }

        RegisterSceneScope(scope);
        return scope;
    }

    private async ValueTask<SceneResourceScope> LoadSceneResourcesAsync(
        SceneDefinition scene,
        string? scenePath,
        bool isDefaultScene,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        SceneResourceScope scope = CreateSceneResourceScope(scene, scenePath, isDefaultScene);
        EnsureSceneScopeIsFree(scope.SceneId);

        List<Task>? tasks = null;
        foreach (string asset in scope.MeshAssets)
        {
            CollectPendingTask(_meshes.LoadAsync(asset, cancellationToken), ref tasks);
        }

        foreach (string asset in scope.TextureAssets)
        {
            CollectPendingTask(_textures.LoadAsync(asset, cancellationToken), ref tasks);
        }

        if (tasks is not null)
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        ThrowIfDisposed();
        RegisterSceneScope(scope);
        return scope;
    }

    public bool UnloadSceneResources(string sceneId)
    {
        ThrowIfDisposed();
        SceneResourceScope? scope;
        lock (_sync)
        {
            if (!_sceneScopes.TryGetValue(sceneId, out scope))
            {
                return false;
            }
        }

        UnloadSceneResources(scope);
        return true;
    }

    public bool UnloadSceneResources(SceneResourceScope scope)
    {
        ThrowIfDisposed();
        lock (_sync)
        {
            if (!_sceneScopes.Remove(scope.SceneId))
            {
                return false;
            }
        }

        UnloadSceneResourcesInternal(scope);
        return true;
    }

    public void SaveScene(string path, SceneDefinition scene)
    {
        ThrowIfDisposed();
        _scenes.Save(path, scene);
    }

    public void SaveDefaultScene(string path)
    {
        ThrowIfDisposed();
        _scenes.SaveDefault(path);
    }

    public bool IsSceneLoaded(string path)
    {
        ThrowIfDisposed();
        return _scenes.IsLoaded(path);
    }

    public bool UnloadScene(string path)
    {
        ThrowIfDisposed();
        return _scenes.Unload(path);
    }

    public ResourceStats GetStats()
    {
        ThrowIfDisposed();
        return new ResourceStats(
            _meshes.Count,
            _textures.Count,
            _textures.Bytes,
            _scenes.Count);
    }

    public void Clear()
    {
        ThrowIfDisposed();
        lock (_sync)
        {
            _sceneScopes.Clear();
            _meshSceneReferences.Clear();
            _textureSceneReferences.Clear();
        }

        _meshes.Clear();
        _textures.Clear();
        _scenes.Clear();
    }

    public void DisposeInject()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        lock (_sync)
        {
            _sceneScopes.Clear();
            _meshSceneReferences.Clear();
            _textureSceneReferences.Clear();
        }

        _meshes.Clear();
        _textures.Clear();
        _scenes.Clear();
        GC.SuppressFinalize(this);
    }

    private static SceneResourceScope CreateSceneResourceScope(
        SceneDefinition scene,
        string? scenePath,
        bool isDefaultScene)
    {
        var meshAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var textureAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectSceneResources(scene.Objects, meshAssets, textureAssets);
        return new SceneResourceScope(scene, scenePath, isDefaultScene, meshAssets, textureAssets);
    }

    private static void CollectPendingTask<T>(ValueTask<T> valueTask, ref List<Task>? tasks)
    {
        if (valueTask.IsCompletedSuccessfully)
        {
            valueTask.GetAwaiter().GetResult();
            return;
        }

        tasks ??= new List<Task>();
        tasks.Add(valueTask.AsTask());
    }

    private static void CollectSceneResources(
        IEnumerable<SceneObjectDefinition> objects,
        HashSet<string> meshAssets,
        HashSet<string> textureAssets)
    {
        foreach (SceneObjectDefinition sceneObject in objects)
        {
            foreach (IComponent component in sceneObject.Components)
            {
                if (component is Mesh mesh && !string.IsNullOrWhiteSpace(mesh.Asset))
                {
                    meshAssets.Add(ResourcePathResolver.NormalizeAssetId(mesh.Asset, ".obj"));
                    continue;
                }

                if (component is Material material &&
                    material.Texture is { } texture &&
                    !string.IsNullOrWhiteSpace(texture.Asset))
                {
                    textureAssets.Add(ResourcePathResolver.NormalizeAssetId(texture.Asset, ".ppm"));
                }
            }

            CollectSceneResources(sceneObject.Children, meshAssets, textureAssets);
        }
    }

    private void EnsureSceneScopeIsFree(string sceneId)
    {
        lock (_sync)
        {
            if (_sceneScopes.ContainsKey(sceneId))
            {
                throw new InvalidOperationException($"Scene resources for '{sceneId}' are already loaded.");
            }
        }
    }

    private void RegisterSceneScope(SceneResourceScope scope)
    {
        lock (_sync)
        {
            if (_sceneScopes.ContainsKey(scope.SceneId))
            {
                throw new InvalidOperationException($"Scene resources for '{scope.SceneId}' are already loaded.");
            }

            _sceneScopes.Add(scope.SceneId, scope);
            AddReferences(_meshSceneReferences, scope.MeshAssets);
            AddReferences(_textureSceneReferences, scope.TextureAssets);
        }
    }

    private void UnloadSceneResourcesInternal(SceneResourceScope scope)
    {
        List<string> meshesToUnload;
        List<string> texturesToUnload;
        lock (_sync)
        {
            meshesToUnload = RemoveReferences(_meshSceneReferences, scope.MeshAssets);
            texturesToUnload = RemoveReferences(_textureSceneReferences, scope.TextureAssets);
        }

        foreach (string asset in meshesToUnload)
        {
            _meshes.Unload(asset);
        }

        foreach (string asset in texturesToUnload)
        {
            _textures.Unload(asset);
        }

        if (scope.ScenePath is not null)
        {
            _scenes.Unload(scope.ScenePath);
            return;
        }

        if (scope.IsDefaultScene)
        {
            _scenes.UnloadDefault();
        }
    }

    private static void AddReferences(Dictionary<string, int> references, IReadOnlyList<string> assets)
    {
        foreach (string asset in assets)
        {
            references.TryGetValue(asset, out int count);
            references[asset] = count + 1;
        }
    }

    private static List<string> RemoveReferences(Dictionary<string, int> references, IReadOnlyList<string> assets)
    {
        var removed = new List<string>();
        foreach (string asset in assets)
        {
            if (!references.TryGetValue(asset, out int count))
            {
                continue;
            }

            count--;
            if (count <= 0)
            {
                references.Remove(asset);
                removed.Add(asset);
                continue;
            }

            references[asset] = count;
        }

        return removed;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ResourceManager));
        }
    }
}
