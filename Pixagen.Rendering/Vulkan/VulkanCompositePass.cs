using Veldrid;

namespace Pixagen.Rendering.Vulkan;

internal sealed class VulkanCompositePass : IDisposable
{
    private ResourceLayout? _resourceLayout;
    private ResourceSet? _resourceSet;
    private Pipeline? _pipeline;

    public void EnsurePipeline(
        GraphicsDevice graphicsDevice,
        Func<ResourceFactory, VulkanShaderResource> loadShaders)
    {
        if (_pipeline is not null)
        {
            return;
        }

        ResourceFactory factory = graphicsDevice.ResourceFactory;
        VulkanShaderResource shaders = loadShaders(factory);

        _resourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("SceneTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("SceneSampler", ResourceKind.Sampler, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("OverlayTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("OverlaySampler", ResourceKind.Sampler, ShaderStages.Fragment)));

        GraphicsPipelineDescription pipelineDescription = new(
            BlendStateDescription.SingleDisabled,
            DepthStencilStateDescription.Disabled,
            RasterizerStateDescription.CullNone,
            PrimitiveTopology.TriangleList,
            new ShaderSetDescription([], shaders.CompositeShaders),
            [_resourceLayout],
            graphicsDevice.SwapchainFramebuffer.OutputDescription,
            ResourceBindingModel.Improved);

        _pipeline = factory.CreateGraphicsPipeline(ref pipelineDescription);
    }

    public void EnsureResourceSet(
        GraphicsDevice graphicsDevice,
        TextureView sceneTextureView,
        TextureView overlayTextureView)
    {
        if (_resourceSet is not null)
        {
            return;
        }

        _resourceSet = graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
            _resourceLayout,
            sceneTextureView,
            graphicsDevice.PointSampler,
            overlayTextureView,
            graphicsDevice.PointSampler));
    }

    public void Draw(GraphicsDevice graphicsDevice, CommandList commandList)
    {
        commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
        commandList.ClearColorTarget(0, RgbaFloat.Black);
        commandList.SetPipeline(_pipeline);
        commandList.SetGraphicsResourceSet(0, _resourceSet);
        commandList.Draw(3);
    }

    public void InvalidateResourceSet()
    {
        _resourceSet?.Dispose();
        _resourceSet = null;
    }

    public void Dispose()
    {
        _pipeline?.Dispose();
        _resourceSet?.Dispose();
        _resourceLayout?.Dispose();
    }
}
