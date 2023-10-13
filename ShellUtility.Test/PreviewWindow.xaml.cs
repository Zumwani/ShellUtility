using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using ShellUtility.Windows.Models;

namespace ShellUtility.Test
{

    public partial class PreviewWindow : Window
    {

        public static readonly DependencyProperty CloseButtonRectProperty =
            DependencyProperty.Register("CloseButtonRect", typeof(Rect), typeof(PreviewWindow), new PropertyMetadata(null));

        public Rect CloseButtonRect
        {
            get => (Rect)GetValue(CloseButtonRectProperty);
            set => SetValue(CloseButtonRectProperty, value);
        }

        public DesktopWindow Window { get; }

        public PreviewWindow(DesktopWindow window)
        {
            Window = window;
            InitializeComponent();
            UpdateCloseButtonRect();
            Show();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Popup.IsOpen = false;
            DragMove();
            UpdateCloseButtonRect();
            Popup.IsOpen = true;
        }

        private async void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
                Border.Margin = new Thickness(8);
                MinHeight = Screens.Screen.FromWindowHandle(new WindowInteropHelper(this).Handle).Bounds.Height;
                Popup.HorizontalOffset = -16;
            }
            else
            {
                MinHeight = 64;
                WindowState = WindowState.Normal;
                Border.Margin = new Thickness(0);
                Popup.HorizontalOffset = 0;
            }

            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(100);
                UpdateCloseButtonRect();
            }

        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) =>
            UpdateCloseButtonRect();

        void UpdateCloseButtonRect()
        {

            CloseButtonRect = new Rect(ActualWidth - 6, 2, 0, 0);
            if (Popup == null)
                return;

            var mode = Popup.Placement;
            Popup.Placement = PlacementMode.Relative;
            Popup.Placement = mode;

        }

        private void Close(object sender, RoutedEventArgs e) =>
            Close();

    }

}
