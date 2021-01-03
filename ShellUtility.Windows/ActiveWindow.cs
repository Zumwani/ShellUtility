using System;
using System.Diagnostics;

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

        private ActiveDesktopWindow() : base(IntPtr.Zero)
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

                (Process process, string path) = WindowUtility.GetProcessAndPath(handle);
                
                this.handle = handle;
                this.path = path;
                this.process = process;
            
                isUWP = WindowUtility.IsUWPWindow(handle);
                UpdateIcon(updateUWP: true);
                UpdateTitle();
                UpdateRect();
                UpdateScreen();
                (preview as IDisposable)?.Dispose();
                preview = new Preview(handle);

                OnPropertyChanged(nameof(Handle));
                OnPropertyChanged(nameof(ProcessPath));
                OnPropertyChanged(nameof(Process));
                OnPropertyChanged(nameof(IsUWP));
            
            }

            base.Update();

            if (prevHandle != handle)
                RaiseActiveWindowChanged();

        }

        #endregion

        IntPtr handle;
        Process process;
        string path;
        bool isUWP;
        Preview preview;

        /// <inheritdoc cref="DesktopWindow.Handle"/>
        public override IntPtr Handle => handle;
        
        /// <inheritdoc cref="DesktopWindow.Process"/>
        public override Process Process => process;

        /// <inheritdoc cref="DesktopWindow.ProcessPath"/>
        public override string ProcessPath => path;

        /// <inheritdoc cref="DesktopWindow.IsUWP"/>
        public override bool IsUWP => isUWP;

        /// <inheritdoc cref="DesktopWindow.Preview"/>
        public override Preview Preview => preview;

    }

}
