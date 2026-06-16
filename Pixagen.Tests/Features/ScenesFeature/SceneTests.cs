using Pixagen.Game.Features.FPSCharacterFeature.Components;
using Pixagen.Game.Features.PhysicsFeature.Components;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.RenderFeature.Raycasting;
using Pixagen.Game.Features.ResourceFeature.Runtime;
using Pixagen.Game.Features.ScenesFeature;
using Pixagen.Game.Features.ScenesFeature.Components;
using Pixagen.Game.Features.ScenesFeature.Serialization;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Game.Features.SharedFeature.Systems;
using Pixagen.Core.App;
using Pixagen.Core.Input;
using Pixagen.Core.Performance;
using Pixagen.Core.Runtime;
using Pixagen.Core.Timing;
using Pixagen.Ecs.Runtime;
using Pixagen.Tests.TestSupport;
using Pixagen.Rendering;
using static Pixagen.Tests.TestSupport.EcsTestAccess;

namespace Pixagen.Tests.Features.ScenesFeature;

public sealed class SceneTests
{
    [Fact]
    public void DefaultSceneFactory_CreatesFPSCharacterWithCameraChild()
    {
        SceneDefinition scene = DefaultSceneFactory.Create();

        SceneObjectDefinition character = scene.Objects.Single(obj => GetInfo(obj).Id == "fps-character");
        Assert.Equal("FPS Character", GetInfo(character).Name);
        Assert.Contains(character.Components, component => component is FPSCharacter);
        Assert.Contains(character.Components, component => component is RigidBody);
        Assert.Contains(character.Components, component => component is Collider);

        SceneObjectDefinition camera = Assert.Single(character.Children);
        Assert.Equal("fps-character-camera", GetInfo(camera).Id);
        Assert.Contains(camera.Components, component => component is Camera);
        Assert.Contains(camera.Components, component => component is FPSCharacterCamera);
        Assert.Contains(camera.Components, component => component is LocalTransform);
    }

    [Fact]
    public void SceneManager_AddScene_CreatesEntitiesWithSceneObjectAndHierarchy()
    {
        using var context = new EcsTestContext();
        SceneManager manager = CreateSceneManager(context);
        SceneDefinition scene = CreateSimpleScene();

        LoadedScene loaded = manager.AddScene(scene);

        Assert.Equal("scene-a", loaded.Id);
        Assert.Equal(2, loaded.Entities.Count);
        Entity root = loaded.Entities[0];
        Entity child = loaded.Entities[1];
        Assert.True(Access(root).Has<SceneObject>());
        Assert.True(Access(child).Has<SceneObject>());
        Assert.True(Access(root).Has<SpawnOneTick>());
        Assert.True(Access(child).Has<SpawnOneTick>());
        Assert.Equal("root", Access(root).Get<Info>().Id);
        Assert.Equal("Root", Access(root).Get<Info>().Name);
        Assert.Equal("child", Access(child).Get<Info>().Id);
        Assert.Equal("Child", Access(child).Get<Info>().Name);
        Assert.Equal(root, Access(child).Get<Parent>().Entity);
        Assert.True(Access(root).Get<Children>().Entities.Contains(child));
    }

    [Fact]
    public void SceneManager_AddScene_RejectsDuplicateSceneIds()
    {
        using var context = new EcsTestContext();
        SceneManager manager = CreateSceneManager(context);
        SceneDefinition scene = CreateSimpleScene();
        manager.AddScene(scene);

        Assert.Throws<InvalidOperationException>(() => manager.AddScene(CreateSimpleScene()));
    }

    [Fact]
    public void SceneManager_RemoveScene_MarksLoadedEntitiesForDestroy()
    {
        using var context = new EcsTestContext();
        SceneManager manager = CreateSceneManager(context);
        LoadedScene loaded = manager.AddScene(CreateSimpleScene());

        Assert.True(manager.RemoveScene("scene-a"));

        Assert.All(loaded.Entities, entity => Assert.True(Access(entity).Has<DestroyOneTick>()));
        Assert.Empty(manager.LoadedScenes);
    }

    [Fact]
    public void SceneManager_RemoveScene_ReturnsFalseForMissingScene()
    {
        using var context = new EcsTestContext();
        SceneManager manager = CreateSceneManager(context);

        Assert.False(manager.RemoveScene("missing"));
    }

    [Fact]
    public void RemovedSceneEntities_AreDestroyedByDestroySystem()
    {
        using var context = new EcsTestContext();
        SceneManager manager = CreateSceneManager(context);
        LoadedScene loaded = manager.AddScene(CreateSimpleScene());
        manager.RemoveScene(loaded);

        var systems = context.BuildSystems(new DestroySystem());
        systems.Update();

        Assert.All(loaded.Entities, AssertEx.Dead);
    }

    [Fact]
    public void ResourceManager_RoundTripsSceneComponentTypesAndHierarchy()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            "console-engine-tests",
            $"{Guid.NewGuid():N}.scene.json");
        using var resources = new ResourceManager();
        SceneDefinition scene = CreateSimpleScene();
        scene.Objects[0].Components.Add(new FPSCharacter(Fix.One, Fix.One));
        scene.Objects[0].Children[0].Components.Add(new FPSCharacterCamera(Fix.Zero));

        resources.SaveScene(path, scene);
        SceneDefinition loaded = resources.LoadScene(path);

        Assert.Equal(scene.Id, loaded.Id);
        SceneObjectDefinition root = Assert.Single(loaded.Objects);
        Assert.Equal("root", GetInfo(root).Id);
        Assert.Contains(root.Components, component => component is Transform);
        Assert.Contains(root.Components, component => component is FPSCharacter);
        SceneObjectDefinition child = Assert.Single(root.Children);
        Assert.Equal("child", GetInfo(child).Id);
        Assert.Contains(child.Components, component => component is LocalTransform);
        Assert.Contains(child.Components, component => component is FPSCharacterCamera);
    }

    [Fact]
    public void StartupSceneSystem_AddsInjectedSceneToWorld()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            "console-engine-tests",
            $"{Guid.NewGuid():N}.scene.json");
        using (var resources = new ResourceManager())
        {
            resources.SaveScene(path, CreateSimpleScene());
        }

        var group = new Pixagen.Game.Features.ScenesFeature.ScenesFeatureSystemsGroup();
        SceneManager manager = group.Injects.OfType<SceneManager>().Single();
        using var context = new EcsTestContext(new EngineOptions { ScenePath = path });
        context.BuildSystems(group);

        Assert.Single(manager.LoadedScenes);
        Assert.Equal(2, manager.LoadedScenes[0].Entities.Count);
        Assert.All(manager.LoadedScenes[0].Entities, entity => Assert.True(Access(entity).Has<SceneObject>()));
        Assert.All(manager.LoadedScenes[0].Entities, entity => Assert.True(Access(entity).Has<Info>()));
    }

    [Fact]
    public async Task SceneManager_SwitchSceneAsync_UnloadsPreviousSceneResources()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            "console-engine-tests",
            $"{Guid.NewGuid():N}.scene.json");
        using var context = new EcsTestContext();
        SceneManager manager = CreateSceneManager(context);
        LoadedScene firstScene = manager.AddScene(CreateRenderScene("scene-a", "cube", "checker"));
        context.Resources.SaveScene(path, CreateRenderScene("scene-b", "sphere", null));

        Assert.True(context.Resources.IsMeshLoaded("cube"));
        Assert.True(context.Resources.IsTextureLoaded("checker"));

        LoadedScene secondScene = await manager.SwitchSceneAsync(path);

        Assert.Equal("scene-b", secondScene.Id);
        Assert.Single(manager.LoadedScenes);
        Assert.All(firstScene.Entities, entity => Assert.True(Access(entity).Has<DestroyOneTick>()));
        Assert.False(context.Resources.IsMeshLoaded("cube"));
        Assert.False(context.Resources.IsTextureLoaded("checker"));
        Assert.True(context.Resources.IsMeshLoaded("sphere"));
    }

    private static SceneManager CreateSceneManager(EcsTestContext context)
    {
        SceneEntityFactory factory = context.Inject(new SceneEntityFactory());
        return context.Inject(new SceneManager(), factory);
    }

    private static SceneDefinition CreateSimpleScene()
    {
        return new SceneDefinition
        {
            Id = "scene-a",
            Name = "Scene A",
            Objects =
            [
                new SceneObjectDefinition
                {
                    Components =
                    [
                        new Info("root", "Root"),
                        new Transform(new Vector3(new Fix(1), new Fix(2), new Fix(3))),
                        new Velocity()
                    ],
                    Children =
                    [
                        new SceneObjectDefinition
                        {
                            Components =
                            [
                                new Info("child", "Child"),
                                new Transform(new Vector3(new Fix(1), new Fix(3), new Fix(3))),
                                new LocalTransform(new Vector3(Fix.Zero, Fix.One, Fix.Zero))
                            ]
                        }
                    ]
                }
            ]
        };
    }

    private static SceneDefinition CreateRenderScene(string id, string mesh, string? texture)
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
                        new Info(id + "-root", id),
                        new Transform(Vector3.Zero),
                        new Mesh(mesh),
                        material
                    ]
                }
            ]
        };
    }

    private static Info GetInfo(SceneObjectDefinition definition)
    {
        return definition.Components.OfType<Info>().Single();
    }
}
