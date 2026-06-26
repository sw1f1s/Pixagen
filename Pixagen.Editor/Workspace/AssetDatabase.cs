namespace Pixagen.Editor.Workspace;

public enum EditorAssetKind
{
    Scene,
    Mesh,
    Texture,
    Shader
}

public sealed record EditorAssetEntry(
    string Name,
    string RelativePath,
    string FullPath,
    EditorAssetKind Kind,
    long SizeBytes);

public sealed class AssetDatabase
{
    private readonly List<EditorAssetEntry> _assets = new();

    public AssetDatabase(string contentRoot)
    {
        ContentRoot = contentRoot;
        Refresh();
    }

    public string ContentRoot { get; }
    public IReadOnlyList<EditorAssetEntry> Assets => _assets;

    public void Refresh()
    {
        _assets.Clear();
        Scan("Scenes", "*.json", EditorAssetKind.Scene);
        Scan("Meshes", "*.obj", EditorAssetKind.Mesh);
        Scan("Textures", "*.ppm", EditorAssetKind.Texture);
        Scan(Path.Combine("Shaders", "Vulkan"), "*.glsl", EditorAssetKind.Shader);

        _assets.Sort((left, right) =>
        {
            int kind = left.Kind.CompareTo(right.Kind);
            return kind != 0 ? kind : string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        });
    }

    private void Scan(string relativeDirectory, string pattern, EditorAssetKind kind)
    {
        string directory = Path.Combine(ContentRoot, relativeDirectory);
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (string path in Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly))
        {
            var info = new FileInfo(path);
            _assets.Add(new EditorAssetEntry(
                info.Name,
                Path.GetRelativePath(ContentRoot, info.FullName).Replace('\\', '/'),
                info.FullName,
                kind,
                info.Length));
        }
    }
}
