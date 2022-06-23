using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Image = System.Windows.Controls.Image;

namespace ShellUtility.Windows;

partial class DesktopWindow
{

    /// <summary>
    /// <para>(attached property) Gets the <see cref="DesktopWindow"/> that this framework element has registered to display a preview for.</para>
    /// </summary>
    public static DesktopWindow GetRegisterPreview(FrameworkElement obj) =>
        (DesktopWindow)obj?.GetValue(RegisterPreviewProperty);

    /// <summary>
    /// <para>(attached property) Sets the <see cref="DesktopWindow"/> that this framework element should display a preview for.</para>
    /// </summary>
    public static void SetRegisterPreview(FrameworkElement obj, DesktopWindow value) =>
        obj?.SetValue(RegisterPreviewProperty, value);

    /// <summary>(attached property) Gets or sets the <see cref="DesktopWindow"/> that this framework element should display a preview for.</summary>
    public static readonly DependencyProperty RegisterPreviewProperty =
        DependencyProperty.RegisterAttached("RegisterPreview", typeof(DesktopWindow), typeof(DesktopWindow), new PropertyMetadata(null, OnRegisterPreviewChanged));

    static void OnRegisterPreviewChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            (e.OldValue as DesktopWindow)?.Preview?.Unregister(element);
            (e.NewValue as DesktopWindow)?.Preview?.Register(element);
        }
    }

}


public class WindowPreview : Viewbox
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

    public DesktopWindow Window
    {
        get => (DesktopWindow)GetValue(WindowProperty);
        set => SetValue(WindowProperty, value);
    }

    public static readonly DependencyProperty WindowProperty =
        DependencyProperty.Register("Window", typeof(DesktopWindow), typeof(WindowPreview), new PropertyMetadata(null, OnWindowChanged));

    static void OnWindowChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {

        if (sender is not WindowPreview preview)
            return;

        preview.Unregister();
        if (e.NewValue is DesktopWindow window)
            preview.Update(window);

    }

    public static BitmapSource DefaultImage { get; } =
        Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Application.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

    readonly Viewbox viewbox;
    readonly Border border;
    public WindowPreview()
    {
        Child = viewbox = new() { Stretch = Stretch.UniformToFill };
        viewbox.Child = border = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        border.Child = new Image() { Source = DefaultImage, Width = 42, Height = 42, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        Loaded += WindowPreview_Loaded;
    }

    void WindowPreview_Unloaded(object sender, RoutedEventArgs e)
    {
        SizeChanged -= WindowPreview_SizeChanged;
        LayoutUpdated -= WindowPreview_LayoutUpdated;
        Loaded -= WindowPreview_Loaded;
        Unloaded -= WindowPreview_Unloaded;
        Unregister();
    }

    void WindowPreview_Loaded(object sender, RoutedEventArgs e)
    {
        SizeChanged += WindowPreview_SizeChanged;
        LayoutUpdated += WindowPreview_LayoutUpdated;
        Unloaded += WindowPreview_Unloaded;
        Update();
    }

    void WindowPreview_LayoutUpdated(object sender, EventArgs e) =>
        Update();

    void WindowPreview_SizeChanged(object sender, SizeChangedEventArgs e) =>
        Update();

    static bool Succeeded(int returnValue) =>
        returnValue is 0 or (-2147024809); //What is this? An error with marshalling or casting types? Throwing Win32Exception says operation completed successfully?

    Window parentWindow;
    IntPtr windowHandle;
    IntPtr thumbHandle;

    void Register(DesktopWindow window)
    {

        if (windowHandle == IntPtr.Zero || !parentWindow.IsLoaded)
        {
            if (System.Windows.Window.GetWindow(this) is not Window parentWindow)
                throw new InvalidOperationException("Cannot display preview unless attached to a window.");
            this.parentWindow = parentWindow;
            windowHandle = new WindowInteropHelper(parentWindow).Handle;

        }

        if (thumbHandle == IntPtr.Zero)
            if (!Succeeded(DwmRegisterThumbnail(windowHandle, window.Handle, out thumbHandle)))
                throw new Win32Exception();

    }

    void Unregister()
    {
        if (thumbHandle != IntPtr.Zero && !Succeeded(DwmUnregisterThumbnail(thumbHandle)))
            throw new Win32Exception();
        thumbHandle = IntPtr.Zero;
    }

    void Update(DesktopWindow window = null)
    {

        if (PresentationSource.FromVisual(border) is null)
            return;

        window ??= Window;

        if (window is null)
            return;

        if (DesignerProperties.GetIsInDesignMode(this))
            return;

        Register(window);

        var scale = ((ScaleTransform)((ContainerVisual)VisualTreeHelper.GetChild(viewbox, 0)).Transform);
        var min = border.TranslatePoint(new(), parentWindow);
        var max = parentWindow.PointFromScreen(border.PointToScreen(new(border.ActualWidth * scale.ScaleX, border.ActualHeight * scale.ScaleY)));

        var props = new DWM_THUMBNAIL_PROPERTIES
        {
            dwFlags = DWM_TNP_VISIBLE | DWM_TNP_RECTDESTINATION,
            fVisible = true,
            rcDestination = new Rect((int)Math.Floor(min.X), (int)Math.Floor(min.Y), (int)Math.Ceiling(max.X), (int)Math.Ceiling(max.Y))
        };

        if (!Succeeded(DwmUpdateThumbnailProperties(thumbHandle, ref props)))
            throw new Win32Exception();

        border.Width = props.rcSource.Right - props.rcSource.Left;
        border.Height = props.rcSource.Bottom - props.rcSource.Top;
        viewbox.UpdateLayout();

    }

}

/// <summary>Manages window previews.</summary>
/// <remarks>Usage: <see cref="DesktopWindow.Preview"/>.</remarks>
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
    #region Event manager

    readonly Dictionary<FrameworkElement, EventManager> eventManagers = new();

    /// <summary>Manages event subscriptions to element and window.</summary>
    class EventManager : IDisposable
    {

        static readonly List<EventManager> managers = new();
        readonly FrameworkElement element;
        readonly Window window;
        readonly Preview preview;

        public EventManager(FrameworkElement element, Window window, Preview preview)
        {

            this.element = element;
            this.window = window;
            this.preview = preview;

            managers.Add(this);

            element.Unloaded += Element_Unloaded;
            element.SizeChanged += Element_SizeChanged;

            window.LayoutUpdated -= ElementWindow_LayoutUpdated;
            window.LocationChanged -= ElementWindow_LocationChanged;
            window.LayoutUpdated += ElementWindow_LayoutUpdated;
            window.LocationChanged += ElementWindow_LocationChanged;

        }

        void Element_SizeChanged(object sender, SizeChangedEventArgs e) =>
           preview.Update(element);

        void Element_Unloaded(object sender, RoutedEventArgs e) =>
           preview.Unregister(element);

        void ElementWindow_LocationChanged(object sender, EventArgs e) =>
            preview.Update(element);

        void ElementWindow_LayoutUpdated(object sender, EventArgs e) =>
            preview.Update(element);

        public void Dispose()
        {

            _ = managers.Remove(this);

            element.Unloaded -= Element_Unloaded;
            element.SizeChanged -= Element_SizeChanged;

            if (!managers.Any(m => m.window == window))
            {
                window.LayoutUpdated -= ElementWindow_LayoutUpdated;
                window.LocationChanged -= ElementWindow_LocationChanged;
            }

        }

    }

    #endregion

    readonly IntPtr window;

    internal Preview(IntPtr window) =>
        this.window = window;

    readonly Dictionary<FrameworkElement, IntPtr> registered = new();

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

        if (DesignerProperties.GetIsInDesignMode(element))
            return;

        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (registered.ContainsKey(element))
            Unregister(element);

        var elementWindow = Window.GetWindow(element);
        if (elementWindow == null)
            throw new ArgumentException("The element must be hosted in a window.");

        var handle = new WindowInteropHelper(elementWindow).EnsureHandle();

        if (!Succeeded(DwmRegisterThumbnail(handle, window, out var thumb)))
            throw new Win32Exception();

        registered.Add(element, thumb);
        Update(element);

        eventManagers.Add(element, new(element, elementWindow, this));

    }

    /// <summary>
    /// <para>Updates the window preview for an element.</para>
    /// <para>Registers the element if necessary.</para>
    /// <para>This is automatically called on <see cref="FrameworkElement.SizeChanged"/> for element registered through <see cref="Register(FrameworkElement)"/> and attached property.</para>
    /// </summary>
    public void Update(FrameworkElement element!!)
    {

        if (!element.IsLoaded)
            return;

        if (!registered.TryGetValue(element, out var thumb))
        {
            Register(element);
            return;
        }

        var elementWindow = Window.GetWindow(element);
        if (elementWindow == null)
            throw new ArgumentException("The element must be hosted in a window.");

        if (!Succeeded(DwmQueryThumbnailSourceSize(thumb, out var size)))
            throw new Win32Exception();

        var rect = new System.Windows.Rect(element.TranslatePoint(new(), elementWindow), element.TranslatePoint(new(element.ActualWidth, element.ActualHeight), elementWindow));
        var props = new DWM_THUMBNAIL_PROPERTIES
        {
            dwFlags = DWM_TNP_VISIBLE | DWM_TNP_RECTDESTINATION,
            fVisible = true,
            rcDestination = new Rect((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height)
        };

        if (size.x < element.ActualWidth)
            props.rcDestination.Right = props.rcDestination.Left + size.x;
        if (size.y < element.ActualHeight)
            props.rcDestination.Bottom = props.rcDestination.Top + size.y;

        if (!Succeeded(DwmUpdateThumbnailProperties(thumb, ref props)))
            throw new Win32Exception();

        //Debug.WriteLine("updated: " + pos);

    }

    /// <summary>Unregisters an element to display a preview for a window.</summary>
    /// <remarks>This is done automatically on <see cref="FrameworkElement.Unloaded"/> for elements registered through <see cref="Register(FrameworkElement)"/> and attached property.</remarks>
    public void Unregister(FrameworkElement element!!)
    {

        if (eventManagers.Remove(element, out var manager))
            manager.Dispose();

        if (registered.TryGetValue(element, out var thumb))
        {
            _ = registered.Remove(element);
            if (!Succeeded(DwmUnregisterThumbnail(thumb)))
                throw new Win32Exception();
        }

    }

    static bool Succeeded(int returnValue) =>
        returnValue is 0 or (-2147024809); //What is this? An error with marshalling or casting types? Throwing Win32Exception says operation completed successfully?

    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var item in registered.Keys.ToArray())
            if (registered.ContainsKey(item))
                Unregister(item);
    }

}
