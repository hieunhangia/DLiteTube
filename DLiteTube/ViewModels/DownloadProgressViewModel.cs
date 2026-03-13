using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLiteTube.Models;
using DLiteTube.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using YoutubeExplode;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos.Streams;

namespace DLiteTube.ViewModels;

public partial class DownloadProgressViewModel(
    YoutubeClient youtubeClient,
    string saveFilePath,
    VideoResult videoResult,
    IStreamInfo selectedStream,
    string ffmpegPath)
    : ObservableObject
{
    [ObservableProperty] private double _downloadProgress;

    [ObservableProperty] private CancellationTokenSource? _downloadCts;

    [ObservableProperty] private string? _title;

    private DownloadProgressWindow GetWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            throw new InvalidOperationException("Application lifetime is not a classic desktop style.");
        }

        var window = desktop.Windows
            .OfType<DownloadProgressWindow>()
            .FirstOrDefault(w => w.DataContext == this);
        return window ?? throw new InvalidOperationException("Download progress window not found.");
    }

    [RelayCommand]
    private async Task StartDownloadAsync()
    {
        var window = GetWindow();
        var progress = new Progress<double>(p => DownloadProgress = p * 100);
        DownloadCts = new CancellationTokenSource();
        try
        {
            switch (selectedStream)
            {
                case AudioStreamInfo audioStreamInfo:
                {
                    Title = $"Đang tải xuống: {videoResult.Title} (Audio - {audioStreamInfo.BitrateString})";
                    await youtubeClient.Videos.DownloadAsync([audioStreamInfo],
                        new ConversionRequestBuilder(saveFilePath)
                            .SetFFmpegPath(ffmpegPath)
                            .SetPreset(ConversionPreset.UltraFast)
                            .Build(),
                        progress, DownloadCts.Token
                    );
                    await MessageBoxManager
                        .GetMessageBoxStandard(new MessageBoxStandardParams
                        {
                            ContentTitle = "Tải xuống hoàn tất",
                            ContentMessage = "Tải xuống đã hoàn tất.",
                            Icon = Icon.Success,
                            ButtonDefinitions = ButtonEnum.Ok
                        }).ShowAsPopupAsync(window);
                    break;
                }
                case VideoStreamInfo videoStreamInfo:
                {
                    Title = $"Đang tải xuống: {videoResult.Title} (Video - {videoStreamInfo.VideoQuality})";
                    var bestAudioStreamInfo = videoResult.AudioStreams?.GetWithHighestBitrate();
                    IReadOnlyList<IStreamInfo> streamInfos = bestAudioStreamInfo != null
                        ? [videoStreamInfo, bestAudioStreamInfo]
                        : [videoStreamInfo];
                    await youtubeClient.Videos.DownloadAsync(streamInfos,
                        new ConversionRequestBuilder(saveFilePath)
                            .SetFFmpegPath(ffmpegPath)
                            .SetPreset(ConversionPreset.UltraFast)
                            .Build(),
                        progress, DownloadCts.Token
                    );
                    await MessageBoxManager
                        .GetMessageBoxStandard(new MessageBoxStandardParams
                        {
                            ContentTitle = "Tải xuống hoàn tất",
                            ContentMessage = "Tải xuống đã hoàn tất.",
                            Icon = Icon.Success,
                            ButtonDefinitions = ButtonEnum.Ok
                        }).ShowAsPopupAsync(window);
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            await MessageBoxManager
                .GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentTitle = "Tải xuống đã hủy",
                    ContentMessage = "Tải xuống đã bị hủy.",
                    Icon = Icon.Info,
                    ButtonDefinitions = ButtonEnum.Ok
                }).ShowAsPopupAsync(window);

            if (File.Exists(saveFilePath))
            {
                try
                {
                    File.Delete(saveFilePath);
                }
                catch
                {
                    // Ignore any errors during file deletion
                }
            }
        }
        catch
        {
            await MessageBoxManager
                .GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentTitle = "Lỗi tải xuống",
                    ContentMessage = "Đã xảy ra lỗi trong quá trình tải xuống.",
                    Icon = Icon.Error,
                    ButtonDefinitions = ButtonEnum.Ok
                }).ShowAsPopupAsync(window);
        }

        window.Close();
    }

    [RelayCommand]
    private async Task CancelDownload()
    {
        var result = await MessageBoxManager
            .GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentTitle = "Xác nhận hủy",
                ContentMessage = "Bạn có chắc chắn muốn hủy tải xuống không?",
                Icon = Icon.Question,
                ButtonDefinitions = ButtonEnum.YesNo
            }).ShowAsPopupAsync(GetWindow());
        if (result == ButtonResult.Yes && DownloadCts != null)
        {
            await DownloadCts.CancelAsync();
        }
    }
}