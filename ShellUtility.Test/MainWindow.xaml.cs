using MahApps.Metro.Controls;
using ShellUtility.NotifyIcons;
using ShellUtility.TaskbarVisibility;
using ShellUtility.Windows;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ShellUtility.Test
{

    public partial class MainWindow : MetroWindow
    {

        public MainWindow() =>
            InitializeComponent();

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e) =>
            ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset - e.Delta);

        #region Windows

        private void Window_Expanded(object sender, RoutedEventArgs e)
        {
            WindowList.ItemsSource = new Windows.DesktopWindowCollection();
        }

        #endregion
        #region Notify icons

        private void Invoke(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && NotifyIconList.SelectedItem is NotifyIcon icon && item.Tag is NotifyIconInvokeAction action)
                icon.Invoke(action);
        }

        private static readonly object _lock = new object();
        NotifyIconCollection collection;

        private void NotifyIcons_Expanded(object sender, RoutedEventArgs e)
        {
            if (NotifyIconList.DataContext is CollectionViewSource source)
            {
                if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                {
                    collection?.Dispose();
                    collection = new NotifyIconCollection();
                    BindingOperations.EnableCollectionSynchronization(collection, _lock);
                    App.Current.Dispatcher.Invoke(() => source.Source = collection);
                }
            }
        }

        #endregion
        #region Screens

        private void Screens_Expanded(object sender, RoutedEventArgs e)
        {
            ScreensList.ItemsSource = Screens.Screen.All();
        }

        #endregion
        #region Taskbar

        private void Taskbar_Expanded(object sender, RoutedEventArgs e)
        {

            TaskbarVisibleToggle.IsOn = Taskbar.IsVisible;
            Closing -= OnClose;
            Closing += OnClose;

            static void OnClose(object sender, CancelEventArgs e)
            {
                if (!Taskbar.IsVisible)
                    switch (MessageBox.Show("The taskbar is hidden, do you wish to restore it?", "Taskbar hidden", MessageBoxButton.YesNoCancel))
                    {
                        case MessageBoxResult.Yes:
                            Taskbar.Show(); break;
                        case MessageBoxResult.Cancel:
                            e.Cancel = true; break;
                    }
            }

        }

        private void TaskbarVisibleToggle_Toggled(object sender, RoutedEventArgs e) =>
            Taskbar.IsVisible = TaskbarVisibleToggle.IsOn;

        #endregion

        private void ShowPreview_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is DesktopWindow window)
                new PreviewWindow(window).Show();
        }

        private void ShowWindow(object sender, RoutedEventArgs e) =>
            (WindowList.SelectedItem as DesktopWindow)?.Show();

        private void ActivateWindow(object sender, RoutedEventArgs e) =>
            (WindowList.SelectedItem as DesktopWindow)?.Activate();

        private void MinimizeWindow(object sender, RoutedEventArgs e) =>
            (WindowList.SelectedItem as DesktopWindow)?.Minimize();

        private void CloseWindow(object sender, RoutedEventArgs e) =>
            (WindowList.SelectedItem as DesktopWindow)?.Close();

        private void OpenNewInstanceOfWindow(object sender, RoutedEventArgs e) =>
            (WindowList.SelectedItem as DesktopWindow)?.OpenNewInstance(out _);

    }

}
