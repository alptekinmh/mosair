# MosaicExporter

> Kaynak: `mosair/Services/MosaicExporter.cs` · Güncelleme: 2026-10-06

## Amaç

Taş dokulu görüntünün (RS) tamamını, kullanıcının dışa aktarma listesinde seçtiği görüntü kalitesiyle (taş başına piksel `n`; 10, 20, …, 100), boyut sınırı olmadan JPEG ya da PNG dosyasına yazan statik sınıf. Ayrıca listedeki seçeneklerde gösterilen piksel boyutunu ve tahmini dosya boyutunu hesaplar. Çizimi [MosaicRenderSource](./MosaicRenderSource.md) yapar; bu sınıf yalnızca görüntünün nasıl parçalanıp kodlanacağına karar verir.

Eski sürümde çok büyük mozaiklerde taş başına piksel kendiliğinden düşürülürdü. Artık hiçbir zaman değiştirilmez; daha küçük bir dosya için daha düşük bir görüntü kalitesi seçmek kullanıcının işidir. Dosya her zaman listede gösterilen boyuttadır.

## Nerede kullanılır

| Çağıran | Kullanım |
|---|---|
| `MainViewModel.ExportImageAsync` | `ImageSize`, `JpegPossible`, `JpegMaxSide` (JPEG sığmıyorsa `ExportJpegTooLarge` uyarısı), `JpegMemoryBytes` (büyük JPEG için bellek onayı `ExportJpegMemoryConfirm`), ardından arka planda `Export(…)`; görüntü `ImageService.MaxBitmapPixels`'tan büyükse ilerleme geri çağrısıyla |
| `MainViewModel` (dışa aktarma listesi: `ExportChoiceLabel`, `QuickExportExtension`, `RefreshExportEstimatesAsync`) | `ImageSize`, `JpegPossible`, `QuickExportUsesJpeg` (mosairEXPORT'un biçimi), `EstimateBytes` (gösterilen tahmini dosya boyutu) |

## Yapı

`public static class MosaicExporter`

| Ad | Tip | Açıklama |
|---|---|---|
| `JpegMaxSide` | `const int` = `65535` | JPEG biçiminin kenar başına en fazla piksel sayısı |
| `Size` | `sealed record Size(int Width, int Height)` | Görüntünün piksel boyutu; `Pixels` = `(long)Width * Height` |
| `StreamingPngWriter` | özel iç sınıf (`IDisposable`) | Satır satır yazan en küçük PNG yazıcısı (aşağıda) |
| `IdatStream` | özel iç sınıf (`Stream`) | Sıkıştırılmış akışı en fazla 1 MB'lık `IDAT` parçalarına böler |
| `CountingStream` | özel iç sınıf (`Stream`) | Baytları saklamadan sayar (tahmin için) |
| `CrcTable` | `uint[]` (özel, static) | PNG parçaları için CRC32 tablosu (`BuildCrcTable`) |

## Public API

| Üye | Ne yapar | Kimden çağrılır |
|---|---|---|
| `ImageSize(src, n)` → `Size` | `src.Cols * n` × `src.Rows * n` | `MainViewModel` |
| `JpegPossible(size)` → `bool` | Her iki kenar ≤ `JpegMaxSide` mı | `MainViewModel`, `QuickExportUsesJpeg`, `Export` |
| `JpegMemoryBytes(size)` → `long` | Büyük JPEG'in bellekte tuttuğu tampon: `Pixels * 4` bayt | `QuickExportUsesJpeg`, `MainViewModel.ExportImageAsync` |
| `FreeMemoryBytes()` → `long` | O an boş bellek: `GC.GetGCMemoryInfo()` ile `TotalAvailableMemoryBytes − MemoryLoadBytes` (0'dan küçük olmaz). | `QuickExportUsesJpeg`, `MainViewModel.ExportImageAsync` |
| `QuickExportUsesJpeg(size)` → `bool` | JPEG mümkün değilse `false`; görüntü `ImageService.MaxBitmapPixels`'a sığıyorsa `true`; daha büyükse JPEG tamponu o an boş belleğin (`FreeMemoryBytes()`) en fazla yarısıysa `true`, değilse `false` (PNG) | `MainViewModel` |
| `EstimateBytes(src, n, grid, gw, gc, jpeg)` → `long` | Tahmini dosya boyutu: mozaiğin birkaç parçası gerçekten aynı biçimde kodlanır, piksel başına bayt bütün görüntüye ölçeklenir (aşağıda) | `MainViewModel` (arka planda) |
| `Export(src, path, jpeg, n, grid, gw, gc, progress = null)` | Dosyayı yazar. JPEG istenip `JpegPossible` değilse `InvalidOperationException` (`ExportJpegTooLarge` metni); bu kontrol dosya açılmadan yapılır. Yazma sırasında hata olursa yarım kalan dosya silinir (`File.Delete`, hatası yutulur) ve özgün hata yeniden atılır. `progress` 0..1 arası değeri işçi iş parçacığından bildirir. | `MainViewModel.ExportImageAsync` (arka planda) |

## Önemli davranışlar ve iş kuralları

1. **Tek bitmap'e sığan görüntü** (`Pixels ≤ ImageService.MaxBitmapPixels`, ≈ 536,9 milyon piksel): Bütün görüntü tek bitmap olarak çizilir (`RenderRegion(0, 0, Rows, Cols, n, …)`) ve `ImageService.ExportImage` ile Skia'ya kodlatılır (taş görüntüleri yüklüyse önceki sürümle aynı kodlama). Sonda `progress(1)`.
2. **Daha büyük PNG, akışla yazma** (`ExportStreamedPng`): Her seferinde bir taş satırı (`RenderRegion(row, 0, 1, Cols, …)`) çizilir; bir sonraki satır `Task.Run` ile paralel hazırlanırken o anki satır doğrudan dosyaya sıkıştırılır. Bellek, görüntü ne kadar büyük olursa olsun birkaç taş satırı kadardır. İlerleme her satırdan sonra `(row + 1) / Rows`. Paralel çizilen satır `GetAwaiter().GetResult()` ile alındığı için çizimde çıkan hata `AggregateException` içinde değil, kendi tipiyle gelir.
3. **`StreamingPngWriter`:** PNG imzası, `IHDR` (8 bit, RGB, interlace yok), her piksel satırı için filtre "none" (0) + RGB baytları, `ZLibStream` (`CompressionLevel.Fastest`) → `IdatStream` (1 MB'lık `IDAT` parçaları), sonda `IEND`. Her parçanın CRC32'si kendi tablosuyla hesaplanır (`WriteChunk`). Alfa kanalı yazılmaz.
4. **Daha büyük JPEG** (`ExportLargeJpeg`): Bütün görüntü tek bir yerel bellek tamponuna (`NativeMemory.Alloc`, ≈ genişlik × yükseklik × 4 bayt) taş satırı taş satırı kopyalanır, sonra `SKPixmap.Encode` ile kodlanır (kalite 100, 4:2:0 alt örnekleme, alfa yok sayılır). İlerleme çizim için 0–0,8, kodlama bitince 1. Skia kodlaması başarısız olursa (ör. bellek yetmezse) `IOException` (`ExportJpegFailed`: "JPEG kaydedilemedi (bellek yetmemiş olabilir)…") atılır. Tampon `finally` içinde serbest bırakılır.
5. **JPEG kenar sınırı:** Kenarlardan biri 65.535 pikseli geçerse JPEG yazılamaz (biçimin kendi sınırı). Bu durumda dışa aktarma başlamaz; `MainViewModel` `ExportJpegTooLarge` uyarısıyla PNG ya da daha düşük bir görüntü kalitesi seçmeyi önerir. PNG için boyut sınırı yoktur.
6. **Hızlı dışa aktarmada biçim:** `QuickExportUsesJpeg` true ise JPEG, değilse PNG. JPEG yine **mosairEXPORT As** ile seçilebilir; o zaman tampon kullanılabilir belleğin yarısını aşıyorsa `MainViewModel.ExportImageAsync` önce `ExportJpegMemoryConfirm` onayını sorar.
7. **Taş görüntüleri yokken** (`HasTextures` false, ör. `02_RS` klasörü yoksa) `RenderRegion` her taşı kendi rengiyle, seçilen boyutta (taş başına `n` piksel) düz renk olarak çizer. Böylece dosya her durumda `ImageSize` ve `EstimateBytes`'ın varsaydığı boyuttadır (eski sürümdeki taş başına 1 piksellik dosya artık yazılmaz).
8. **Boyut tahmini** (`EstimateBytes`):
   - **JPEG:** Mozaiğin beş noktasından (dört köşeye yakın nokta ve orta) ≈ 512 × 512 px'lik kareler (`max(1, 512 / n)` taş) çizilip Skia ile kodlanır. Ölçümde gerçek boyutun ≈ %104'ü.
   - **PNG:** Mozaiğin %25, %50 ve %75 yüksekliğindeki üç tam genişlikte taş satırı, gerçek dışa aktarmanın kullanacağı yolla kodlanır: sığan görüntüde Skia PNG, akışla yazılacak görüntüde `StreamingPngWriter` + `CountingStream`. Ölçümde gerçek boyutun %106–116'sı. Taş görüntüleri satır boyunca tekrarlandığından küçük kareler PNG'yi ≈ 2 kat fazla tahmin ediyordu; bu yüzden tam satır kullanılır.
   - Sonuç: toplam bayt / toplam örnek piksel × bütün görüntünün piksel sayısı.
9. **Anlık görüntü:** `Export` ve `EstimateBytes` arka planda çalışır. Dışa aktarmada `MainViewModel` çizim kaynağı olarak `WithStoneSnapshot()` kopyasını verir; böylece dışa aktarma sürerken yapılan düzenlemeler dosyaya girmez.

### Ölçümler

| Mozaik | Görüntü | PNG | JPEG |
|---|---|---|---|
| 300 × 300 taş, N = 40 | 12.000 × 12.000 px | 93 MB, 9,4 s | 113 MB, 4,0 s |
| 600 × 600 taş, N = 40 | 24.000 × 24.000 px (576 Mpx, eski sınırın üstünde) | 498 MB, 4,6 s | 450 MB, 14,8 s |
| 20 m mozaik, 1667 × 1667 taş, N = 40 | 66.680 × 66.680 px (4,4 Gpx) | 2,8 GB, 26 s | Yazılamaz (kenar > 65.535) |

Büyük dosyalar doğrulanmıştır: bütün CRC'ler doğru, bütün satırlar çözülüyor, kesilen parçalar normal çiziciyle birebir aynı.

## Dikkat / bilinen sınırlamalar

- Büyük JPEG ≈ genişlik × yükseklik × 4 bayt RAM ister (ör. 60.000 × 60.000 px için ≈ 13,4 GB). mosairEXPORT bu yüzden belleğin yetmeyeceği durumda PNG seçer; **mosairEXPORT As** ile JPEG seçilirse kullanıcıya sorulur (`ExportJpegMemoryConfirm`). "Boş bellek" `FreeMemoryBytes()` = `TotalAvailableMemoryBytes − MemoryLoadBytes` (kurulu ya da izin verilen bellek eksi tüm sistemin o an kullandığı).
- `unsafe` kod kullanır (doğrudan piksel işaretçisi, `NativeMemory`).

## İlgili dosyalar

- [MosaicRenderSource](./MosaicRenderSource.md)
- [ImageService](./ImageService.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
- [MainWindow](../MainWindow.md)
- [Loc](./Loc.md)
- [Arayüz rehberi §13](../../ARAYUZ.md#13-dışa-aktarma-mosairexport)
