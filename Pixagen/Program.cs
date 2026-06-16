using Pixagen.Core.App;
using Pixagen.Core.Input;
using Pixagen.Core.Performance;
using Pixagen.Core.Runtime;
using Pixagen.Core.Timing;
using Pixagen.Game.Features.ResourceFeature.Runtime;

NativeRuntimeEnvironment.Configure();

EngineOptions options = EngineOptions.FromArgs(args);
if (!string.IsNullOrWhiteSpace(options.SaveDefaultScenePath))
{
    using var resources = new ResourceManager();
    resources.SaveDefaultScene(Path.GetFullPath(options.SaveDefaultScenePath));
}

using EngineApp app = EngineApp.CreateDefault(options);
app.Run();
