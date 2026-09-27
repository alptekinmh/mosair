# App.axaml + App.axaml.cs

## Genel Bakis

Avalonia uygulamasinin tanim dosyalari. AXAML dosyasi global tema ve stil yapilandirmasini, C# code-behind dosyasi ise uygulama yasam dongusu olaylarini yonetir. Standart Avalonia sablon dosyasidir.

## App.axaml

```xml
<Application RequestedThemeVariant="Default">
    <Application.Styles>
        <FluentTheme />
    </Application.Styles>
</Application>
```

- **RequestedThemeVariant="Default"** — Isletim sisteminin tema tercihini takip eder (Dark veya Light). Kullanici sistemi karanlik temaya alirsa uygulama da karanlik temaya gecer.
- **FluentTheme** — Microsoft Fluent Design stilini tum kontrollere uygular. Avalonia'nin dahili tema paketi.

## App.axaml.cs

### Initialize()
`AvaloniaXamlLoader.Load(this)` ile AXAML tanimi yuklenir. Tema, stiller ve kaynaklarin kullanilabilir hale gelmesini saglar.

### OnFrameworkInitializationCompleted()
Framework hazir oldugunda cagrilir:
- `ApplicationLifetime` tipi `IClassicDesktopStyleApplicationLifetime` ise (masaustu modu), `MainWindow` olusturulur ve `desktop.MainWindow` olarak atanir.
- `base.OnFrameworkInitializationCompleted()` cagrisiyla Avalonia'nin dahili baslangic islemleri tamamlanir.

## Diger Dosyalarla Iliskisi

| Dosya | Iliski |
|-------|--------|
| `Program.cs` | `BuildAvaloniaApp().Configure<App>()` ile bu sinif referans edilir |
| `MainWindow.axaml` | `OnFrameworkInitializationCompleted` icinde `new MainWindow()` ile olusturulur |
| `mosairMac.csproj` | FluentTheme ve Inter font icin `Avalonia.Themes.Fluent` ve `Avalonia.Fonts.Inter` NuGet paketleri gereklidir |
