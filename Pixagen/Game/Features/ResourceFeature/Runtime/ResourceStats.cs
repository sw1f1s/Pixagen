namespace Pixagen.Game.Features.ResourceFeature.Runtime;

public readonly record struct ResourceStats(
    int MeshCount,
    int TextureCount,
    long TextureBytes,
    int SceneCount);
