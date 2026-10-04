# App

> Kaynak: `mosair/App.axaml`, `mosair/App.axaml.cs` · Güncelleme: 2026-10-04

## Amaç

Avalonia `Application` sınıfı. Uygulama genelindeki temayı (Fluent, koyu) yükler ve masaüstü yaşam döngüsünde ana pencereyi (`MainWindow`) oluşturur.

## Nerede kullanılır

`Program.BuildAvaloniaApp` içinde `AppBuilder.Configure<App>()` ile tanıtılır; `StartWithClassicDesktopLifetime` çağrısı `App`'i başlatır.

## Yapı

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `RequestedThemeVariant` (XAML) | `ThemeVariant` | `Dark` | Başlangıç teması. Kullanıcı `MainWindow` üzerindeki tema düğmesiyle çalışma anında `Light`/`Dark` arasında geçer. |
| `Application.Styles` | — | `<FluentTheme />` | Avalonia Fluent teması. |

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `Initialize()` | `AvaloniaXamlLoader.Load(this)` ile `App.axaml`'ı yükler. | Avalonia |
| `OnFrameworkInitializationCompleted()` | `ApplicationLifetime` bir `IClassicDesktopStyleApplicationLifetime` ise `desktop.MainWindow = new MainWindow()`. | Avalonia |

## Önemli davranışlar ve iş kuralları

- Uygulamaya özel renkler `App.axaml` içindeki `Application.Resources` → `ResourceDictionary.ThemeDictionaries` (`Dark`/`Light`) altında tanımlıdır (`BgMain`, `BgBar`, `BgPanel`, `BgCard`, `BgHover`, `FgPrimary`, `FgSecondary`, `FgMuted`, `FgMenu`, `FgWarn` …). Uygulama düzeyinde oldukları için ana pencere, iletişim kutuları ve kullanım kılavuzu aynı renkleri `DynamicResource` ile kullanır ve tema değişince birlikte değişir.
- Tema değişimi `MainWindow.OnToggleTheme` içinde `Application.Current!.RequestedThemeVariant` atanarak yapılır; seçim kalıcı değildir, her açılışta koyu tema ile başlanır.
- Tek pencereli masaüstü uygulaması: mobil/tarayıcı yaşam döngüsü desteklenmez.

## Dikkat / bilinen sınırlamalar

- `App.axaml`'daki yorum `"Default"` değerinin sistem temasını izleyeceğini belirtir; şu an bilinçli olarak `Dark` sabitlenmiştir.

## İlgili dosyalar

- [Program](Program.md)
- [MainWindow](MainWindow.md)
- [mosair.csproj](mosair.csproj.md)
