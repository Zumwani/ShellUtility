using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Automation;

namespace ShellUtility.Windows
{

    public class WindowCollection : ReadOnlyObservableCollection<Window>, INotifyCollectionChanged, INotifyPropertyChanged
    {

        #region Events

        /// <summary>Occurs when an icon is added.</summary>
        public event Action<Window> ItemAdded;

        /// <summary>Occurs when an icon is removed.</summary>
        public event Action<Window> ItemRemoved;


        //CollectionChanged and PropertyChanged events are protected in base class, we want them to be public so lets create some proxy events

        /// <inheritdoc cref="ObservableCollection{T}.CollectionChanged">
        public new event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => base.CollectionChanged += value;
            remove => base.CollectionChanged -= value;
        }

        ///// <inheritdoc cref="ObservableCollection{T}.PropertyChanged">
        //public new event PropertyChangedEventHandler PropertyChanged
        //{
        //    add { base.PropertyChanged += value; }
        //    remove { base.PropertyChanged -= value; }
        //}

        #endregion

        public WindowCollection() : base(new ObservableCollection<Window>())
        {

            //Get existing windows
            var windows = WindowUtility.Enumerate();
            foreach (var window in windows)
                Items.Add(window);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            //Add window open handler through ui automation, since polling WindowUtility.Enumerate every update is unnecessarily costly
            Automation.AddAutomationEventHandler(WindowPattern.WindowOpenedEvent, AutomationElement.RootElement, TreeScope.Subtree, WindowCreated);

            //Regular properties and window closing we'll just check every update
            Poller.Update += Update;
            
        }

        private void Update()
        {

            foreach (var window in Items)
            {

                if (!window.IsOpen)
                {
                    Items.Remove(window);
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, window));
                }
                else
                    window.Update();

            }

        }

        void WindowCreated(object sender, AutomationEventArgs e)
        {
            if (sender is AutomationElement element && 
                element?.Current.NativeWindowHandle is int handle)
                {
                    var window = new Window((IntPtr)handle);
                    Items.Add(window);
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, window));
                }
        }

    }

}
