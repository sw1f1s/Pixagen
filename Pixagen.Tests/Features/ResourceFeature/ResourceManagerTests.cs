using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.ResourceFeature.Runtime;
using Pixagen.Game.Features.ScenesFeature.Serialization;
using Pixagen.Rendering;

namespace Pixagen.Tests.Features.ResourceFeature;

public sealed class ResourceManagerTests
{
    [Fact]
    public void ResourceManager_LoadsAndUnloadsMesh()
    {
        using var resources = new ResourceManager();

        var first = resources.LoadMesh("cube");
        var second = resources.LoadMesh("cube.obj");

        Assert.Same(first, second);
        Assert.True(resources.IsMeshLoaded("cube"));
        Assert.Equal(1, resources.GetStats().MeshCount);

        Assert.True(resources.UnloadMesh("cube.obj"));
        Assert.False(resources.IsMeshLoaded("cube"));

        var third = resources.LoadMesh("cube");
        Assert.NotSame(first, third);
    }

    [Fact]
    public void ResourceManager_LoadsAndUnloadsTextureAndUpdatesStats()
    {
        using var resources = new ResourceManager();

        var first = resources.LoadTexture("checker");
        var second = resources.LoadTexture("checker.ppm");
        ResourceStats loadedStats = resources.GetStats();

        Assert.Same(first, second);
        Assert.True(loadedStats.TextureCount > 0);
        Assert.True(loadedStats.TextureBytes > 0);

        Assert.True(resources.UnloadTexture("checker"));
        ResourceStats unloadedStats = resources.GetStats();

        Assert.Equal(0, unloadedStats.TextureCount);
        Assert.Equal(0, unloadedStats.TextureBytes);
    }

    [Fact]
    public void ResourceManager_LoadsSkyboxTextureAsset()
    {
        using var resources = new ResourceManager();

        var texture = resources.LoadTexture("sky_clouds");

        Assert.Equal("sky_clouds.ppm", texture.Id);
        Assert.Equal(512, texture.Width);
        Assert.Equal(256, texture.Height);
        Assert.True(texture.MipCount > 1);
    }

    [Fact]
    public void ResourceManager_LoadsSceneFromCacheUntilUnloaded()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            "console-engine-tests",
            $"{Guid.NewGuid():N}.scene.json");
        using var resources = new ResourceManager();
        var scene = new SceneDefinition
        {
            Id = "resource-scene",
            Name = "Resource Scene"
        };

        resources.SaveScene(path, scene);
        SceneDefinition first = resources.LoadScene(path);
        SceneDefinition second = resources.LoadScene(path);

        Assert.Same(first, second);
        Assert.True(resources.IsSceneLoaded(path));
        Assert.Equal(1, resources.GetStats().SceneCount);

        Assert.True(resources.UnloadScene(path));
        Assert.False(resources.IsSceneLoaded(path));
        SceneDefinition third = resources.LoadScene(path);

        Assert.NotSame(first, third);
        Assert.Equal("resource-scene", third.Id);
    }

    [Fact]
    public void ResourceManager_LoadsDefaultSceneResource()
    {
        using var resources = new ResourceManager();

        SceneDefinition scene = resources.LoadDefaultScene();

        Assert.Equal("default", scene.Id);
        Assert.True(scene.Objects.Count > 0);
        Assert.Equal(1, resources.GetStats().SceneCount);
    }

    [Fact]
    public void ResourceManager_LoadStartupSceneWithResources_UsesDefaultSceneFileWhenPathIsMissing()
    {
        using var resources = new ResourceManager();
        string defaultPath = resources.ResolveStartupScenePath(null) ??
            throw new InvalidOperationException("Default scene file is expected to exist in test output.");

        SceneResourceScope scope = resources.LoadStartupSceneWithResources(null);

        Assert.Equal("default", scope.SceneId);
        Assert.True(resources.IsSceneLoaded(defaultPath));
    }

    [Fact]
    public void ResourceManager_LoadStartupSceneWithResources_ResolvesContentScenePath()
    {
        using var resources = new ResourceManager();

        SceneResourceScope scope = resources.LoadStartupSceneWithResources("Content/Scenes/default.scene.json");

        Assert.Equal("default", scope.SceneId);
        Assert.True(resources.IsMeshLoaded("cube"));
        Assert.True(resources.IsTextureLoaded("checker"));
        Assert.True(resources.IsTextureLoaded("sky_clouds"));
    }

    [Fact]
    public async Task ResourceManager_LoadAsync_ReturnsCompletedValueTaskForCachedResources()
    {
        using var resources = new ResourceManager();

        var mesh = await resources.LoadMeshAsync("cube");
        var texture = await resources.LoadTextureAsync("checker");
        SceneDefinition scene = await resources.LoadDefaultSceneAsync();

        var cachedMesh = resources.LoadMeshAsync("cube.obj");
        var cachedTexture = resources.LoadTextureAsync("checker.ppm");
        var cachedScene = resources.LoadDefaultSceneAsync();

        Assert.True(cachedMesh.IsCompletedSuccessfully);
        Assert.True(cachedTexture.IsCompletedSuccessfully);
        Assert.True(cachedScene.IsCompletedSuccessfully);
        Assert.Same(mesh, cachedMesh.GetAwaiter().GetResult());
        Assert.Same(texture, cachedTexture.GetAwaiter().GetResult());
        Assert.Same(scene, cachedScene.GetAwaiter().GetResult());
    }

    [Fact]
    public async Task ResourceManager_LoadSceneResourcesAsync_LoadsAndUnloadsReferencedAssets()
    {
        using var resources = new ResourceManager();
        SceneDefinition scene = CreateSceneWithResources("scene-assets", "cube", "checker");

        SceneResourceScope scope = await resources.LoadSceneResourcesAsync(scene);

        Assert.Equal("scene-assets", scope.SceneId);
        Assert.Contains("cube.obj", scope.MeshAssets);
        Assert.Contains("checker.ppm", scope.TextureAssets);
        Assert.True(resources.IsMeshLoaded("cube"));
        Assert.True(resources.IsTextureLoaded("checker"));

        Assert.True(resources.UnloadSceneResources(scope));
        Assert.False(resources.IsMeshLoaded("cube"));
        Assert.False(resources.IsTextureLoaded("checker"));
        Assert.False(resources.UnloadSceneResources(scope));
    }

    [Fact]
    public async Task ResourceManager_LoadSceneResourcesAsync_LoadsAndUnloadsSkyboxTexture()
    {
        using var resources = new ResourceManager();
        var scene = new SceneDefinition
        {
            Id = "skybox-scene",
            Name = "Skybox Scene",
            Objects =
            [
                new SceneObjectDefinition
                {
                    Components =
                    [
                        new SkyboxTexture("sky_clouds")
                    ]
                }
            ]
        };

        SceneResourceScope scope = await resources.LoadSceneResourcesAsync(scene);

        Assert.Contains("sky_clouds.ppm", scope.TextureAssets);
        Assert.True(resources.IsTextureLoaded("sky_clouds"));

        Assert.True(resources.UnloadSceneResources(scope));
        Assert.False(resources.IsTextureLoaded("sky_clouds"));
    }

    [Fact]
    public async Task ResourceManager_UnloadSceneResources_KeepsSharedAssetsUntilLastSceneUnloads()
    {
        using var resources = new ResourceManager();
        SceneDefinition firstScene = CreateSceneWithResources("scene-a", "cube", "checker");
        SceneDefinition secondScene = CreateSceneWithResources("scene-b", "cube", null);

        SceneResourceScope firstScope = await resources.LoadSceneResourcesAsync(firstScene);
        SceneResourceScope secondScope = await resources.LoadSceneResourcesAsync(secondScene);

        Assert.True(resources.UnloadSceneResources(firstScope));
        Assert.True(resources.IsMeshLoaded("cube"));
        Assert.False(resources.IsTextureLoaded("checker"));

        Assert.True(resources.UnloadSceneResources(secondScope));
        Assert.False(resources.IsMeshLoaded("cube"));
    }

    [Fact]
    public async Task ResourceManager_UnloadSceneResources_UnloadsSceneDefinitionCache()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            "console-engine-tests",
            $"{Guid.NewGuid():N}.scene.json");
        using var resources = new ResourceManager();
        resources.SaveScene(path, CreateSceneWithResources("scene-file", "sphere", null));

        SceneResourceScope scope = await resources.LoadSceneWithResourcesAsync(path);

        Assert.True(resources.IsSceneLoaded(path));
        Assert.True(resources.IsMeshLoaded("sphere"));

        Assert.True(resources.UnloadSceneResources(scope));
        Assert.False(resources.IsSceneLoaded(path));
        Assert.False(resources.IsMeshLoaded("sphere"));
        Assert.Equal(0, resources.GetStats().SceneCount);
    }

    private static SceneDefinition CreateSceneWithResources(string id, string mesh, string? texture)
    {
        Material material = texture is null
            ? new Material(PixelColor.FromRgb(200, 200, 200))
            : new Material(PixelColor.FromRgb(200, 200, 200), new MaterialTexture(texture));

        return new SceneDefinition
        {
            Id = id,
            Name = id,
            Objects =
            [
                new SceneObjectDefinition
                {
                    Components =
                    [
                        new Mesh(mesh),
                        material
                    ]
                }
            ]
        };
    }
}
