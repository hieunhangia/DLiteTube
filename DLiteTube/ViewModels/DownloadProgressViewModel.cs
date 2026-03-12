using System;
using System.Collections.Generic;
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
    VideoResult videoResult,
    IStreamInfo selectedStream,
    string ffmpegPath)
    : ObservableObject
{
    [ObservableProperty] private double _downloadProgress;

    [ObservableProperty] private CancellationTokenSource? _downloadCts;

    [ObservableProperty] private string? _title;

    private static DownloadProgressWindow GetWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            throw new InvalidOperationException("Download progress window not found.");
        }

        var window = desktop.Windows.FirstOrDefault(w => w is DownloadProgressWindow);
        return window as DownloadProgressWindow ??
               throw new InvalidOperationException("Download progress window not found.");
    }

    [RelayCommand]
    private async Task StartDownloadAsync()
    {
        var window = GetWindow();
        var safeFilename = GetSafeFileName(videoResult.Title);
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
                        new ConversionRequestBuilder($"{safeFilename}.{audioStreamInfo.Container}")
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
                        new ConversionRequestBuilder($"{safeFilename}.{videoStreamInfo.Container}")
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
        return;

        static string GetSafeFileName(string fileName) => fileName
            .Replace("/", "⁄")
            .Replace("\\", "＼")
            .Replace(":", "：")
            .Replace("?", "？")
            .Replace("|", "｜")
            .Replace("\"", "”")
            .Replace("*", "＊")
            .Replace("<", "＜")
            .Replace(">", "＞");
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