using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace Pixagen.Rendering.Vulkan;

internal sealed class VulkanShaderLibrary : IDisposable
{
    private const string ShaderRoot = "Content/Shaders/Vulkan";
    private const string CompositeVertexShader = "composite.vert.glsl";
    private const string CompositeFragmentShader = "composite.frag.glsl";
    private const string RaycastComputeShader = "raycast.comp.glsl";

    private bool _loaded;

    public Shader[] CompositeShaders { get; private set; } = [];
    public Shader? RaycastShader { get; private set; }

    public void Load(ResourceFactory factory)
    {
        if (_loaded)
        {
            return;
        }

        string compositeVertexSource = LoadSource(CompositeVertexShader);
        string compositeFragmentSource = LoadSource(CompositeFragmentShader);
        string raycastComputeSource = LoadSource(RaycastComputeShader);

        try
        {
            CompositeShaders = factory.CreateFromSpirv(
                new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(compositeVertexSource), "main"),
                new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(compositeFragmentSource), "main"));

            RaycastShader = factory.CreateFromSpirv(
                new ShaderDescription(ShaderStages.Compute, Encoding.UTF8.GetBytes(raycastComputeSource), "main"));
        }
        catch (Exception exception)
        {
            Unload();
            throw new InvalidOperationException("Failed to compile Vulkan shaders.", exception);
        }

        _loaded = true;
    }

    public void Unload()
    {
        foreach (Shader shader in CompositeShaders)
        {
            shader.Dispose();
        }

        CompositeShaders = [];
        RaycastShader?.Dispose();
        RaycastShader = null;
        _loaded = false;
    }

    public void Dispose()
    {
        Unload();
    }

    private static string LoadSource(string fileName)
    {
        string shaderPath = ResolveShaderPath(fileName);
        return File.ReadAllText(shaderPath, Encoding.UTF8);
    }

    private static string ResolveShaderPath(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, ShaderRoot, fileName);
        if (File.Exists(path))
        {
            return path;
        }

        throw new FileNotFoundException(
            $"Vulkan shader file '{fileName}' was not found. Expected path: {path}",
            path);
    }
}
