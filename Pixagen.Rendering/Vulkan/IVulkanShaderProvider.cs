using Veldrid;

namespace Pixagen.Rendering.Vulkan;

public interface IVulkanShaderProvider
{
    VulkanShaderResource LoadVulkanShaders(ResourceFactory factory);
    void UnloadVulkanShaders();
}
