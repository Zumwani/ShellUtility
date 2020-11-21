using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using ShellUtility.NotifyIcons;
using System.Linq;

internal static class CallbackMessageUtility
{

    //Code taken from:
    //Dual Monitor Taskbar (Cristi Diaconu)
    //https://github.com/skoant/Dual-Monitor-Taskbar/blob/master/DualMonitorSolution/NotificationAreaProxy.cs
    //https://github.com/skoant/Dual-Monitor-Taskbar

    #region Pinvoke

    const int TB_BUTTONCOUNT = WM_USER + 24;
    const int WM_USER = 0x0400;
    const int TB_GETBUTTON = WM_USER + 23;

    [Flags]
    enum ProcessAccess : int
    {
        /// <summary>Specifies all possible access flags for the process object.</summary>
        AllAccess = CreateThread | DuplicateHandle | QueryInformation | SetInformation | Terminate | VMOperation | VMRead | VMWrite | Synchronize,
        /// <summary>Enables usage of the process handle in the CreateRemoteThread function to create a thread in the process.</summary>
        CreateThread = 0x2,
        /// <summary>Enables usage of the process handle as either the source or target process in the DuplicateHandle function to duplicate a handle.</summary>
        DuplicateHandle = 0x40,
        /// <summary>Enables usage of the process handle in the GetExitCodeProcess and GetPriorityClass functions to read information from the process object.</summary>
        QueryInformation = 0x400,
        /// <summary>Enables usage of the process handle in the SetPriorityClass function to set the priority class of the process.</summary>
        SetInformation = 0x200,
        /// <summary>Enables usage of the process handle in the TerminateProcess function to terminate the process.</summary>
        Terminate = 0x1,
        /// <summary>Enables usage of the process handle in the VirtualProtectEx and WriteProcessMemory functions to modify the virtual memory of the process.</summary>
        VMOperation = 0x8,
        /// <summary>Enables usage of the process handle in the ReadProcessMemory function to' read from the virtual memory of the process.</summary>
        VMRead = 0x10,
        /// <summary>Enables usage of the process handle in the WriteProcessMemory function to write to the virtual memory of the process.</summary>
        VMWrite = 0x20,
        /// <summary>Enables usage of the process handle in any of the wait functions to wait for the process to terminate.</summary>
        Synchronize = 0x100000
    }

    [Flags]
    enum AllocationType
    {
        Commit = 0x1000,
        Reserve = 0x2000,
        Decommit = 0x4000,
        Release = 0x8000,
        Reset = 0x80000,
        Physical = 0x400000,
        TopDown = 0x100000,
        WriteWatch = 0x200000,
        LargePages = 0x20000000
    }

    [Flags]
    enum MemoryProtection
    {
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadWrite = 0x40,
        ExecuteWriteCopy = 0x80,
        NoAccess = 0x01,
        ReadOnly = 0x02,
        ReadWrite = 0x04,
        WriteCopy = 0x08,
        GuardModifierflag = 0x100,
        NoCacheModifierflag = 0x200,
        WriteCombineModifierflag = 0x400
    }

    [Flags]
    enum FreeType
    {
        Decommit = 0x4000,
        Release = 0x8000,
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SysTrayData
    {
        public int hwnd;
        public int uID;
        public int wParam;
        public uint nMsg;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TBBUTTON32
    {
        public int iBitmap;
        public int idCommand;
        public byte fsState;
        public byte fsStyle;
        public byte bReserved0;
        public byte bReserved1;
        public ulong dwData;
        public int iString;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TBBUTTON64
    {
        public int iBitmap;
        public int idCommand;
        public byte fsState;
        public byte fsStyle;
        public byte bReserved0;
        public byte bReserved1;
        public byte bReserved2;
        public byte bReserved3;
        public byte bReserved4;
        public byte bReserved5;
        public ulong dwData;
        public int iString;
    }

    [DllImport("kernel32.dll")]
    static extern IntPtr OpenProcess(ProcessAccess dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, FreeType dwFreeType);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out, MarshalAs(UnmanagedType.AsAny)] object lpBuffer, int dwSize, out int lpNumberOfBytesRead);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint lpdwProcessId);

    #endregion

    public static void GetCallbackMessages(ref NotifyIcon icon)
    {

        if (GetCallbackMessages().TryGetValue(icon.Handle, out var messages))
        {
            icon.CallbackMessage = messages.nMsg;
            icon.CallbackParam = messages.wParam;
        }

    }


    public static Dictionary<IntPtr, (uint nMsg, int wParam)> GetCallbackMessages()
    {

        var items1 = GetCallbackMessages(GetUserToolbar());
        var items2 = GetCallbackMessages(GetUserToolbarOverflow());

        return items1.Concat(items2).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

    }

    static Dictionary<IntPtr, (uint nMsg, int wParam)> GetCallbackMessages(IntPtr handle)
    {

        try
        {

            var result = new Dictionary<IntPtr, (uint nMsg, int wParam)>();
            if (handle == IntPtr.Zero) 
                return result;

            var count = (int)SendMessage(handle, TB_BUTTONCOUNT, IntPtr.Zero, IntPtr.Zero);
            if (count == 0) 
                return result;

            if (GetWindowThreadProcessId(handle, out uint pid) == 0)
                return result;

            var process = OpenProcess(ProcessAccess.AllAccess, false, (int)pid);

            for (int i = 0; i < count; i++)
            {

                var sizeOfTbButton = Marshal.SizeOf(Is64Bits() ? typeof(TBBUTTON64) : typeof(TBBUTTON32));

                // Allocate memory for TBBUTTON structure
                var pTBBUTTON = VirtualAllocEx(process, IntPtr.Zero, (uint)sizeOfTbButton, AllocationType.Commit, MemoryProtection.ReadWrite);

                // Ask explorer.exe to fill the structure we just allocated
                SendMessage(handle, TB_GETBUTTON, new IntPtr(i), pTBBUTTON);

                // Read the structure from explorer.exe's memory
                object obj;
                obj = new TBBUTTON64();
                ReadProcessMemory(process, pTBBUTTON, obj, sizeOfTbButton, out int read);
                var tbbutton = ConvertToTBButton32(obj);

                VirtualFreeEx(process, pTBBUTTON, sizeOfTbButton, FreeType.Decommit | FreeType.Release);

                // Get data associated with icon
                var data = new IntPtr((int)tbbutton.dwData);

                obj = new SysTrayData();
                ReadProcessMemory(process, data, obj, Marshal.SizeOf(typeof(SysTrayData)), out read);
                var trayData = (SysTrayData)obj;

                FixTrayDataAnyCPU(ref trayData);

                var window = (IntPtr)trayData.hwnd;
                if (result.ContainsKey(window))
                    result[window] = (trayData.nMsg, trayData.wParam);
                else
                    result.Add(window, (trayData.nMsg, trayData.wParam));

            }

            CloseHandle(process);
            return result;

        }
        catch (OverflowException)
        {
            return new Dictionary<IntPtr, (uint nMsg, int wParam)>();
        }

    }

    static IntPtr GetUserToolbar() =>
        FromClassName("Shell_TrayWnd")
        .FindWindow("TrayNotifyWnd")
        .FindWindow("SysPager")
        .FindWindow("ToolbarWindow32");

    static IntPtr GetUserToolbarOverflow() =>
        FromClassName("NotifyIconOverflowWindow")
        .FindWindow("ToolbarWindow32");

    static IntPtr FromClassName(string className) =>
        FindWindow(className, null);

    static IntPtr FindWindow(this IntPtr handle, string className) =>
        handle == IntPtr.Zero
        ? IntPtr.Zero
        : FindWindowEx(handle, IntPtr.Zero, className, null);

    static bool Is64Bits() =>
        IntPtr.Size == 8;

    static void FixTrayDataAnyCPU(ref SysTrayData trayData)
    {
        if (!Is64Bits())
        {
            trayData.nMsg = (uint)trayData.wParam;
            trayData.wParam = trayData.uID;
        }
    }

    static TBBUTTON32 ConvertToTBButton32(object obj) =>
        obj is not TBBUTTON64 tbb64
        ? (TBBUTTON32)obj
        : new TBBUTTON32()
        {
            dwData = tbb64.dwData,
            fsState = tbb64.fsState,
            fsStyle = tbb64.fsStyle,
            iBitmap = tbb64.iBitmap,
            idCommand = tbb64.idCommand,
            iString = tbb64.iString
        };

}
