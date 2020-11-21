using System;
using System.Runtime.InteropServices;

namespace ShellUtility.NotifyIcons
{

    internal static class PathUtility
    {

        [DllImport("shell32.dll")]
        static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr pszPath);

        /// <summary>Expands a path containing known folders (as guid variables).</summary>
        public static string ExpandPath(string path)
        {

            var splits = path.Split("\\");
            for (int i = 0; i < splits.Length; i++)
                if (Guid.TryParse(splits[i], out var guid))
                    if (SHGetKnownFolderPath(guid, 0, IntPtr.Zero, out var pPath) == 0)
                    {
                        var expanded = Marshal.PtrToStringUni(pPath);
                        Marshal.FreeCoTaskMem(pPath);
                        splits[i] = expanded;
                    }

            return string.Join("\\", splits);

        }

    }

}
