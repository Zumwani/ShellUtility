using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

using static ShellUtility.Screens.WindowsDesktopAPI.WindowsDesktopAPI;

namespace ShellUtility.Screens
{

    /// <summary>Represents a screen on the users system.</summary>
    public class Screen
    {

        #region Constructors

        private Screen(IntPtr handle)
        {

            var info = new MonitorInfoEx();
            _ = GetMonitorInfo(new HandleRef(null, handle), info);

            Bounds = info.rcMonitor;
            WorkArea = info.rcWork;
            IsPrimary = ((info.dwFlags & MONITORINFOF_PRIMARY) != 0);
            DeviceName = new string(info.szDevice).TrimEnd((char)0).TrimStart(@"\\.\".ToCharArray());
            Handle = handle;

            var (adapter, name, index) = GetDisplayInfo();
            Name = name;
            Adapter = adapter;
            Index = index;

        }

        /// <summary>Gets the screen at the specified index.</summary>
        /// <exception cref="IndexOutOfRangeException"/>
        public static Screen FromIndex(int index, bool throwOnNotFound = true)
        {
            var screen = All()?.ElementAtOrDefault(index);
            if (screen != null)
                return screen;
            else if (throwOnNotFound)
                throw new IndexOutOfRangeException();
            else
                return null;
        }

        /// <summary>Gets the screen that the specified window is located on.</summary>
        /// <exception cref="ArgumentNullException"/>
        public static Screen FromWindowHandle(IntPtr handle, bool throwIfNull = true)
        {

            if (handle == IntPtr.Zero)
                return !throwIfNull ? null : throw new ArgumentNullException(nameof(handle));

            handle = MonitorFromWindow(handle, MonitorOptions.MONITOR_DEFAULTTONEAREST);
            return FromScreenHandle(handle, throwIfNull);

        }

        /// <summary>Gets the screen with the specified screen handle.</summary>
        /// <exception cref="ArgumentNullException"/>
        public static Screen FromScreenHandle(IntPtr handle, bool throwIfNull = true)
        {

            if (handle == IntPtr.Zero)
                return !throwIfNull ? null : throw new ArgumentNullException(nameof(handle));

            return new Screen(handle);

        }

        /// <summary>Enumerates all screens on the users system.</summary>
        public static Screen[] All()
        {

            var l = new List<Screen>();
            bool EnumMonitor(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
            {
                var screen = FromScreenHandle(hMonitor);
                if (screen != null)
                    l.Add(screen);
                return true;
            }

            _ = EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, new EnumMonitors(EnumMonitor), IntPtr.Zero);

            return l.ToArray();

        }

        /// <summary>Gets the primary screen on the users system.</summary>
        public static Screen Primary() =>
            All().FirstOrDefault(s => s.IsPrimary);

        (string adapter, string name, int index) GetDisplayInfo()
        {

            var adapter = "";
            var device = new DISPLAY_DEVICE(0);

            var i = 0;
            while (EnumDisplayDevices(null, i, ref device, 0))
            {
                if (device.DeviceName.EndsWith(DeviceName))
                {
                    adapter = device.DeviceString;
                    break;
                }
                i += 1;
            }

            _ = EnumDisplayDevices(device.DeviceName, 0, ref device, 0);
            var deviceName = device.DeviceString;

            return (adapter, deviceName, i);

        }

        #endregion
        #region Properties

        /// <summary>The name of this screen.</summary>
        public string Name { get; }

        /// <summary>The device name of this screen.</summary>
        public string DeviceName { get; }

        /// <summary>The adapter (graphics card) that this screen is connected to.</summary>
        public string Adapter { get; }

        /// <summary>The bounds of this screen.</summary>
        public Rectangle Bounds { get; }

        /// <summary>The bounds of this screen, excluding reserved areas, like the taskbar.</summary>
        public Rectangle WorkArea { get; }

        /// <summary>Gets if this screen is set as the primary one.</summary>
        public bool IsPrimary { get; }

        /// <summary>The handle of this screen.</summary>
        public IntPtr Handle { get; }

        /// <summary>The index of this screen.</summary>
        public int Index { get; }

        #endregion

    }

}
