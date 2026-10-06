# Program

> Kaynak: `mosair/Program.cs` · Güncelleme: 2026-10-06

## Amaç

Uygulamanın giriş noktası. Komut satırına göre üç moddan birini başlatır:

- `--compare` ile başlarsa: arayüzsüz geliştirici karşılaştırma aracı (`CompareRunner`).
- `--stockcompare` ile başlarsa: arayüzsüz "Stoğa göre" ölçüm aracı (`StockCompareRunner`).
- Aksi halde: Avalonia masaüstü uygulaması.

## Nerede kullanılır

İşletim sistemi tarafından çalıştırılır. `BuildAvaloniaApp` ayrıca Avalonia görsel tasarımcısı (previewer) tarafından da kullanılır; bu yüzden silinmemeli ve imzası değiştirilmemelidir.

## Yapı

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `Main` | `static int` | — | `[STAThread]` öznitelikli giriş noktası; çıkış kodu döndürür. |

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `Main(string[] args)` | `args[0] == "--compare"` ise `CompareRunner.Run(args)`, `args[0] == "--stockcompare"` ise `StockCompareRunner.Run(args)` sonucunu döndürür; değilse `BuildAvaloniaApp().StartWithClassicDesktopLifetime(args)`. | İşletim sistemi |
| `BuildAvaloniaApp()` | `AppBuilder.Configure<App>()` → `UsePlatformDetect()` → (yalnızca `DEBUG`) `WithDeveloperTools()` → `WithInterFont()` → `LogToTrace()`. | `Main`, Avalonia previewer |

## Önemli davranışlar ve iş kuralları

- `--compare` ve `--stockcompare` kontrolleri Avalonia başlatılmadan önce yapılır; bu modlar hiçbir pencere açmaz.
- `WithDeveloperTools()` yalnızca Debug derlemesinde vardır (Avalonia geliştirici araçları, `AvaloniaUI.DiagnosticsSupport` paketi). Release derlemesinde bu paket `IncludeAssets=None` ile dışarıda bırakılır (bkz. csproj).
- `WithInterFont()` Inter yazı tipini (`Avalonia.Fonts.Inter`) gömülü olarak yükler; işletim sistemine bağımlı değildir.
- `Main` başlamadan önce Avalonia API'si veya `SynchronizationContext`'e bağlı kod kullanılmamalıdır (dosyadaki yorum).

## Dikkat / bilinen sınırlamalar

- Proje `OutputType=WinExe` olduğu için Windows'ta `--compare` / `--stockcompare` modlarının konsol çıktısı görünmeyebilir; ayrıntı için [CompareRunner](CompareRunner.md) ve [StockCompareRunner](StockCompareRunner.md).

## İlgili dosyalar

- [App](App.md)
- [CompareRunner](CompareRunner.md)
- [StockCompareRunner](StockCompareRunner.md)
- [MainWindow](MainWindow.md)
- [mosair.csproj](mosair.csproj.md)
