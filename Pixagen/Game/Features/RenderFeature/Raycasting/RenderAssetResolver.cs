using Pixagen.Ecs.DI;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.ResourceFeature.Runtime;

namespace Pixagen.Game.Features.RenderFeature.Raycasting;

public sealed class RenderAssetResolver : IDisposeInject
{
    private readonly Dictionary<string, MeshAsset> _meshes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TextureAsset> _textures = new(StringComparer.Ordinal);
    private int _resourceRevision = -1;

    public bool Sync(ResourceManager resources)
    {
        int revision = resources.Revision;
        if (_resourceRevision == revision)
        {
            return false;
        }

        ClearCaches();
        _resourceRevision = revision;
        return true;
    }

    public MeshAsset ResolveMesh(in Mesh mesh, ResourceManager resources)
    {
        string asset = mesh.Asset ?? string.Empty;
        if (_meshes.TryGetValue(asset, out MeshAsset? resolved))
        {
            return resolved;
        }

        string id = ResourcePathResolver.NormalizeAssetId(asset, ".obj");
        resolved = resources.GetMesh(id);
        _meshes.Add(asset, resolved);
        return resolved;
    }

    public SurfaceMaterial ResolveMaterial(in Material material, ResourceManager resources)
    {
        TextureAsset? texture = null;
        if (material.Texture is { } textureComponent && !string.IsNullOrWhiteSpace(textureComponent.Asset))
        {
            texture = ResolveTexture(textureComponent.Asset, resources);
        }

        return RenderPrimitiveFactory.ResolveMaterial(material, texture);
    }

    public void Clear()
    {
        ClearCaches();
        _resourceRevision = -1;
    }

    public void DisposeInject()
    {
        Clear();
    }

    public TextureAsset ResolveTexture(string asset, ResourceManager resources)
    {
        if (_textures.TryGetValue(asset, out TextureAsset? texture))
        {
            return texture;
        }

        string id = ResourcePathResolver.NormalizeAssetId(asset, ".ppm");
        texture = resources.GetTexture(id);
        _textures.Add(asset, texture);
        return texture;
    }

    private void ClearCaches()
    {
        _meshes.Clear();
        _textures.Clear();
    }
}
