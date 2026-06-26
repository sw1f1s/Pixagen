namespace Pixagen.Editor.Preview;

public sealed record EditorPreviewLaunchOptions(
    string ScenePath,
    string OverlayPath,
    bool RunSingleFrame)
{
    public static EditorPreviewLaunchOptions Parse(string[] args)
    {
        string? scenePath = null;
        string overlayPath = Path.Combine(Path.GetTempPath(), "pixagen-editor-preview-overlay.json");
        bool runSingleFrame = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (string.Equals(arg, "--single-frame", StringComparison.OrdinalIgnoreCase))
            {
                runSingleFrame = true;
                continue;
            }

            if (TryReadValue(args, ref i, "--scene", out string? scene))
            {
                scenePath = scene;
                continue;
            }

            if (TryReadValue(args, ref i, "--overlay", out string? overlay))
            {
                overlayPath = overlay ?? overlayPath;
            }
        }

        if (string.IsNullOrWhiteSpace(scenePath))
        {
            throw new InvalidOperationException("Pixagen.Editor scene preview requires --scene <path>.");
        }

        return new EditorPreviewLaunchOptions(
            Path.GetFullPath(scenePath),
            Path.GetFullPath(overlayPath),
            runSingleFrame);
    }

    private static bool TryReadValue(string[] args, ref int index, string name, out string? value)
    {
        string arg = args[index];
        value = null;
        if (arg.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
        {
            value = arg[(name.Length + 1)..];
            return true;
        }

        if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
        {
            value = args[++index];
            return true;
        }

        return false;
    }
}
