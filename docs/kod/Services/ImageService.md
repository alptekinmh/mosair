# ImageService

> Kaynak: `mosair/Services/ImageService.cs` · Güncelleme: 2026-10-06

## Amaç

SkiaSharp tabanlı görüntü yardımcıları: görsel yükleme (EXIF yönü düzeltilerek), yeniden boyutlandırma, `SKBitmap` ↔ `byte[,,]` (BGR) dönüşümü, Avalonia bitmap'e çevirme, dosyaya dışa aktarma ve ızgara/kalıp çizgisi bindirmesi. WPF'teki OpenCV (EmguCV) çağrılarının karşılığıdır.

Dosya ayrıca `InterpolationMethod` enum'unu tanımlar.

## Nerede kullanılır

| Çağıran | Kullanım |
|---|---|
| `MosaicEngine` | `LoadImage`, `Resize`, `ToByteArray`, `FromByteArray`; `ToByteArray` çıktısı (BGR) `OptimalPaletteService.Analyze` ve `StockAwareAssigner`'a girdi olarak verilir |
| `StoneTextureService` | `ToByteArray`, `FromByteArray`, `Resize` |
| `MainViewModel` | `LoadImage` (proje açılışında), `FromByteArray`, `ToAvaloniaBitmap` (görsel, genel görünüm), `MaxBitmapPixels` (dışa aktarmada ilerleme yüzdesi gösterme ve büyük JPEG için bellek onayı sorma eşiği) |
| `MosaicExporter` | `ExportImage` (tek bitmap'e sığan görüntü), `MaxBitmapPixels` (tek bitmap ile parça parça yazma arasındaki eşik) |
| `MosaicView` | `ToAvaloniaBitmap` (512 px'lik karolar) |
| `MainViewModel`, `MainWindow.axaml.cs`, `MosaicEngine`, `ProjectService`, `CompareRunner`, `StockCompareRunner` | `InterpolationMethod` enum'u |

## Yapı

### `InterpolationMethod` (enum)

`.mos` dosyasında `int` olarak saklanır (`ProjectData.InterpolationMethod`).

| Değer | Sayı | `Resize` davranışı |
|---|---|---|
| `Area` | 0 | Küçültmede alan ağırlıklı ortalama (kendi uygulaması); büyütmede Mitchell kübik |
| `Nearest` | 1 | `SKFilterMode.Nearest` |
| `Linear` | 2 | `SKFilterMode.Linear` |
| `Cubic` | 3 | `SKCubicResampler.Mitchell` |
| `Lanczos4` | 4 | `SKCubicResampler.CatmullRom` (gerçek Lanczos değil) |
| `LinearExact` | 5 | `Linear` ile aynı |
| `NearestExact` | 6 | `Nearest` ile aynı |

### `ImageService` (static)

Durum tutmaz. Özel yardımcılar: `ApplyOrientation` (EXIF yönü) ve `ResizeWithSampling` (`Area` dışındaki yöntemler için `SKBitmap.Resize` + `SKSamplingOptions`; tanınmayan değer `Nearest` olur).

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `LoadImage(path)` → `SKBitmap?` | Dosyayı `SKCodec` ile çözer; `EncodedOrigin` varsayılan değilse `ApplyOrientation` ile döndürür/aynalar. Dosya yoksa veya çözülemezse `null` | `MosaicEngine`, `MainViewModel.OpenProject` |
| `Resize(src, dstW, dstH, method = Area)` → `SKBitmap` | Yeniden boyutlandırır (yukarıdaki tablo) | `MosaicEngine`, `StoneTextureService` |
| `FromByteArray(data, rows, cols)` → `SKBitmap` | BGR `byte[rows, cols, 3]` → `Rgba8888` opak bitmap | `MosaicEngine`, `StoneTextureService`, `MainViewModel` |
| `ToByteArray(bitmap)` → `byte[,,]` | `Bgra8888` veya `Rgba8888` bitmap → BGR `byte[rows, cols, 3]` | `MosaicEngine`, `OptimalPaletteService`, `StoneTextureService` |
| `DrawOverlay(src, stoneSize, showGrid, showMouldLines, showRowColNum, showMouldId, penWidth, gridColor = null)` → `SKBitmap` | Kopya üzerine ızgara, kalıp çizgileri (26 taşta bir), kalıp numaraları ve satır/sütun numaraları çizer | Şu an çağıran yok |
| `ToAvaloniaBitmap(bmp)` → `Avalonia.Media.Imaging.Bitmap` | `Bgra8888`'e kopyalayıp `WriteableBitmap`'e aktarır (96 DPI, `Premul`) | `MainViewModel`, `MosaicView` |
| `ExportImage(bmp, path, format = Png, quality = 100)` | `SKImage.Encode` ile dosyaya yazar | `MosaicExporter` |

## Önemli davranışlar ve iş kuralları

- **Kanal sırası:** Projedeki tüm `byte[,,]` diziler (`MosaicData.dataM1`, `dataM3`, doku dizileri) **BGR** sırasındadır: `[y, x, 0] = B`, `[y, x, 1] = G`, `[y, x, 2] = R`. `FromByteArray` ve `ToByteArray` bu çeviriyi yapar.
- **Area küçültme:** Her hedef piksel için kaynakta kapladığı dikdörtgen alan, kısmi piksellerin kesir ağırlıklarıyla ortalanır (OpenCV INTER_AREA benzeri). Satırlar `Parallel.For` ile işlenir; sonuç `Rgba8888`'dir. Hedef her iki eksende de kaynaktan büyük/eşitse Mitchell kübik büyütme kullanılır.
- **EXIF yönü:** `TopRight`, `BottomRight`, `BottomLeft`, `LeftTop`, `RightTop`, `RightBottom`, `LeftBottom` durumları için tuval dönüşümü uygulanır; 90°'lik durumlarda genişlik/yükseklik yer değiştirir.
- **Boyut sınırı:** `ToAvaloniaBitmap`, `MaxBitmapPixels` (= `int.MaxValue / 4` ≈ 536,9 milyon piksel; SkiaSharp 2 GB'tan büyük bitmap ayıramaz) üstündeki bitmap'lerde `OutOfMemoryException` (`StatusRsBitmapTooLarge` metniyle) atar. Taş dokulu görüntü ekranda artık 512 px'lik karolar ve taş başına 1 piksellik genel görünüm olarak çevrildiği için bu sınır gösterimi kısıtlamaz. Dışa aktarma bu sınıra sığan görüntüyü eskisi gibi tek bitmap olarak `ExportImage` ile yazar; daha büyük görüntüyü [MosaicExporter](./MosaicExporter.md) parça parça yazar. Taş başına piksel (N) düşürülmez.
- `ToAvaloniaBitmap` satır uzunlukları eşitse tek `Buffer.MemoryCopy`, değilse satır satır kopyalar.

## Dikkat / bilinen sınırlamalar

- `Resize` (Area dalı), `FromByteArray` ve `ToByteArray` piksel başına 4 bayt varsayar. Area dalı kaynak satır uzunluğu için `RowBytes`'ı kullanır; `FromByteArray` ve `ToByteArray` ise boşluksuz satır (`RowBytes == Width * 4`) varsayar ve `RowBytes`'ı dikkate almaz. 8888 dışı renk tipleri yanlış okunur.
- Area dalında `Bgra8888` dışındaki her tip RGBA kabul edilir.
- `ApplyOrientation`'daki `LeftTop` ve `RightBottom` (aynalı + döndürülmüş) dönüşümleri karmaşık ve test edilmemiş görünmektedir; bu EXIF yönlerine sahip fotoğraflarda sonuç kontrol edilmelidir.
- `DrawOverlay` kullanılmıyor; ızgara artık `GridOverlay` kontrolüyle çiziliyor.
- `ToAvaloniaBitmap` içinde `bmp.Copy(...)` `null` dönerse boş bir `WriteableBitmap` döner.

## İlgili dosyalar

- [MosaicEngine](./MosaicEngine.md)
- [StoneTextureService](./StoneTextureService.md), [MosaicRenderSource](./MosaicRenderSource.md), [MosaicView](../Controls/MosaicView.md)
- [OptimalPaletteService](./OptimalPaletteService.md)
- [ProjectService](./ProjectService.md) (`InterpolationMethod` saklanır)
- [GridOverlay](../Controls/GridOverlay.md)
- [MosaicData](../Models/MosaicData.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
- [MosaicExporter](./MosaicExporter.md)
