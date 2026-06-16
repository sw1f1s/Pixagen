namespace Pixagen.Core.Runtime;

internal static class NativeRuntimeEnvironment
{
    public static void Configure()
    {
#if MACOS
        SetDefaultEnvironmentValue("MVK_CONFIG_LOG_LEVEL", "0");
        SetDefaultEnvironmentValue("MVK_CONFIG_DEBUG", "0");
#endif
    }

    private static void SetDefaultEnvironmentValue(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
        {
            Environment.SetEnvironmentVariable(name, value);
#if MACOS
            SetNativeEnvironmentValue(name, value, overwrite: false);
#endif
        }
    }

#if MACOS
    private static void SetNativeEnvironmentValue(string name, string value, bool overwrite)
    {
        setenv(name, value, overwrite ? 1 : 0);
    }

    [System.Runtime.InteropServices.DllImport("libSystem.dylib", EntryPoint = "setenv")]
    private static extern int setenv(string name, string value, int overwrite);
#endif
}
