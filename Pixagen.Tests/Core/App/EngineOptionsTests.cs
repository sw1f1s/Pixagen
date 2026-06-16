using Pixagen.Core.App;

namespace Pixagen.Tests.Core.App;

public sealed class EngineOptionsTests
{
    [Fact]
    public void FromArgs_LeavesOptionalPathsEmptyByDefault()
    {
        EngineOptions options = EngineOptions.FromArgs([]);

        Assert.Null(options.ScenePath);
        Assert.Null(options.SaveDefaultScenePath);
    }

    [Fact]
    public void FromArgs_ReadsExplicitScenePath()
    {
        EngineOptions options = EngineOptions.FromArgs(["--scene", "Content/Scenes/default.scene.json"]);

        Assert.Equal("Content/Scenes/default.scene.json", options.ScenePath);
    }
}
