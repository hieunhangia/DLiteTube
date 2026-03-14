using System;
using System.IO;
using System.Text.Json;
using DLiteTube.Consts;

namespace DLiteTube.Models;

public record Setting(string FfmpegPath, string DownloadPath, bool AlwaysAskDownloadPath)
{

    public static Setting GetDefault() => 
        new(DefaultSetting.FfmpegPath, DefaultSetting.DownloadPath, DefaultSetting.AlwaysAskDownloadPath);

    public static void SaveToFile(Setting setting) =>
        File.WriteAllText(FilePath.SettingsFileName,
            JsonSerializer.Serialize(setting, AppJsonSerializerContext.Default.Setting));

    public static Setting LoadSettings()
    {
        try
        {
            return JsonSerializer.Deserialize<Setting>(File.ReadAllText(FilePath.SettingsFileName),
                AppJsonSerializerContext.Default.Setting) ?? throw new Exception();
        }
        catch
        {
            SaveToFile(GetDefault());
            return GetDefault();
        }
    }

    public static void CheckAndInitializeIfNotValid() => LoadSettings();
}