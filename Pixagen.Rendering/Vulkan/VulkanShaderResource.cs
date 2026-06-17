using Veldrid;

namespace Pixagen.Rendering.Vulkan;

public sealed class VulkanShaderResource
{
    public VulkanShaderResource(Shader[] compositeShaders, Shader raycastShader)
    {
        CompositeShaders = compositeShaders;
        RaycastShader = raycastShader;
    }

    public Shader[] CompositeShaders { get; }
    public Shader RaycastShader { get; }
}
