using System;
using System.Windows.Threading;

namespace ShellUtility.Windows.Utility;

/// <summary>An helper class for polling.</summary>
public static class Poller
{

    /// <summary>The interval to invoke <see cref="Update"/> at.</summary>
    public static TimeSpan Time
    {
        get => timer.Interval;
        set => timer.Interval = value;
    }

    /// <summary>Enables or disables polling, if disabled then <see cref="DesktopWindow.Update"/> must be called manually.</summary>
    public static bool IsEnabled
    {
        get => timer.IsEnabled;
        set => timer.IsEnabled = value;
    }

    private static readonly DispatcherTimer timer = new DispatcherTimer(TimeSpan.FromSeconds(0.5), DispatcherPriority.Background, (s, e) => Update?.Invoke(), Dispatcher.CurrentDispatcher);

    /// <summary>Occurs every <see cref="Time"/> intervals, if <see cref="IsEnabled"/>.</summary>
    public static event Action? Update;

}
