using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media.Imaging;

namespace ShellUtility.Windows.Utility;

public static class UWPWindowUtility
{

    #region Pinvoke

    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    const int QueryLimitedInformation = 0x1000;
    const int ERROR_INSUFFICIENT_BUFFER = 0x7a;
    const int ERROR_SUCCESS = 0x0;

    [DllImport("kernel32.dll")]
    static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr hHandle);

    [DllImport("kernel32.dll")]
    static extern int GetApplicationUserModelId(IntPtr hProcess, ref uint AppModelIDLength, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder sbAppUserModelID);

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    #endregion

    public static string GetAppID(IntPtr window)
    {

        var result = "";
        _ = EnumChildWindows(window, new EnumWindowsProc((handle, lParam) =>
          {

              if (InternalGetAppID(handle, out var appID))
              {
                  result = appID;
                  return false;
              }

              return true;

          }), IntPtr.Zero);

        return result;

    }

    static bool InternalGetAppID(IntPtr window, out string appID)
    {

        appID = "";

        if (GetWindowThreadProcessId(window, out var pID) == 0)
            return false;

        var ptrProcess = OpenProcess(QueryLimitedInformation, false, (int)pID);
        if (ptrProcess != IntPtr.Zero)
        {

            uint cchLen = 130;
            var sbName = new StringBuilder((int)cchLen);
            var lResult = GetApplicationUserModelId(ptrProcess, ref cchLen, sbName);

            if (lResult == ERROR_SUCCESS)
                appID = sbName.ToString();
            else if (lResult == ERROR_INSUFFICIENT_BUFFER)
            {
                sbName = new StringBuilder((int)cchLen);
                if (ERROR_SUCCESS == GetApplicationUserModelId(ptrProcess, ref cchLen, sbName))
                    appID = sbName.ToString();
            }

            _ = CloseHandle(ptrProcess);

        }

        return !string.IsNullOrEmpty(appID);

    }

    public static BitmapSource GetIcon(IntPtr window) =>
        GetIcon(GetAppID(window));

    public static BitmapSource GetIcon(string appID)
    {

        if (string.IsNullOrEmpty(appID))
            return null;

        //AppIDs for uwp apps will be formatted like this:
        //4DF9E0F8.Netflix_mcm4njqhnhss8!Netflix.App
        //Removing '!Netflix.App', gets us the family name
        var familyName = appID[..appID.LastIndexOf("!")];

        var package = new global::Windows.Management.Deployment.PackageManager().FindPackagesForUser("").FirstOrDefault(p => p.Id.FamilyName == familyName);
        return UIThreadHelper.Dispatcher.Invoke(() => new BitmapImage(package.Logo));

    }

    public static bool IsOpen(IntPtr window) =>
        !string.IsNullOrEmpty(GetAppID(window));

}
