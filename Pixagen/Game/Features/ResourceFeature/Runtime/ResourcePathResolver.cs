namespace Pixagen.Game.Features.ResourceFeature.Runtime;

internal static class ResourcePathResolver
{
    private const string MeshRoot = "Content/Meshes";
    private const string TextureRoot = "Content/Textures";
    private const string SceneRoot = "Content/Scenes";
    private const string VulkanShaderRoot = "Content/Shaders/Vulkan";
    private const string DefaultSceneName = "default.scene.json";

    public static string NormalizeAssetId(string asset, string extension)
    {
        if (string.IsNullOrWhiteSpace(asset))
        {
            throw new InvalidOperationException("Resource asset name is empty.");
        }

        string id = asset
            .Replace('\\', '/')
            .Trim()
            .TrimStart('/');

        return id.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
            ? id
            : id + extension;
    }

    public static string ResolveMeshPath(string id)
    {
        return ResolveContentPath(MeshRoot, id, "Mesh");
    }

    public static string ResolveTexturePath(string id)
    {
        return ResolveContentPath(TextureRoot, id, "Texture");
    }

    public static string ResolveDefaultScenePath()
    {
        return Path.Combine(AppContext.BaseDirectory, SceneRoot, DefaultSceneName);
    }

    public static string ResolveVulkanShaderPath(string fileName)
    {
        return ResolveContentPath(VulkanShaderRoot, fileName, "Vulkan shader");
    }

    public static string NormalizeScenePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("Scene path is empty.");
        }

        string trimmed = path.Trim();
        if (Path.IsPathRooted(trimmed))
        {
            return Path.GetFullPath(trimmed);
        }

        string currentDirectoryPath = Path.GetFullPath(trimmed);
        if (File.Exists(currentDirectoryPath))
        {
            return currentDirectoryPath;
        }

        string appDirectoryPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, trimmed));
        string normalized = trimmed.Replace('\\', '/');
        return normalized.StartsWith("Content/", StringComparison.OrdinalIgnoreCase)
            ? appDirectoryPath
            : currentDirectoryPath;
    }

    private static string ResolveContentPath(string root, string id, string kind)
    {
        string path = Path.Combine(AppContext.BaseDirectory, root, id);
        if (File.Exists(path))
        {
            return path;
        }

        throw new FileNotFoundException($"{kind} asset '{id}' was not found. Expected path: {path}", path);
    }
}
