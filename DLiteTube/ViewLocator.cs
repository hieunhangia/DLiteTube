using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DLiteTube.ViewModels;
using DLiteTube.Views;

namespace DLiteTube;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null) return null;

        return param switch
        {
            MainWindowViewModel => new MainWindow(),
            DownloadProgressViewModel => new DownloadProgressWindow(),
            _ => new TextBlock { Text = "Not Found: " + param.GetType().Name }
        };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}