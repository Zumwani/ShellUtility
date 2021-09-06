using ShellUtility.Windows.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace ShellUtility.Windows
{

    /// <summary>A collection of desktop windows. Automatically updates when windows open or close.</summary>
    public class DesktopWindowCollection : ReadOnlyObservableCollection<DesktopWindow>, INotifyCollectionChanged, INotifyPropertyChanged
    {

        /// <summary>Contains the windows that could be shown, but isn't due to properties that may change.</summary>
        readonly List<DesktopWindow> standbyWindows = new List<DesktopWindow>();

        public DesktopWindowCollection() : base(new ObservableCollection<DesktopWindow>())
        {

            //Make sure that the collection is synchronized with ui thread. 
            this.SynchronizeWithUIThread();

            //Get existing windows
            AddRange(WindowUtility.Enumerate());

            HookUtility.AddHook(HookUtility.Event.OBJECT_CREATE, Add);
            HookUtility.AddHook(HookUtility.Event.OBJECT_DESTROY, OnWindowDestroyed);

            Poller.Update += Update;
            Update();

        }

        ~DesktopWindowCollection()
        {
            HookUtility.RemoveHook(HookUtility.Event.OBJECT_CREATE, Add);
            HookUtility.RemoveHook(HookUtility.Event.OBJECT_DESTROY, OnWindowDestroyed);
            Poller.Update -= Update;
        }

        void OnWindowDestroyed(IntPtr handle)
        {
            //Event.OBJECT_DESTROY is called for child windows, but handle is still refers to root window,
            //adding this check makes sure we don't remove window prematurely
            if (!WindowUtility.IsDesktopWindow(handle))
                Remove(handle);
        }

        private void Update()
        {

            foreach (var window in Items.ToArray())
                window.Update();

            foreach (var window in standbyWindows.ToArray())
                window.Update();

        }

        void OnWindowIsVisibleInTaskbarChanged(DesktopWindow window)
        {
            Remove(window);
            Add(window);
        }

        #region Events

        /// <summary>Occurs when an icon is added.</summary>
        public event Action<DesktopWindow> ItemAdded;

        /// <summary>Occurs when an icon is removed.</summary>
        public event Action<DesktopWindow> ItemRemoved;


        //CollectionChanged event is protected in base class, we need it to be public

        /// <inheritdoc cref="ObservableCollection{T}.CollectionChanged">
        public new event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => base.CollectionChanged += value;
            remove => base.CollectionChanged -= value;
        }

        #endregion
        #region Add remove

        void Add(IntPtr handle) =>
            Add(DesktopWindow.FromHandle(handle));

        void Add(DesktopWindow window) =>
            AddRange(window);

        void AddRange(params DesktopWindow[] window) =>
            AddRange(windows: window);

        void AddRange(IEnumerable<DesktopWindow> windows)
        {

            foreach (var w in windows)
            //if (!Items.Contains(w))
            {
                w.IsVisibleInTaskbarChanged += OnWindowIsVisibleInTaskbarChanged;
                if (w.IsVisibleInTaskbar)
                {
                    Items.Add(w);
                    ItemAdded?.Invoke(w);
                }
                else
                    standbyWindows.Add(w);
            }

            var action = windows.Count() == 1 ? NotifyCollectionChangedAction.Add : NotifyCollectionChangedAction.Reset;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, action == NotifyCollectionChangedAction.Add ? windows : null));

        }

        void Remove(IntPtr handle)
        {
            var window = Items.ToArray().FirstOrDefault(w => w.Handle == handle);
            if (window != null)
                Remove(window);
        }

        void Remove(DesktopWindow window)
        {

            if (Items.Contains(window))
            {
                Items.Remove(window);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, window));
                ItemRemoved?.Invoke(window);
            }

            if (standbyWindows.Contains(window))
                standbyWindows.Remove(window);

            window.IsVisibleInTaskbarChanged -= OnWindowIsVisibleInTaskbarChanged;

        }

        #endregion

    }

}
