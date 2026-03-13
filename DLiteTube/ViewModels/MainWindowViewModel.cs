using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
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

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private IStreamInfo? _selectedStream;

    private static MainWindow GetMainWindow()
    {
        return Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow window }
            ? window
            : throw new InvalidOperationException(
                "Application lifetime is not a classic desktop style or main window is not found.");
    }

    private bool CanDownload() => SelectedStream != null;

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

                    // Filter out WebM video-only streams
                    tempResult.VideoStreams = streamManifest.GetVideoOnlyStreams()
                        .Where(s => s.Container != Container.WebM)
                        .OrderByDescending(s => s.VideoQuality.MaxHeight)
                        .Select(s => new VideoStreamInfo
                        {
                            Url = s.Url,
                            Container = s.Container,
                            VideoQuality = s.VideoQuality.Label,
                            Bitrate = s.Bitrate,
                            Size = s.Size
                        });

                    // Filter out WebM audio-only streams
                    tempResult.AudioStreams = streamManifest.GetAudioOnlyStreams()
                        .Where(s => s.Container != Container.WebM)
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

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAsync()
    {
        if (VideoResult == null || SelectedStream == null) return;

        var options = new FilePickerSaveOptions
        {
            Title = "Chọn nơi lưu file tải xuống",
            DefaultExtension = SelectedStream.Container.Name,
            SuggestedFileName = $"{GetSafeFileName(VideoResult.Title)}.{SelectedStream.Container.Name}",
            FileTypeChoices = [new FilePickerFileType(SelectedStream.Container.Name)],
            SuggestedFileType = new FilePickerFileType(SelectedStream.Container.Name)
        };

        var file = await GetMainWindow().StorageProvider.SaveFilePickerAsync(options);
        if (file == null) return;

        var downloadProgressWindow = new DownloadProgressWindow
        {
            DataContext = new DownloadProgressViewModel(_youtubeClient, file.TryGetLocalPath()!, VideoResult,
                SelectedStream, FfmpegPath),
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        downloadProgressWindow.Show(GetMainWindow());

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
}