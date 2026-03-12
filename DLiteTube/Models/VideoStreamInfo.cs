using System;
using YoutubeExplode.Videos.Streams;

namespace DLiteTube.Models;

public class VideoStreamInfo : IStreamInfo
{
    public required string Url { get; init; }
    public required Container Container { get; init; }
    public required string VideoQuality { get; init; }
    public required Bitrate Bitrate { get; init; }
    public string BitrateString => Math.Round(Bitrate.KiloBitsPerSecond, 2) + " Kbps";
    public required FileSize Size { get; init; }
}