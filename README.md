Provides utilities for interacting with the shell.

Utilities can all be installed from NuGet:


> [Install-Package ShellUtility.NotifyIcons](https://www.nuget.org/packages/ShellUtility.NotifyIcons/)\
> [Install-Package ShellUtility.Screens](https://www.nuget.org/packages/ShellUtility.Screens/)\
> [Install-Package ShellUtility.Windows](https://www.nuget.org/packages/ShellUtility.Windows/)

# ShellUtility.Screens

Provides info about the users screens. </br>
A replacement for System.Windows.Forms.Screen class.

```csharp
using System.Interop;
using ShellUtility.Screens;

Rect GetCurrentScreenWorkArea()
{

    //WinForms:
    var handle = this.Handle;
    //WPF:
    var handle = New WindowInteropHelper(this).Handle;

    var screen = Screen.FromWindowHandle(handle);
    return screen.WorkArea;

}
```

# ShellUtility.NotifyIcons

Enumerates the notify icons on the taskbar and provides functions to interact with them.

```csharp
using System.Linq;
using ShellUtility.NotifyIcons;

void OpenDiscord()
{
    var collection = new NotifyIconCollection();
    var icon = collection.FirstOrDefault(icon => icon.Path.EndsWith("Discord.exe"));
    icon?.Invoke(NotifyIconInvokeAction.LeftClick);
}
```

# ShellUtility.Windows

Enumerates windows on the users desktop. DesktopWindowCollection automatically adds and removes windows when they open or close.

Functions for minimizing, unminimizing and close are provided, properties are automatically updated. Supports easily registering with the DWM to receive window previews.

```csharp
using ShellUtility.Windows;

void Initialize()
{
    var windows = new DesktopWindowCollection();
    windows.ItemAdded += (window) => Debug.WriteLine("Opened: " + window.Title);
    windows.ItemRemoved += (window) => Debug.WriteLine("Closed: " window.Title);
    DesktopWindow.ActiveWindowChanged += () => Debug.WriteLine("Active changed: " + DesktopWindow.Active.Title);
}
```
