using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.RenderFeature.Raycasting;
using Pixagen.Game.Features.ResourceFeature.Runtime;
using Pixagen.Game.Features.SharedFeature.Components;
using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;

namespace Pixagen.Game.Features.RenderFeature.Systems;

public sealed class StaticRenderCacheSystem : IInitSystem, IUpdateSystem
{
    private readonly CustomInject<RenderSceneCache> _sceneCache = default;
    private readonly CustomInject<ResourceManager> _resources = default;
    private readonly FilterInject<Include<Transform, Mesh, IsStaticRender>> _meshes = default;
    private readonly FilterInject<Include<Transform, Mesh, IsStaticRender, ShadowCaster>> _shadowMeshes = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<Mesh> _meshComponents = default;
    private readonly ComponentInject<Material> _materials = default;

    public void Init()
    {
        Rebuild();
    }

    public void Update()
    {
        if (_sceneCache.Value.StaticDirty)
        {
            Rebuild();
        }
    }

    private void Rebuild()
    {
        RenderPrimitiveBatch primitives = _sceneCache.Value.Static;
        primitives.Clear();

        foreach (Entity entity in _meshes.Value)
        {
            ref Transform transform = ref _transforms.Get(entity);
            ref Mesh mesh = ref _meshComponents.Get(entity);
            RenderPrimitiveFactory.AddMeshTriangles(transform, mesh, ResolveMaterial(entity), _resources.Value, primitives.Triangles);
        }

        foreach (Entity entity in _shadowMeshes.Value)
        {
            ref Transform transform = ref _transforms.Get(entity);
            ref Mesh mesh = ref _meshComponents.Get(entity);
            RenderPrimitiveFactory.AddShadowMeshTriangles(transform, mesh, ResolveMaterial(entity), _resources.Value, primitives.ShadowTriangles);
        }

        primitives.RefreshShadowState();
        _sceneCache.Value.MarkStaticClean();
    }

    private SurfaceMaterial ResolveMaterial(Entity entity)
    {
        return _materials.Has(entity)
            ? RenderPrimitiveFactory.ResolveMaterial(_materials.Get(entity), _resources.Value)
            : SurfaceMaterial.Default;
    }
}
