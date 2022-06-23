using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace ShellUtility.Windows.Utility;

/// <summary>
/// <para>An helper class to ensure that <see cref="DesktopWindowCollection"/> is synchronized with ui thread.</para>
/// <para>
/// If <see cref="Application.Current"/> is null and you are creating an <see cref="DesktopWindowCollection"/> from a thread different
/// to ui thread, please use <see cref="SetDispatcher(Dispatcher)"/> and pass ui thread <see cref="System.Windows.Threading.Dispatcher"/> to
/// make sure that all <see cref="DesktopWindowCollection"/> are synchronized with ui thread.
/// </para>
/// </summary>
public static class UIThreadHelper
{

    static UIThreadHelper() =>
        SetDispatcher(Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher);

    static object lockObj;
    static readonly List<DesktopWindowCollection> collectionsSynced = new List<DesktopWindowCollection>();

    /// <summary>The dispatcher that all <see cref="DesktopWindowCollection"/> are synchronized to.</summary>
    public static Dispatcher Dispatcher { get; private set; }

    /// <summary>Sets the dispatcher that all <see cref="DesktopWindowCollection"/> should be synchronized to. This will resynchronize all existing instances of <see cref="DesktopWindowCollection"/>.</summary>
    public static void SetDispatcher(Dispatcher dispatcher)
    {
        Dispatcher = dispatcher;
        dispatcher?.Invoke(() =>
        {
            lockObj = new object();
            foreach (var collection in collectionsSynced)
                BindingOperations.EnableCollectionSynchronization(collection, lockObj);
        });
    }

    /// <summary>Calls <see cref="BindingOperations.EnableCollectionSynchronization(System.Collections.IEnumerable, object)"/> for this <see cref="DesktopWindowCollection"/> with a lock object created on the thread that <see cref="Dispatcher"/> is associated with.</summary>
    public static void SynchronizeWithUIThread(this DesktopWindowCollection collection)
    {
        BindingOperations.EnableCollectionSynchronization(collection, lockObj);
        collectionsSynced.Add(collection);
    }

}
