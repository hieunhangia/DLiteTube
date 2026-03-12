using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
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
using YoutubeExplode.Videos.Streams;

namespace DLiteTube.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly Lazy<string> _ffmpegPath = new(() =>
    {
        if (OperatingSystem.IsWindows())
            return "ffmpeg/ffmpeg.exe";
        if (OperatingSystem.IsLinux())
            return "ffmpeg/ffmpeg_linux";
        return OperatingSystem.IsMacOS()
            ? "ffmpeg/ffmpeg_mac"
            : throw new PlatformNotSupportedException("Unsupported operating system.");
    });

    private string FfmpegPath => _ffmpegPath.Value;

    private readonly YoutubeClient _youtubeClient = new();

    [ObservableProperty] private string _searchUrl = string.Empty;

    [ObservableProperty] private bool _isSearching;

    [ObservableProperty] private VideoResult? _videoResult;

    [ObservableProperty] private bool _hasResult;

    [ObservableProperty] private VideoStreamInfo? _selectedVideoStream;

    [ObservableProperty] private AudioStreamInfo? _selectedAudioStream;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(WatchCommand), nameof(DownloadCommand))]
    private IStreamInfo? _selectedStream;

    private static MainWindow GetMainWindow()
    {
        return Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow window }
            ? window
            : throw new InvalidOperationException("Main window not found.");
    }

    private bool CanWatchOrDownload() => SelectedStream != null;

    partial void OnSelectedVideoStreamChanged(VideoStreamInfo? value)
    {
        if (value == null) return;
        SelectedStream = value;
        SelectedAudioStream = null;
    }

    partial void OnSelectedAudioStreamChanged(AudioStreamInfo? value)
    {
        if (value == null) return;
        SelectedStream = value;
        SelectedVideoStream = null;
    }

    [RelayCommand]
    private async Task PasteFromClipboard()
    {
        var window = GetMainWindow();
        if (window.Clipboard is null) return;
        var text = await window.Clipboard.TryGetTextAsync();
        if (!string.IsNullOrWhiteSpace(text))
        {
            SearchUrl = text.Trim();
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var window = GetMainWindow();
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
                    tempResult.VideoStreams = streamManifest.GetVideoOnlyStreams()
                        .OrderByDescending(s => s.VideoQuality.MaxHeight)
                        .Select(s => new VideoStreamInfo
                        {
                            Url = s.Url,
                            Container = s.Container,
                            VideoQuality = s.VideoQuality.Label,
                            Bitrate = s.Bitrate,
                            Size = s.Size
                        });
                    tempResult.AudioStreams = streamManifest.GetAudioOnlyStreams()
                        .OrderByDescending(s => s.Bitrate.BitsPerSecond)
                        .Select(s => new AudioStreamInfo
                        {
                            Url = s.Url,
                            Container = s.Container,
                            Bitrate = s.Bitrate,
                            AudioLanguage = s.AudioLanguage.ToString() ?? "Default",
                            Size = s.Size
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

    [RelayCommand]
    private void WatchLiveStream()
    {
    }

    [RelayCommand(CanExecute = nameof(CanWatchOrDownload))]
    private void Watch()
    {
    }

    [RelayCommand(CanExecute = nameof(CanWatchOrDownload))]
    private async Task DownloadAsync()
    {
        if (VideoResult == null || SelectedStream == null) return;
        var downloadProgressWindow = new DownloadProgressWindow
        {
            DataContext =
                new DownloadProgressViewModel(_youtubeClient, VideoResult, SelectedStream, FfmpegPath),
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        await downloadProgressWindow.ShowDialog(GetMainWindow());
    }
}