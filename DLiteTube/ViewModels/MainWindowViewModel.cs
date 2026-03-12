using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLiteTube.Models;
using DLiteTube.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace DLiteTube.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly YoutubeClient _youtubeClient = new();

    [ObservableProperty] private string _searchUrl = string.Empty;

    [ObservableProperty] private bool _isSearching;

    [ObservableProperty] private VideoResult? _videoResult;

    [ObservableProperty] private bool _hasResult;

    [ObservableProperty] private VideoStreamInfo? _selectedVideoStream;

    [ObservableProperty] private AudioOnlyStreamInfo? _selectedAudioOnlyStream;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(WatchCommand), nameof(DownloadCommand))]
    private IStreamInfo? _selectedStream;

    private bool CanWatchOrDownload() => SelectedStream != null;

    partial void OnSelectedVideoStreamChanged(VideoStreamInfo? value)
    {
        if (value == null) return;
        SelectedStream = value;
        SelectedAudioOnlyStream = null;
    }

    partial void OnSelectedAudioOnlyStreamChanged(AudioOnlyStreamInfo? value)
    {
        if (value == null) return;
        SelectedStream = value;
        SelectedVideoStream = null;
    }

    [RelayCommand]
    private async Task PasteFromClipboard()
    {
        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow window })
        {
            if (window.Clipboard is null) return;
            var text = await window.Clipboard.TryGetTextAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                SearchUrl = text.Trim();
            }
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow window })
        {
            if (!string.IsNullOrWhiteSpace(SearchUrl))
            {
                IsSearching = true;
                VideoResult = null;
                HasResult = false;
                SelectedStream = null;
                try
                {
                    var video = await _youtubeClient.Videos.GetAsync(SearchUrl);
                    var duration = video.Duration?.ToString(@"hh\:mm\:ss") ?? string.Empty;
                    var tempResult = new VideoResult
                    {
                        Title = video.Title,
                        Author = video.Author.ChannelTitle,
                        Duration = duration,
                        Url = video.Url,
                        ThumbnailUrl = video.Thumbnails.GetWithHighestResolution().Url
                    };
                    if (!tempResult.IsLiveStream)
                    {
                        var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(video.Url);
                        var audioStreamOrdered = streamManifest.GetAudioOnlyStreams()
                            .OrderByDescending(s => s.Bitrate.BitsPerSecond).ToList();
                        var bestAudioFileSize = audioStreamOrdered.FirstOrDefault()?.Size.MegaBytes ?? 0;
                        tempResult.VideoStreams = streamManifest.GetVideoOnlyStreams()
                            .OrderByDescending(s => s.VideoQuality.MaxHeight)
                            .Select(s => new VideoStreamInfo
                            {
                                Url = s.Url,
                                Container = s.Container.Name,
                                VideoQuality = s.VideoQuality.Label,
                                Bitrate = GetKbpsBitrateString(s.Bitrate.KiloBitsPerSecond),
                                FileSizeWithBestAudio = GetMbFileSizeString(s.Size.MegaBytes + bestAudioFileSize)
                            });
                        tempResult.AudioOnlyStreams = audioStreamOrdered
                            .Select(s => new AudioOnlyStreamInfo
                            {
                                Url = s.Url,
                                Container = s.Container.Name,
                                AudioBitrate = GetKbpsBitrateString(s.Bitrate.KiloBitsPerSecond),
                                AudioLanguage = s.AudioLanguage.ToString() ?? "Default",
                                FileSize = GetMbFileSizeString(s.Size.MegaBytes)
                            });
                    }

                    VideoResult = tempResult;
                    HasResult = true;
                }
                catch
                {
                    await MessageBoxManager
                        .GetMessageBoxStandard(new MessageBoxStandardParams
                        {
                            ContentTitle = "Lỗi tìm kiếm",
                            ContentMessage = "URL video không hợp lệ hoặc có lỗi xảy ra khi tìm kiếm.",
                            Icon = Icon.Error,
                            ButtonDefinitions = ButtonEnum.Ok
                        }).ShowAsPopupAsync(window);
                }

                IsSearching = false;
            }
            else
            {
                await MessageBoxManager
                    .GetMessageBoxStandard(new MessageBoxStandardParams
                    {
                        ContentTitle = "Thông báo",
                        ContentMessage = "Vui lòng nhập URL video để tìm kiếm.",
                        Icon = Icon.Info,
                        ButtonDefinitions = ButtonEnum.Ok
                    }).ShowAsPopupAsync(window);
            }
        }
    }

    [RelayCommand]
    private void WatchLiveStream()
    {
    }

    [RelayCommand(CanExecute = nameof(CanWatchOrDownload))]
    private void Watch()
    {
    }

    [RelayCommand(CanExecute = nameof(CanWatchOrDownload))]
    private void Download()
    {
    }

    private static string GetKbpsBitrateString(double kbpsBitrate) => Math.Round(kbpsBitrate, 2) + " Kbps";

    private static string GetMbFileSizeString(double mbFileSize) => Math.Round(mbFileSize, 2) + " MB";
}