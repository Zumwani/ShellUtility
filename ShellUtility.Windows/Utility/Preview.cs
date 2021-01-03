using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.ComponentModel;
using System.Windows.Markup;

namespace ShellUtility.Windows.Utility
{

    /// <summary>
    /// <para>Manages window previews.</para>
    /// <para>Usage: <see cref="DesktopWindow.Preview"/>.</para>
    /// </summary>
    public class Preview : IDisposable
    {

        #region Pinvoke

        [DllImport("dwmapi.dll")]
        static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);

        [DllImport("dwmapi.dll")]
        static extern int DwmUnregisterThumbnail(IntPtr thumb);

        [DllImport("dwmapi.dll")]
        static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out PSIZE size);
        
        [DllImport("dwmapi.dll")]
        static extern int DwmUpdateThumbnailProperties(IntPtr hThumb, ref DWM_THUMBNAIL_PROPERTIES props);

        static readonly int DWM_TNP_VISIBLE = 0x8;
        static readonly int DWM_TNP_RECTDESTINATION = 0x1;

        [StructLayout(LayoutKind.Sequential)]
        struct PSIZE
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DWM_THUMBNAIL_PROPERTIES
        {
            public int dwFlags;
            public Rect rcDestination;
            public Rect rcSource;
            public byte opacity;
            public bool fVisible;
            public bool fSourceClientAreaOnly;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Rect
        {

            public Rect(int x, int y, int width, int height)
            {
                Left = x;
                Top = y;
                Right = x + width;
                Bottom = y + height;
            }

            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

        }

        #endregion

        readonly IntPtr window;

        internal Preview(IntPtr window) =>
            this.window = window;

        readonly Dictionary<FrameworkElement, IntPtr> registered = new Dictionary<FrameworkElement, IntPtr>();

        /// <inheritdoc cref="Register(FrameworkElement)"/>
        public static void Register(FrameworkElement element, DesktopWindow window) =>
            window.Preview.Register(element);

        /// <inheritdoc cref="Unregister(FrameworkElement)"/>
        public static void Unregister(FrameworkElement element, DesktopWindow window) =>
            window.Preview.Unregister(element);

        /// <inheritdoc cref="Update(FrameworkElement)"/>
        public static void Update(FrameworkElement element, DesktopWindow window) =>
            window.Preview.Update(element);

        /// <summary>Registers an element to display a preview for a window.</summary>
        public void Register(FrameworkElement element)
        {

            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (registered.ContainsKey(element))
                Unregister(element);

            var elementWindow = Window.GetWindow(element);
            if (elementWindow == null)
                throw new ArgumentException("The element must be hosted in a window.");

            var handle = new WindowInteropHelper(elementWindow).EnsureHandle();

            if (DwmRegisterThumbnail(handle, window, out var thumb) != 0)
                throw new Win32Exception();

            registered.Add(element, thumb);
            Update(element);

            element.Unloaded += Element_Unloaded;
            element.SizeChanged += Element_SizeChanged;

        }

        /// <summary>
        /// <para>Updates the window preview for an element.</para>
        /// <para>Registers the element if necessary.</para>
        /// <para>This is automatically called on <see cref="FrameworkElement.SizeChanged"/> for element registered through <see cref="Register(FrameworkElement)"/> and attached property.</para>
        /// </summary>
        public void Update(FrameworkElement element)
        {

            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (!registered.TryGetValue(element, out var thumb))
            {
                Register(element);
                return;
            }

            var elementWindow = Window.GetWindow(element);
            if (elementWindow == null)
                throw new ArgumentException("The element must be hosted in a window.");

            if (DwmQueryThumbnailSourceSize(thumb, out var size) != 0)
                throw new Win32Exception();

            var pos = element.TranslatePoint(new Point(), elementWindow);
            var props = new DWM_THUMBNAIL_PROPERTIES
            {
                dwFlags = DWM_TNP_VISIBLE | DWM_TNP_RECTDESTINATION,
                fVisible = true,
                rcDestination = new Rect((int)pos.X, (int)pos.Y, (int)element.ActualWidth, (int)element.ActualHeight)
            };

            if (size.x < element.ActualWidth)
                props.rcDestination.Right = props.rcDestination.Left + size.x;
            if (size.y < element.ActualHeight)
                props.rcDestination.Bottom = props.rcDestination.Top + size.y;

            if (DwmUpdateThumbnailProperties(thumb, ref props) != 0)
                throw new Win32Exception();

        }

        /// <summary>
        /// <para>Unregisters an element to display a preview for a window.</para>
        /// <para>This is done automatically on <see cref="FrameworkElement.Unloaded"/> for elements registered through <see cref="Register(FrameworkElement)"/> and attached property.</para>
        /// </summary>
        public void Unregister(FrameworkElement element)
        {

            if (element == null)
                throw new ArgumentNullException(nameof(element));

            element.Unloaded -= Element_Unloaded;
            element.SizeChanged -= Element_SizeChanged;

            if (registered.TryGetValue(element, out var thumb))
            {
                registered.Remove(element);
                if (DwmUnregisterThumbnail(thumb) != 0)
                    throw new Win32Exception();
            }

        }

        void Element_SizeChanged(object sender, SizeChangedEventArgs e) =>
            Update((FrameworkElement)sender);

        void Element_Unloaded(object sender, RoutedEventArgs e) =>
            Unregister((FrameworkElement)sender);

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
            foreach (var item in registered.Keys.ToArray())
                if (registered.ContainsKey(item))
                    Unregister(item);
        }

    }

}
