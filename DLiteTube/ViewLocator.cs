using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DLiteTube.ViewModels;
using DLiteTube.Views;

namespace DLiteTube;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null) return null;

        return param switch
        {
            MainWindowViewModel => new MainWindow(),
            DownloadProgressViewModel => new DownloadProgressWindow(),
            SettingViewModel  => new SettingWindow(),
            _ => new TextBlock { Text = "Not Found: " + param.GetType().Name }
        };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}