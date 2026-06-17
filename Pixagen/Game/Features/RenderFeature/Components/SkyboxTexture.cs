namespace Pixagen.Game.Features.RenderFeature.Components;

public struct SkyboxTexture : IComponent
{
    public string Asset;

    public SkyboxTexture(string asset)
    {
        Asset = asset;
    }
}
