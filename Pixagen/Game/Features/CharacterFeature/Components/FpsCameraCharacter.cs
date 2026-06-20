
namespace Pixagen.Game.Features.CharacterFeature.Components;

public struct FpsCameraCharacter : IComponent
{
    public static readonly Fix DefaultMaxPitch = Fix.Pi * new Fix(85) / new Fix(180);
    public static readonly Fix DefaultMinPitch = Fix.Zero - DefaultMaxPitch;

    public Fix Pitch;
    public Fix MinPitch;
    public Fix MaxPitch;

    public FpsCameraCharacter(Fix pitch)
        : this(pitch, DefaultMinPitch, DefaultMaxPitch)
    {
    }

    public FpsCameraCharacter(Fix pitch, Fix minPitch, Fix maxPitch)
    {
        Pitch = pitch;
        MinPitch = minPitch;
        MaxPitch = maxPitch;

        EnsurePitchLimits();
    }

    public void EnsurePitchLimits()
    {
        if (MinPitch == Fix.Zero && MaxPitch == Fix.Zero)
        {
            MinPitch = DefaultMinPitch;
            MaxPitch = DefaultMaxPitch;
        }

        if (MinPitch > MaxPitch)
        {
            (MinPitch, MaxPitch) = (MaxPitch, MinPitch);
        }

        Pitch = Clamp(Pitch, MinPitch, MaxPitch);
    }

    public static Fix Clamp(Fix value, Fix min, Fix max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }
}
