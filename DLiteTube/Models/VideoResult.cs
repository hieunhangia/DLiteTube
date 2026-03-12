using System.Collections.Generic;

namespace DLiteTube.Models;

public class VideoResult
{
    public required string Title { get; init; }
    public required string Author { get; init; }
    public required string Duration { get; init; }
    public required string Url { get; init; }
    public required string ThumbnailUrl { get; init; }
    public bool IsLiveStream => string.IsNullOrWhiteSpace(Duration) || Duration == "00:00:00";
    public IEnumerable<VideoStreamInfo>? VideoStreams { get; set; }
    public IEnumerable<AudioOnlyStreamInfo>? AudioOnlyStreams { get; set; }
}