using System.Runtime.InteropServices;

namespace TaskTimerWidget.Helpers
{
    internal sealed class AppBarDockManager : IDisposable
    {
        private readonly IntPtr _windowHandle;
        private readonly int _dockWidth;
        private bool _isRegistered;
        private int _lastWindowHeight;
        private bool _lastFillHeight = true;
        private RECT _lastRect;
        private bool _hasApplied;

        public AppBarDockManager(IntPtr windowHandle, int dockWidth)
        {
            _windowHandle = windowHandle;
            _dockWidth = dockWidth;
        }

        public void RegisterRightDock()
        {
            if (_windowHandle == IntPtr.Zero || _isRegistered)
            {
                return;
            }

            var appBarData = CreateAppBarData();
            appBarData.uEdge = (uint)AppBarEdge.Right;

            var result = SHAppBarMessage((uint)AppBarMessage.New, ref appBarData);
            _isRegistered = result != IntPtr.Zero;
        }

        public void UpdatePosition(int windowHeight, bool fillHeight = true)
        {
            if (!_isRegistered || _windowHandle == IntPtr.Zero)
            {
                return;
            }

            _lastWindowHeight = windowHeight;
            _lastFillHeight = fillHeight;

            var monitor = MonitorFromWindow(_windowHandle, MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero)
            {
                return;
            }

            var monitorInfo = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(monitor, ref monitorInfo))
            {
                return;
            }

            var appBarData = CreateAppBarData();
            appBarData.uEdge = (uint)AppBarEdge.Right;
            appBarData.rc.top = monitorInfo.rcMonitor.top;
            appBarData.rc.bottom = monitorInfo.rcMonitor.bottom;
            appBarData.rc.right = monitorInfo.rcMonitor.right;
            appBarData.rc.left = appBarData.rc.right - _dockWidth;

            // QueryPos is cheap and does NOT alter the work area; it only proposes a rectangle.
            SHAppBarMessage((uint)AppBarMessage.QueryPos, ref appBarData);
            // Keep our requested width after the system adjusts the rectangle (e.g. for the taskbar).
            appBarData.rc.left = appBarData.rc.right - _dockWidth;

            var x = appBarData.rc.left;
            var y = appBarData.rc.top;
            var width = appBarData.rc.right - appBarData.rc.left;
            var reservedHeight = appBarData.rc.bottom - appBarData.rc.top;
            var height = fillHeight
                ? Math.Max(1, reservedHeight)
                : Math.Min(Math.Max(1, windowHeight), Math.Max(1, reservedHeight));

            var target = new RECT { left = x, top = y, right = x + width, bottom = y + height };

            // Only touch the work area / move the window when the target actually changed.
            // SetPos forces the shell to recompute the work area and notify every top-level
            // window, which is the source of the brief system-wide hitch; avoid redundant calls.
            if (_hasApplied && RectEquals(target, _lastRect))
            {
                return;
            }

            // SetPos is what reserves/updates the docked work area (the expensive operation).
            SHAppBarMessage((uint)AppBarMessage.SetPos, ref appBarData);
            MoveWindow(_windowHandle, x, y, width, height, true);

            _lastRect = target;
            _hasApplied = true;
        }

        /// <summary>
        /// Re-asserts the dock position if the window has drifted out of place
        /// (e.g. after a display change, resume from sleep, or work-area reset).
        /// </summary>
        public void EnsureDocked()
        {
            if (!_isRegistered || _windowHandle == IntPtr.Zero || !_hasApplied)
            {
                return;
            }

            if (GetWindowRect(_windowHandle, out var current) && RectEquals(current, _lastRect))
            {
                return;
            }

            // Window drifted: re-apply. Force the move even if the computed target matches the
            // cached one, because the window itself is no longer where we expect.
            _hasApplied = false;
            UpdatePosition(_lastWindowHeight, _lastFillHeight);
        }

        private static bool RectEquals(RECT a, RECT b)
        {
            return a.left == b.left && a.top == b.top && a.right == b.right && a.bottom == b.bottom;
        }

        public void Dispose()
        {
            if (!_isRegistered || _windowHandle == IntPtr.Zero)
            {
                return;
            }

            var appBarData = CreateAppBarData();
            SHAppBarMessage((uint)AppBarMessage.Remove, ref appBarData);
            _isRegistered = false;
            GC.SuppressFinalize(this);
        }

        private APPBARDATA CreateAppBarData()
        {
            return new APPBARDATA
            {
                cbSize = Marshal.SizeOf<APPBARDATA>(),
                hWnd = _windowHandle
            };
        }

        private const uint MONITOR_DEFAULTTONEAREST = 2;

        private enum AppBarMessage : uint
        {
            New = 0x00000000,
            Remove = 0x00000001,
            QueryPos = 0x00000002,
            SetPos = 0x00000003
        }

        private enum AppBarEdge : uint
        {
            Right = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("shell32.dll")]
        private static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool MoveWindow(
            IntPtr hWnd,
            int x,
            int y,
            int nWidth,
            int nHeight,
            [MarshalAs(UnmanagedType.Bool)] bool bRepaint);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    }
}
