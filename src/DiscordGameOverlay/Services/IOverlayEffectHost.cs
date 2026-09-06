using DiscordGameOverlay.Models;

namespace DiscordGameOverlay.Services
{
    public interface IOverlayEffectHost
    {
        void PlayEffect(OverlayEffectRequest request);
    }
}
