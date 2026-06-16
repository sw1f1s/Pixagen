namespace Pixagen.Game.Features.UIFeature.Components;

public struct ProfilerUI : IComponent
{
    public Fix UpdateInterval;
    public Fix Elapsed;

    public ProfilerUI(Fix updateInterval)
    {
        UpdateInterval = updateInterval;
        Elapsed = Fix.Zero;
    }
}
