using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Interop;
using System.Windows.Threading;
using DiscordGameOverlay.Models;
using DiscordGameOverlay.Services;

namespace DiscordGameOverlay.Views
{
    public partial class StreamWindow : Window
    {
        private const double LaneHeight = 46;
        private const double HorizontalPadding = 24;
        private const double DanmakuSpeed = 170;
        private const int MaxPendingMessages = 100;

        private readonly MessageManager messageManager;
        private readonly Queue<ChatMessage> pendingMessages = new();
        private readonly DispatcherTimer danmakuTimer;
        private readonly List<DateTimeOffset> laneReadyTimes = new();

        private GpuFramePresenter? gpuFramePresenter;
        private GameCaptureService? gameCaptureService;
        private bool allowClose;

        public event Action<string>? CaptureStatusChanged;

        public StreamWindow(MessageManager manager)
        {
            InitializeComponent();

            messageManager = manager;

            foreach (ChatMessage message in messageManager.Messages)
            {
                EnqueueMessage(message);
            }

            messageManager.Messages.CollectionChanged += Messages_CollectionChanged;

            danmakuTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(80)
            };
            danmakuTimer.Tick += DanmakuTimer_Tick;

            Loaded += StreamWindow_Loaded;
            Closed += StreamWindow_Closed;
        }

        private void StreamWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeGpuCapture();
            danmakuTimer.Start();
        }

        private void StreamWindow_Closed(object? sender, EventArgs e)
        {
            danmakuTimer.Stop();
            danmakuTimer.Tick -= DanmakuTimer_Tick;
            messageManager.Messages.CollectionChanged -= Messages_CollectionChanged;

            if (gameCaptureService != null)
            {
                gameCaptureService.FrameArrived -= GameCaptureService_FrameArrived;
                gameCaptureService.CaptureStarted -= GameCaptureService_CaptureStarted;
                gameCaptureService.CaptureStopped -= GameCaptureService_CaptureStopped;
                gameCaptureService.CaptureFailed -= GameCaptureService_CaptureFailed;
                gameCaptureService.Dispose();
                gameCaptureService = null;
            }

            GamePreviewImage.Source = null;
            gpuFramePresenter?.Dispose();
            gpuFramePresenter = null;
        }

        public Task<bool> SelectCaptureSourceAsync(IntPtr ownerWindowHandle)
        {
            InitializeGpuCapture();
            return gameCaptureService!.SelectAndStartAsync(ownerWindowHandle);
        }

        public void StopGameCapture()
        {
            gameCaptureService?.StopCapture();
        }

        private void InitializeGpuCapture()
        {
            if (gameCaptureService != null)
                return;

            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            gpuFramePresenter = new GpuFramePresenter(windowHandle);
            GamePreviewImage.Source = gpuFramePresenter;

            gameCaptureService = new GameCaptureService(
                gpuFramePresenter.CaptureDevice);

            gameCaptureService.FrameArrived += GameCaptureService_FrameArrived;
            gameCaptureService.CaptureStarted += GameCaptureService_CaptureStarted;
            gameCaptureService.CaptureStopped += GameCaptureService_CaptureStopped;
            gameCaptureService.CaptureFailed += GameCaptureService_CaptureFailed;
        }

        private void GameCaptureService_FrameArrived(
            object? sender,
            GameCaptureFrameEventArgs e)
        {
            if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
                return;

            try
            {
                Dispatcher.Invoke(() =>
                {
                    gpuFramePresenter?.Present(e.TexturePointer);
                    VideoPlaceholder.Visibility = Visibility.Collapsed;
                });
            }
            catch (TaskCanceledException)
            {
                // The application is shutting down.
            }
        }

        private void GameCaptureService_CaptureStarted(string sourceName)
        {
            RunOnUiThread(() =>
                CaptureStatusChanged?.Invoke($"正在采集：{sourceName}"));
        }

        private void GameCaptureService_CaptureStopped()
        {
            RunOnUiThread(() =>
            {
                ResetGamePreview();
                CaptureStatusChanged?.Invoke("未选择直播画面");
            });
        }

        private void GameCaptureService_CaptureFailed(string message)
        {
            RunOnUiThread(() =>
                CaptureStatusChanged?.Invoke(message));
        }

        private void ResetGamePreview()
        {
            gpuFramePresenter?.Clear();
            VideoPlaceholder.Visibility = Visibility.Visible;
        }

        private void RunOnUiThread(Action action)
        {
            if (Dispatcher.CheckAccess())
            {
                action();
                return;
            }

            if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
            {
                Dispatcher.BeginInvoke(action);
            }
        }

        private void Messages_CollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                pendingMessages.Clear();
                laneReadyTimes.Clear();
                DanmakuCanvas.Children.Clear();
                return;
            }

            if (e.NewItems == null)
                return;

            foreach (ChatMessage message in e.NewItems.OfType<ChatMessage>())
            {
                EnqueueMessage(message);
            }
        }

        private void EnqueueMessage(ChatMessage message)
        {
            while (pendingMessages.Count >= MaxPendingMessages)
            {
                pendingMessages.Dequeue();
            }

            pendingMessages.Enqueue(message);
        }

        private void DanmakuTimer_Tick(object? sender, EventArgs e)
        {
            if (pendingMessages.Count == 0 ||
                DanmakuCanvas.ActualWidth <= 0 ||
                DanmakuCanvas.ActualHeight <= 0)
            {
                return;
            }

            EnsureLaneCount();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            int laneIndex = laneReadyTimes.FindIndex(readyAt => readyAt <= now);

            while (pendingMessages.Count > 0 && laneIndex >= 0)
            {
                ChatMessage message = pendingMessages.Dequeue();
                ShowDanmaku(message, laneIndex, now);

                laneIndex = laneReadyTimes.FindIndex(readyAt => readyAt <= now);
            }
        }

        private void EnsureLaneCount()
        {
            int requiredLaneCount = Math.Max(
                1,
                (int)Math.Floor(
                    (DanmakuCanvas.ActualHeight - HorizontalPadding * 2) /
                    LaneHeight));

            while (laneReadyTimes.Count < requiredLaneCount)
            {
                laneReadyTimes.Add(DateTimeOffset.MinValue);
            }

            if (laneReadyTimes.Count > requiredLaneCount)
            {
                laneReadyTimes.RemoveRange(
                    requiredLaneCount,
                    laneReadyTimes.Count - requiredLaneCount);
            }
        }

        private void ShowDanmaku(
            ChatMessage message,
            int laneIndex,
            DateTimeOffset now)
        {
            Border visual = CreateDanmakuVisual(message);

            visual.Measure(new Size(
                double.PositiveInfinity,
                double.PositiveInfinity));

            double visualWidth = visual.DesiredSize.Width;
            double startX = DanmakuCanvas.ActualWidth;
            double endX = -visualWidth;
            double top = HorizontalPadding + laneIndex * LaneHeight;

            Canvas.SetLeft(visual, startX);
            Canvas.SetTop(visual, top);
            DanmakuCanvas.Children.Add(visual);

            TimeSpan duration = TimeSpan.FromSeconds(
                (startX + visualWidth) / DanmakuSpeed);

            var animation = new DoubleAnimation
            {
                From = startX,
                To = endX,
                Duration = duration,
                FillBehavior = FillBehavior.Stop
            };

            animation.Completed += (_, _) =>
            {
                DanmakuCanvas.Children.Remove(visual);
            };

            laneReadyTimes[laneIndex] = now.AddSeconds(
                (visualWidth + 36) / DanmakuSpeed);

            visual.BeginAnimation(Canvas.LeftProperty, animation);
        }

        private static Border CreateDanmakuVisual(ChatMessage message)
        {
            string content = message.Content
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();

            if (content.Length > 160)
            {
                content = $"{content[..160]}…";
            }

            var text = new TextBlock
            {
                FontSize = 22,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.NoWrap,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 4,
                    ShadowDepth = 1,
                    Opacity = 1
                }
            };

            text.Inlines.Add(new Run(message.DisplayName)
            {
                Foreground = new SolidColorBrush(Color.FromRgb(88, 101, 242)),
                FontWeight = FontWeights.Bold
            });
            text.Inlines.Add(new Run(": "));
            text.Inlines.Add(new Run(content));

            return new Border
            {
                Child = text,
                Background = new SolidColorBrush(Color.FromArgb(96, 0, 0, 0)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 3, 8, 3)
            };
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!allowClose)
            {
                // 用户点击 X：不允许关闭
                e.Cancel = true;
                return;
            }

            base.OnClosing(e);
        }

        public void AllowClose()
        {
            allowClose = true;
        }
    }
}
