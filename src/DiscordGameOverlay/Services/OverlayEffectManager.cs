using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using DiscordGameOverlay.Config;
using DiscordGameOverlay.Models;

namespace DiscordGameOverlay.Services
{
    public sealed class OverlayEffectManager
    {
        private const int MaxConcurrentEffects = 20;

        private readonly Canvas canvas;
        private readonly Dictionary<string, BitmapSource> imageCache = new();

        private int activeEffectCount;

        public OverlayEffectManager(Canvas canvas)
        {
            this.canvas = canvas;
        }

        public void Play(OverlayEffectRequest request)
        {
            if (!canvas.Dispatcher.CheckAccess())
            {
                canvas.Dispatcher.BeginInvoke(() => Play(request));
                return;
            }

            foreach (OverlayEffectInstance instance in request.Instances)
            {
                if (instance.DelayMilliseconds <= 0)
                {
                    PlaySingle(request.Type, instance);
                }
                else
                {
                    _ = PlayAfterDelayAsync(
                        request.Type,
                        instance,
                        TimeSpan.FromMilliseconds(
                            instance.DelayMilliseconds));
                }
            }
        }

        private async Task PlayAfterDelayAsync(
            OverlayEffectType effect,
            OverlayEffectInstance instance,
            TimeSpan delay)
        {
            await Task.Delay(delay);

            if (canvas.Dispatcher.HasShutdownStarted ||
                canvas.Dispatcher.HasShutdownFinished)
            {
                return;
            }

            await canvas.Dispatcher.InvokeAsync(
                () => PlaySingle(effect, instance));
        }

        private void PlaySingle(
            OverlayEffectType effect,
            OverlayEffectInstance instance)
        {
            if (canvas.ActualWidth <= 1 ||
                canvas.ActualHeight <= 1 ||
                activeEffectCount >= MaxConcurrentEffects)
            {
                return;
            }

            activeEffectCount++;

            try
            {
                var effectRandom = new Random(instance.RandomSeed);

                switch (effect)
                {
                    case OverlayEffectType.Poop:
                        PlayPoop(instance, effectRandom);
                        break;
                    case OverlayEffectType.PigeonPoop:
                        PlayPigeonPoop(instance, effectRandom);
                        break;
                    case OverlayEffectType.Heart:
                        PlayHeart(instance, effectRandom);
                        break;
                    case OverlayEffectType.Egg:
                        PlayEgg(instance, effectRandom);
                        break;
                }
            }
            catch (Exception ex)
            {
                activeEffectCount--;
                LogEffectFailure(effect, ex);
            }
        }

        private void PlayPoop(
            OverlayEffectInstance instance,
            Random effectRandom)
        {
            PlayThrownProjectile(
                OverlayEffectType.Poop,
                instance,
                effectRandom,
                "poop.png",
                "poop-splat.png",
                0.078,
                76,
                124,
                0.31,
                260,
                470,
                0.62);
        }

        private void PlayPigeonPoop(
            OverlayEffectInstance instance,
            Random effectRandom)
        {
            double size = Math.Clamp(canvas.ActualWidth * 0.085, 74, 132);
            double left = LeftFromNormalizedX(instance.NormalizedX, size);
            double impactTop = canvas.ActualHeight - size * 0.72;

            Image poop = CreateImage("pigeon-poop.png", size, size);
            var rotation = new RotateTransform(0);
            poop.RenderTransformOrigin = new Point(0.5, 0.5);
            poop.RenderTransform = rotation;

            Canvas.SetLeft(poop, left);
            Canvas.SetTop(poop, -size);
            canvas.Children.Add(poop);

            TimeSpan fallDuration = TimeSpan.FromSeconds(
                RandomBetween(effectRandom, 0.78, 1.08));

            var fall = new DoubleAnimation
            {
                From = -size,
                To = impactTop,
                Duration = fallDuration,
                AccelerationRatio = 0.68,
                FillBehavior = FillBehavior.HoldEnd
            };

            var spin = new DoubleAnimation
            {
                From = RandomBetween(effectRandom, -20, 20),
                To = RandomBetween(effectRandom, -540, 540),
                Duration = fallDuration,
                FillBehavior = FillBehavior.HoldEnd
            };

            fall.Completed += (_, _) =>
            {
                ContinueEffect(
                    OverlayEffectType.PigeonPoop,
                    () =>
                    {
                        canvas.Children.Remove(poop);
                        ShowPigeonPoopSplat(left + size / 2, size);
                    },
                    poop);
            };

            rotation.BeginAnimation(RotateTransform.AngleProperty, spin);
            poop.BeginAnimation(Canvas.TopProperty, fall);
        }

        private void ShowPigeonPoopSplat(double centerX, double sourceSize)
        {
            double width = sourceSize * 1.9;
            double height = width * 0.62;
            double left = Math.Clamp(
                centerX - width / 2,
                -width * 0.08,
                canvas.ActualWidth - width * 0.92);
            double top = canvas.ActualHeight - height * 0.82;

            Image splat = CreateImage(
                "pigeon-poop-splat.png",
                width,
                height);
            var scale = new ScaleTransform(0.76, 0.18);
            splat.RenderTransformOrigin = new Point(0.5, 1);
            splat.RenderTransform = scale;

            Canvas.SetLeft(splat, left);
            Canvas.SetTop(splat, top);
            canvas.Children.Add(splat);

            scale.BeginAnimation(
                ScaleTransform.ScaleXProperty,
                CreateImpactScale(0.76, 1));
            scale.BeginAnimation(
                ScaleTransform.ScaleYProperty,
                CreateImpactScale(0.18, 1));

            FadeAndRemove(splat, TimeSpan.FromSeconds(2.2));
        }

        private void PlayHeart(
            OverlayEffectInstance instance,
            Random effectRandom)
        {
            double size = Math.Clamp(canvas.ActualWidth * 0.075, 78, 126);
            double left = LeftFromNormalizedX(instance.NormalizedX, size);
            double targetTop = RandomBetween(
                effectRandom,
                canvas.ActualHeight * 0.34,
                canvas.ActualHeight * 0.54);

            Image heart = CreateImage("heart.png", size, size);
            var rotation = new RotateTransform(
                RandomBetween(effectRandom, -10, 10));
            heart.RenderTransformOrigin = new Point(0.5, 0.5);
            heart.RenderTransform = rotation;

            Canvas.SetLeft(heart, left);
            Canvas.SetTop(heart, -size);
            canvas.Children.Add(heart);

            var fall = new DoubleAnimation
            {
                From = -size,
                To = targetTop,
                Duration = TimeSpan.FromSeconds(0.95),
                EasingFunction = new QuadraticEase
                {
                    EasingMode = EasingMode.EaseIn
                },
                FillBehavior = FillBehavior.HoldEnd
            };

            heart.BeginAnimation(Canvas.TopProperty, fall);

            _ = FireArrowDuringFallAsync(
                heart,
                left,
                targetTop,
                size,
                instance.Direction == OverlayEffectDirection.LeftToRight);
        }

        private async Task FireArrowDuringFallAsync(
            Image heart,
            double heartLeft,
            double impactTop,
            double heartSize,
            bool fromLeft)
        {
            await Task.Delay(TimeSpan.FromSeconds(0.62));

            if (canvas.Dispatcher.HasShutdownStarted ||
                canvas.Dispatcher.HasShutdownFinished)
            {
                return;
            }

            await canvas.Dispatcher.InvokeAsync(() =>
            {
                if (!canvas.Children.Contains(heart))
                    return;

                ContinueEffect(
                    OverlayEffectType.Heart,
                    () => FireArrow(
                        heart,
                        heartLeft,
                        impactTop,
                        heartSize,
                        fromLeft),
                    heart);
            });
        }

        private void FireArrow(
            Image heart,
            double heartLeft,
            double heartTop,
            double heartSize,
            bool fromLeft)
        {
            double arrowWidth = heartSize * 2.35;
            double arrowHeight = heartSize * 0.58;
            double heartCenterX = heartLeft + heartSize / 2;

            Image arrow = CreateImage("arrow.png", arrowWidth, arrowHeight);
            if (!fromLeft)
            {
                arrow.RenderTransformOrigin = new Point(0.5, 0.5);
                arrow.RenderTransform = new ScaleTransform(-1, 1);
            }

            Canvas.SetLeft(
                arrow,
                fromLeft ? -arrowWidth : canvas.ActualWidth);
            Canvas.SetTop(
                arrow,
                heartTop + (heartSize - arrowHeight) / 2);
            Panel.SetZIndex(arrow, -1);
            canvas.Children.Add(arrow);

            double hitLeft = fromLeft
                ? heartCenterX - arrowWidth * 0.72
                : heartCenterX - arrowWidth * 0.28;

            var flight = new DoubleAnimation
            {
                To = hitLeft,
                Duration = TimeSpan.FromSeconds(0.33),
                EasingFunction = new QuadraticEase
                {
                    EasingMode = EasingMode.EaseIn
                },
                FillBehavior = FillBehavior.HoldEnd
            };

            flight.Completed += (_, _) =>
            {
                ContinueEffect(
                    OverlayEffectType.Heart,
                    () =>
                    {
                        canvas.Children.Remove(heart);
                        canvas.Children.Remove(arrow);
                        PinHeartToOppositeSide(
                            heartCenterX,
                            heartTop,
                            heartSize,
                            fromLeft);
                    },
                    heart,
                    arrow);
            };

            arrow.BeginAnimation(Canvas.LeftProperty, flight);
        }

        private void PinHeartToOppositeSide(
            double heartCenterX,
            double top,
            double heartSize,
            bool movingRight)
        {
            double width = heartSize * 2.35;
            double height = heartSize;

            var group = new Grid
            {
                Width = width,
                Height = height,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(
                    movingRight ? 3 : -3)
            };

            Image arrow = CreateImage("arrow.png", width, heartSize * 0.58);
            arrow.HorizontalAlignment = HorizontalAlignment.Center;
            arrow.VerticalAlignment = VerticalAlignment.Center;
            if (!movingRight)
            {
                arrow.RenderTransformOrigin = new Point(0.5, 0.5);
                arrow.RenderTransform = new ScaleTransform(-1, 1);
            }

            Image heart = CreateImage("heart.png", heartSize, heartSize);
            heart.HorizontalAlignment = HorizontalAlignment.Center;
            heart.VerticalAlignment = VerticalAlignment.Center;

            group.Children.Add(arrow);
            group.Children.Add(heart);

            double startLeft = heartCenterX - width / 2;
            double endLeft = movingRight
                ? canvas.ActualWidth - width * 0.66
                : -width * 0.34;

            Canvas.SetLeft(group, startLeft);
            Canvas.SetTop(group, top);
            canvas.Children.Add(group);

            var pin = new DoubleAnimation
            {
                From = startLeft,
                To = endLeft,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                },
                FillBehavior = FillBehavior.HoldEnd
            };

            group.BeginAnimation(Canvas.LeftProperty, pin);
            FadeAndRemove(group, TimeSpan.FromSeconds(2.5));
        }

        private void PlayEgg(
            OverlayEffectInstance instance,
            Random effectRandom)
        {
            PlayThrownProjectile(
                OverlayEffectType.Egg,
                instance,
                effectRandom,
                "egg.png",
                "egg-splat.png",
                0.075,
                72,
                118,
                0.34,
                280,
                510,
                0.91);
        }

        private void PlayThrownProjectile(
            OverlayEffectType effect,
            OverlayEffectInstance instance,
            Random effectRandom,
            string projectileFileName,
            string splatFileName,
            double projectileWidthFraction,
            double minimumProjectileSize,
            double maximumProjectileSize,
            double splatWidthFraction,
            double minimumSplatWidth,
            double maximumSplatWidth,
            double splatAspectRatio)
        {
            double size = Math.Clamp(
                canvas.ActualWidth * projectileWidthFraction,
                minimumProjectileSize,
                maximumProjectileSize);
            double startLeft = RandomBetween(
                effectRandom,
                canvas.ActualWidth * 0.2,
                canvas.ActualWidth * 0.8);
            double targetLeft = LeftFromNormalizedX(instance.NormalizedX, size);
            double targetTop = RandomBetween(
                effectRandom,
                canvas.ActualHeight * 0.22,
                canvas.ActualHeight * 0.5);

            Image egg = CreateImage(projectileFileName, size, size);
            var transforms = new TransformGroup();
            var scale = new ScaleTransform(0.18, 0.18);
            var rotation = new RotateTransform(0);
            transforms.Children.Add(scale);
            transforms.Children.Add(rotation);
            egg.RenderTransformOrigin = new Point(0.5, 0.5);
            egg.RenderTransform = transforms;

            Canvas.SetLeft(egg, startLeft);
            Canvas.SetTop(egg, canvas.ActualHeight + size);
            canvas.Children.Add(egg);

            TimeSpan duration = TimeSpan.FromSeconds(0.72);
            var easing = new QuadraticEase
            {
                EasingMode = EasingMode.EaseIn
            };

            egg.BeginAnimation(
                Canvas.LeftProperty,
                new DoubleAnimation(startLeft, targetLeft, duration)
                {
                    EasingFunction = easing,
                    FillBehavior = FillBehavior.HoldEnd
                });

            var rise = new DoubleAnimation
            {
                From = canvas.ActualHeight + size,
                To = targetTop,
                Duration = duration,
                EasingFunction = easing,
                FillBehavior = FillBehavior.HoldEnd
            };

            scale.BeginAnimation(
                ScaleTransform.ScaleXProperty,
                new DoubleAnimation(0.18, 2.7, duration)
                {
                    EasingFunction = easing,
                    FillBehavior = FillBehavior.HoldEnd
                });
            scale.BeginAnimation(
                ScaleTransform.ScaleYProperty,
                new DoubleAnimation(0.18, 2.7, duration)
                {
                    EasingFunction = easing,
                    FillBehavior = FillBehavior.HoldEnd
                });
            rotation.BeginAnimation(
                RotateTransform.AngleProperty,
                new DoubleAnimation(
                    0,
                    RandomBetween(effectRandom, -540, 540),
                    duration)
                {
                    FillBehavior = FillBehavior.HoldEnd
                });

            rise.Completed += (_, _) =>
            {
                ContinueEffect(
                    effect,
                    () =>
                    {
                        canvas.Children.Remove(egg);
                        ShowThrownSplat(
                            splatFileName,
                            targetLeft + size / 2,
                            targetTop + size / 2,
                            splatWidthFraction,
                            minimumSplatWidth,
                            maximumSplatWidth,
                            splatAspectRatio);
                    },
                    egg);
            };

            egg.BeginAnimation(Canvas.TopProperty, rise);
        }

        private void ShowThrownSplat(
            string splatFileName,
            double centerX,
            double centerY,
            double widthFraction,
            double minimumWidth,
            double maximumWidth,
            double aspectRatio)
        {
            double width = Math.Clamp(
                canvas.ActualWidth * widthFraction,
                minimumWidth,
                maximumWidth);
            double height = width * aspectRatio;
            double left = Math.Clamp(
                centerX - width / 2,
                -width * 0.08,
                canvas.ActualWidth - width * 0.92);
            double top = Math.Clamp(
                centerY - height / 2,
                -height * 0.08,
                canvas.ActualHeight - height * 0.92);

            Image splat = CreateImage(splatFileName, width, height);
            var scale = new ScaleTransform(0.62, 0.62);
            splat.RenderTransformOrigin = new Point(0.5, 0.5);
            splat.RenderTransform = scale;

            Canvas.SetLeft(splat, left);
            Canvas.SetTop(splat, top);
            canvas.Children.Add(splat);

            scale.BeginAnimation(
                ScaleTransform.ScaleXProperty,
                CreateImpactScale(0.62, 1));
            scale.BeginAnimation(
                ScaleTransform.ScaleYProperty,
                CreateImpactScale(0.62, 1));

            FadeAndRemove(splat, TimeSpan.FromSeconds(2.2));
        }

        private static DoubleAnimation CreateImpactScale(
            double from,
            double to)
        {
            return new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(210),
                EasingFunction = new BackEase
                {
                    Amplitude = 0.35,
                    EasingMode = EasingMode.EaseOut
                },
                FillBehavior = FillBehavior.HoldEnd
            };
        }

        private void FadeAndRemove(
            FrameworkElement visual,
            TimeSpan beginTime)
        {
            var fade = new DoubleAnimation
            {
                From = 1,
                To = 0,
                BeginTime = beginTime,
                Duration = TimeSpan.FromMilliseconds(450),
                FillBehavior = FillBehavior.HoldEnd
            };

            fade.Completed += (_, _) =>
            {
                canvas.Children.Remove(visual);
                activeEffectCount = Math.Max(0, activeEffectCount - 1);
            };

            visual.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        private void ContinueEffect(
            OverlayEffectType effect,
            Action transition,
            params UIElement[] currentVisuals)
        {
            try
            {
                transition();
            }
            catch (Exception ex)
            {
                foreach (UIElement visual in currentVisuals)
                {
                    canvas.Children.Remove(visual);
                }

                activeEffectCount = Math.Max(0, activeEffectCount - 1);
                LogEffectFailure(effect, ex);
            }
        }

        private static void LogEffectFailure(
            OverlayEffectType effect,
            Exception exception)
        {
            System.Diagnostics.Trace.TraceError(
                $"Overlay effect {effect} failed: {exception}");

            try
            {
                Directory.CreateDirectory(AppConfig.ConfigDirectory);
                File.AppendAllText(
                    Path.Combine(
                        AppConfig.ConfigDirectory,
                        "effect-errors.log"),
                    $"[{DateTimeOffset.Now:O}] {effect}{Environment.NewLine}" +
                    $"{exception}{Environment.NewLine}{Environment.NewLine}");
            }
            catch
            {
                // Logging must never interrupt the stream.
            }
        }

        private Image CreateImage(
            string fileName,
            double width,
            double height)
        {
            if (!imageCache.TryGetValue(fileName, out BitmapSource? source))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(
                    $"pack://application:,,,/DiscordGameOverlay;component/" +
                    $"Assets/Effects/{fileName}",
                    UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                source = bitmap;
                imageCache[fileName] = source;
            }

            return new Image
            {
                Source = source,
                Width = width,
                Height = height,
                Stretch = Stretch.Uniform,
                SnapsToDevicePixels = true,
                CacheMode = new BitmapCache()
            };
        }

        private double LeftFromNormalizedX(
            double normalizedX,
            double visualWidth)
        {
            double centerX = Math.Clamp(normalizedX, 0, 1) * canvas.ActualWidth;

            return Math.Clamp(
                centerX - visualWidth / 2,
                0,
                Math.Max(0, canvas.ActualWidth - visualWidth));
        }

        private static double RandomBetween(
            Random effectRandom,
            double minimum,
            double maximum)
        {
            if (maximum <= minimum)
                return minimum;

            return minimum +
                effectRandom.NextDouble() * (maximum - minimum);
        }
    }
}
