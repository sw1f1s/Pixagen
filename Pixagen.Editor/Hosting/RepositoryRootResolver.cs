namespace Pixagen.Editor.Hosting;

public static class RepositoryRootResolver
{
    public static string Resolve()
    {
        string current = Environment.CurrentDirectory;
        DirectoryInfo? directory = new(current);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Pixagen")) &&
                Directory.Exists(Path.Combine(directory.FullName, "Pixagen.Editor")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Pixagen")) &&
                Directory.Exists(Path.Combine(directory.FullName, "Pixagen.Editor")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Environment.CurrentDirectory;
    }
}
