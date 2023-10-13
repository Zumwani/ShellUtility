using System.Windows;
using System.Windows.Threading;

namespace ShellUtility.NotifyIcons.Utility
{

    public static class UIThreadHelper
    {

        static UIThreadHelper() =>
            SetDispatcher(Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher);

        /// <summary>The dispatcher that all <see cref="DesktopWindowCollection"/> are synchronized to.</summary>
        public static Dispatcher Dispatcher { get; private set; }

        /// <summary>Sets the dispatcher that all <see cref="DesktopWindowCollection"/> should be synchronized to. This will resynchronize all existing instances of <see cref="DesktopWindowCollection"/>.</summary>
        public static void SetDispatcher(Dispatcher dispatcher) =>
            Dispatcher = dispatcher;

    }

}
