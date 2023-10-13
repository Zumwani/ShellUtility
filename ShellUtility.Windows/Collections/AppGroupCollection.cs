using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using ShellUtility.Windows.Models;

namespace ShellUtility.Windows.Collections;

/// <summary>A collection of desktop windows that have been grouped by app. Automatically updates when windows open or close.</summary>
public class AppGroupCollection : ReadOnlyObservableCollection<AppGroup>, INotifyCollectionChanged, INotifyPropertyChanged
{

    readonly DesktopWindowCollection windows = new();

    public AppGroupCollection() : base(new())
    {
        windows.OnWindowAdded(OnWindowAdded);
        windows.OnWindowRemoved(OnWindowRemoved);
    }

    /// <inheritdoc cref="ReadOnlyCollection{T}.Contains(T)"/>
    public bool Contains(DesktopWindow window) =>
        windows.Contains(window);

    /// <summary>Gets a <see cref="AppGroup"/>, if one exists.</summary>
    public bool GetGroup(DesktopWindow window, [NotNullWhen(true)] out AppGroup? combined) =>
        (combined = this.FirstOrDefault(w => w.AppPath == window.ProcessPath)) is not null;

    void OnWindowAdded(DesktopWindow window)
    {
        if (GetGroup(window, out var combined))
            combined.windows.Add(window);
        else
        {

            if (!File.Exists(window.ProcessPath))
                return;

            combined = new AppGroup(window.ProcessPath);
            combined.windows.Add(window);

            Items.Add(combined);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, combined));

        }
    }

    void OnWindowRemoved(DesktopWindow window)
    {
        if (GetGroup(window, out var combined))
            if (combined.windows.Remove(window) && !combined.windows.Any())
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, combined));
                _ = Items.Remove(combined);
            }
    }

}
