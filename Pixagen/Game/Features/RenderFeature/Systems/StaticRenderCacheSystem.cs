using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.RenderFeature.Raycasting;
using Pixagen.Game.Features.ResourceFeature.Runtime;
using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.RenderFeature.Systems;

public sealed class StaticRenderCacheSystem : IInitSystem, IUpdateSystem
{
    private readonly CustomInject<RenderSceneCache> _sceneCache = default;
    private readonly CustomInject<RenderAssetResolver> _assets = default;
    private readonly CustomInject<ResourceManager> _resources = default;
    private readonly FilterInject<Include<Transform, Mesh, IsStaticRender>> _meshes = default;
    private readonly FilterInject<Include<Transform, Mesh, IsStaticRender, ShadowCaster>> _shadowMeshes = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<Mesh> _meshComponents = default;
    private readonly ComponentInject<Material> _materials = default;

    public void Init()
    {
        SyncResourceCaches();
        Rebuild();
    }

    public void Update()
    {
        SyncResourceCaches();
        if (_sceneCache.Value.StaticDirty)
        {
            Rebuild();
        }
    }

    private void Rebuild()
    {
        RenderPrimitiveBatch primitives = _sceneCache.Value.Static;
        RenderAssetResolver assets = _assets.Value;
        ResourceManager resources = _resources.Value;
        primitives.Clear();

        foreach (Entity entity in _meshes.Value)
        {
            ref Transform transform = ref _transforms.Get(entity);
            ref Mesh mesh = ref _meshComponents.Get(entity);
            RenderPrimitiveFactory.AddMeshTriangles(
                transform,
                assets.ResolveMesh(mesh, resources),
                ResolveMaterial(entity, resources, assets),
                primitives.Triangles);
        }

        foreach (Entity entity in _shadowMeshes.Value)
        {
            ref Transform transform = ref _transforms.Get(entity);
            ref Mesh mesh = ref _meshComponents.Get(entity);
            RenderPrimitiveFactory.AddShadowMeshTriangles(
                transform,
                assets.ResolveMesh(mesh, resources),
                ResolveMaterial(entity, resources, assets),
                primitives.ShadowTriangles);
        }

        primitives.RefreshShadowState();
        _sceneCache.Value.MarkStaticClean();
    }

    private void SyncResourceCaches()
    {
        if (_assets.Value.Sync(_resources.Value))
        {
            _sceneCache.Value.Clear();
        }
    }

    private SurfaceMaterial ResolveMaterial(
        Entity entity,
        ResourceManager resources,
        RenderAssetResolver assets)
    {
        return _materials.Has(entity)
            ? assets.ResolveMaterial(_materials.Get(entity), resources)
            : SurfaceMaterial.Default;
    }
}
