using Avalonia.Controls;
using DLiteTube.ViewModels;

namespace DLiteTube.Views;

public partial class DownloadProgressWindow : Window
{
    public DownloadProgressWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is DownloadProgressViewModel vm)
            {
                vm.StartDownloadCommand.Execute(null);
            }
        };
    }
}