using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Media;
using ShellUtility.Windows.Utility;

namespace ShellUtility.Windows.Models;

/// <summary>Represents a group of <see cref="DesktopWindow"/>.</summary>
public class AppGroup
{

    internal readonly ObservableCollection<DesktopWindow> windows = new();

    internal AppGroup(string appPath)
    {

        AppPath = appPath;
        Windows = new(windows);

        var fvi = FileVersionInfo.GetVersionInfo(appPath);
        if (fvi is null)
            throw new ArgumentException("appPath must point to a valid exe file.");

        Title = fvi.ProductName ?? fvi.FileName;
        Icon = IconUtility.GetIcon(appPath, false, false);

    }

    /// <summary>Gets the path to the app.</summary>
    public string AppPath { get; }

    /// <summary>Gets the title of this app.</summary>
    public string Title { get; }

    /// <summary>Gets the icon of this app.</summary>
    public ImageSource? Icon { get; }

    /// <summary>Gets the list of <see cref="DesktopWindow"/> of this app.</summary>
    public ReadOnlyObservableCollection<DesktopWindow> Windows { get; }

}
