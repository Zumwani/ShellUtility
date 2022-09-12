namespace ShellUtility.Windows.Utility;

public static class Extensions
{

    /// <summary>Sets the style to the window.</summary>
    public static WindowStyles SetFlag(this WindowStyles styles, WindowStyles style) => styles | style;

    /// <summary>Removes the style from the window.</summary>
    public static WindowStyles RemoveFlag(this WindowStyles styles, WindowStyles style) => styles &= ~style;

}