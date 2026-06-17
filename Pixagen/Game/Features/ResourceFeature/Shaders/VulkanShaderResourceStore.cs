using Pixagen.Game.Features.ResourceFeature.Runtime;
using Pixagen.Rendering.Vulkan;
using Veldrid;
using Veldrid.SPIRV;

namespace Pixagen.Game.Features.ResourceFeature.Shaders;

internal sealed class VulkanShaderResourceStore
{
    private const string CompositeVertexShader = "composite.vert.glsl";
    private const string CompositeFragmentShader = "composite.frag.glsl";
    private const string RaycastComputeShader = "raycast.comp.glsl";

    private readonly object _sync = new();
    private ResourceFactory? _factory;
    private VulkanShaderResource? _resource;

    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _resource is null ? 0 : 3;
            }
        }
    }

    public VulkanShaderResource Load(ResourceFactory factory)
    {
        lock (_sync)
        {
            if (_resource is not null && ReferenceEquals(_factory, factory))
            {
                return _resource;
            }
        }

        VulkanShaderResource loaded = Compile(factory);
        lock (_sync)
        {
            if (_resource is not null && ReferenceEquals(_factory, factory))
            {
                DisposeResource(loaded);
                return _resource;
            }

            UnloadLocked();
            _factory = factory;
            _resource = loaded;
            return loaded;
        }
    }

    public bool Unload()
    {
        lock (_sync)
        {
            return UnloadLocked();
        }
    }

    public void Clear()
    {
        Unload();
    }

    private static VulkanShaderResource Compile(ResourceFactory factory)
    {
        Shader[] compositeShaders = [];
        Shader? raycastShader = null;
        try
        {
            compositeShaders = factory.CreateFromSpirv(
                CreateDescription(ShaderStages.Vertex, CompositeVertexShader),
                CreateDescription(ShaderStages.Fragment, CompositeFragmentShader));
            raycastShader = factory.CreateFromSpirv(
                CreateDescription(ShaderStages.Compute, RaycastComputeShader));

            return new VulkanShaderResource(compositeShaders, raycastShader);
        }
        catch (Exception exception)
        {
            DisposeShaders(compositeShaders);
            raycastShader?.Dispose();
            throw new InvalidOperationException("Failed to compile Vulkan shaders.", exception);
        }
    }

    private static ShaderDescription CreateDescription(ShaderStages stage, string fileName)
    {
        return new ShaderDescription(
            stage,
            File.ReadAllBytes(ResourcePathResolver.ResolveVulkanShaderPath(fileName)),
            "main");
    }

    private bool UnloadLocked()
    {
        if (_resource is null)
        {
            _factory = null;
            return false;
        }

        DisposeResource(_resource);
        _resource = null;
        _factory = null;
        return true;
    }

    private static void DisposeResource(VulkanShaderResource resource)
    {
        DisposeShaders(resource.CompositeShaders);
        resource.RaycastShader.Dispose();
    }

    private static void DisposeShaders(Shader[] shaders)
    {
        foreach (Shader shader in shaders)
        {
            shader.Dispose();
        }
    }
}
