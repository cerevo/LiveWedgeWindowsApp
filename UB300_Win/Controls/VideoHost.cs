using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Cerevo.UB300_Win.Controls {
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class VideoHost : HwndHost {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct RECT {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PAINTSTRUCT {
            public IntPtr hdc;
            public bool fErase;
            public RECT rcPaint;
            public bool fRestore;
            public bool fIncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] rgbReserved;
        }

        [DllImport("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);
        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(IntPtr hWnd);
        [DllImport("User32.dll")]
        private static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT lpPaint);
        [DllImport("User32.dll")]
        private static extern bool EndPaint(IntPtr hWnd, [In] ref PAINTSTRUCT lpPaint);
        [DllImport("User32.dll")]
        private static extern int FillRect(IntPtr hDC, [In] ref RECT lprc, IntPtr hbr);

        private const int WM_PAINT = 0x000F;
        private const int WS_CHILD = 0x40000000;
        private const int WS_VISIBLE = 0x10000000;
        private const int HOST_ID = 0x00000002;
        private const int COLOR_WINDOW = 5;
        private const int COLOR_BTNTEXT = 18;

        public event EventHandler<EventArgs> Paint;
        public event EventHandler<EventArgs> PositionChanged;
        public int HostWidth { get; private set; }
        public int HostHeight { get; private set; }

        public VideoHost(double width, double height) {
            HostWidth = (int)width;
            HostHeight = (int)height;
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent) {
            var hwndHost = CreateWindowEx(0, "static", "", WS_CHILD | WS_VISIBLE,
                0, 0, HostWidth, HostHeight, hwndParent.Handle, (IntPtr)HOST_ID, IntPtr.Zero, IntPtr.Zero);

            return new HandleRef(this, hwndHost);
        }

        protected override void DestroyWindowCore(HandleRef hwnd) {
            DestroyWindow(hwnd.Handle);
        }

        protected override void OnWindowPositionChanged(Rect rcBoundingBox) {
            base.OnWindowPositionChanged(rcBoundingBox);

            HostWidth = (int)rcBoundingBox.Width;
            HostHeight = (int)rcBoundingBox.Height;

            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            if(msg == WM_PAINT) {
                var handler = Paint;
                if(handler != null) {
                    handler.Invoke(this, EventArgs.Empty);
                } else {
                    DoDefaultPaint();
                }
                handled = true;
                return IntPtr.Zero;
            }

            handled = false;
            return IntPtr.Zero;
        }

        public void DoDefaultPaint() {
            PAINTSTRUCT ps;
            var hdc = BeginPaint(Handle, out ps);
            FillRect(hdc, ref ps.rcPaint, new IntPtr(COLOR_BTNTEXT + 1));
            EndPaint(Handle, ref ps);
        }
    }
}
