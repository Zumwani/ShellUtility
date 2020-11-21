# ShellUtility

Provides utilities for interacting with the shell.

Utilities can all be installed from NuGet:

> Install-Package ShellUtility.NotifyIcons
> Install-Package ShellUtility.Screens



# ShellUtility.Screens

Provides info about the users screens. A replacement for System.Windows.Forms.Screen class targeting dotnet standard.

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

Enumerates notify icons on taskbar and provides functions to interact with them.

```csharp
using ShellUtility.NotifyIcons;
using System.Linq;

void OpenDiscord()
{
    var collection = new NotifyIconCollection();
    var icon = collection.FirstOrDefault(icon => icon.Path.EndsWith("Discord.exe"));
    icon?.Invoke(NotifyIconInvokeAction.LeftClick);
}
```
