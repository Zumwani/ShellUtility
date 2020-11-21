using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ShellUtility.NotifyIcons
{

    internal static class ClickUtility
    {

        #region Pinvoke

        const int WM_LBUTTONDOWN = 0x201;
        const int WM_LBUTTONUP = 0x202;
        const int WM_LBUTTONDBLCLK = 0x203;
        const int WM_RBUTTONDOWN = 0x204;
        const int WM_RBUTTONUP = 0x205;

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, StringBuilder lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, ref IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, IntPtr lParam);

        #endregion

        /// <summary>Simulates a mouse click on the specified icon.</summary>
        public static void SimulateClick(NotifyIcon icon, NotifyIconInvokeAction action)
        {

            if (!GetCursorPos(out var mousePos))
                return;

            var pos = (mousePos.Y << 16) | mousePos.X;

            GetAction()?.Invoke();
            Action GetAction() =>
                action switch
                {
                    NotifyIconInvokeAction.LeftClick   => () => SimulateClick(icon, (IntPtr)0x400, WM_LBUTTONDOWN, WM_LBUTTONUP),
                    NotifyIconInvokeAction.RightClick  => () => SimulateClick(icon, (IntPtr)((icon.CallbackParam << 16) | 0x7B), WM_RBUTTONDOWN, WM_RBUTTONUP, specificMessageWParam: pos),
                    NotifyIconInvokeAction.DoubleClick => () => SimulateClick(icon, (IntPtr)0x200, WM_LBUTTONDBLCLK),
                    _ => null,
                };

        }

        static void SimulateClick(NotifyIcon icon, IntPtr specificMessage, int? down = null, int? up = null, int? specificMessageWParam = null) =>
            Task.Run(() => //Some apps might not return until context menus are closed and will as such freeze up ui 
            {              //while some will not, resulting in inconsistent behavior, running as task prevents blocking 
                           //and makes it consistent

                if (!(down ?? up).HasValue)
                    return;

                if (down.HasValue)
                    SendMessage(icon.Handle, icon.CallbackMessage, icon.CallbackParam, (IntPtr)down);
                if (up.HasValue)
                    SendMessage(icon.Handle, icon.CallbackMessage, icon.CallbackParam, (IntPtr)up);

                SendMessage(icon.Handle, icon.CallbackMessage, specificMessageWParam ?? icon.CallbackParam, specificMessage);

            });

    }

}
