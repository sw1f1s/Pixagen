using System.Text.Json;

namespace Pixagen.Editor.Preview;

public sealed class PreviewOverlayState
{
    public string SelectedName { get; set; } = "None";
    public string SelectionKind { get; set; } = "None";
    public string Transform { get; set; } = string.Empty;
    public string ContentRoot { get; set; } = string.Empty;

    public static PreviewOverlayState Empty { get; } = new();

    public static PreviewOverlayState Load(string path)
    {
        if (!File.Exists(path))
        {
            return Empty;
        }

        try
        {
            using FileStream stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<PreviewOverlayState>(stream) ?? Empty;
        }
        catch
        {
            return Empty;
        }
    }

    public void Save(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (FileStream stream = File.Create(tempPath))
            {
                JsonSerializer.Serialize(stream, this);
            }

            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
