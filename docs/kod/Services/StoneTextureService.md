# StoneTextureService

> Kaynak: `mosair/Services/StoneTextureService.cs` · Güncelleme: 2026-10-06

## Amaç

Gerçek taş fotoğraflarını (`02_RS` klasörü) yükler ve mozaiği çizmek için bir anlık görüntü ([MosaicRenderSource](./MosaicRenderSource.md)) kurar. Dokuların taş boyutuna küçültülmesi ve görüntünün çizimi artık `MosaicRenderSource` içinde, istenen bölge ve boyut için yapılır; taş dokulu görüntünün (RS) tamamı ekran için hiç oluşturulmaz. Katalog listesi için küçük resim ve ipucu (tooltip) görselleri de sağlar.

## Nerede kullanılır

| Çağıran | Kullanım |
|---|---|
| `MosaicEngine` (`ApplyOptimalK`, `RunM3` sonu, stoğa göre düzeltme `FixToStock`) | `PopulateRandomIndices`, `LoadTextures` (yalnızca `prepareTextures` açıksa; `FixToStock` ayrıca mozaik değiştiyse veya `texturesAlways` ise); `MosaicEngine.Reset` içinde `Reset` |
| `MainViewModel.RefreshMosaicView` | `CreateRenderSource` |
| `MainViewModel.OpenProject` | `Reset`, ardından arka planda `LoadTextures` |
| `MainViewModel.LoadImage` | `Reset` |
| `MosaicRenderSource` | `LoadTextureSet` (palette olmayan taş için), `ResizeSet` |
| `ProjectService.Open` | Dosyada geçerli `Arn` yoksa `PopulateRandomIndices` |
| `MainViewModel.UpdatePropTexture`, `SelectStone` | `FindFolderForCode` |
| `ColorItem.ThumbnailBitmap`, `ColorItem.BuildTooltipBitmap` (`MainViewModel.cs`) | `LoadSingleThumbnail`, `LoadTooltipImages` |

## Yapı

Static sınıf, özel durum alanları:

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `_textures` | `Dictionary<string, List<byte[,,]>>` | boş | Kod adı → 16 orijinal doku (BGR `byte[,,]`). Hiç temizlenmez, her seferinde yeni bir sözlükle **değiştirilir**; böylece bu sözlüğü yakalamış bir `MosaicRenderSource`, motor başka iş parçacığında sonraki mozaiği hazırlarken de geçerli kalır. |
| `_rsDirNames` | `string[]?` | `null` | `02_RS` altındaki klasörler (önbellek) |
| `_rsBasePath` | `string?` | `null` | Bulunan `02_RS` yolu |

Özel yardımcılar: `FindFolder(codeName)` (önbellekteki klasör listesinde arar) ve `CreateSolidTexture(color, size)` (doku dosyası yoksa kullanılan BGR düz renk doku).

### Doku klasörü yapısı

`02_RS/<önek><kodAdı>.../1.jpg … 16.jpg`. Klasör, adında taş kod adını **0'dan büyük bir konumda** içeren ilk klasördür (`IndexOf(codeName) > 0`).

`FindRSPath` sırasıyla şunlara bakar (`AppContext.BaseDirectory` altında):

1. `Assets/02_RS`
2. `mosaicFiles/02_RS`
3. `02_RS`

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `FindRSPath()` → `string?` | `02_RS` klasörünü bulur | `LoadTextures`, `FindFolderForCode` |
| `PopulateRandomIndices(R, C)` | `MosaicData.arn`'ı `R*C` uzunlukta, `1..15` arası rastgele doku indeksiyle doldurur | `MosaicEngine`, `ProjectService.Open` |
| `LoadTextures()` | `MosaicData.arMA`'daki her benzersiz kod adı için `LoadTextureSet` ile 16 doku yükler (paralel) ve sonucu yeni bir `_textures` sözlüğü olarak koyar. `02_RS` bulunamazsa sözlük boş kalır. | `MosaicEngine`, `MainViewModel.OpenProject` |
| `LoadTextureSet(codeName, color)` → `List<byte[,,]>` (`internal`) | Bir taşın `1.jpg … 16.jpg` dokularını yükler; eksik dosya yerine 3×3 düz renk doku koyar | `LoadTextures`, `MosaicRenderSource` |
| `FindFolderForCode(codeName)` → `string?` | Gerekirse yolu/klasör listesini hazırlayıp kod adının klasörünü döndürür | `MainViewModel` (`UpdatePropTexture`, `SelectStone`) |
| `LoadSingleThumbnail(codeName, width, height)` → `SKBitmap?` | `1.jpg`'yi verilen boyuta küçültür | `ColorItem.ThumbnailBitmap` |
| `LoadTooltipImages(codeName, thumbSize = 60)` → `List<SKBitmap>` | `1..16.jpg`'yi kare küçük resimlere çevirir | `ColorItem.BuildTooltipBitmap` |
| `ResizeSet(originals, N)` → `List<byte[,,]>` (`internal`) | Bir taşın doku setini `N×N`'e küçültür (`ImageService.Resize`, Area); zaten `N×N` olanları kopyalamadan kullanır | `MosaicRenderSource` |
| `CreateRenderSource()` → `MosaicRenderSource` | O anki `MosaicData.dataM3`, `MosaicData.arn` ve `_textures` ile çizim anlık görüntüsü kurar | `MainViewModel.RefreshMosaicView` |
| `GenerateRSBitmap(R, C, N, showGrid = false, gridWidth = 0, gridColor = default)` → `SKBitmap?` | Bütün mozaiği tek bitmap olarak üretir: `CreateRenderSource().RenderRegion(0, 0, R, C, N, …)`. Doku yoksa `null`; `C·N·R·N > ImageService.MaxBitmapPixels` ise `OutOfMemoryException` (`StatusRsBitmapTooLarge`). Tam görüntü API'si olarak duruyor; şu an çağıranı yok (dışa aktarma `RenderRegion`'ı doğrudan kullanır). | — |
| `Reset()` | `_textures`'ı yeni boş sözlükle değiştirir, `_rsDirNames`'i temizler | `MosaicEngine`, `MainViewModel.LoadImage`, `MainViewModel.OpenProject` |

## Önemli davranışlar ve iş kuralları

- **Çizim kuralları** (renk → kod eşlemesi, en yakın renk yedeği, varyant seçimi, ızgara) [MosaicRenderSource](./MosaicRenderSource.md) sayfasındadır. `RenderRegion` eski tek parça RS ile aynı yerde birebir aynı pikselleri üretir.
- **Anlık görüntü güvenliği:** `_textures` yerinde değiştirilmez; `LoadTextures` ve `Reset` yeni sözlük atar. Küçültülmüş kopyalar servis düzeyinde tutulmaz, her `MosaicRenderSource` kendi önbelleğini tutar.
- **Bellek sınırı:** Yalnızca `GenerateRSBitmap` için geçerlidir: toplam piksel `ImageService.MaxBitmapPixels`'ı (≈ 536,9 milyon; Skia'nın 2 GB sınırı) aşarsa `OutOfMemoryException` atılır. Ekran gösterimi karolarla çalıştığı için böyle bir sınırı yoktur; dışa aktarma sığan en büyük N'yi kendisi seçer (`MainViewModel.ExportImageAsync`).
- `LoadTextures` dokuları `Parallel.ForEach` ile yükler; sonuçlar önce `ConcurrentDictionary`'de toplanıp sonra yeni sözlüğe aktarılır.

## Dikkat / bilinen sınırlamalar

- Klasör eşleşmesi alt dize araması olduğundan bir kod adı başka bir kod adını içeriyorsa (ör. `A1` / `A10`) yanlış klasör seçilebilir; ayrıca kod adı klasör adının en başındaysa (`pos == 0`) eşleşme sayılmaz.
- `arn` değerleri `1..15` olduğu için her taşın ilk dokusu (`1.jpg`, indeks 0) RS görünümünde hiç kullanılmaz.
- Düz renk yedek dokusu 3×3'tür; `ResizeSet` ile `N×N`'e büyütülür.
- `LoadTextureSet`, `_rsBasePath`/`_rsDirNames` henüz hazır değilse onları da hazırlar; `MosaicRenderSource` bunu arka plan iş parçacığından çağırabilir.

## İlgili dosyalar

- [MosaicRenderSource](./MosaicRenderSource.md), [MosaicView](../Controls/MosaicView.md)
- [ImageService](./ImageService.md)
- [PixelEditService](./PixelEditService.md)
- [MosaicEngine](./MosaicEngine.md)
- [ColorCatalogService](./ColorCatalogService.md)
- [MosaicData](../Models/MosaicData.md), [Rgb](../Models/Rgb.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
