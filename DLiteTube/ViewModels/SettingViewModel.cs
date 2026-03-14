using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLiteTube.Models;
using DLiteTube.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;

namespace DLiteTube.ViewModels;

public partial class SettingViewModel : ViewModelBase
{
    [ObservableProperty] private Setting? _originalSettings;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveSettingsCommand))]
    private string _ffmpegPath = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveSettingsCommand))]
    private string _downloadPath = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(SaveSettingsCommand))]
    private bool _alwaysAskDownloadPath;

    private bool HasChanges() => new Setting(FfmpegPath, DownloadPath, AlwaysAskDownloadPath) != OriginalSettings;

    private static SettingWindow GetWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            throw new InvalidOperationException("Application lifetime is not a classic desktop style.");
        }

        var window = desktop.Windows.OfType<SettingWindow>().FirstOrDefault();
        return window ?? throw new InvalidOperationException("Setting window not found.");
    }

    [RelayCommand]
    private void Initialize()
    {
        OriginalSettings = Setting.LoadSettings();
        FfmpegPath = OriginalSettings.FfmpegPath;
        DownloadPath = OriginalSettings.DownloadPath;
        AlwaysAskDownloadPath = OriginalSettings.AlwaysAskDownloadPath;
    }

    [RelayCommand]
    private async Task BrowseFfmpegPathAsync()
    {
        var options = new FilePickerOpenOptions
        {
            Title = "Chọn file binary ffmpeg",
            AllowMultiple = false
        };
        var files = await GetWindow().StorageProvider.OpenFilePickerAsync(options);
        if (files.Count > 0)
        {
            FfmpegPath = files[0].TryGetLocalPath() ??
                         throw new InvalidOperationException("Failed to get local path from the selected file.");
        }
    }

    [RelayCommand]
    private async Task BrowseDownloadPathAsync()
    {
        var options = new FolderPickerOpenOptions
        {
            Title = "Chọn thư mục tải xuống",
            AllowMultiple = false
        };
        var folders = await GetWindow().StorageProvider.OpenFolderPickerAsync(options);
        if (folders.Count > 0)
        {
            DownloadPath = folders[0].TryGetLocalPath() ??
                           throw new InvalidOperationException("Failed to get local path from the selected folder.");
        }
    }

    [RelayCommand(CanExecute = nameof(HasChanges))]
    private async Task SaveSettings()
    {
        if (!File.Exists(FfmpegPath))
        {
            await MessageBoxManager
                .GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentTitle = "Lỗi đường dẫn",
                    ContentMessage = "Đường dẫn đến ffmpeg không hợp lệ. Vui lòng kiểm tra lại.",
                    Icon = Icon.Error,
                    ButtonDefinitions = ButtonEnum.Ok
                }).ShowAsPopupAsync(GetWindow());
            return;
        }

        if (!Directory.Exists(DownloadPath))
        {
            await MessageBoxManager
                .GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentTitle = "Lỗi đường dẫn",
                    ContentMessage = "Đường dẫn đến thư mục tải xuống không hợp lệ. Vui lòng kiểm tra lại.",
                    Icon = Icon.Error,
                    ButtonDefinitions = ButtonEnum.Ok
                }).ShowAsPopupAsync(GetWindow());
            return;
        }

        Setting.SaveToFile(new Setting(FfmpegPath, DownloadPath, AlwaysAskDownloadPath));

        await MessageBoxManager
            .GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentTitle = "Lưu cài đặt",
                ContentMessage = "Cài đặt đã được lưu thành công.",
                Icon = Icon.Success,
                ButtonDefinitions = ButtonEnum.Ok
            }).ShowAsPopupAsync(GetWindow());
    }
}