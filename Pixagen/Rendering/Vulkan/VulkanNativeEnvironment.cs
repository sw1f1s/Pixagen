using Veldrid.Sdl2;

namespace Pixagen.Rendering.Vulkan;

internal static class VulkanNativeEnvironment
{
    public static void Configure()
    {
#if MACOS
        Environment.SetEnvironmentVariable(
            "MVK_CONFIG_LOG_LEVEL",
            Environment.GetEnvironmentVariable("MVK_CONFIG_LOG_LEVEL") ?? "0");

        string icdPath = Path.Combine(AppContext.BaseDirectory, "MoltenVK_icd.json");
        if (!File.Exists(icdPath))
        {
            return;
        }

        Environment.SetEnvironmentVariable("VK_ICD_FILENAMES", icdPath);
        Environment.SetEnvironmentVariable("VK_DRIVER_FILES", icdPath);
#endif
    }

    public static void ApplyWindowMode(Sdl2Window window, RenderBackendOptions options)
    {
        if (!options.Fullscreen)
        {
            return;
        }

        Sdl2Native.SDL_SetWindowBordered(window.SdlWindowHandle, 0u);
    }

    public static bool IsVulkanLoaderFailure(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is DllNotFoundException)
            {
                return true;
            }

            if (current.Message.Contains("libvulkan", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("vulkan loader", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
