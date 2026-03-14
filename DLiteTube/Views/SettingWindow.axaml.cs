using Avalonia.Controls;
using DLiteTube.ViewModels;

namespace DLiteTube.Views;

public partial class SettingWindow : Window
{
    public SettingWindow()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            if (DataContext is SettingViewModel vm)
            {
                vm.InitializeCommand.Execute(null);
            }
        };
    }
}