using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShellUtility.Windows.Utility;

/// <summary>Contains utility functions for dealing with icons.</summary>
public static class IconUtility
{

    /// <summary>Extracts icon from file.</summary>
    public static ImageSource? GetIcon(string path, bool smallIcon, bool isDirectory)
    {

        // SHGFI_USEFILEATTRIBUTES takes the file name and attributes into account if it doesn't exist
        var flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES;
        if (smallIcon)
            flags |= SHGFI_SMALLICON;
        else
            flags |= SHGFI_LARGEICON;

        var attributes = FILE_ATTRIBUTE_NORMAL;
        if (isDirectory)
            attributes |= FILE_ATTRIBUTE_DIRECTORY;

        return
            0 != SHGetFileInfo(path, attributes, out var shfi, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), flags)
            ? Imaging.CreateBitmapSourceFromHIcon(shfi.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
            : null;

    }

    [StructLayout(LayoutKind.Sequential)]
    struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32", CharSet = CharSet.Unicode)]
    static extern int SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint flags);

    const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
    const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

    const uint SHGFI_ICON = 0x000000100;     // get icon
    const uint SHGFI_LARGEICON = 0x000000000;     // get large icon
    const uint SHGFI_SMALLICON = 0x000000001;     // get small icon
    const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;     // use passed dwFileAttribute

}
