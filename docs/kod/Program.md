# Program

> Kaynak: `mosair/Program.cs` · Güncelleme: 2026-10-04

## Amaç

Uygulamanın giriş noktası. Komut satırına göre iki moddan birini başlatır:

- `--compare` ile başlarsa: arayüzsüz geliştirici karşılaştırma aracı (`CompareRunner`).
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
| `Main(string[] args)` | `args[0] == "--compare"` ise `CompareRunner.Run(args)` sonucunu döndürür; değilse `BuildAvaloniaApp().StartWithClassicDesktopLifetime(args)`. | İşletim sistemi |
| `BuildAvaloniaApp()` | `AppBuilder.Configure<App>()` → `UsePlatformDetect()` → (yalnızca `DEBUG`) `WithDeveloperTools()` → `WithInterFont()` → `LogToTrace()`. | `Main`, Avalonia previewer |

## Önemli davranışlar ve iş kuralları

- `--compare` kontrolü Avalonia başlatılmadan önce yapılır; karşılaştırma modu hiçbir pencere açmaz.
- `WithDeveloperTools()` yalnızca Debug derlemesinde vardır (Avalonia geliştirici araçları, `AvaloniaUI.DiagnosticsSupport` paketi). Release derlemesinde bu paket `IncludeAssets=None` ile dışarıda bırakılır (bkz. csproj).
- `WithInterFont()` Inter yazı tipini (`Avalonia.Fonts.Inter`) gömülü olarak yükler; işletim sistemine bağımlı değildir.
- `Main` başlamadan önce Avalonia API'si veya `SynchronizationContext`'e bağlı kod kullanılmamalıdır (dosyadaki yorum).

## Dikkat / bilinen sınırlamalar

- Proje `OutputType=WinExe` olduğu için Windows'ta `--compare` modunun konsol çıktısı görünmeyebilir; ayrıntı için [CompareRunner](CompareRunner.md).

## İlgili dosyalar

- [App](App.md)
- [CompareRunner](CompareRunner.md)
- [MainWindow](MainWindow.md)
- [mosair.csproj](mosair.csproj.md)
