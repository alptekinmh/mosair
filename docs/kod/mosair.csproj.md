# mosair.csproj

> Kaynak: `mosair/mosair.csproj` (+ `mosair/app.manifest`, `mosair/Info.plist`, `mosair/Assets/`) · Güncelleme: 2026-10-06

## Amaç

Uygulamanın MSBuild proje dosyası: hedef çatı, derleme seçenekleri, sürüm/marka bilgileri, NuGet paketleri ve çıktıya kopyalanacak varlıklar (renk kataloğu, taş dokuları, simgeler, macOS `Info.plist`).

## Nerede kullanılır

- Yerel geliştirme: `dotnet build mosair/mosair.csproj`, `dotnet run --project mosair`.
- CI: `.github/workflows/build.yml` içinde `dotnet publish mosair/mosair.csproj ...` (bkz. [build-workflow](build-workflow.md)).

## Yapı

### `PropertyGroup`

| Ad | Değer | Açıklama |
|---|---|---|
| `OutputType` | `WinExe` | Windows'ta konsol penceresi açılmaz (GUI alt sistemi). |
| `TargetFramework` | `net10.0` | .NET 10. |
| `Nullable` | `enable` | — |
| `AllowUnsafeBlocks` | `true` | Bitmap piksel işlemlerinde `unsafe` işaretçi kodu için (`PixelEditService`, `StoneTextureService`). |
| `ApplicationManifest` | `app.manifest` | Yalnızca derleme makinesi Windows ise (`IsOSPlatform('Windows')`). |
| `AssemblyName` | `mosair` | Çıktı ikilisinin adı; `Info.plist` içindeki `CFBundleExecutable` ile aynı olmalı. |
| `Version` | `1.3.1` | Yerel derleme sürümü (son sürümle aynı tutulur). **CI'da etiketten gelen sürümle ezilir** (`-p:Version=...`). |
| `Company` / `Product` / `Copyright` | `ESCRobotics` / `mosair` / `© 2026 ESCRobotics` | Dosya özellikleri. |
| `Description` | `Mozaik Tas Uretim Sistemi — Musteri Uygulamasi` | — |
| `ApplicationIcon` | `Assets\mosair.ico` | Windows exe simgesi. |

### Paketler

| Paket | Sürüm | Not |
|---|---|---|
| `Avalonia` | 12.1.2 | Arayüz çatısı. |
| `Avalonia.Desktop` | 12.1.2 | Masaüstü platform desteği (`UsePlatformDetect`). |
| `Avalonia.Themes.Fluent` | 12.1.2 | `App.axaml` içindeki `FluentTheme`. |
| `Avalonia.Fonts.Inter` | 12.1.2 | `WithInterFont()`. |
| `SkiaSharp` | 3.119.4 | Görüntü yükleme, yeniden boyutlandırma, doku ve dışa aktarma. |
| `AvaloniaUI.DiagnosticsSupport` | 2.2.3 | Yalnızca Debug: Release'te `IncludeAssets=None`, `PrivateAssets=All` (`WithDeveloperTools()` ile eşleşir). |

Avalonia paketlerinin dördü aynı sürümde tutulmalıdır; biri yükseltilirken hepsi birlikte yükseltilmelidir.

### Varlıklar (items)

| Öğe | Tür | Davranış |
|---|---|---|
| `Assets\mosair.ico`, `Assets\mosair-icon.png` | `AvaloniaResource` | Derlemeye gömülür; `avares://mosair/Assets/mosair-icon.png` ile pencere simgesi olarak kullanılır. CI ayrıca PNG'den macOS `.icns` üretir. |
| `Info.plist` | `Content`, `PreserveNewest` | Yalnızca derleme makinesi macOS ise (`IsOSPlatform('OSX')`) çıktıya kopyalanır. |
| `Assets\colorsBas.txt` | `None Update`, `PreserveNewest` | Renk kataloğu, çıktıda `Assets/colorsBas.txt`. |
| `Assets\02_RS\**\*` | `Content`, `PreserveNewest`, `Link` | Taş dokuları, klasör yapısı korunarak `Assets/02_RS/...` altına kopyalanır. |

### `Assets/` klasörü

| İçerik | Açıklama |
|---|---|
| `colorsBas.txt` | 124 satırlık taş kataloğu. Her satır boşlukla ayrılmış: `R G B Kod Ad Yüzey…` (örn. kod `B101`). Satır sırası taş `ID`'sidir (1'den başlar). `ColorCatalogService.LoadCatalog` okur. |
| `02_RS/` | Her taş için bir alt klasör (124 adet), adı `<ID>_<Kod>_<ad>_<R G B>` biçiminde. Her klasörde 16–22 adet `.jpg` doku fotoğrafı (`1.jpg`, `2.jpg` …). `StoneTextureService` kod adına göre eşleştirip mozaik görüntüsünü ("RS") dokulu üretir. |
| `mosair.ico` | Windows simgesi. |
| `mosair-icon.png` | Pencere simgesi ve macOS `.icns` kaynağı. |

### `app.manifest` (yalnızca Windows)

Windows 10 uyumluluk kimliğini (`supportedOS`) bildirir; Avalonia'nın pencere saydamlığı ve gömülü kontrolleri için şablonda bırakılması önerilen dosyadır.

### `Info.plist` (yalnızca macOS)

| Anahtar | Değer |
|---|---|
| `CFBundleName` / `CFBundleDisplayName` | `mosair` |
| `CFBundleIdentifier` | `com.escrobotics.mosair` |
| `CFBundleVersion` / `CFBundleShortVersionString` | `1.3.1` (CI'da `plutil` ile etiket sürümüne çevrilir) |
| `CFBundleExecutable` | `mosair` |
| `CFBundleIconFile` | `mosair` (CI'nın ürettiği `mosair.icns`) |
| `LSMinimumSystemVersion` | `12.0` |
| `NSHighResolutionCapable` | `true` |

## Public API

Yoktur (derleme yapılandırması).

## Önemli davranışlar ve iş kuralları

- **Sürüm:** Sürümün tek doğru kaynağı git etiketidir. CI `v1.2.3` etiketinden `1.2.3` üretip hem `-p:Version` hem `Info.plist` için kullanır. csproj ve `Info.plist` içindeki `1.3.1` değerleri yalnızca yerel derlemeyi etkiler; her yeni sürümde ikisi de etiketle aynı yapılır.
- Varlıklar `AppContext.BaseDirectory` altında aranır; tek dosya yayında (`PublishSingleFile`) `Content` dosyaları ikilinin yanına çıkarılır, gömülmez.

## Dikkat / bilinen sınırlamalar

- `IsOSPlatform(...)` koşulları **derlemeyi yapan makinenin** işletim sistemine bakar, hedef RID'e değil. Windows'ta `-r osx-arm64` ile yayın yapılırsa `Info.plist` çıktıya girmez ve CI'daki `.app` adımı (`mv .../Info.plist`) başarısız olur. macOS paketleri macOS üzerinde derlenmelidir (CI böyle yapar).
- `app.manifest` içindeki `assemblyIdentity` adı hâlâ eski `mosairMac.Desktop` değerini taşır.
- `colorsBas.txt` ile `02_RS` klasör adları elle eşlenir; kataloğa taş eklerken ikisi birlikte güncellenmelidir.

## İlgili dosyalar

- [build-workflow](build-workflow.md)
- [Program](Program.md)
- [App](App.md)
- [ColorCatalogService](Services/ColorCatalogService.md)
- [StoneTextureService](Services/StoneTextureService.md)
