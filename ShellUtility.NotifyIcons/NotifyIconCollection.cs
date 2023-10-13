using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using static ShellUtility.NotifyIcons.Explorer;

namespace ShellUtility.NotifyIcons
{

    /// <summary>A collection of icons. Automatically updated when icons are added or removed, and provides event for when an icon is updated.</summary>
    public sealed class NotifyIconCollection : ReadOnlyObservableCollection<NotifyIcon>, IDisposable, INotifyCollectionChanged, INotifyPropertyChanged
    {

        #region Events


        /// <summary>Occurs when an icon is added.</summary>
        public event Action<NotifyIcon> ItemAdded;

        /// <summary>Occurs when an icon is removed.</summary>
        public event Action<NotifyIcon> ItemRemoved;

        /// <summary>Occurs when an icon is updated.</summary>
        public event Action<NotifyIcon> ItemUpdated;


        //CollectionChanged and PropertyChanged events are protected in base class, we want them to be public so lets create some proxy events

        /// <inheritdoc cref="ObservableCollection{T}.CollectionChanged">
        public new event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => base.CollectionChanged += value;
            remove => base.CollectionChanged -= value;
        }

        /// <inheritdoc cref="ObservableCollection{T}.PropertyChanged">
        public new event PropertyChangedEventHandler PropertyChanged
        {
            add { base.PropertyChanged += value; }
            remove { base.PropertyChanged -= value; }
        }


        #endregion

        readonly NotifyIconNotifier notifier;

        [DllImport("shell32.dll")]
        static extern bool Shell_NotifyIcon(uint dwMessage, [In] ref NOTIFYITEM pnid);

        public NotifyIconCollection() : base(new ObservableCollection<NotifyIcon>()) =>
            notifier = new NotifyIconNotifier(OnNotify);

        /// <summary>Finds an icon with a specified path.</summary>
        public bool Find(string path, out NotifyIcon icon)
        {
            icon = Items.FirstOrDefault(i => i.Path == path);
            return !string.IsNullOrEmpty(icon?.Path ?? null);
        }

        //Called when an icon is added, removed or modified in explorer.exe
        void OnNotify(NOTIFYITEM item, NotifyIconMessage action)
        {

            var path = PathUtility.ExpandPath(item.exe_name);

            if (item.hwnd == IntPtr.Zero)
                return;

            if (action == NotifyIconMessage.NIM_ADD)
            {

                var icon = new NotifyIcon(
                    path,
                    tooltip: item.tip,
                    icon: null,
                    pinStatus: (PinStatus)item.preference,
                    handle: item.hwnd, item.id, item.guid)
                {
                    Icon = IconUtility.IconFromResourceHandle(item.icon, path)
                };

                CallbackMessageUtility.GetCallbackMessages(ref icon);

                Items.Add(icon);
                ItemAdded?.Invoke(icon);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, icon));

            }
            else if (action == NotifyIconMessage.NIM_DELETE && Find(path, out var deletedIcon))
            {

                _ = Items.Remove(deletedIcon);
                ItemRemoved?.Invoke(deletedIcon);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, deletedIcon));

            }
            else if (action is NotifyIconMessage.NIM_MODIFY or NotifyIconMessage.NIM_SETFOCUS or NotifyIconMessage.NIM_SETVERSION)
            {

                var icon = Items.FirstOrDefault(i => i?.Path == path);
                if (icon is null)
                    return;

                icon.Tooltip = item.tip;
                icon.PinStatus = (PinStatus)item.preference;

                if (icon.iconHandle != item.icon)
                    icon.Icon = IconUtility.IconFromResourceHandle(item.icon, path);
                icon.iconHandle = item.icon;

                CallbackMessageUtility.GetCallbackMessages(ref icon);

                ItemUpdated?.Invoke(icon);

            }

        }

        public void Dispose() =>
            notifier?.Dispose();

    }

}
