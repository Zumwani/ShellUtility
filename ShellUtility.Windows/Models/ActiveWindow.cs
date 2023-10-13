using System;
using System.Diagnostics;
using ShellUtility.Windows.Models;
using ShellUtility.Windows.Utility;

namespace ShellUtility.Windows.Models
{

    public partial class DesktopWindow
    {

        /// <inheritdoc cref="ActiveDesktopWindow"/>
        public static DesktopWindow Active { get; } = ActiveDesktopWindow.Instance;

        /// <summary>Occurs when the active window changes.</summary>
        public static event Action? ActiveWindowChanged;

        internal static void RaiseActiveWindowChanged() =>
            ActiveWindowChanged?.Invoke();

    }

}

namespace ShellUtility.Windows.Utility
{

    /// <summary>
    /// <para>Represents the active window. Properties are automatically updated to reflect the active window.</para>
    /// <para>Usage: <see cref="DesktopWindow.Active"/>.</para>
    /// </summary>
    public class ActiveDesktopWindow : DesktopWindow
    {

        /// <inheritdoc cref="ActiveDesktopWindow"/>
        internal static DesktopWindow Instance { get; } = new ActiveDesktopWindow();

        ActiveDesktopWindow() : base(IntPtr.Zero)
        {
            Update();
            HookUtility.AddHook(HookUtility.Event.SYSTEM_FOREGROUND, Update);
        }

        ~ActiveDesktopWindow()
        {
            HookUtility.RemoveHook(HookUtility.Event.SYSTEM_FOREGROUND, Update);
        }

        #region Update

        public override void Update() =>
            Update(WindowUtility.GetActiveWindow());

        void Update(IntPtr handle)
        {

            var prevHandle = Handle;

            if (prevHandle != handle)
            {

                (var process, var path) = WindowUtility.GetProcessAndPath(handle);

                this.handle = handle;
                this.path = path;
                this.process = process;
                classname = WindowUtility.GetClassname(handle);

                UpdateIcon();
                UpdateTitle();
                UpdateRect();
                UpdateStyles();
                (preview as IDisposable)?.Dispose();
                preview = new Preview(handle);

                OnPropertyChanged(nameof(Handle));
                OnPropertyChanged(nameof(ProcessPath));
                OnPropertyChanged(nameof(Process));
                OnPropertyChanged(nameof(Classname));

            }

            base.Update();

            if (prevHandle != handle)
                RaiseActiveWindowChanged();

        }

        #endregion

        IntPtr handle;
        Process? process;
        string? path;
        Preview? preview;
        string? classname;

        /// <inheritdoc/>
        public override IntPtr Handle => handle;

        /// <inheritdoc/>
        public override Process? Process => process;

        /// <inheritdoc/>
        public override string? ProcessPath => path;

        /// <inheritdoc/>
        public override Preview? Preview => preview;

        /// <inheritdoc/>
        public override string? Classname => classname;

    }

}
