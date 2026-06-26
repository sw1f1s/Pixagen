using Avalonia.Controls.ApplicationLifetimes;
using Pixagen.Editor.Workspace;

namespace Pixagen.Editor.Avalonia;

public sealed class EditorAvaloniaApplication : global::Avalonia.Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            string[] args = desktop.Args ?? [];
            EditorWorkspace workspace = EditorWorkspace.Load(FilterEditorArgs(args));
            desktop.MainWindow = new EditorMainWindow(workspace);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static string[] FilterEditorArgs(IEnumerable<string> args)
    {
        return args
            .Where(arg =>
                !string.Equals(arg, "--single-frame", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(arg, "--scene-preview", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
