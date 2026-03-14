using System;
using System.IO;
using System.Text.Json;
using DLiteTube.Consts;

namespace DLiteTube.Models;

public class Setting
{
    public required string FfmpegPath { get; init; }
    public required string DownloadPath { get; init; }
    public required bool AlwaysAskDownloadPath { get; init; }

    public static Setting GetDefault() =>
        new()
        {
            FfmpegPath = DefaultSetting.FfmpegPath,
            DownloadPath = DefaultSetting.DownloadPath,
            AlwaysAskDownloadPath = DefaultSetting.AlwaysAskDownloadPath
        };

    public static void SaveToFile(Setting setting, string filePath) =>
        File.WriteAllText(filePath, JsonSerializer.Serialize(setting));

    public static Setting LoadSettings()
    {
        const string filePath = FilePath.SettingsFileName;
        try
        {
            return JsonSerializer.Deserialize<Setting>(File.ReadAllText(filePath)) ?? throw new Exception();
        }
        catch
        {
            SaveToFile(GetDefault(), filePath);
            return GetDefault();
        }
    }

    public static void CheckAndInitializeIfNotValid() => LoadSettings();
}