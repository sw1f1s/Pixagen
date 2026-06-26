namespace Pixagen.Editor.Workspace;

public static class EditorContentPaths
{
    public static string ResolveContentRoot()
    {
        foreach (string candidate in EnumerateCandidates())
        {
            if (Directory.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        string fallback = Path.Combine(AppContext.BaseDirectory, "Content");
        Directory.CreateDirectory(fallback);
        return Path.GetFullPath(fallback);
    }

    private static IEnumerable<string> EnumerateCandidates()
    {
        string current = Environment.CurrentDirectory;
        yield return Path.Combine(current, "Pixagen", "Content");
        yield return Path.Combine(current, "Content");
        yield return Path.Combine(AppContext.BaseDirectory, "Content");

        DirectoryInfo? directory = new(current);
        while (directory is not null)
        {
            yield return Path.Combine(directory.FullName, "Pixagen", "Content");
            yield return Path.Combine(directory.FullName, "Content");
            directory = directory.Parent;
        }
    }
}
