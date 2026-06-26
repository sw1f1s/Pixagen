using Avalonia;
using Pixagen.Editor.Avalonia;
using Pixagen.Editor.Preview;

NativeRuntimeEnvironment.Configure();

if (EditorPreviewApp.ShouldRun(args))
{
    using var preview = EditorPreviewApp.Create(args);
    preview.Run();
    return;
}

BuildAvaloniaApp()
    .StartWithClassicDesktopLifetime(args);

static AppBuilder BuildAvaloniaApp()
{
    return AppBuilder
        .Configure<EditorAvaloniaApplication>()
        .UsePlatformDetect();
}
