using DiscordGameOverlay.Models;

namespace DiscordGameOverlay.Services
{
    public sealed class EffectTriggerCoordinator : IDisposable
    {
        private static readonly TimeSpan DefaultCollectionWindow =
            TimeSpan.FromSeconds(3);
        private static readonly TimeSpan DefaultCooldown =
            TimeSpan.FromSeconds(20);

        private readonly object stateLock = new();
        private readonly Dictionary<OverlayEffectType, TriggerState> states = new();
        private readonly CancellationTokenSource shutdown = new();
        private readonly TimeSpan collectionWindow;
        private readonly TimeSpan cooldown;

        public event Action<OverlayEffectRequest>? EffectReady;

        public EffectTriggerCoordinator(
            TimeSpan? collectionWindow = null,
            TimeSpan? cooldown = null)
        {
            this.collectionWindow =
                collectionWindow ?? DefaultCollectionWindow;
            this.cooldown = cooldown ?? DefaultCooldown;
        }

        public void Register(OverlayEffectType effect)
        {
            int generation;

            lock (stateLock)
            {
                if (!states.TryGetValue(effect, out TriggerState? state))
                {
                    state = new TriggerState();
                    states[effect] = state;
                }

                if (state.IsCollecting)
                {
                    state.TriggerCount++;
                    return;
                }

                if (DateTimeOffset.UtcNow < state.CooldownUntil)
                    return;

                state.IsCollecting = true;
                state.TriggerCount = 1;
                generation = ++state.Generation;
            }

            _ = CompleteCollectionAsync(effect, generation, shutdown.Token);
        }

        private async Task CompleteCollectionAsync(
            OverlayEffectType effect,
            int generation,
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(collectionWindow, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            int triggerCount;

            lock (stateLock)
            {
                if (!states.TryGetValue(effect, out TriggerState? state) ||
                    !state.IsCollecting ||
                    state.Generation != generation)
                {
                    return;
                }

                triggerCount = state.TriggerCount;
                state.IsCollecting = false;
                state.TriggerCount = 0;
                state.CooldownUntil = DateTimeOffset.UtcNow + cooldown;
            }

            EffectReady?.Invoke(CreateRequest(effect, triggerCount));
        }

        private static OverlayEffectRequest CreateRequest(
            OverlayEffectType effect,
            int triggerCount)
        {
            int intensityLevel = triggerCount > 10
                ? 3
                : triggerCount > 5
                    ? 2
                    : 1;

            int instanceCount = intensityLevel switch
            {
                3 => 5,
                2 => 3,
                _ => 1
            };

            int batchSeed = Random.Shared.Next();
            var batchRandom = new Random(batchSeed);
            var instances = new List<OverlayEffectInstance>(instanceCount);

            for (int index = 0; index < instanceCount; index++)
            {
                instances.Add(new OverlayEffectInstance(
                    batchRandom.NextDouble() * 0.76 + 0.12,
                    batchRandom.Next(2) == 0
                        ? OverlayEffectDirection.LeftToRight
                        : OverlayEffectDirection.RightToLeft,
                    batchRandom.Next(),
                    index * 140));
            }

            return new OverlayEffectRequest(
                effect,
                triggerCount,
                intensityLevel,
                instances);
        }

        public void Dispose()
        {
            shutdown.Cancel();
            shutdown.Dispose();
        }

        private sealed class TriggerState
        {
            public bool IsCollecting { get; set; }
            public int TriggerCount { get; set; }
            public int Generation { get; set; }
            public DateTimeOffset CooldownUntil { get; set; }
        }
    }
}
