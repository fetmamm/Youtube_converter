# YouTube Converter

A clean, simple Windows app written in C# / WPF that downloads YouTube videos as **MP3** (audio) or **MP4** (video).

---

## ⚠️ Important legal disclaimer — read before using

**This software is provided for personal, lawful use only.**

YouTube's [Terms of Service](https://www.youtube.com/static?template=terms) generally **prohibit** downloading content from the platform unless a download button or link is explicitly provided by YouTube, or you have the rights holder's written permission.

By downloading, building, or running this software, **you acknowledge that:**

- You will use it **only** with content you own, Creative Commons / public-domain material, or content you have explicit permission to download.
- You will **not** use it to download copyrighted material without authorisation, circumvent paywalls or geo-restrictions, or redistribute downloaded content.
- You understand that misuse may expose you to **civil damages, fines, account suspension, or criminal liability** under the copyright laws of your country.
- The author and contributors of this project **provide no warranty** and **assume no legal or financial responsibility** for how you use the software. Liability rests entirely with the end user.

This tool is published under the [MIT License](LICENSE), which explicitly disclaims all warranties and liability. If you do not agree to these terms, do not use this software.

---

## Features

- **Paste / drag-and-drop** a YouTube URL — or use the Paste shortcut.
- **Analyze** before download — preview title, channel, length and thumbnail.
- Choose **MP3** (audio) or **MP4** (video) output.
- **Quality picker** for MP4 (Best / 1080p / 720p / 480p / 360p).
- **Trim video** — set start / end timestamps before export.
- **Save As** dialog — pick destination and filename freely, last used folder is remembered.
- **Show folder** button — opens the destination folder in Explorer.
- **Cancel** an in-progress download anytime.
- **History** of the last 20 downloads with quick "open folder" and "remove" actions.
- **Auto-update check** against GitHub releases on startup.
- **Fluent / Mica** dark theme (built on WPF-UI).
- **Self-contained** — no .NET runtime required for end users.

## Download

Grab the latest release from <https://github.com/fetmamm/Youtube_converter/releases>:

- **`.msi`** — Windows installer (adds Start menu + desktop shortcut, uninstall via Settings).
- **`.zip`** — Portable build, extract and run `YoutubeConverter.exe`.

## Versioning & releases

The version number lives in the [`VERSION`](VERSION) file at repo root (format `MAJOR.MINOR.PATCH`, e.g. `1.0.0`).

**To cut a new release:**

1. Edit `VERSION`, bump the number, commit & push to `main`.
2. GitHub Actions ([release.yml](.github/workflows/release.yml)) automatically:
   - Builds a self-contained Windows x64 binary.
   - Downloads FFmpeg.
   - Produces `YoutubeConverter-v{version}-win-x64.zip` (portable).
   - Produces `YoutubeConverter-v{version}-win-x64.msi` (installer via WiX 5).
   - Publishes a GitHub release tagged `v{version}` with both files attached.

The app displays its own version next to the title and checks GitHub on startup. If a newer release exists, a yellow **Updates** button highlights it.

**Manual trigger:** GitHub → Actions → Release → Run workflow.

## Building from source

### Prerequisites

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **FFmpeg** — drop `ffmpeg.exe` into `src/YoutubeConverter/ffmpeg/ffmpeg.exe`. Get a Windows build from <https://www.gyan.dev/ffmpeg/builds/> (the "release essentials" zip).

### Build & run

```powershell
.\Build.ps1            # dotnet build -c Release
.\Run.vbs              # launches the built app
```

Or directly:

```powershell
cd src\YoutubeConverter
dotnet build -c Release
dotnet run -c Release
```

### Publish a portable build locally

```powershell
.\Publish.ps1
```

Output lands in `dist/YoutubeConverter.exe` — self-contained, no .NET runtime required on the target machine.

## Project structure

```
.
├── VERSION                                    # Single source of version truth
├── Run.vbs / Build.ps1 / Publish.ps1          # Convenience launchers
├── .github/workflows/release.yml              # Auto-release pipeline
├── installer/Product.wxs                      # WiX installer definition
├── icons/                                     # Source for the app icon
└── src/YoutubeConverter/
    ├── App.xaml(.cs)                          # App entry + theme
    ├── MainWindow.xaml(.cs)                   # Main UI
    ├── ViewModels/MainViewModel.cs            # MVVM state (URL, format, history, etc.)
    ├── Services/
    │   ├── YoutubeDownloadService.cs          # YoutubeExplode + FFmpeg integration
    │   ├── SettingsService.cs                 # JSON-persisted settings (history, last folder)
    │   └── UpdateService.cs                   # GitHub releases check
    ├── Converters/                            # XAML value converters
    ├── app.ico                                # Application icon
    └── ffmpeg/ffmpeg.exe                      # (gitignored — add yourself)
```

User data (history, settings) is stored in `%APPDATA%\YoutubeConverter\settings.json`.

## Libraries

- [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) — stream extraction
- [YoutubeExplode.Converter](https://github.com/Tyrrrz/YoutubeExplode) — FFmpeg-based muxing/conversion
- [WPF-UI](https://github.com/lepoco/wpfui) — Fluent design controls
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) — MVVM helpers

## License

Released under the [MIT License](LICENSE) — you may use, modify, and distribute this code freely, provided the copyright notice is preserved. The MIT License explicitly disclaims all warranties and liability.

## Contributing

Bug reports, feature ideas and pull requests are welcome. Join the [Discord](https://discord.gg/9BJcKrDmbG) for chat.
