NativeRuntimeEnvironment.Configure();
EngineOptions options = EngineOptions.FromArgs(args);
using EngineApp app = EngineApp.CreateDefault(options);
app.Run();
