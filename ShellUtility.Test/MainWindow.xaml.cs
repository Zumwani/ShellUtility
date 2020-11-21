using MahApps.Metro.Controls;
using ShellUtility.NotifyIcons;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ShellUtility.Test
{

    public partial class MainWindow : MetroWindow
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset - e.Delta);
        }

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

        private void Screens_Expanded(object sender, RoutedEventArgs e)
        {
          ScreensList.ItemsSource = Screens.Screen.All();
        }

    }

}
