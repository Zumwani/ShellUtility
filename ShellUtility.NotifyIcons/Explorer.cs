#pragma warning disable 0649

using System;
using System.Runtime.InteropServices;

namespace ShellUtility.NotifyIcons
{

    internal static class Explorer
    {

        public struct NOTIFYITEM
        {

            [MarshalAs(UnmanagedType.LPWStr)]
            public string exe_name;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string tip;

            public IntPtr icon;
            public IntPtr hwnd;
            public NOTIFYITEM_PREFERENCE preference;
            public uint id;
            public Guid guid;

        };

        public enum NOTIFYITEM_PREFERENCE
        {
            PREFERENCE_SHOW_WHEN_ACTIVE = 0,
            PREFERENCE_SHOW_NEVER = 1,
            PREFERENCE_SHOW_ALWAYS = 2
        };

        public enum NotifyIconMessage : int
        {
            NIM_ADD = 0x00000000,
            NIM_MODIFY = 0x00000001,
            NIM_DELETE = 0x00000002,
            NIM_SETFOCUS = 0x00000003,
            NIM_SETVERSION = 0x00000004,
        }

        [ComImport]
        [Guid("D782CCBA-AFB0-43F1-94DB-FDA3779EACCB")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface INotificationCb
        {
            void Notify([In] uint nEvent, [In] ref NOTIFYITEM notifyItem);
        }

        [ComImport]
        [Guid("D133CE13-3537-48BA-93A7-AFCD5D2053B4")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ITrayNotify
        {
            void RegisterCallback([MarshalAs(UnmanagedType.Interface)] INotificationCb callback, [Out] out ulong handle);
            void UnregisterCallback([In] ulong handle);
        }

        [ComImport, Guid("25DEAD04-1EAC-4911-9E3A-AD0A4AB560FD")]
        public class TrayNotify
        { }

        public sealed class NotifyIconNotifier : INotificationCb, IDisposable
        {

            public readonly Action<NOTIFYITEM, NotifyIconMessage> onNotify;
            public readonly ITrayNotify notifier;
            public readonly ulong handle;

            public NotifyIconNotifier(Action<NOTIFYITEM, NotifyIconMessage> onNotify)
            {
                try
                {
                    this.onNotify = onNotify;
                    notifier = (ITrayNotify)new TrayNotify();
                    notifier.RegisterCallback(this, out handle);
                }
                catch (Exception e)
                {
                    throw new Exception("Could not register callback.", e);
                }
            }

            public void Dispose()
            {
                if (notifier != null)
                {
                    try
                    {
                        notifier.UnregisterCallback(handle);
                        Marshal.ReleaseComObject(notifier);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Could not unregister callback", e);
                    }
                }
            }

            void INotificationCb.Notify([In] uint nEvent, [In] ref NOTIFYITEM notifyItem)
            {
                onNotify?.Invoke(notifyItem, (NotifyIconMessage)nEvent);
            }

        }

    }

}
