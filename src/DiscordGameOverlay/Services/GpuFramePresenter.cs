using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Windows.Graphics.DirectX.Direct3D11;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Direct3D9;
using Vortice.DXGI;
using D3D11Device = Vortice.Direct3D11.ID3D11Device;
using D3D11DeviceContext = Vortice.Direct3D11.ID3D11DeviceContext;
using D3D11Texture2D = Vortice.Direct3D11.ID3D11Texture2D;
using D3D9PresentParameters = Vortice.Direct3D9.PresentParameters;
using D3D9Format = Vortice.Direct3D9.Format;
using D3D9SwapEffect = Vortice.Direct3D9.SwapEffect;
using D3D9Usage = Vortice.Direct3D9.Usage;
using DxgiFormat = Vortice.DXGI.Format;

namespace DiscordGameOverlay.Services
{
    public sealed class GpuFramePresenter : D3DImage, IDisposable
    {
        private D3D11Device? d3d11Device;
        private D3D11DeviceContext? d3d11Context;
        private IDirect3DDevice? captureDevice;
        private IDirect3D9Ex? d3d9;
        private IDirect3DDevice9Ex? d3d9Device;
        private D3D11Texture2D? sharedTexture;
        private IDirect3DTexture9? d3d9Texture;
        private bool isDisposed;

        public GpuFramePresenter(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                throw new ArgumentException("窗口句柄无效。", nameof(windowHandle));

            try
            {
                FeatureLevel[] featureLevels =
                {
                    FeatureLevel.Level_11_1,
                    FeatureLevel.Level_11_0,
                    FeatureLevel.Level_10_1,
                    FeatureLevel.Level_10_0
                };

                d3d11Device = D3D11.D3D11CreateDevice(
                    DriverType.Hardware,
                    DeviceCreationFlags.BgraSupport,
                    featureLevels);

                d3d11Context = d3d11Device.ImmediateContext;
                captureDevice = Direct3DDeviceFactory.CreateFromNativeDevice(
                    d3d11Device.NativePointer);

                d3d9 = D3D9.Direct3DCreate9Ex();

                var presentParameters = new D3D9PresentParameters
                {
                    BackBufferWidth = 1,
                    BackBufferHeight = 1,
                    BackBufferFormat = D3D9Format.Unknown,
                    BackBufferCount = 1,
                    DeviceWindowHandle = windowHandle,
                    Windowed = true,
                    SwapEffect = D3D9SwapEffect.Discard,
                    PresentationInterval = PresentInterval.Default
                };

                d3d9Device = d3d9.CreateDeviceEx(
                    0,
                    DeviceType.Hardware,
                    windowHandle,
                    CreateFlags.HardwareVertexProcessing |
                    CreateFlags.Multithreaded |
                    CreateFlags.FpuPreserve,
                    presentParameters);

                IsFrontBufferAvailableChanged +=
                    GpuFramePresenter_IsFrontBufferAvailableChanged;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public IDirect3DDevice CaptureDevice =>
            captureDevice ?? throw new ObjectDisposedException(
                nameof(GpuFramePresenter));

        public void Present(IntPtr sourceTexturePointer)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);
            VerifyAccess();

            if (sourceTexturePointer == IntPtr.Zero)
                return;

            Marshal.AddRef(sourceTexturePointer);

            using var sourceTexture =
                new D3D11Texture2D(sourceTexturePointer);

            Texture2DDescription sourceDescription =
                sourceTexture.Description;

            if (sourceDescription.Format != DxgiFormat.B8G8R8A8_UNorm)
            {
                throw new NotSupportedException(
                    $"不支持的采集画面格式：{sourceDescription.Format}");
            }

            EnsureSharedTexture(
                checked((int)sourceDescription.Width),
                checked((int)sourceDescription.Height));

            d3d11Context!.CopyResource(sharedTexture!, sourceTexture);
            d3d11Context.Flush();
            InvalidateFrame();
        }

        public void Clear()
        {
            if (isDisposed)
                return;

            VerifyAccess();
            ReleaseSharedTextures();
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            IsFrontBufferAvailableChanged -=
                GpuFramePresenter_IsFrontBufferAvailableChanged;

            ReleaseSharedTextures();

            (captureDevice as IDisposable)?.Dispose();
            captureDevice = null;

            d3d11Context?.ClearState();
            d3d11Context?.Flush();
            d3d11Context?.Dispose();
            d3d11Context = null;

            d3d11Device?.Dispose();
            d3d11Device = null;

            d3d9Device?.Dispose();
            d3d9Device = null;

            d3d9?.Dispose();
            d3d9 = null;
        }

        private void EnsureSharedTexture(int width, int height)
        {
            if (sharedTexture != null)
            {
                Texture2DDescription current = sharedTexture.Description;
                if (current.Width == width && current.Height == height)
                    return;
            }

            ReleaseSharedTextures();

            var description = new Texture2DDescription
            {
                Width = checked((uint)width),
                Height = checked((uint)height),
                MipLevels = 1,
                ArraySize = 1,
                Format = DxgiFormat.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget |
                            BindFlags.ShaderResource,
                CPUAccessFlags = CpuAccessFlags.None,
                MiscFlags = ResourceOptionFlags.Shared
            };

            sharedTexture = d3d11Device!.CreateTexture2D(description);

            using IDXGIResource dxgiResource =
                sharedTexture.QueryInterface<IDXGIResource>();

            IntPtr sharedHandle = dxgiResource.SharedHandle;

            d3d9Texture = d3d9Device!.CreateTexture(
                checked((uint)width),
                checked((uint)height),
                1,
                D3D9Usage.RenderTarget,
                D3D9Format.A8R8G8B8,
                Pool.Default,
                ref sharedHandle);

            BindBackBuffer();
        }

        private void BindBackBuffer()
        {
            if (d3d9Texture == null || !IsFrontBufferAvailable)
                return;

            using IDirect3DSurface9 surface =
                d3d9Texture.GetSurfaceLevel(0);

            Lock();
            try
            {
                SetBackBuffer(
                    D3DResourceType.IDirect3DSurface9,
                    surface.NativePointer);
            }
            finally
            {
                Unlock();
            }
        }

        private void InvalidateFrame()
        {
            if (!IsFrontBufferAvailable || PixelWidth <= 0 || PixelHeight <= 0)
                return;

            Lock();
            try
            {
                AddDirtyRect(new Int32Rect(
                    0,
                    0,
                    PixelWidth,
                    PixelHeight));
            }
            finally
            {
                Unlock();
            }
        }

        private void ReleaseSharedTextures()
        {
            if (Dispatcher.CheckAccess())
            {
                Lock();
                try
                {
                    SetBackBuffer(
                        D3DResourceType.IDirect3DSurface9,
                        IntPtr.Zero);
                }
                finally
                {
                    Unlock();
                }
            }

            d3d9Texture?.Dispose();
            d3d9Texture = null;

            sharedTexture?.Dispose();
            sharedTexture = null;
        }

        private void GpuFramePresenter_IsFrontBufferAvailableChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (isDisposed || !IsFrontBufferAvailable)
                return;

            BindBackBuffer();
            InvalidateFrame();
        }
    }
}
