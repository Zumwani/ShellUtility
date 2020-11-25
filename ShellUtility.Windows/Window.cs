using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;

using screen = ShellUtility.Screens.Screen;

namespace ShellUtility.Windows
{

    public class Window : INotifyPropertyChanged
    {

        public Window(IntPtr handle)
        {

            (Process process, string path) = WindowUtility.GetProcessAndPath(handle);
            Handle = handle;
            ProcessPath = path;
            Process = process;

            Update();

        }

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        //TODO: Previews
        //TODO: Screen
        //TODO: Win32/UWP
        //TODO: Badge

        //TODO: Notify:
        //Screen
        //Badge

        //Init
        public IntPtr Handle { get; init; }
        public string ProcessPath { get; init; }
        public Process Process { get; init; }

        //Get
        public string Title { get; private set; }
        public bool IsOpen { get; private set; }
        public bool IsActive { get; private set; }
        public BitmapSource Icon { get; private set; } //Set by IconHandle
        public Rect Rect { get; private set; }
        public int Screen { get; private set; }

        //Get, Set
        bool isVisible;
        public bool IsVisible
        {
            get => isVisible;
            set 
            {
                WindowUtility.SetVisible(Handle, value); 
                isVisible = WindowUtility.GetIsVisibleAndRect(Handle).isVisible; 
            }
        }

        IntPtr m_iconHandle;

        //Methods
        public void Show() => IsVisible = true;
        public void Hide() =>  IsVisible = false;
        public void Activate() => WindowUtility.Activate(Handle);
        public void Close() => WindowUtility.Close(Handle);

        public void Update()
        {

            (bool isVisible, Rect rect) = WindowUtility.GetIsVisibleAndRect(Handle);

            CheckValueChanged(WindowUtility.GetTitle(Handle),        Title,         nameof(Title),    (v) => Title = v);
            CheckValueChanged(WindowUtility.IsOpen(Handle),          IsOpen,        nameof(IsOpen),   (v) => IsOpen = v);
            CheckValueChanged(WindowUtility.IsActive(Handle),        IsActive,      nameof(IsActive), (v) => IsActive = v);
            CheckValueChanged(WindowUtility.GetIconHandle(Handle),   m_iconHandle,  nameof(Icon),     (v) => m_iconHandle = v);
            CheckValueChanged(screen.FromWindowHandle(Handle)?.Index ?? -1, Screen,        nameof(Screen),  (v) => Screen = v);

        }

        void CheckValueChanged<T>(T newValue, T oldValue, string propertyNameToNotify, Action<T> set)
        {

            if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
                return;

            set?.Invoke(newValue);
            OnPropertyChanged(propertyNameToNotify);

        }

    }

}
