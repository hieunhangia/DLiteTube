namespace DLiteTube.Models;

public class AudioOnlyStreamInfo : IStreamInfo
{
    public required string Url { get; init; }
    public required string Container { get; init; }
    public required string AudioBitrate { get; init; }
    public required string AudioLanguage { get; init; }
    public required string FileSize { get; init; }
}