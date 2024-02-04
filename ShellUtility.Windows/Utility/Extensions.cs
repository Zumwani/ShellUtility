using System.Windows;
using System.Windows.Interop;
using ShellUtility.Windows.Models;

namespace ShellUtility.Windows.Utility;

public static class Extensions
{

    /// <summary>Sets the style to the window.</summary>
    public static WindowStyles SetFlag(this WindowStyles styles, WindowStyles style) => styles | style;

    /// <summary>Removes the style from the window.</summary>
    public static WindowStyles RemoveFlag(this WindowStyles styles, WindowStyles style) => styles &= ~style;

    /// <summary>Sets the style to the window.</summary>
    public static WindowStylesEx SetFlag(this WindowStylesEx styles, WindowStylesEx style) => styles | style;

    /// <summary>Removes the style from the window.</summary>
    public static WindowStylesEx RemoveFlag(this WindowStylesEx styles, WindowStylesEx style) => styles &= ~style;

    /// <summary>Gets the <see cref="DesktopWindow"/> from this <see cref="Window"/>.</summary>
    public static DesktopWindow AsDesktopWindow(this Window window)
    {
        var handle = new WindowInteropHelper(window);
        return DesktopWindow.FromHandle(handle.Handle);
    }

}