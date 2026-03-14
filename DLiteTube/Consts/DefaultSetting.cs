using System;

namespace DLiteTube.Consts;

public static class DefaultSetting
{
    public static string FfmpegPath
    {
        get
        {
            if (OperatingSystem.IsWindows())
                return "ffmpeg/ffmpeg.exe";
            if (OperatingSystem.IsLinux())
                return "ffmpeg/ffmpeg_linux";
            return OperatingSystem.IsMacOS()
                ? "ffmpeg/ffmpeg_mac"
                : throw new PlatformNotSupportedException("Unsupported operating system.");
        }
    }

    public static string DownloadPath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    public static bool AlwaysAskDownloadPath => true;
}