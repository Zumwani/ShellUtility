using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace ShellUtility.NotifyIcons
{

    public enum PinStatus
    {
        /// <summary>Shows the icon in the pinned area when it is displaying an notification.</summary>
        WhenActive = Explorer.NOTIFYITEM_PREFERENCE.PREFERENCE_SHOW_WHEN_ACTIVE,
        /// <summary>The icon is not pinned, and will only show in the overflow area.</summary>
        NotPinned = Explorer.NOTIFYITEM_PREFERENCE.PREFERENCE_SHOW_NEVER,
        /// <summary>The icon is pinned on the taskbar.</summary>
        Pinned = Explorer.NOTIFYITEM_PREFERENCE.PREFERENCE_SHOW_ALWAYS
    }

    public enum NotifyIconInvokeAction
    {
        LeftClick, RightClick, DoubleClick, MouseMove
    }

    /// <summary>Represents an icon that is displayed on the notification area of the taskbar.</summary>
    public class NotifyIcon : INotifyPropertyChanged
    {

        internal NotifyIcon(string path, string tooltip, BitmapSource icon, PinStatus pinStatus, IntPtr handle, uint uid, Guid guid)
        {
            Path = path;
            Tooltip = tooltip;
            Icon = icon;
            PinStatus = pinStatus;
            Handle = handle;
            ID = uid;
            GUID = guid;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion
        #region Properties

        private string path;
        private uint id;
        private Guid guid;
        private IntPtr handle;
        private BitmapSource icon;
        private string tooltip;
        private PinStatus pinStatus;
        private uint callbackMessage;
        private int callbackParam;

        /// <summary>The id of the icon, used internally within the creating app to identify multiple icons.</summary>
        public uint ID
        {
            get => id;
            internal set { id = value; OnPropertyChanged(); }
        }

        /// <summary>The guid of the icon, used internally within the creating app to identify multiple icons.</summary>
        public Guid GUID
        {
            get => guid;
            internal set { guid = value; OnPropertyChanged(); }
        }

        /// <summary>The path to the process of the icon.</summary>
        public string Path
        {
            get => path;
            internal set { path = value; OnPropertyChanged(); }
        }

        /// <summary>The hwnd of the icon.</summary>
        public IntPtr Handle
        {
            get => handle;
            internal set { handle = value; OnPropertyChanged(); }
        }

        internal IntPtr iconHandle;
        /// <summary>The icon of the icon.</summary>
        public BitmapSource Icon
        {
            get => icon;
            internal set { icon = value; OnPropertyChanged(); }
        }

        /// <summary>The tooltip of the icon.</summary>
        public string Tooltip
        {
            get => tooltip;
            internal set { tooltip = value; OnPropertyChanged(); }
        }

        /// <summary>The visibility of the icon.</summary>
        public PinStatus PinStatus
        {
            get => pinStatus;
            internal set { pinStatus = value; OnPropertyChanged(); }
        }

        /// <summary>The callback message (nMsg) which is passed when invoking icon.</summary>
        public uint CallbackMessage
        {
            get => callbackMessage;
            set { callbackMessage = value; OnPropertyChanged(); }
        }

        /// <summary>The callback parameter (wParam) which is passed when invoking icon.</summary>
        public int CallbackParam
        {
            get => callbackParam;
            set { callbackParam = value; OnPropertyChanged(); }
        }

        #endregion

        public override string ToString() =>
            Path;

        /// <summary>Invokes the selected action on the icon.</summary>
        public void Invoke(NotifyIconInvokeAction action) =>
            CallbackUtility.Simulate(this, action);

    }

}
