using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ShellUtility.NotifyIcons
{

    internal static class IconUtility
    {

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr ExtractAssociatedIcon(IntPtr hInst, string lpIconPath, out ushort lpiIcon);

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);

        public static BitmapSource IconFromResourceHandle(IntPtr handle, string path)
        {

            try
            {

                if (handle == IntPtr.Zero)
                    return ExtractIcon();

                var icon = Imaging.CreateBitmapSourceFromHIcon(handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                icon.Freeze();
                _ = DeleteObject(handle);
                return icon;

            }
            catch (COMException)
            {
                return ExtractIcon();
            }

            BitmapSource ExtractIcon()
            {

                var extractedHandle = ExtractAssociatedIcon(handle, path, out var result);
                if (result != 0)
                    return null;

                var img = Imaging.CreateBitmapSourceFromHIcon(extractedHandle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                img.Freeze();
                _ = DeleteObject(extractedHandle);

                return img;

            }

        }

    }

}
