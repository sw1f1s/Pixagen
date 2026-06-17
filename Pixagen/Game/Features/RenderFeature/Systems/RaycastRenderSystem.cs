using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.RenderFeature.Raycasting;
using Pixagen.Game.Features.ResourceFeature.Runtime;
using Pixagen.Rendering;
using Pixagen.Game.Features.SharedFeature.Helper;
using Pixagen.Ecs.DI;
using Float3 = System.Numerics.Vector3;

namespace Pixagen.Game.Features.RenderFeature.Systems;

public sealed class RaycastRenderSystem : IUpdateSystem
{
    private readonly CustomInject<FrameBuffer> _frameBuffer = default;
    private readonly CustomInject<IRaycastComputeRenderer> _computeRenderer = default;
    private readonly CustomInject<ResourceManager> _resources = default;
    private readonly CustomInject<RenderSceneCache> _sceneCache = default;
    private readonly CustomInject<RenderAssetResolver> _assets = default;
    private readonly CustomInject<RenderSettings> _settings = default;
    private readonly CustomInject<PerformanceStats> _performanceStats = default;
    private readonly FilterInject<Include<Transform, Camera>> _cameras = default;
    private readonly FilterInject<Include<Transform, LightDirection>> _lights = default;
    private readonly FilterInject<Include<Transform, Mesh>, Exclude<IsStaticRender>> _meshes = default;
    private readonly FilterInject<Include<Transform, Mesh, ShadowCaster>, Exclude<IsStaticRender>> _shadowMeshes = default;
    private readonly CustomInject<EntityStateHelper> _entityState = default;
    private readonly ComponentInject<Transform> _transforms = default;
    private readonly ComponentInject<Mesh> _meshComponents = default;
    private readonly ComponentInject<Material> _materials = default;
    private readonly ComponentInject<Camera> _cameraComponents = default;
    private readonly ComponentInject<LightDirection> _lightDirections = default;
    private readonly RenderFrustumCuller _frustumCuller = new();
    private readonly RenderPrimitiveBatch _visibleStaticPrimitives = new();
    private readonly RenderPrimitiveBatch _visibleDynamicPrimitives = new();
    private readonly RaycastTileBinBuilder _tileBinBuilder = new();
    private readonly RaycastShadowBinBuilder _shadowBinBuilder = new();

    public void Update()
    {
        FrameBuffer buffer = _frameBuffer.Value;
        SyncResourceCaches();

        Entity cameraEntity = GetCamera();
        if (cameraEntity == Entity.Empty)
        {
            buffer.Clear(FrameCell.Transparent);
            RecordRenderStats(RenderPrimitiveBatch.Empty, RenderPrimitiveBatch.Empty, ShadowQuality.Off);
            return;
        }

        BuildDynamicCache();

        RenderSettings settings = _settings.Value;
        RenderResolution internalResolution = ResolveInternalResolution(settings, buffer.Width, buffer.Height);
        ref Transform transform = ref _transforms.Get(cameraEntity);
        ref Camera camera = ref _cameraComponents.Get(cameraEntity);
        CameraBasis basis = CameraBasis.FromRotation(transform.Rotation);
        DirectionalLight light = GetLight();
        float maxDistance = ResolveDrawDistance(settings, camera);
        float shadowRenderDistance = ResolveShadowRenderDistance(settings);
        RenderViewFrustum frustum = RenderViewFrustum.Create(
            internalResolution.Width,
            internalResolution.Height,
            transform.Position,
            basis,
            camera,
            maxDistance);
        Float3 cameraPosition = RenderMath.ToFloat(transform.Position);
        _frustumCuller.Cull(_sceneCache.Value.Static, _visibleStaticPrimitives, frustum, cameraPosition, shadowRenderDistance);
        _frustumCuller.Cull(_sceneCache.Value.Dynamic, _visibleDynamicPrimitives, frustum, cameraPosition, shadowRenderDistance);

        RenderPrimitiveBatch staticPrimitives = _visibleStaticPrimitives;
        RenderPrimitiveBatch dynamicPrimitives = _visibleDynamicPrimitives;
        ShadowQuality shadowQuality = light.CastsShadows ? settings.ShadowQuality : ShadowQuality.Off;
        float shadowSoftness = ResolveShadowSoftness(settings);
        RenderResolution binnedResolution = internalResolution;
        RaycastTileBins tileBins = _tileBinBuilder.Build(
            binnedResolution,
            transform.Position,
            basis,
            camera,
            staticPrimitives,
            dynamicPrimitives);
        RaycastShadowBins shadowBins = _shadowBinBuilder.Build(
            binnedResolution,
            light,
            staticPrimitives,
            dynamicPrimitives);
        RaycastWorkloadBudgetResult budget = RaycastWorkloadBudget.Limit(
            internalResolution,
            shadowQuality,
            tileBins.EstimatedPrimaryTriangleTests,
            EstimateShadowTriangleTests(shadowBins, shadowSoftness));
        internalResolution = budget.Resolution;
        shadowQuality = budget.ShadowQuality;
        RayBuilder rayBuilder = RayBuilderFactory.Create(
            internalResolution.Width,
            internalResolution.Height,
            transform.Position,
            basis,
            camera);
        if (internalResolution != binnedResolution)
        {
            tileBins = _tileBinBuilder.Build(
                internalResolution,
                transform.Position,
                basis,
                camera,
                staticPrimitives,
                dynamicPrimitives);
            shadowBins = _shadowBinBuilder.Build(
                internalResolution,
                light,
                staticPrimitives,
                dynamicPrimitives);
        }

        RecordRenderStats(staticPrimitives, dynamicPrimitives, shadowQuality);
        if (!TryRenderWithCompute(
                buffer,
                internalResolution,
                maxDistance,
                shadowQuality,
                shadowSoftness,
                rayBuilder,
                light,
                tileBins,
                shadowBins,
                staticPrimitives,
                dynamicPrimitives))
        {
            buffer.Clear(FrameCell.Transparent);
        }
    }

    private void RecordRenderStats(
        RenderPrimitiveBatch staticPrimitives,
        RenderPrimitiveBatch dynamicPrimitives,
        ShadowQuality shadowQuality)
    {
        ResourceStats resourceStats = _resources.Value.GetStats();
        int shadowTriangles = shadowQuality == ShadowQuality.Off
            ? 0
            : staticPrimitives.ShadowTriangles.Count + dynamicPrimitives.ShadowTriangles.Count;

        _performanceStats.Value.RecordRenderScene(new RenderPerformanceReport(
            staticPrimitives.Triangles.Count + dynamicPrimitives.Triangles.Count,
            shadowTriangles,
            resourceStats.TextureCount,
            resourceStats.TextureBytes));
    }

    private bool TryRenderWithCompute(
        FrameBuffer buffer,
        RenderResolution internalResolution,
        float maxDistance,
        ShadowQuality shadowQuality,
        float shadowSoftness,
        RayBuilder rayBuilder,
        DirectionalLight light,
        RaycastTileBins tileBins,
        RaycastShadowBins shadowBins,
        RenderPrimitiveBatch staticPrimitives,
        RenderPrimitiveBatch dynamicPrimitives)
    {
        var request = new RaycastComputeRequest(
            internalResolution.Width,
            internalResolution.Height,
            maxDistance,
            shadowQuality,
            shadowSoftness,
            rayBuilder,
            light,
            tileBins,
            shadowBins,
            staticPrimitives,
            dynamicPrimitives);

        if (!_computeRenderer.Value.TryRenderRaycast(request))
        {
            return false;
        }

        buffer.Clear(FrameCell.Transparent);
        return true;
    }

    private void BuildDynamicCache()
    {
        RenderPrimitiveBatch primitives = _sceneCache.Value.Dynamic;
        RenderAssetResolver assets = _assets.Value;
        ResourceManager resources = _resources.Value;
        primitives.Clear();

        foreach (Entity entity in _meshes.Value)
        {
            if (!_entityState.Value.IsEnabled(entity))
            {
                continue;
            }

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
            if (!_entityState.Value.IsEnabled(entity))
            {
                continue;
            }

            ref Transform transform = ref _transforms.Get(entity);
            ref Mesh mesh = ref _meshComponents.Get(entity);
            RenderPrimitiveFactory.AddShadowMeshTriangles(
                transform,
                assets.ResolveMesh(mesh, resources),
                ResolveMaterial(entity, resources, assets),
                primitives.ShadowTriangles);
        }

        primitives.RefreshShadowState();
    }

    private DirectionalLight GetLight()
    {
        Entity lightEntity = GetLightEntity();
        if (lightEntity == Entity.Empty)
        {
            return DirectionalLight.Default;
        }

        ref Transform transform = ref _transforms.Get(lightEntity);
        ref LightDirection light = ref _lightDirections.Get(lightEntity);
        return new DirectionalLight(
            RenderMath.ToFloat(transform.Rotation.Normalized.Rotate(Vector3.Forward).Normalized),
            MathF.Max(RenderMath.ToFloat(light.Intensity), 0f),
            Math.Clamp(RenderMath.ToFloat(light.AmbientIntensity), 0f, 1f),
            Math.Clamp(RenderMath.ToFloat(light.ShadowIntensity), 0f, 1f),
            MathF.Max(RenderMath.ToFloat(light.ShadowBias), RenderMath.Epsilon),
            MathF.Max(RenderMath.ToFloat(light.ShadowMaxDistance), 1f));
    }

    private Entity GetCamera()
    {
        foreach (Entity entity in _cameras.Value)
        {
            if (_entityState.Value.IsEnabled(entity))
            {
                return entity;
            }
        }

        return Entity.Empty;
    }

    private Entity GetLightEntity()
    {
        foreach (Entity entity in _lights.Value)
        {
            if (_entityState.Value.IsEnabled(entity))
            {
                return entity;
            }
        }

        return Entity.Empty;
    }

    private static float ResolveDrawDistance(RenderSettings settings, Camera camera)
    {
        Fix distance = settings.DrawDistance > Fix.Epsilon
            ? settings.DrawDistance
            : camera.MaxDistance;

        return MathF.Max(RenderMath.ToFloat(distance), RenderMath.Epsilon);
    }

    private static float ResolveShadowRenderDistance(RenderSettings settings)
    {
        Fix distance = settings.ShadowRenderDistance > Fix.Epsilon
            ? settings.ShadowRenderDistance
            : settings.DrawDistance;

        return MathF.Max(RenderMath.ToFloat(distance), RenderMath.Epsilon);
    }

    private static float ResolveShadowSoftness(RenderSettings settings)
    {
        return Math.Clamp(RenderMath.ToFloat(settings.ShadowSoftness), 0f, 0.25f);
    }

    private static double EstimateShadowTriangleTests(RaycastShadowBins shadowBins, float shadowSoftness)
    {
        return shadowBins.EstimatedShadowTriangleTests * ResolveFullShadowSampleCount(shadowSoftness);
    }

    private static int ResolveFullShadowSampleCount(float shadowSoftness)
    {
        return shadowSoftness > RenderMath.Epsilon ? 4 : 1;
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

    private static RenderResolution ResolveInternalResolution(RenderSettings settings, int outputWidth, int outputHeight)
    {
        outputWidth = Math.Max(1, outputWidth);
        outputHeight = Math.Max(1, outputHeight);
        int maxWidth = Math.Max(1, settings.MaxInternalResolution.Width);
        int maxHeight = Math.Max(1, settings.MaxInternalResolution.Height);

        return settings.RenderScaleMode switch
        {
            RenderScaleMode.Native => new RenderResolution(outputWidth, outputHeight),
            RenderScaleMode.Fixed => new RenderResolution(maxWidth, maxHeight),
            _ => FitToMaxResolution(outputWidth, outputHeight, maxWidth, maxHeight)
        };
    }

    private static RenderResolution FitToMaxResolution(int outputWidth, int outputHeight, int maxWidth, int maxHeight)
    {
        if (outputWidth <= maxWidth && outputHeight <= maxHeight)
        {
            return new RenderResolution(outputWidth, outputHeight);
        }

        float scale = MathF.Min((float)maxWidth / outputWidth, (float)maxHeight / outputHeight);
        return new RenderResolution(
            Math.Max(1, (int)MathF.Round(outputWidth * scale)),
            Math.Max(1, (int)MathF.Round(outputHeight * scale)));
    }

}
