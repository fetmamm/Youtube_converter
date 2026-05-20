# YouTube Converter

En enkel, stilren Windows-app i C# / WPF för att ladda ner YouTube-videor som **MP3** (ljud) eller **MP4** (video).

## Funktioner
- Klistra in en YouTube-URL (eller använd "Klistra in"-knappen).
- Välj MP3 eller MP4.
- Klicka **Exportera…** → Spara som-dialog → välj plats och filnamn.
- Progress-bar och status visas under nedladdning.
- Fluent/Mica-design med mörkt tema (WPF-UI).

## Förutsättningar
- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **FFmpeg** — placera `ffmpeg.exe` i `src/YoutubeConverter/ffmpeg/ffmpeg.exe`.
  Ladda ner från <https://www.gyan.dev/ffmpeg/builds/> (välj "release essentials" zip, kopiera `bin/ffmpeg.exe`).
  Filen kopieras automatiskt till output-mappen vid build.

## Bygga och köra
```powershell
cd src\YoutubeConverter
dotnet restore
dotnet run
```

## Publicera som självständig .exe
```powershell
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```
Resultatet hamnar i `src/YoutubeConverter/bin/Release/net8.0-windows/win-x64/publish/`.

## Projektstruktur
```
src/YoutubeConverter/
├── App.xaml(.cs)              Applikationsstart + tema
├── MainWindow.xaml(.cs)       UI
├── ViewModels/MainViewModel   URL, format, progress, ExportCommand
├── Services/YoutubeDownload   YoutubeExplode + FFmpeg-wrapper
├── Converters/                XAML-konverterare
└── ffmpeg/ffmpeg.exe          (lägg till själv)
```

## Bibliotek
- [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) — strömhämtning
- [YoutubeExplode.Converter](https://github.com/Tyrrrz/YoutubeExplode) — FFmpeg-mux/konvertering
- [WPF-UI](https://github.com/lepoco/wpfui) — Fluent-design
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) — MVVM

## Juridisk notis
YouTubes användarvillkor förbjuder nedladdning utan tillstånd. Använd endast för eget material, Creative Commons-innehåll eller där rättighetsinnehavaren godkänner nedladdning.
