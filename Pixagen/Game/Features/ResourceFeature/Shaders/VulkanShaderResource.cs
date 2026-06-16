using Veldrid;

namespace Pixagen.Game.Features.ResourceFeature.Shaders;

public sealed class VulkanShaderResource
{
    internal VulkanShaderResource(Shader[] compositeShaders, Shader raycastShader)
    {
        CompositeShaders = compositeShaders;
        RaycastShader = raycastShader;
    }

    public Shader[] CompositeShaders { get; }
    public Shader RaycastShader { get; }
}
