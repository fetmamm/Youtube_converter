# YouTube Converter

En enkel, stilren Windows-app i C# / WPF för att ladda ner YouTube-videor som **MP3** (ljud) eller **MP4** (video).

## Funktioner
- Klistra in en YouTube-URL (eller använd "Klistra in"-knappen).
- Välj MP3 eller MP4.
- Klicka **Exportera…** → Spara som-dialog → välj plats och filnamn.
- Progress-bar och status visas under nedladdning.
- Fluent/Mica-design med mörkt tema (WPF-UI).

## Versionshantering & releases

Versionsnummer styrs av filen [`VERSION`](VERSION) i projektroten (format: `MAJOR.MINOR.PATCH`, t.ex. `1.0.0`).

**Så skapar du en ny release:**
1. Öppna `VERSION`, ändra numret (t.ex. `1.0.0` → `1.0.1`), commit och push till `main`.
2. GitHub Actions ([release.yml](.github/workflows/release.yml)) triggar automatiskt:
   - Bygger appen self-contained för Windows x64
   - Laddar ner FFmpeg
   - Skapar `YoutubeConverter-v{version}-win-x64.zip` (portable)
   - Skapar `YoutubeConverter-v{version}-win-x64.msi` (installer, via WiX 5)
   - Publicerar release `v{version}` på GitHub med båda filerna bifogade
3. Användare kan ladda ner från <https://github.com/fetmamm/Youtube_converter/releases>

Appen visar sin egen version nere i högra hörnet och kollar GitHub vid start — om en nyare release finns dyker en gul "Ny version tillgänglig"-knapp upp.

**Manuell trigger:** GitHub → Actions → Release → Run workflow.

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
