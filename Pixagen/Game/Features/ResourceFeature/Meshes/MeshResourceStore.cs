using System.Globalization;
using Pixagen.Game.Features.RenderFeature.Meshes;
using Pixagen.Game.Features.ResourceFeature.Runtime;
using Float2 = System.Numerics.Vector2;
using Float3 = System.Numerics.Vector3;

namespace Pixagen.Game.Features.ResourceFeature.Meshes;

internal sealed class MeshResourceStore
{
    private readonly object _sync = new();
    private readonly Dictionary<string, MeshAsset> _cache = new(StringComparer.OrdinalIgnoreCase);
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

    public MeshAsset Load(string asset)
    {
        return LoadTracked(asset).Resource;
    }

    public ResourceLoadResult<MeshAsset> LoadTracked(string asset)
    {
        string id = ResourcePathResolver.NormalizeAssetId(asset, ".obj");
        Task<ResourceLoadResult<MeshAsset>>? loading = null;
        lock (_sync)
        {
            if (_cache.TryGetValue(id, out MeshAsset? mesh))
            {
                return new ResourceLoadResult<MeshAsset>(mesh, false);
            }

            if (_loading.TryGetValue(id, out LoadOperation? operation))
            {
                loading = operation.LoadTask;
            }
        }

        if (loading is not null)
        {
            return new ResourceLoadResult<MeshAsset>(loading.GetAwaiter().GetResult().Resource, false);
        }

        MeshAsset loaded = LoadObj(id);
        lock (_sync)
        {
            if (_cache.TryGetValue(id, out MeshAsset? mesh))
            {
                return new ResourceLoadResult<MeshAsset>(mesh, false);
            }

            _cache[id] = loaded;
            return new ResourceLoadResult<MeshAsset>(loaded, true);
        }
    }

    public ValueTask<MeshAsset> LoadAsync(string asset, CancellationToken cancellationToken = default)
    {
        ValueTask<ResourceLoadResult<MeshAsset>> loadTask = LoadTrackedAsync(asset, cancellationToken);
        if (loadTask.IsCompletedSuccessfully)
        {
            return new ValueTask<MeshAsset>(loadTask.GetAwaiter().GetResult().Resource);
        }

        return CompleteLoadAsync(loadTask);
    }

    public ValueTask<ResourceLoadResult<MeshAsset>> LoadTrackedAsync(
        string asset,
        CancellationToken cancellationToken = default)
    {
        string id = ResourcePathResolver.NormalizeAssetId(asset, ".obj");
        lock (_sync)
        {
            if (_cache.TryGetValue(id, out MeshAsset? mesh))
            {
                return new ValueTask<ResourceLoadResult<MeshAsset>>(new ResourceLoadResult<MeshAsset>(mesh, false));
            }

            if (_loading.TryGetValue(id, out LoadOperation? operation))
            {
                return AwaitExistingLoadAsync(operation.LoadTask);
            }

            var newOperation = new LoadOperation(_version);
            newOperation.LoadTask = LoadAndCacheAsync(id, newOperation, cancellationToken);
            _loading[id] = newOperation;
            return new ValueTask<ResourceLoadResult<MeshAsset>>(newOperation.LoadTask);
        }
    }

    private async Task<ResourceLoadResult<MeshAsset>> LoadAndCacheAsync(
        string id,
        LoadOperation operation,
        CancellationToken cancellationToken)
    {
        try
        {
            MeshAsset loaded = await Task.Run(() => LoadObj(id), cancellationToken).ConfigureAwait(false);
            lock (_sync)
            {
                if (_loading.TryGetValue(id, out LoadOperation? currentOperation) &&
                    ReferenceEquals(currentOperation, operation))
                {
                    _loading.Remove(id);
                }

                if (operation.Version != _version)
                {
                    return new ResourceLoadResult<MeshAsset>(loaded, false);
                }

                if (_cache.TryGetValue(id, out MeshAsset? mesh))
                {
                    return new ResourceLoadResult<MeshAsset>(mesh, false);
                }

                _cache[id] = loaded;
                return new ResourceLoadResult<MeshAsset>(loaded, true);
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

    private static async ValueTask<MeshAsset> CompleteLoadAsync(
        ValueTask<ResourceLoadResult<MeshAsset>> loadTask)
    {
        ResourceLoadResult<MeshAsset> result = await loadTask.ConfigureAwait(false);
        return result.Resource;
    }

    private static async ValueTask<ResourceLoadResult<MeshAsset>> AwaitExistingLoadAsync(
        Task<ResourceLoadResult<MeshAsset>> loadTask)
    {
        ResourceLoadResult<MeshAsset> result = await loadTask.ConfigureAwait(false);
        return new ResourceLoadResult<MeshAsset>(result.Resource, false);
    }

    public bool IsLoaded(string asset)
    {
        string id = ResourcePathResolver.NormalizeAssetId(asset, ".obj");
        lock (_sync)
        {
            return _cache.ContainsKey(id);
        }
    }

    public bool Unload(string asset)
    {
        string id = ResourcePathResolver.NormalizeAssetId(asset, ".obj");
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

    private static MeshAsset LoadObj(string id)
    {
        string path = ResourcePathResolver.ResolveMeshPath(id);
        var vertices = new List<Float3>();
        var texCoords = new List<Float2>();
        var triangles = new List<MeshTriangle>();

        foreach (string rawLine in File.ReadLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
            {
                continue;
            }

            if (parts[0] == "v" && parts.Length >= 4)
            {
                vertices.Add(new Float3(ParseFloat(parts[1]), ParseFloat(parts[2]), ParseFloat(parts[3])));
                continue;
            }

            if (parts[0] == "vt" && parts.Length >= 3)
            {
                texCoords.Add(new Float2(ParseFloat(parts[1]), ParseFloat(parts[2])));
                continue;
            }

            if (parts[0] == "f" && parts.Length >= 4)
            {
                var face = new MeshFaceVertex[parts.Length - 1];
                for (int i = 1; i < parts.Length; i++)
                {
                    face[i - 1] = ParseFaceVertex(parts[i], vertices, texCoords);
                }

                for (int i = 2; i < face.Length; i++)
                {
                    AddTriangle(face[0], face[i - 1], face[i], vertices, triangles);
                }
            }
        }

        if (vertices.Count == 0 || triangles.Count == 0)
        {
            throw new InvalidOperationException($"Mesh '{id}' does not contain vertices and triangles.");
        }

        return new MeshAsset(id, vertices.ToArray(), triangles.ToArray());
    }

    private static float ParseFloat(string value)
    {
        return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static MeshFaceVertex ParseFaceVertex(string token, IReadOnlyList<Float3> vertices, IReadOnlyList<Float2> texCoords)
    {
        string[] parts = token.Split('/');
        int vertexIndex = ParseIndex(parts[0], vertices.Count, "vertex", token);
        bool hasUv = parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]);
        Float2 uv = hasUv
            ? texCoords[ParseIndex(parts[1], texCoords.Count, "texture coordinate", token)]
            : Float2.Zero;

        return new MeshFaceVertex(vertexIndex, uv, hasUv);
    }

    private static int ParseIndex(string token, int count, string kind, string sourceToken)
    {
        int index = int.Parse(token, NumberStyles.Integer, CultureInfo.InvariantCulture);
        if (index < 0)
        {
            index = count + index + 1;
        }

        index--;
        if ((uint)index >= (uint)count)
        {
            throw new InvalidOperationException($"OBJ face references missing {kind} index '{sourceToken}'.");
        }

        return index;
    }

    private static void AddTriangle(
        MeshFaceVertex a,
        MeshFaceVertex b,
        MeshFaceVertex c,
        IReadOnlyList<Float3> vertices,
        List<MeshTriangle> triangles)
    {
        Float2 uvA = a.Uv;
        Float2 uvB = b.Uv;
        Float2 uvC = c.Uv;

        if (!a.HasUv || !b.HasUv || !c.HasUv)
        {
            (uvA, uvB, uvC) = GenerateFallbackUvs(vertices[a.Vertex], vertices[b.Vertex], vertices[c.Vertex]);
        }

        triangles.Add(new MeshTriangle(a.Vertex, b.Vertex, c.Vertex, uvA, uvB, uvC));
    }

    private static (Float2 A, Float2 B, Float2 C) GenerateFallbackUvs(Float3 a, Float3 b, Float3 c)
    {
        Float3 normal = Float3.Cross(b - a, c - a);
        Float3 abs = Float3.Abs(normal);

        if (abs.Y >= abs.X && abs.Y >= abs.Z)
        {
            return (ProjectXz(a), ProjectXz(b), ProjectXz(c));
        }

        if (abs.X >= abs.Z)
        {
            return (ProjectZy(a), ProjectZy(b), ProjectZy(c));
        }

        return (ProjectXy(a), ProjectXy(b), ProjectXy(c));
    }

    private static Float2 ProjectXz(Float3 vertex)
    {
        return new Float2(vertex.X + 0.5f, vertex.Z + 0.5f);
    }

    private static Float2 ProjectZy(Float3 vertex)
    {
        return new Float2(vertex.Z + 0.5f, vertex.Y + 0.5f);
    }

    private static Float2 ProjectXy(Float3 vertex)
    {
        return new Float2(vertex.X + 0.5f, vertex.Y + 0.5f);
    }

    private readonly record struct MeshFaceVertex(int Vertex, Float2 Uv, bool HasUv);

    private sealed class LoadOperation
    {
        public LoadOperation(int version)
        {
            Version = version;
        }

        public int Version { get; }
        public Task<ResourceLoadResult<MeshAsset>> LoadTask { get; set; } = null!;
    }
}
