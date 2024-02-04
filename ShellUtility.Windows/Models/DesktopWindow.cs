using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using ShellUtility.Windows.Utility;

namespace ShellUtility.Windows.Models;

/// <summary>An representation of a window on the users desktop.</summary>
public partial class DesktopWindow : INotifyPropertyChanged
{

    #region Constructor

    /// <summary>Finds the desktop window with the specified handle.</summary>
    public static DesktopWindow FromHandle(IntPtr handle) =>
        new(handle);

    internal DesktopWindow(IntPtr handle)
    {

        if (handle == IntPtr.Zero)
            return;

        Handle = handle;
        UpdateIfVisibleInTaskbar();

        (var process, var path) = WindowUtility.GetProcessAndPath(handle);
        ProcessPath = path;
        Process = process;
        Preview = new Preview(handle);
        Classname = WindowUtility.GetClassname(handle);

        UpdateTitle();
        UpdateRect();
        //UpdateScreen();
        Update();

        HookUtility.AddHook(HookUtility.Event.OBJECT_NAMECHANGE, handle, UpdateTitle);
        HookUtility.AddHook(HookUtility.Event.OBJECT_PARENTCHANGE, handle, UpdateIfVisibleInTaskbar);
        HookUtility.AddHook(HookUtility.Event.OBJECT_DESTROY, handle, OnWindowDestroyed);
        HookUtility.AddHook(HookUtility.Event.SYSTEM_MOVESIZESTART, handle, OnWindowMoveOrResizeStart);
        HookUtility.AddHook(HookUtility.Event.SYSTEM_MOVESIZEEND, handle, OnWindowMoveOrResizeEnd);

        ActiveWindowChanged += OnActiveWindowChanged;

    }

    ~DesktopWindow()
    {

        HookUtility.RemoveHook(HookUtility.Event.OBJECT_NAMECHANGE, Handle, UpdateTitle);
        HookUtility.RemoveHook(HookUtility.Event.OBJECT_PARENTCHANGE, Handle, UpdateIfVisibleInTaskbar);
        HookUtility.RemoveHook(HookUtility.Event.OBJECT_DESTROY, Handle, OnWindowDestroyed);
        HookUtility.RemoveHook(HookUtility.Event.SYSTEM_MOVESIZESTART, Handle, OnWindowMoveOrResizeStart);
        HookUtility.RemoveHook(HookUtility.Event.SYSTEM_MOVESIZEEND, Handle, OnWindowMoveOrResizeEnd);

        ActiveWindowChanged -= OnActiveWindowChanged;
        (Preview as IDisposable)?.Dispose();

    }

    #endregion
    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    #endregion
    #region Callbacks

    void OnWindowDestroyed()
    {
        //Event.OBJECT_DESTROY is called for child windows, but handle is still refers to root window,
        //adding this check fixes this
        if (!WindowUtility.IsDesktopWindow(Handle))
            IsOpen = false;
    }

    async void OnWindowMoveOrResizeStart()
    {

        IsMovingOrResizing = true;
        OnPropertyChanged(nameof(IsMovingOrResizing));

        while (IsMovingOrResizing)
        {
            //UpdateScreen();
            await Task.Delay(100);
        }

    }

    void OnWindowMoveOrResizeEnd()
    {

        Rect = WindowUtility.GetIsVisibleAndRect(Handle).rect ?? default;
        IsMovingOrResizing = false;

        OnPropertyChanged(nameof(Rect));
        OnPropertyChanged(nameof(IsMovingOrResizing));

    }

    void OnActiveWindowChanged()
    {

        var isUs = Active.Handle == Handle;
        if (isUs || IsActive)
        {
            IsActive = isUs;
            OnPropertyChanged(nameof(IsActive));
        }

    }

    #endregion
    #region Properties

    #region Init properties

    /// <summary>The handle of this <see cref="DesktopWindow"/>.</summary>
    public virtual IntPtr Handle { get; protected set; }

    /// <summary>The classname of this <see cref="DesktopWindow"/>.</summary>
    public virtual string? Classname { get; protected set; }

    /// <summary>The path to the owning <see cref="System.Diagnostics.Process"/> of this <see cref="DesktopWindow"/>.</summary>
    public virtual string? ProcessPath { get; protected set; }

    /// <summary>The owning <see cref="System.Diagnostics.Process"/> of this <see cref="DesktopWindow"/>.</summary>
    public virtual Process? Process { get; protected set; }

    /// <summary>Gets the preview for this <see cref="DesktopWindow"/>.</summary>
    public virtual Preview? Preview { get; protected set; }

    #endregion
    #region Get properties

    /// <summary>The title of this <see cref="DesktopWindow"/>.</summary>
    public string? Title { get; private set; }

    /// <summary>Gets whatever this <see cref="DesktopWindow"/> is still open.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>Gets whatever this <see cref="DesktopWindow"/> is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// <para>Get if the user is currently moving or resizing the <see cref="DesktopWindow"/>.</para>
    /// <para>Note that <see cref="Rect"/> will not be updated until move or resize ends. Use <see cref="WindowUtility.GetIsVisibleAndRect(IntPtr)"/> to get <see cref="System.Windows.Rect"/> during move or resize.</para>
    /// </summary>
    public bool IsMovingOrResizing { get; private set; }

    /// <summary>The title of this <see cref="DesktopWindow"/>.</summary>
    public BitmapSource? Icon { get; private set; } //Set by IconHandle

    /// <summary>The <see cref="System.Windows.Rect"/> of this <see cref="DesktopWindow"/> on the users desktop.</summary>
    public Rect? Rect { get; private set; }

    #endregion
    #region Get / Set properties

    bool isVisible;

    /// <summary>
    /// <para>Gets whatever the window is visible on the screen.</para>
    /// <para>False equals minimized.</para>
    /// </summary>
    public bool IsVisible
    {
        get => isVisible;
        set
        {
            WindowUtility.SetVisible(Handle, value);
            isVisible = WindowUtility.GetIsVisibleAndRect(Handle).isVisible;
        }
    }

    #endregion

    #endregion
    #region Public methods

    /// <summary>
    /// <para>Shows this <see cref="DesktopWindow"/> and activates it.</para>
    /// <para>(aka normalize, if minimized).</para>
    /// </summary>
    public void Show() => IsVisible = true;

    /// <summary>
    /// <para>Hides this <see cref="DesktopWindow"/>.</para>
    /// <para>(aka minimizing).</para>
    /// </summary>
    public void Hide() => IsVisible = false;

    /// <inheritdoc cref="Hide"/>
    public void Minimize() => Hide();

    /// <summary>Activates this <see cref="DesktopWindow"/>.</summary>
    public void Activate() => WindowUtility.Activate(Handle);

    /// <summary>Closes this <see cref="DesktopWindow"/>.</summary>
    public void Close() => WindowUtility.Close(Handle);

    /// <summary>Opens a new instance of the associated app.</summary>
    public bool OpenNewInstance([NotNullWhen(true)] out Process? process) =>
        (process = File.Exists(ProcessPath) ? Process.Start(ProcessPath) : null) is not null;

    public override string ToString() =>
        Title + " (" + Handle + ")";

    #endregion
    #region Update

    IntPtr iconHandle;

    protected void UpdateIcon()
    {

        if (!IsOpen)
            return;

        var handle = WindowUtility.GetIconHandle(Handle);
        if (iconHandle == handle)
            return;

        iconHandle = handle;
        WindowUtility.GetIcon(handle, out var icon);
        Icon = icon;
        OnPropertyChanged(nameof(Icon));

    }

    protected void UpdateTitle()
    {
        if (WindowUtility.GetTitle(Handle, out var title))
            CheckValueChanged(title, Title, nameof(Title), (v) => Title = v);
    }

    protected void UpdateRect() =>
        CheckValueChanged(WindowUtility.GetIsVisibleAndRect(Handle).rect, Rect, nameof(Rect), (v) => Rect = v ?? default);

    /// <summary>Manually updates <see cref="IsVisibleInTaskbar"/>, <see cref="IsOpen"/> and <see cref="Icon"/>.</summary>
    /// <remarks>This is called by <see cref="Poller.Update"/>, if enabled.</remarks>
    public virtual void Update()
    {

        if (Handle == IntPtr.Zero)
            return;

        UpdateIfVisibleInTaskbar();

        CheckValueChanged(WindowUtility.IsOpen(Handle), IsOpen, nameof(IsOpen), (v) => IsOpen = v);
        CheckValueChanged(WindowUtility.GetIsVisibleAndRect(Handle).isVisible, isVisible, nameof(IsVisible), (v) => isVisible = v);
        UpdateIcon();

    }

    /// <summary>Checks if value has changed and calls <see cref="PropertyChanged"/> and calls param <paramref name="set"/>, if it has.</summary>
    void CheckValueChanged<T>(T newValue, T oldValue, string propertyNameToNotify, Action<T> set)
    {

        if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
            return;

        set?.Invoke(newValue);
        OnPropertyChanged(propertyNameToNotify);

    }

    #endregion
    #region Styles

    /// <summary>Gets the styles for the window.</summary>
    public bool GetStyles([NotNullWhen(true)] out WindowStyles style, [NotNullWhen(true)] out WindowStylesEx exStyle) =>
        WindowUtility.GetWindowStyle(Handle, out style, out exStyle);

    /// <summary>Sets the styles on the window.</summary>
    /// <remarks>Specify <see langword="null"/> to not set those styles.</remarks>
    public void SetStyles(WindowStyles? style = null, WindowStylesEx? exStyle = null)
    {
        if (style.HasValue)
            WindowUtility.SetWindowStyle(Handle, style.Value);
        if (exStyle.HasValue)
            WindowUtility.SetWindowStyle(Handle, exStyle.Value);
    }

    /// <inheritdoc cref="AddStyle(WindowStyles?, WindowStylesEx?)"/>
    public bool AddStyle(WindowStyles style) => AddStyle(style, null);

    /// <inheritdoc cref="AddStyle(WindowStyles?, WindowStylesEx?)"/>
    public bool AddStyle(WindowStylesEx exStyle) => AddStyle(null, exStyle);

    /// <summary>Adds the style to the window.</summary>
    public bool AddStyle(WindowStyles? style = null, WindowStylesEx? exStyle = null)
    {

        if (!GetStyles(out var currentStyle, out var currentExStyle))
            return false;

        if (style.HasValue && !currentStyle.HasFlag(style.Value))
        {
            if (!WindowUtility.SetWindowStyle(Handle, currentStyle.SetFlag(style.Value)))
                return false;
        }
        if (exStyle.HasValue && !currentExStyle.HasFlag(exStyle.Value))
        {
            if (!WindowUtility.SetWindowStyle(Handle, currentExStyle.SetFlag(exStyle.Value)))
                return false;
        }

        return true;

    }

    /// <inheritdoc cref="RemoveStyle(WindowStyles?, WindowStylesEx?)"/>
    public bool RemoveStyle(WindowStyles style) => RemoveStyle(style, null);

    /// <inheritdoc cref="RemoveStyle(WindowStyles?, WindowStylesEx?)"/>
    public bool RemoveStyle(WindowStylesEx exStyle) => RemoveStyle(null, exStyle);

    /// <summary>Removes the style from the window.</summary>
    public bool RemoveStyle(WindowStyles? style = null, WindowStylesEx? exStyle = null)
    {

        if (!GetStyles(out var currentStyle, out var currentExStyle))
            return false;

        if (style.HasValue && currentStyle.HasFlag(style.Value))
        {
            if (!WindowUtility.SetWindowStyle(Handle, currentStyle.RemoveFlag(style.Value)))
                return false;
        }
        if (exStyle.HasValue && currentExStyle.HasFlag(exStyle.Value))
        {
            if (!WindowUtility.SetWindowStyle(Handle, currentExStyle.RemoveFlag(exStyle.Value)))
                return false;
        }

        return true;

    }

    #endregion
    #region IsVisibleInTaskbar

    internal event Action<DesktopWindow>? IsVisibleInTaskbarChanged;

    /// <summary>Gets whatever this window is visible in taskbar.</summary>
    public bool IsVisibleInTaskbar { get; private set; }

    void UpdateIfVisibleInTaskbar()
    {

        var value = WindowUtility.IsVisibleInTaskbar(Handle);

        if (value != IsVisibleInTaskbar)
        {
            IsVisibleInTaskbar = value;
            IsVisibleInTaskbarChanged?.Invoke(this);
            OnPropertyChanged(nameof(IsVisibleInTaskbar));
        }

    }

    #endregion
    #region Equality

    public override bool Equals(object? obj)
    {
        if (obj is DesktopWindow window)
            return Equals(window);
        else if (obj is IntPtr handle)
            return Equals(handle);
        return false;
    }

    public bool Equals(DesktopWindow? window) => window is not null && window.Handle == Handle;
    public bool Equals(IntPtr? handle) => handle is not null && handle == Handle;
    public override int GetHashCode() => Handle.GetHashCode();

    public static bool operator ==(DesktopWindow? left, DesktopWindow? right) => left?.Equals(right) ?? false;
    public static bool operator !=(DesktopWindow? left, DesktopWindow? right) => !(left?.Equals(right) ?? false);

    public static bool operator ==(DesktopWindow? left, IntPtr? right) => left?.Equals(right) ?? false;
    public static bool operator !=(DesktopWindow? left, IntPtr? right) => !(left?.Equals(right) ?? false);

    public static bool operator ==(IntPtr? left, DesktopWindow? right) => right?.Equals(left) ?? false;
    public static bool operator !=(IntPtr? left, DesktopWindow? right) => !(right?.Equals(left) ?? false);

    #endregion

    public static implicit operator IntPtr(DesktopWindow window) =>
        window.Handle;

}
