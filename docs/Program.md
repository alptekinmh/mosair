# Program.cs

## Genel Bakis

Uygulamanin giris noktasi. `Main()` metodu Avalonia UI framework'unu yapilandirir ve masaustu uygulamayi baslatir. Standart Avalonia sablon dosyasidir.

## Yapisi

### Main(string[] args)
- `[STAThread]` attribute ile isaretli — Windows'ta COM ve UI thread modeli icin gereklidir.
- `BuildAvaloniaApp().StartWithClassicDesktopLifetime(args)` cagrisiyla uygulamayi baslatir.

### BuildAvaloniaApp()
Avalonia yapilandirmasini olusturur:
- `Configure<App>()` — `App` sinifini uygulama kok nesnesi olarak belirler.
- `UsePlatformDetect()` — Calisma platformunu (Windows, macOS, Linux) otomatik algilar ve uygun renderer'i secer.
- `WithDeveloperTools()` — Yalnizca `DEBUG` modunda aktif; F12 ile acilan Avalonia DevTools penceresini etkinlestirir.
- `WithInterFont()` — Inter yazi tipini varsayilan olarak yukler.
- `LogToTrace()` — Avalonia log ciktisini `System.Diagnostics.Trace` uzerinden yonlendirir.

## Diger Dosyalarla Iliskisi

| Dosya | Iliski |
|-------|--------|
| `App.axaml` / `App.axaml.cs` | `Configure<App>()` ile referans edilir; uygulama yasam dongusu buradan baslar |
| `mosairMac.csproj` | `OutputType: WinExe` ayari bu sinifin giris noktasi olmasini saglar |
