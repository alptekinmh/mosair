# App

> Kaynak: `mosair/App.axaml`, `mosair/App.axaml.cs` · Güncelleme: 2026-10-07

## Amaç

Avalonia `Application` sınıfı. Uygulama genelindeki temayı (Fluent, koyu) yükler ve masaüstü yaşam döngüsünde ana pencereyi (`MainWindow`) oluşturur.

## Nerede kullanılır

`Program.BuildAvaloniaApp` içinde `AppBuilder.Configure<App>()` ile tanıtılır; `StartWithClassicDesktopLifetime` çağrısı `App`'i başlatır.

## Yapı

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `RequestedThemeVariant` (XAML) | `ThemeVariant` | `Dark` | Başlangıç teması. Kullanıcı `MainWindow` üzerindeki tema düğmesiyle çalışma anında `Light`/`Dark` arasında geçer. |
| `Application.Styles` | — | `FluentTheme` + paylaşılan stiller | Avalonia Fluent teması; `FluentTheme.Palettes` içinde `Dark` ve `Light` için `ColorPaletteResources Accent` (`#2D6BD9` / `#1F5FCC`) verilir. Böylece Fluent'in kendi vurgu rengi (CheckBox, Slider, ProgressBar, ToggleButton, odak çerçevesi) işletim sisteminin vurgu rengini değil uygulamanınkini izler. |
| `MonoFont` (kaynak) | `FontFamily` | `JetBrains Mono, Cascadia Mono, Consolas, Menlo, monospace` | Eş aralıklı yazılar (taş kodları, ölçüler, değerler) için tek tanım; `{StaticResource MonoFont}` ile kullanılır (temaya bağlı değil). |
| `Button.primary` (stil) | — | — | Ana eylem düğmesi: `AccentFill` zemin, `OnAccent` yazı, köşe 4, SemiBold; `:pointerover` → `AccentFillHover`, `:pressed` → `AccentFillPressed`, `:disabled` → `BgHover` / `FgDisabled` (`/template/ ContentPresenter` hedeflenir). Alert, Confirm, Stock/Drive ayarları ve Drive'dan Aç pencerelerinin onay düğmesi ve yeni görsel bildirimindeki **Aç** kullanır. |
| `ToggleButton.chip` (stil) | — | — | Katalog üstündeki küçük aç/kapa düğmesi (Kalıp Dolgu): kapalıyken `BgInput`/`FgSecondary`, açıkken `AccentFill`/`OnAccent`. |

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `Initialize()` | `AvaloniaXamlLoader.Load(this)` ile `App.axaml`'ı yükler. | Avalonia |
| `OnFrameworkInitializationCompleted()` | `ApplicationLifetime` bir `IClassicDesktopStyleApplicationLifetime` ise `desktop.MainWindow = new MainWindow()`. | Avalonia |

## Önemli davranışlar ve iş kuralları

- Uygulamaya özel renkler `App.axaml` içindeki `Application.Resources` → `ResourceDictionary.ThemeDictionaries` (`Dark`/`Light`) altında `Color` olarak tanımlıdır. Uygulama düzeyinde oldukları için ana pencere, iletişim kutuları ve kullanım kılavuzu aynı renkleri `DynamicResource` ile kullanır ve tema değişince birlikte değişir. Değerler (koyu / açık):

| Anahtar | Koyu | Açık | Kullanım |
|---|---|---|---|
| `BgMain` / `BgBar` / `BgPanel` | `#18191C` / `#1D1E22` / `#222328` | `#F3F4F6` / `#E9EBEE` / `#E3E5E9` | Pencere, çubuklar, paneller |
| `BgInput` / `BgCard` / `BgCanvas` | `#16171A` / `#2A2B31` / `#2C2D31` | `#FFFFFF` / `#FBFBFC` / `#E6E7EA` | Giriş/kart, iletişim kutusu, görsel alanı (nötr gri; taş renkleri yanıltmasın diye). Görsel yüklenince MainWindow görsel alanını ve ölçü bölümünü görselin rengine uyan sakin bir tonla boyar (`ApplyImageTint`); `BgCanvas` / `BgBar` görsel yokken kullanılır. |
| `BgHover` / `BgPressed` | `#33343B` / `#3C3D45` | `#D8DBE0` / `#CDD0D6` | Üzerine gelme / basılı |
| `BrdrMain` / `BrdrSec` / `BrdrTer` | `#303137` / `#41424A` / `#6B6C76` | `#D0D3D9` / `#BCC0C7` / `#7D8089` | Ayırıcılar, çerçeveler; artık yazı rengi olarak kullanılmaz |
| `FgPrimary` / `FgSecondary` / `FgMuted` / `FgDisabled` | `#ECECF0` / `#B4B5BE` / `#9294A0` / `#62636C` | `#1B1C20` / `#464953` / `#5C5F68` / `#8D9099` | Metin; Muted en az 4,6:1 okunurluk |
| `FgIcon` / `FgMenu` / `NavBg` / `FgWarn` | `#B4B5BE` / `#D6D7DD` / `#CC18191C` / `#E3A54F` | `#464953` / `#2A2B31` / `#CCF3F4F6` / `#8A5106` | İkon, menü metni, mini harita zemini, uyarı |
| `AccentFill` / `AccentFillHover` / `AccentFillPressed` | `#2D6BD9` / `#3672DE` / `#255DC0` | `#1F5FCC` / `#2766D4` / `#1A50AD` | Vurgu ("Lapis" mavisi): Mos ve ana düğmeler, ilerleme, dalga |
| `OnAccent` / `AccentText` / `AccentBorder` / `AccentSubtle` | `#FFFFFF` / `#6FA3FF` / `#6FA3FF` / `#262D6BD9` | `#FFFFFF` / `#1D5BC4` / `#1F5FCC` / `#1F1F5FCC` | Vurgu üstü yazı, vurgu renkli yazı/ikon, seçim/mini harita çerçevesi, mini harita dolgusu |
| `Success` / `Danger` / `DangerText` / `EditMode` | `#4CC27A` / `#E53935` / `#F2665E` / `#FF7A29` | `#17703D` / `#E53935` / `#B71C1C` / `#B23A0A` | Kaydedildi ✓; kırmızı nokta ve İptal çerçevesi; kırmızı yazı; piksel düzenleme |
| `Brand` / `BrandFill` | `#6FAF6F` / `#3F7A3F` | `#356B35` / `#3F7A3F` | "mosair" yazısı (logodaki adaçayı yeşili), kılavuz başlığındaki logo kutusu |
- Tema değişimi `MainWindow.OnToggleTheme` içinde `Application.Current!.RequestedThemeVariant` atanarak yapılır; seçim kalıcı değildir, her açılışta koyu tema ile başlanır.
- Tek pencereli masaüstü uygulaması: mobil/tarayıcı yaşam döngüsü desteklenmez.

## Dikkat / bilinen sınırlamalar

- `App.axaml`'daki yorum `"Default"` değerinin sistem temasını izleyeceğini belirtir; şu an bilinçli olarak `Dark` sabitlenmiştir.
- Kodda sabit kalan birkaç renk temayı izlemez: `MainViewModel.KgOkBrush`/`KgShortBrush` (ipucundaki kg yeşil/kırmızı), Stock/Drive ayar pencerelerindeki örnek adres vurgusu, ızgara rengi menüsündeki gri çerçeve. Ayrıntı: [ARAYUZ_DEGISIKLIKLERI.md](../ARAYUZ_DEGISIKLIKLERI.md).
- `:pointerover` / `:pressed` görünümleri derlenip koyu temada ekran görüntüsüyle kontrol edildi; üzerine gelme ve basma durumları elle denenmedi.

## İlgili dosyalar

- [Program](Program.md)
- [MainWindow](MainWindow.md)
- [mosair.csproj](mosair.csproj.md)
