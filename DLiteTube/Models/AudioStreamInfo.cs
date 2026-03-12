using YoutubeExplode.Videos.Streams;

namespace DLiteTube.Models;

public class AudioStreamInfo : IStreamInfo
{
    public required string Url { get; init; }
    public required Container Container { get; init; }
    public required string ContainerString { get; init; }
    public required Bitrate Bitrate { get; init; }
    public required string BitrateString { get; init; }
    public required string AudioLanguage { get; init; }
    public required FileSize Size { get; init; }
    public required string SizeString { get; init; }
}