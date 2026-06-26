using System.Diagnostics;
using Pixagen.Editor.Workspace;

namespace Pixagen.Editor.Hosting;

public sealed class GameProcessHost : EditorProcessHost
{
    public bool Play(EditorWorkspace workspace)
    {
        string scenePath = workspace.SavePlaySceneSnapshot();
        return Restart(CreateGameStartInfo(scenePath), scenePath);
    }

    private static ProcessStartInfo CreateGameStartInfo(string scenePath)
    {
        string repositoryRoot = RepositoryRootResolver.Resolve();
        string gameArgs = $"--scene {Quote(scenePath)} --window-size 1280x720 --show-cursor";

        string projectPath = Path.Combine(repositoryRoot, "Pixagen", "Pixagen.csproj");
        return new ProcessStartInfo("dotnet", $"run --no-restore --project {Quote(projectPath)} -- {gameArgs}");
    }
}
