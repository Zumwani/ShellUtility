using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ShellUtility.Windows.Models;
using ShellUtility.Windows.Utility;

namespace ShellUtility.Windows.Collections;

/// <summary>A collection of desktop windows. Automatically updates when windows open or close.</summary>
public class DesktopWindowCollection : ReadOnlyObservableCollection<DesktopWindow>, INotifyCollectionChanged, INotifyPropertyChanged
{

    public DesktopWindowCollection() : base(new())
    {

        //Make sure that the collection is synchronized with ui thread. 
        this.SynchronizeWithUIThread();

        InitialUpdate();

        HookUtility.AddHook(HookUtility.Event.OBJECT_CREATE, Add);
        HookUtility.AddHook(HookUtility.Event.OBJECT_DESTROY, OnWindowDestroyed);

        Poller.Update += Update;
        Update();

    }

    async void InitialUpdate()
    {

        IntPtr[] handles = Array.Empty<IntPtr>();
        await Task.Run(() =>
        {
            //Get existing windows
            handles = WindowUtility.EnumerateHandles().ToArray();
        });

        AddRange(handles.Select(DesktopWindow.FromHandle).OfType<DesktopWindow>());
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null));

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

    void Update()
    {

        foreach (var window in Items.ToArray())
            window.Update();

        foreach (var window in hiddenWindows.ToArray())
            window.Update();

    }

    /// <inheritdoc cref="ReadOnlyCollection{T}.Contains(T)"/>
    public bool Contains(IntPtr handle) =>
        this.Any(w => w.Handle == handle);

    #region Hidden windows

    readonly List<DesktopWindow> hiddenWindows = new();

    /// <summary>Gets the tracked windows that are not visible in taskbar.</summary>
    public IEnumerable<DesktopWindow> HiddenWindows => hiddenWindows;

    void OnWindowIsVisibleInTaskbarChanged(DesktopWindow window)
    {
        Remove(window);
        Add(window);
    }

    #endregion
    #region Events

    /// <summary>Occurs when an icon is added.</summary>
    public event Action<DesktopWindow>? ItemAdded;

    /// <summary>Occurs when an icon is removed.</summary>
    public event Action<DesktopWindow>? ItemRemoved;


    //CollectionChanged event is protected in base class, we need it to be public

    /// <inheritdoc cref="ObservableCollection{T}.CollectionChanged">
    public new event NotifyCollectionChangedEventHandler? CollectionChanged
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

    void AddRange(IEnumerable<DesktopWindow> windows, bool notify = true) =>
        UIThreadHelper.Dispatcher?.Invoke(() =>
        {

            foreach (var window in windows)
                if (!Contains(window))
                {
                    window.IsVisibleInTaskbarChanged += OnWindowIsVisibleInTaskbarChanged;
                    if (!window.IsVisibleInTaskbar)
                        hiddenWindows.Add(window);
                    else
                    {
                        Items.Add(window);
                        ItemAdded?.Invoke(window);
                        foreach (var callback in addCallbacks)
                            callback?.Invoke(window);
                    }
                }

            if (notify)
            {
                var action = windows.Count() == 1 ? NotifyCollectionChangedAction.Add : NotifyCollectionChangedAction.Reset;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, action == NotifyCollectionChangedAction.Add ? windows : null));
            }

        });

    void Remove(IntPtr handle)
    {
        var window = Items.ToArray().FirstOrDefault(w => w.Handle == handle);
        if (window != null)
            Remove(window);
    }

    void Remove(DesktopWindow window) =>
        UIThreadHelper.Dispatcher?.Invoke(() =>
        {

            if (window is null)
                return;

            if (Items.Contains(window))
                if (Items.Remove(window))
                {

                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, window));

                    ItemRemoved?.Invoke(window);
                    foreach (var callback in removeCallbacks)
                        callback?.Invoke(window);

                }

            if (hiddenWindows.Contains(window))
                _ = hiddenWindows.Remove(window);

            window.IsVisibleInTaskbarChanged -= OnWindowIsVisibleInTaskbarChanged;

        });

    #endregion
    #region Callbacks

    readonly List<Action<DesktopWindow>> addCallbacks = new();
    readonly List<Action<DesktopWindow>> removeCallbacks = new();

    /// <summary>Adds or removes a callback for when a window has been added to the collection.</summary>
    public void OnWindowAdded(Action<DesktopWindow> callback, bool enabled = true)
    {
        if (enabled && !addCallbacks.Contains(callback))
            addCallbacks.Add(callback);
        else if (!enabled)
            _ = addCallbacks.Remove(callback);
    }

    /// <summary>Adds or removes a callback for when a window has been removed from the collection.</summary>
    public void OnWindowRemoved(Action<DesktopWindow> callback, bool enabled = true)
    {
        if (enabled && !removeCallbacks.Contains(callback))
            removeCallbacks.Add(callback);
        else if (!enabled)
            _ = removeCallbacks.Remove(callback);
    }

    #endregion

}
