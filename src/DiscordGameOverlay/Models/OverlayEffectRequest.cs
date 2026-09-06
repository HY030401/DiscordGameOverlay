namespace DiscordGameOverlay.Models
{
    public enum OverlayEffectDirection
    {
        LeftToRight,
        RightToLeft
    }

    public sealed record OverlayEffectInstance(
        double NormalizedX,
        OverlayEffectDirection Direction,
        int RandomSeed,
        int DelayMilliseconds);

    public sealed record OverlayEffectRequest(
        OverlayEffectType Type,
        int TriggerCount,
        int IntensityLevel,
        IReadOnlyList<OverlayEffectInstance> Instances);
}
