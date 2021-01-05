using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using ShellUtility.Windows.Utility;
using screen = ShellUtility.Screens.Screen;

[assembly: XmlnsDefinition("shellutility://windows", "ShellUtility.Windows")]
[assembly: XmlnsPrefix("shellutility://windows", "windows")]
namespace ShellUtility.Windows
{

    /// <summary>An representation of a window on the users desktop.</summary>
    public partial class DesktopWindow : INotifyPropertyChanged
    {

        #region Constructor

        /// <summary>Finds the desktop window with the specified handle.</summary>
        public static DesktopWindow FromHandle(IntPtr handle) =>
            new DesktopWindow(handle);

        internal DesktopWindow(IntPtr handle)
        {

            if (handle == IntPtr.Zero)
                return;

            (Process process, string path) = WindowUtility.GetProcessAndPath(handle);
            Handle = handle;
            ProcessPath = path;
            Process = process;
            IsUWP = WindowUtility.IsUWPWindow(handle);
            Preview = new Preview(handle);

            UpdateTitle();
            UpdateRect();
            UpdateScreen();
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

            HookUtility.RemoveHook(HookUtility.Event.OBJECT_NAMECHANGE,    Handle, UpdateTitle);
            HookUtility.RemoveHook(HookUtility.Event.OBJECT_PARENTCHANGE,  Handle, UpdateIfVisibleInTaskbar);
            HookUtility.RemoveHook(HookUtility.Event.OBJECT_DESTROY,       Handle, OnWindowDestroyed);
            HookUtility.RemoveHook(HookUtility.Event.SYSTEM_MOVESIZESTART, Handle, OnWindowMoveOrResizeStart);
            HookUtility.RemoveHook(HookUtility.Event.SYSTEM_MOVESIZEEND,   Handle, OnWindowMoveOrResizeEnd);

            ActiveWindowChanged -= OnActiveWindowChanged;
            (Preview as IDisposable)?.Dispose();

        }

        #endregion
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

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
                UpdateScreen();
                await Task.Delay(100);
            }

        }

        void OnWindowMoveOrResizeEnd()
        {

            Rect = WindowUtility.GetIsVisibleAndRect(Handle).rect;
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
        public virtual IntPtr Handle { get; init; }
        
        /// <summary>The path to the owning <see cref="System.Diagnostics.Process"/> of this <see cref="DesktopWindow"/>.</summary>
        public virtual string ProcessPath { get; init; }

        /// <summary>The owning <see cref="System.Diagnostics.Process"/> of this <see cref="DesktopWindow"/>.</summary>
        public virtual Process Process { get; init; }

        /// <summary>Gets whatever this is a UWP window.</summary>
        public virtual bool IsUWP { get; init; }

        /// <summary>Gets the preview for this <see cref="DesktopWindow"/>.</summary>
        public virtual Preview Preview { get; init; }

        #endregion
        #region Get properties

        /// <summary>The title of this <see cref="DesktopWindow"/>.</summary>
        public string Title { get; private set; }

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
        public BitmapSource Icon { get; private set; } //Set by IconHandle

        /// <summary>The <see cref="System.Windows.Rect"/> of this <see cref="DesktopWindow"/> on the users desktop.</summary>
        public Rect Rect { get; private set; }

        /// <summary>The index of the screen that this <see cref="DesktopWindow"/> is currently on.</summary>
        public int Screen { get; private set; }

        IntPtr iconHandle;

        protected void UpdateIcon(bool updateUWP = false)
        {

            if (!IsOpen)
                return;

            if (!IsUWP)
            {

                var handle = WindowUtility.GetIconHandle(Handle);
                if (iconHandle == handle)
                    return;

                iconHandle = handle;
                Icon = WindowUtility.GetIcon(handle);
                OnPropertyChanged(nameof(Icon));

            }
            else if (Icon == null || updateUWP)
            {
                Icon = UWPWindowUtility.GetIcon(Handle);
                iconHandle = IntPtr.Zero;
                OnPropertyChanged(nameof(Icon));
            }

        }

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
        public void Hide() =>  IsVisible = false;

        /// <inheritdoc cref="Hide"/>
        public void Minimize() => Hide();

        /// <summary>Activates this <see cref="DesktopWindow"/>.</summary>
        public void Activate() => WindowUtility.Activate(Handle);

        /// <summary>Closes this <see cref="DesktopWindow"/>.</summary>
        public void Close() => WindowUtility.Close(Handle);

        /// <summary>Opens a new instance of the associated app.</summary>
        public void OpenNewInstance() =>
            Process.Start(Process.StartInfo ?? new ProcessStartInfo(ProcessPath));

        #endregion
        #region Update

        protected void UpdateTitle() =>
            CheckValueChanged(WindowUtility.GetTitle(Handle), Title, nameof(Title), (v) => Title = v);

        protected void UpdateRect() =>
            CheckValueChanged(WindowUtility.GetIsVisibleAndRect(Handle).rect, Rect, nameof(Rect), (v) => Rect = v);

        protected void UpdateScreen() =>
            CheckValueChanged(screen.FromWindowHandle(Handle, false)?.Index ?? -1, Screen, nameof(Screen), v => Screen = v);

        /// <summary>
        /// <para>Manually updates <see cref="IsVisibleInTaskbar"/>, <see cref="IsOpen"/> and <see cref="Icon"/>.</para>
        /// <para>This is called by <see cref="Poller.Update"/>, if enabled.</para>
        /// </summary>
        public virtual void Update()
        {

            if (Handle == IntPtr.Zero)
                return;

            UpdateIfVisibleInTaskbar();

            if (!IsVisibleInTaskbar)
                return;

            CheckValueChanged(WindowUtility.IsOpen(Handle), IsOpen, nameof(IsOpen), (v) => IsOpen = v);
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
        #region IsVisibleInTaskbar

        internal event Action<DesktopWindow> IsVisibleInTaskbarChanged;

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

        public override bool Equals(object obj) =>
            Equals(obj as DesktopWindow);

        public bool Equals(DesktopWindow obj) =>
            obj != null && obj.Handle == this.Handle;

        public override int GetHashCode() =>
            Handle.GetHashCode();

        #endregion

        public override string ToString() =>
            Title + " (" + Handle + ")";

    }

}
