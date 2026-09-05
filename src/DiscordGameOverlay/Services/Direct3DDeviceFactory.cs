using System.Runtime.InteropServices;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace DiscordGameOverlay.Services
{
    internal static class Direct3DDeviceFactory
    {
        private static readonly Guid IdxgiDeviceGuid =
            new("54EC77FA-1377-44E6-8C32-88FD5F44C84C");

        [DllImport(
            "d3d11.dll",
            EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice",
            ExactSpelling = true)]
        private static extern int CreateDirect3D11DeviceFromDxgiDevice(
            IntPtr dxgiDevice,
            out IntPtr graphicsDevice);

        public static IDirect3DDevice CreateFromNativeDevice(
            IntPtr nativeDevice)
        {
            IntPtr dxgiDevice = IntPtr.Zero;
            IntPtr graphicsDevice = IntPtr.Zero;

            try
            {
                Guid iid = IdxgiDeviceGuid;
                Marshal.ThrowExceptionForHR(
                    Marshal.QueryInterface(nativeDevice, in iid, out dxgiDevice));

                Marshal.ThrowExceptionForHR(
                    CreateDirect3D11DeviceFromDxgiDevice(
                        dxgiDevice,
                        out graphicsDevice));

                return MarshalInterface<IDirect3DDevice>.FromAbi(graphicsDevice);
            }
            finally
            {
                if (graphicsDevice != IntPtr.Zero)
                    Marshal.Release(graphicsDevice);

                if (dxgiDevice != IntPtr.Zero)
                    Marshal.Release(dxgiDevice);
            }
        }
    }
}
