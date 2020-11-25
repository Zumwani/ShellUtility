using System;
using System.Windows.Threading;

namespace ShellUtility.Windows
{

    public static class Poller
    {

        public static TimeSpan Time
        {
            get => timer.Interval;
            set => timer.Interval = value;
        }

        private static readonly DispatcherTimer timer = new DispatcherTimer(TimeSpan.FromSeconds(0.5), DispatcherPriority.Background, (s, e) => Update?.Invoke(), Dispatcher.CurrentDispatcher);

        public static event Action Update;

    }

}
