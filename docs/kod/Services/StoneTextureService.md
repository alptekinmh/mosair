# StoneTextureService

> Kaynak: `mosair/Services/StoneTextureService.cs` · Güncelleme: 2026-10-04

## Amaç

Gerçek taş fotoğraflarını (`02_RS` klasörü) yükler, mozaik taş boyutuna (`N`) küçültür ve mozaiğin gerçek taş dokulu görünümünü (RS bitmap) üretir. Katalog listesi için küçük resim ve ipucu (tooltip) görselleri de sağlar.

## Nerede kullanılır

| Çağıran | Kullanım |
|---|---|
| `MosaicEngine` (`RunOptimal`, `RunM3` sonu) | `PopulateRandomIndices`, `LoadTextures`, `ResizeTextures`; `Reset` |
| `MainViewModel.BuildRsBitmap`, `OpenProject`, `RegenerateRS` | `ResizeTextures`, `GenerateRSBitmap`, `LoadTextures` |
| `MainViewModel.LoadImage` | `Reset` |
| `MainViewModel.UpdatePropTexture`, `SelectStone` | `FindFolderForCode` |
| `ColorItem.ThumbnailBitmap`, `ColorItem.BuildTooltipBitmap` (`MainViewModel.cs`) | `LoadSingleThumbnail`, `LoadTooltipImages` |
| `PixelEditService.PatchRSRegion` | `FindFolderForCode` |

## Yapı

Static sınıf, özel durum alanları:

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `_textures` | `Dictionary<string, List<byte[,,]>>` | boş | Kod adı → 16 orijinal doku (BGR `byte[,,]`) |
| `_resizedTextures` | `Dictionary<string, List<byte[,,]>>` | boş | Kod adı → `N×N`'e küçültülmüş dokular |
| `_rsDirNames` | `string[]?` | `null` | `02_RS` altındaki klasörler (önbellek) |
| `_rsBasePath` | `string?` | `null` | Bulunan `02_RS` yolu |

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
| `PopulateRandomIndices(R, C)` | `MosaicData.arn`'ı `R*C` uzunlukta, `1..15` arası rastgele doku indeksiyle doldurur | `MosaicEngine` |
| `LoadTextures()` | `MosaicData.arMA`'daki her benzersiz kod adı için 16 doku yükler (paralel); dosya yoksa 3×3 düz renk doku koyar | `MosaicEngine`, `MainViewModel.OpenProject` |
| `FindFolderForCode(codeName)` → `string?` | Gerekirse yolu/klasör listesini hazırlayıp kod adının klasörünü döndürür | `PixelEditService`, `MainViewModel` |
| `LoadSingleThumbnail(codeName, width, height)` → `SKBitmap?` | `1.jpg`'yi verilen boyuta küçültür | `ColorItem.ThumbnailBitmap` |
| `LoadTooltipImages(codeName, thumbSize = 60)` → `List<SKBitmap>` | `1..16.jpg`'yi kare küçük resimlere çevirir | `ColorItem.BuildTooltipBitmap` |
| `ResizeTextures(N)` | Tüm dokuları `N×N`'e küçültür (`ImageService.Resize`, Area) ve `_resizedTextures`'a koyar | `MosaicEngine`, `MainViewModel` |
| `GenerateRSBitmap(R, C, N, showGrid = false, gridWidth = 0, gridColor = default)` → `SKBitmap?` | `C*N × R*N` boyutlu `Rgba8888` RS bitmap üretir; doku yoksa `null` | `MainViewModel` |
| `Reset()` | Doku sözlüklerini ve `_rsDirNames`'i temizler | `MosaicEngine`, `MainViewModel.LoadImage` |

## Önemli davranışlar ve iş kuralları

- **Renk → kod adı eşlemesi** (`GenerateRSBitmap`): önce `MosaicData.arMB`, sonra `MosaicData.arMA` girişlerinden `(b, g, r) → codeName` sözlüğü kurulur (ilk eklenen kazanır). Her hücrenin rengi `MosaicData.dataM3[i, j, 0..2]`'den (BGR) okunur.
- **Yedek eşleme:** Hücre rengi sözlükte yoksa veya dokusu yoksa, dokusu olan girişler arasında RGB öklid uzaklığına göre en yakın kod adı seçilir ve renk başına önbelleğe alınır.
- **Varyant seçimi:** Hücre `idx = i*C + j` için doku `textures[MosaicData.arn[idx]]` olur (`arn` 1..15 aralığında, liste 0 tabanlı).
- **Izgara:** `showGrid && gridWidth > 0` ise tüm bitmap önce ızgara rengiyle boyanır, taş dokusu `gridWidth/2` kaydırılarak `N - gridWidth` boyutunda kopyalanır. `MainViewModel.BuildRsBitmap` bu durumda dokuları önceden `N - gw` boyutuna küçültür (`gw = max(1, N/11)`).
- **Bellek sınırı:** Toplam piksel 800 milyonu aşarsa `OutOfMemoryException` (`StatusRsBitmapTooLarge` metniyle) atılır.
- Yükleme, küçültme ve kopyalama `Parallel.ForEach` / `Parallel.For` ile yapılır; sonuçlar önce `ConcurrentDictionary`'de toplanıp sonra tek iş parçacığında sözlüklere aktarılır.

## Dikkat / bilinen sınırlamalar

- Klasör eşleşmesi alt dize araması olduğundan bir kod adı başka bir kod adını içeriyorsa (ör. `A1` / `A10`) yanlış klasör seçilebilir; ayrıca kod adı klasör adının en başındaysa (`pos == 0`) eşleşme sayılmaz.
- `arn` değerleri `1..15` olduğu için her taşın ilk dokusu (`1.jpg`, indeks 0) RS görünümünde hiç kullanılmaz.
- Düz renk yedek dokusu 3×3'tür; `ResizeTextures` ile `N×N`'e büyütülür.
- `GenerateRSBitmap`, `MosaicData.arn`'ın en az `R*C` uzunlukta olduğunu varsayar.
- `unsafe` kod kullanır (doğrudan piksel işaretçisi).

## İlgili dosyalar

- [ImageService](./ImageService.md)
- [PixelEditService](./PixelEditService.md)
- [MosaicEngine](./MosaicEngine.md)
- [ColorCatalogService](./ColorCatalogService.md)
- [MosaicData](../Models/MosaicData.md), [Rgb](../Models/Rgb.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
