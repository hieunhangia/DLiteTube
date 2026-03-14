# DLiteTube

Cross-platform Avalonia desktop app to search and download YouTube videos or audio.


## Usage
1) Launch the app, paste a YouTube URL, and press Enter or the search button.
2) If the video is not live, pick a video quality or an audio-only stream.
3) Click "Download" to download. If "Always ask for download location" is enabled, pick a save location; otherwise the file saves to the configured download folder.
4) Monitor progress in the popup window; cancel if needed.
5) A binary of FFmpeg for each platform is bundled with the app, but you can also configure a custom FFmpeg path in the settings if you have a different version or want to use your own build.


## Building (Native AOT)
To build the app, you need the .NET 10 SDK installed. You can publish the app as a self-contained executable for each platform using the following commands:
```bash
# Windows x64
 dotnet publish -c Release -r win-x64 -p:PublishAot=true
 
# Linux x64
 dotnet publish -c Release -r linux-x64 -p:PublishAot=true
 
# macOS x64
 dotnet publish -c Release -r osx-x64 -p:PublishAot=true
```
The output executable will be located in the `bin/Release/net10.0/<runtime>/publish` directory, where `<runtime>` corresponds to the target platform (e.g., `win-x64`, `linux-x64`, or `osx-x64`).
