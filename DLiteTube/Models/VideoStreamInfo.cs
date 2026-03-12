namespace DLiteTube.Models;

public class VideoStreamInfo : IStreamInfo
{
    public required string Url { get; init; }
    public required string Container { get; init; }
    public required string VideoQuality { get; init; }
    public required string Bitrate { get; init; }
    public required string FileSizeWithBestAudio { get; init; }
}