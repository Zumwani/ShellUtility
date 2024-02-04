using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using ShellUtility.Windows.Models;

namespace ShellUtility.Windows.Utility;

public static class WindowUtility
{

    #region Pinvoke       

    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    #region Helpers

    public static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex) =>
        IntPtr.Size > 4
        ? GetClassLongPtr64(hWnd, nIndex)
        : new IntPtr(GetClassLongPtr32(hWnd, nIndex));

    static bool IsAppWindow(IntPtr hWnd)
    {

        var style = GetWindowLongPtr(hWnd, GWL.GWL_STYLE); // GWL_STYLE

        // check for WS_VISIBLE and WS_CAPTION flags
        // (that the window is visible and has a title bar)
        return (style & 0x10C00000) == 0x10C00000 && (style & 0x10000000) == 0x10000000;

    }

    const int MaxLastActivePopupIterations = 50;
    static readonly string[] WindowsClassNamesToSkip =
    {
            "Shell_TrayWnd",
            "DV2ControlHost",
            "MsgrIMEWindowClass",
            "SysShadow",
            "Button"
        };

    static bool EligibleForActivation(IntPtr hWnd, IntPtr lShellWindow)
    {

        // http://stackoverflow.com/questions/210504/enumerate-windows-like-alt-tab-does

        if (hWnd == lShellWindow)
            return false;

        var root = GetAncestor(hWnd, GetAncestorFlags.GetRootOwner);

        if (GetLastVisibleActivePopUpOfWindow(root) != hWnd)
            return false;

        var classNameStringBuilder = new StringBuilder(256);
        var length = GetClassName(hWnd, classNameStringBuilder, classNameStringBuilder.Capacity);
        if (length == 0)
            return false;

        var className = classNameStringBuilder.ToString();

        if (Array.IndexOf(WindowsClassNamesToSkip, className) > -1)
            return false;

        if (className.StartsWith("WMP9MediaBarFlyout")) //WMP's "now playing" taskbar-toolbar
            return false;

        return true;

    }

    static IntPtr GetLastVisibleActivePopUpOfWindow(IntPtr window)
    {

        var level = MaxLastActivePopupIterations;
        var currentWindow = window;

        while (level-- > 0)
        {
            var lastPopUp = GetLastActivePopup(currentWindow);

            if (IsWindowVisible(lastPopUp))
                return lastPopUp;

            if (lastPopUp == currentWindow)
                return IntPtr.Zero;

            currentWindow = lastPopUp;
        }

        return IntPtr.Zero;

    }

    #endregion
    #region Methods

    [DllImport("user32.dll")]
    static extern IntPtr GetTopWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    static IntPtr GetNextWindow(IntPtr handle) =>
        GetWindow(handle, (uint)GetWindow_Cmd.GW_HWNDNEXT);

    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
    static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = false)]
    static extern IntPtr GetDesktopWindow();

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] int dwFlags, [Out] StringBuilder lpExeName, ref int lpdwSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(ProcessAccess processAccess, bool bInheritHandle, int processId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out IntPtr ProcessId);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "GetClassLong")]
    static extern uint GetClassLongPtr32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
    static extern IntPtr GetClassLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
    static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

    [DllImport("user32.dll", SetLastError = true)]
    static extern int GetWindowLongPtr(IntPtr hWnd, GWL nIndex);

    // This helper static method is required because the 32-bit version of user32.dll does not contain this API
    // (on any versions of Windows), so linking the method will fail at run-time. The bridge dispatches the request
    // to the correct function (GetWindowLong in 32-bit mode and GetWindowLongPtr in 64-bit mode)
    static IntPtr SetWindowLongPtr(IntPtr hWnd, GWL nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
            : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    static extern int SetWindowLong32(IntPtr hWnd, GWL nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, GWL nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", ExactSpelling = true)]
    static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);

    [DllImport("user32.dll")]
    static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    static extern IntPtr GetLastActivePopup(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumDesktopWindowsDelegate lpfn, IntPtr lParam);
    delegate bool EnumDesktopWindowsDelegate(IntPtr hWnd, int lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern bool ShowWindowAsync(IntPtr hWnd, SW nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr GetWindow(IntPtr hWnd, GetWindowType uCmd);

    [DllImport("user32.dll")]
    static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    #endregion
    #region Enums and consts

    const int GCL_HICONSM = -34;
    const int GCL_HICON = -14;

    const int ICON_SMALL = 0;
    const int ICON_BIG = 1;
    const int ICON_SMALL2 = 2;

    const int WM_GETICON = 0x7F;
    const int WM_CLOSE = 0x0010;

    enum GetWindow_Cmd : uint
    {
        GW_HWNDFIRST = 0,
        GW_HWNDLAST = 1,
        GW_HWNDNEXT = 2,
        GW_HWNDPREV = 3,
        GW_OWNER = 4,
        GW_CHILD = 5,
        GW_ENABLEDPOPUP = 6
    }

    [Flags]
    public enum ProcessAccess : uint
    {
        All = 0x001F0FFF,
        Terminate = 0x00000001,
        CreateThread = 0x00000002,
        VirtualMemoryOperation = 0x00000008,
        VirtualMemoryRead = 0x00000010,
        VirtualMemoryWrite = 0x00000020,
        DuplicateHandle = 0x00000040,
        CreateProcess = 0x000000080,
        SetQuota = 0x00000100,
        SetInformation = 0x00000200,
        QueryInformation = 0x00000400,
        QueryLimitedInformation = 0x00001000,
        Synchronize = 0x00100000
    }

    enum ShowWindowCommands : int
    {
        Hide = 0,
        Normal = 1,
        Minimized = 2,
        Maximized = 3,
    }

    enum GetWindowType : uint
    {
        /// <summary>
        /// The retrieved handle identifies the window of the same type that is highest in the Z order.
        /// <para/>
        /// If the specified window is a topmost window, the handle identifies a topmost window.
        /// If the specified window is a top-level window, the handle identifies a top-level window.
        /// If the specified window is a child window, the handle identifies a sibling window.
        /// </summary>
        GW_HWNDFIRST = 0,
        /// <summary>
        /// The retrieved handle identifies the window of the same type that is lowest in the Z order.
        /// <para />
        /// If the specified window is a topmost window, the handle identifies a topmost window.
        /// If the specified window is a top-level window, the handle identifies a top-level window.
        /// If the specified window is a child window, the handle identifies a sibling window.
        /// </summary>
        GW_HWNDLAST = 1,
        /// <summary>
        /// The retrieved handle identifies the window below the specified window in the Z order.
        /// <para />
        /// If the specified window is a topmost window, the handle identifies a topmost window.
        /// If the specified window is a top-level window, the handle identifies a top-level window.
        /// If the specified window is a child window, the handle identifies a sibling window.
        /// </summary>
        GW_HWNDNEXT = 2,
        /// <summary>
        /// The retrieved handle identifies the window above the specified window in the Z order.
        /// <para />
        /// If the specified window is a topmost window, the handle identifies a topmost window.
        /// If the specified window is a top-level window, the handle identifies a top-level window.
        /// If the specified window is a child window, the handle identifies a sibling window.
        /// </summary>
        GW_HWNDPREV = 3,
        /// <summary>
        /// The retrieved handle identifies the specified window's owner window, if any.
        /// </summary>
        GW_OWNER = 4,
        /// <summary>
        /// The retrieved handle identifies the child window at the top of the Z order,
        /// if the specified window is a parent window; otherwise, the retrieved handle is NULL.
        /// The function examines only child windows of the specified window. It does not examine descendant windows.
        /// </summary>
        GW_CHILD = 5,
        /// <summary>
        /// The retrieved handle identifies the enabled popup window owned by the specified window (the
        /// search uses the first such window found using GW_HWNDNEXT); otherwise, if there are no enabled
        /// popup windows, the retrieved handle is that of the specified window.
        /// </summary>
        GW_ENABLEDPOPUP = 6
    }

    enum SW : int
    {
        HIDE = 0,
        SHOWNORMAL = 1,
        SHOWMINIMIZED = 2,
        SHOWMAXIMIZED = 3,
        SHOWNOACTIVATE = 4,
        SHOW = 5,
        MINIMIZE = 6,
        SHOWMINNOACTIVE = 7,
        SHOWNA = 8,
        RESTORE = 9,
        SHOWDEFAULT = 10
    }

    enum GWL
    {
        GWL_WNDPROC = (-4),
        GWL_HINSTANCE = (-6),
        GWL_HWNDPARENT = (-8),
        GWL_STYLE = (-16),
        GWL_EXSTYLE = (-20),
        GWL_USERDATA = (-21),
        GWL_ID = (-12)
    }

    enum WindowLongFlags : int
    {
        GWL_EXSTYLE = -20,
        GWLP_HINSTANCE = -6,
        GWLP_HWNDPARENT = -8,
        GWL_ID = -12,
        GWL_STYLE = -16,
        GWL_USERDATA = -21,
        GWL_WNDPROC = -4,
        DWLP_USER = 0x8,
        DWLP_MSGRESULT = 0x0,
        DWLP_DLGPROC = 0x4
    }

    enum GetAncestorFlags
    {
        /// <summary>Retrieves the parent window. This does not include the owner, as it does with the GetParent function.</summary>
        GetParent = 1,
        /// <summary>Retrieves the root window by walking the chain of parent windows.</summary>
        GetRoot = 2,
        /// <summary>Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent.</summary>
        GetRootOwner = 3
    }

    #endregion
    #region Classes and structs

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public ShowWindowCommands showCmd;
        public POINT ptMinPosition;
        public POINT ptMaxPosition;
        public RECT rcNormalPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    class RECT
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    #endregion

    #endregion

    /// <summary>Get z index of a window.</summary>
    public static bool GetZIndex(IntPtr handle, [NotNullWhen(true)] out int? index)
    {

        index = 0;
        var window = GetTopWindow(GetParent(handle));
        while (window != handle)
        {

            index += 1;
            window = GetNextWindow(window);

            if (window == IntPtr.Zero)
            {
                index = null;
                return false;
            }

        }

        return index is not null;

    }

    static int GetZIndexInternal(IntPtr handle)
    {
        _ = GetZIndex(handle, out var h);
        return h ?? 0;
    }

    /// <summary>Gets all windows at the specified point.</summary>
    /// <remarks>Ordered by z index.</remarks>
    public static IEnumerable<DesktopWindow> GetWindowsAtPoint(Point point) =>
        GetWindowHandlesAtPoint(point).Select(DesktopWindow.FromHandle);

    /// <summary>Gets all windows at the specified point.</summary>
    /// <remarks>Ordered by z index.</remarks>
    public static IEnumerable<IntPtr> GetWindowHandlesAtPoint(Point point) =>
        EnumerateHandles().Where(h => GetIsVisibleAndRect(h).rect?.Contains(point) ?? false).OrderBy(GetZIndexInternal);

    public static string GetClassname(IntPtr handle)
    {
        var sb = new StringBuilder(256);
        _ = GetClassName(handle, sb, 256);
        return sb.ToString();
    }

    public static bool IsKnownWindowToFilterOut(IntPtr handle)
    {
        _ = GetTitle(handle, out var title);
        return
            GetClassname(handle) == "Windows.UI.Core.CoreWindow" ||
            (GetProcessAndPath(handle).path.EndsWith("explorer.exe") && string.IsNullOrEmpty(title));
    }

    static bool IsUWPWindow(IntPtr handle) =>
        GetClassname(handle) is "Windows.UI.Core.CoreWindow" or "ApplicationFrameWindow";

    public static bool IsVisibleInTaskbar(IntPtr handle)
    {

        if (IsOwned(handle))
            return false;

        var shellWindow = GetShellWindow();

        var exstyle = (WindowStylesEx)GetWindowLongPtr(handle, GWL.GWL_EXSTYLE);

        var isAppWindow = exstyle.HasFlag(WindowStylesEx.WS_EX_APPWINDOW);
        var isEligbleForActivation = EligibleForActivation(handle, shellWindow);
        var isToolWindow = exstyle.HasFlag(WindowStylesEx.WS_EX_TOOLWINDOW);

        if (!isAppWindow && !(isEligbleForActivation && !isToolWindow))
            return false;

        if (IsUWPWindow(handle))
            return false;

        return true;

    }

    public static bool GetWindowStyle(IntPtr handle, out WindowStyles style) =>
        GetWindowStyle(handle, out style, out _);

    public static bool GetWindowStyle(IntPtr handle, out WindowStyles style, out WindowStylesEx exStyle)
    {

        style = default;
        exStyle = default;

        var (process, _) = GetProcessAndPath(handle);

        //Getting window style too early causes some windows, like ffxiv_dx11, to become weird.
        //This is a game though, so we probably shouldn't mess with window styles anyways
        if (process?.ProcessName == "ffxiv_dx11")
            return false;

        try
        {

            style = (WindowStyles)GetWindowLongPtr(handle, GWL.GWL_STYLE);
            exStyle = (WindowStylesEx)GetWindowLongPtr(handle, GWL.GWL_EXSTYLE);

            return true;

        }
        catch (Exception ex)
        {
            return false;
        }

    }

    public static bool SetWindowStyle(IntPtr handle, WindowStyles style) =>
        SetWindowLongPtr(handle, GWL.GWL_STYLE, (IntPtr)style) == IntPtr.Zero;

    public static bool SetWindowStyle(IntPtr handle, WindowStylesEx style) =>
        SetWindowLongPtr(handle, GWL.GWL_EXSTYLE, (IntPtr)style) == IntPtr.Zero;

    public static bool IsDesktopWindow(IntPtr handle) =>
        IsWindowVisible(handle) && !IsKnownWindowToFilterOut(handle);

    /// <summary>Finds all desktop windows based on the filters.</summary>
    /// <remarks>Each filter supports '*' character at beginning or end to match ending or beginning of string respectively.</remarks>
    public static IEnumerable<DesktopWindow> Enumerate(string? processName = null, string? title = null, string? className = null, bool matchCase = true)
    {

        if (!matchCase)
        {
            processName = processName?.ToLower();
            title = title?.ToLower();
            className = className?.ToLower();
        }

        if (!string.IsNullOrWhiteSpace(processName) || !string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(className))
            foreach (var window in Find())
                yield return window;

        IEnumerable<DesktopWindow> Find() =>
            Enumerate().
            Where(IsProcessNameMatch).
            Where(IsTitleMatch).
            Where(IsClassNameMatch);

        bool IsProcessNameMatch(DesktopWindow window) => IsMatch(window.ProcessPath, processName, matchCase);
        bool IsTitleMatch(DesktopWindow window) => IsMatch(window.Title, title, matchCase);
        bool IsClassNameMatch(DesktopWindow window) => IsMatch(window.Classname, className, matchCase);

        static bool IsMatch(string? str, string? filter, bool matchCase)
        {

            if (string.IsNullOrWhiteSpace(str) || string.IsNullOrWhiteSpace(filter))
                return true;

            if (!matchCase)
                str = str.ToLower();

            if (filter.StartsWith('*'))
                return str.EndsWith(filter.TrimStart('*'));
            else if (filter.EndsWith('*'))
                return str.StartsWith(filter.TrimEnd('*'));
            else
                return str == filter;

        }


    }

    /// <summary>Enumerates all desktop windows.</summary>
    public static IEnumerable<DesktopWindow> Enumerate() =>
        EnumerateHandles().Select(DesktopWindow.FromHandle);

    /// <summary>Enumerates all desktop windows.</summary>
    public static IEnumerable<IntPtr> EnumerateHandles()
    {

        var list = new List<IntPtr>();
        _ = EnumDesktopWindows(IntPtr.Zero, (handle, lParam) =>
        {
            if (IsDesktopWindow(handle))
                list.Add(handle);
            return true;
        }, IntPtr.Zero);

        return list;

    }

    public static bool IsOwned(IntPtr handle) =>
        GetWindow(handle, GetWindowType.GW_OWNER) != IntPtr.Zero;

    public static IntPtr GetActiveWindow() =>
        GetForegroundWindow();

    public static bool IsOpen(IntPtr handle) =>
        IsWindow(handle);

    public static (bool isVisible, Rect? rect) GetIsVisibleAndRect(IntPtr handle)
    {

        var placement = new WINDOWPLACEMENT();
        _ = GetWindowPlacement(handle, ref placement);
        var isVisible =
            placement.showCmd is ShowWindowCommands.Normal or ShowWindowCommands.Maximized &&
            GetWindowStyle(handle, out var style) && style.HasFlag(WindowStyles.WS_VISIBLE);

        return (isVisible, ToRect(placement.rcNormalPosition));

    }

    static Rect ToRect(RECT r) =>
        new(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);

    public static bool IsActive(IntPtr handle) =>
        GetForegroundWindow() == handle;

    public static void Activate(IntPtr handle) =>
        SetForegroundWindow(handle);

    public static void Deactivate(IntPtr handle) =>
        ShowWindowAsync(handle, SW.MINIMIZE);

    public static void SetVisible(IntPtr handle, bool visible)
    {
        _ = SetForegroundWindow(handle);
        _ = ShowWindowAsync(handle, visible ? SW.RESTORE : SW.MINIMIZE);
    }

    public static void Close(IntPtr handle) =>
        SendMessage(handle, WM_CLOSE, 0, 0);

    public static bool GetTitle(IntPtr handle, [NotNullWhen(true)] out string? title)
    {

        title = null;

        try
        {
            var length = GetWindowTextLength(handle);
            var sb = new StringBuilder(length + 1);
            if (GetWindowText(handle, sb, 256) != 0)
                title = sb.ToString();

        }
        catch (Exception)
        { }

        return title is not null;

    }

    public static IntPtr GetIconHandle(IntPtr handle)
    {

        if (!IsWindow(handle) || IsUWPWindow(handle))
            return IntPtr.Zero;

        Func<IntPtr>[] qualities =
        {
                () => GetClassLongPtr(handle, GCL_HICON),
                () => SendMessage(handle, WM_GETICON, ICON_BIG, 0),
                () => SendMessage(handle, WM_GETICON, ICON_SMALL2, 0),
            };

        foreach (var method in qualities)
            if (method?.Invoke() is IntPtr hIcon && hIcon != IntPtr.Zero)
                return hIcon;

        return LoadIcon(IntPtr.Zero, (IntPtr)0x7F00/*IDI_APPLICATION*/);

    }

    public static bool GetIcon(IntPtr iconHandle, [NotNullWhen(true)] out BitmapSource? icon)
    {

        icon = null;
        if (iconHandle != IntPtr.Zero)
        {
            icon = Imaging.CreateBitmapSourceFromHIcon(iconHandle, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(26, 26));
            icon.Freeze();
        }

        return icon is not null;

    }

    public static (Process process, string path) GetProcessAndPath(IntPtr handle)
    {

        if (handle == IntPtr.Zero)
            return default;

        _ = GetWindowThreadProcessId(handle, out var pid);
        var process = Process.GetProcessById((int)pid);

        var processHandle = OpenProcess(ProcessAccess.QueryLimitedInformation, false, process.Id);
        var capacity = 1024;
        var path = new StringBuilder(capacity);
        _ = QueryFullProcessImageName(processHandle, 0, path, ref capacity);

        return (process, path.ToString());

    }

}
