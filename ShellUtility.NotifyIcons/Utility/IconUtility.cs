using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ShellUtility.NotifyIcons
{

    internal static class IconUtility
    {

        [DllImport("shell32.dll")]
        static extern IntPtr ExtractAssociatedIcon(IntPtr hInst, string lpIconPath, out ushort lpiIcon);

        public static BitmapSource IconFromResourceHandle(IntPtr handle, string path)
        {

            var extractedHandle = ExtractAssociatedIcon(handle, path, out var result);
            if (result != 0)
                return null;

            var img = Imaging.CreateBitmapSourceFromHIcon(extractedHandle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            img.Freeze();

            return img;

        }

    }

}
