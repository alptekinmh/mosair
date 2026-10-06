# MosaicRenderSource

> Kaynak: `mosair/Services/MosaicRenderSource.cs` · Güncelleme: 2026-10-06

## Amaç

Bir mozaiğin taş dokulu görüntüsünü (RS) çizmek için gereken her şeyi tek bir nesnede toplayan, mozaik başına bir kez kurulan anlık görüntü (snapshot). Görüntünün tamamını hiçbir zaman bellekte tutmaz; istenen herhangi bir taş dikdörtgenini (`RenderRegion`) ya da taş başına 1 piksellik genel görünümü (`RenderOverview`) üretir. Arka planda karo (tile) çizen [MosaicView](../Controls/MosaicView.md) ile dışa aktarma aynı çiziciyi kullanır.

`RenderRegion`, eski tek parça `GenerateRSBitmap` çıktısıyla aynı yerde birebir aynı pikselleri üretir. Bu, 48 durumda bayt bayt karşılaştırılarak doğrulanmıştır: 2 görsel, 2 boyut, Optimum ve klasik Mos, N = 10/20/40, ızgara açık/kapalı; ayrıca rastgele karolar tam görüntüden kesilen parçalarla karşılaştırılmıştır.

## Nerede kullanılır

| Çağıran | Kullanım |
|---|---|
| `StoneTextureService.CreateRenderSource()` | Nesneyi kurar (yapıcı `internal`) |
| `MainViewModel.RefreshMosaicView` / `RefreshOverview` | `RenderSource` özelliğine koyar, `RenderOverview` ile `OverviewBitmap`'i üretir |
| `MosaicView.Request` | Arka plan işçilerinde `RenderRegion` ile 512 px'lik karolar |
| `MainViewModel.ExportImageAsync` | `WithStoneSnapshot()` (taş renkleri ve varyantlarının kopyası; dışa aktarma sürerken yapılan düzenlemeler dosyaya girmez); kopya `MosaicExporter.Export`'a verilir |
| `MosaicExporter.Export` | Görüntü tek bitmap'e sığıyorsa (`ImageService.MaxBitmapPixels`) tamamı: `RenderRegion(0, 0, Rows, Cols, N, …)` (doku yoksa da; taşlar kendi renginde, taş başına N piksel); sığmıyorsa her seferinde bir taş satırı: `RenderRegion(row, 0, 1, Cols, N, …)` |
| `MosaicExporter.EstimateBytes` | Dosya boyutu tahmini için `RenderRegion` ile birkaç kare (JPEG) ya da tam genişlikte taş satırı (PNG) |
| `StoneTextureService.GenerateRSBitmap` | Tüm mozaik için `RenderRegion` (şu an çağıranı yok) |

## Yapı

`public sealed class MosaicRenderSource`. Yapıcı çağrıldığı anda statik verilerden aşağıdaki tabloları kurar; sonra statik verilerdeki değişikliklerden (ör. motorun bir sonraki mozaik için tabloları yeniden doldurması) etkilenmez.

| Ad | Tip | Açıklama |
|---|---|---|
| `Rows`, `Cols` | `int` | Taş satır/sütun sayısı (`dataM3` boyutları) |
| `Version` | `int` | Her yeni nesnede artan sayı (`Interlocked.Increment`). Şu an hiçbir yerde okunmuyor; `MosaicView` kaynağı `ReferenceEquals` ile karşılaştırır. |
| `HasTextures` | `bool` | Palet dokusu yüklü mü (`_paletteTextures.Count > 0`) |
| `_data` | `byte[,,]` | `MosaicData.dataM3` **referansı** (kopya değil) |
| `_arn` | `int[]?` | `MosaicData.arn` **referansı**: taş başına doku varyantı |
| `_codeOf` | `Dictionary<(b,g,r), string>` | Palet rengi → taş kodu; önce `MosaicData.arMB`, sonra `arMA` (ilk eklenen kazanır) |
| `_fallbackEntries` | liste | `_codeOf` girişlerinden dokusu yüklü olanlar; en yakın renk yedeği bunlar arasından seçilir |
| `_catalogOf` | `Dictionary<(b,g,r), (code, rgb)>` | Tüm katalog (`MosaicData.arRGBAll`) rengi → kod; yalnızca palette olmayan renkler için |
| `_paletteTextures` | `IReadOnlyDictionary<string, List<byte[,,]>>` | O anki `StoneTextureService._textures` sözlüğü (orijinal boyutlu dokular) |
| `_fallbackCache` | `ConcurrentDictionary` | Renk → en yakın kod önbelleği |
| `_extraTextures` | `ConcurrentDictionary` | Paletde olmayan taşlar için sonradan yüklenen doku setleri |
| `_resized` | `ConcurrentDictionary<(code, size), List<byte[,,]>>` | Bu nesneye ait küçültülmüş doku önbelleği (kod ve boyut başına) |

## Public API

| Üye | Ne yapar | Kimden çağrılır |
|---|---|---|
| `RenderRegion(row0, col0, rows, cols, N, showGrid, gridWidth, gridColor)` → `SKBitmap` | `[row0, row0+rows) × [col0, col0+cols)` taşlarını taş başına `N` piksel ile `Rgba8888` opak bitmap'e çizer; ızgara açıksa bitmap'e işlenir. Her taş satırının başında `WorkCancellation.Check()` çağırır (aşağıda) | `MosaicView`, `MosaicExporter`, `StoneTextureService.GenerateRSBitmap` |
| `RenderOverview()` → `SKBitmap` | Taş başına 1 piksel, her taşın kendi rengi (`Cols × Rows`) | `MainViewModel` (genel görünüm, gezgin) |
| `WithStoneSnapshot()` → `MosaicRenderSource` | Aynı mozaik, taş renkleri (`dataM3`) ve varyantlarının (`arn`) kopyasıyla; dışa aktarma bunu kullanır, böylece aktarma sürerken yapılan piksel düzenlemeleri dosyaya girmez. Dokular ve kod tabloları paylaşılır. | `MainViewModel.ExportImageAsync` |
| `HasTextures`, `Rows`, `Cols`, `Version` | Yukarıdaki tablo | `MosaicView`, `MainViewModel` |

## Önemli davranışlar ve iş kuralları

1. **Taşın kodu** (`CodeFor`):
   1. Renk `_codeOf` içinde ve o kodun dokusu yüklüyse o kod.
   2. Renk palette hiç yoksa (yalnızca piksel düzenleme böyle bir renk getirebilir), doku varsa ve renk katalogda bulunuyorsa katalogdaki kod. Bu taşın dokusu palette yüklü değilse `StoneTextureService.LoadTextureSet` ile ilk kullanımda yüklenir (`_extraTextures`).
   3. Aksi halde dokusu yüklü palet girişleri arasından RGB öklid uzaklığına göre en yakın kod (renk başına önbelleğe alınır).
2. **Varyant:** Taş `(i, j)` için doku `textures[arn[i*C + j]]`. `arn` yoksa ya da kısa kalırsa varyant 0 kullanılır. `arn` değerleri `1..15` olduğundan her taşın `1.jpg` dosyası (indeks 0) normalde görünmez.
3. **Küçültme:** Doku seti, çizilecek taş boyutuna (`stoneN`) ilk ihtiyaçta `StoneTextureService.ResizeSet` ile küçültülür ve `(kod, boyut)` anahtarıyla saklanır. Zaten o boyuttaki dokular kopyalanmadan kullanılır.
4. **Izgara:** `showGrid && gridWidth > 0` ise bitmap önce ızgara rengiyle boyanır; her taşın dokusu `gridWidth / 2` kaydırılarak `N - gridWidth` boyutunda kopyalanır. `N - gridWidth ≤ 0` ise yalnızca ızgara rengi kalır. Izgara kalınlığını çağıran belirler: `MosaicView` ve dışa aktarma, tam detayda `max(1, N/11)` kullanır.
5. **Doku yoksa:** Hiç taş görüntüsü yüklü değilse (`HasTextures` false) her taş kendi rengiyle düz boyanır. Doku varsa ama bir taşın kodu bulunamazsa o taşın yeri boş kalır (ızgara açıksa ızgara rengi, değilse yeni bitmap'in tanımsız, çoğunlukla siyah pikselleri).
6. **Canlı veri:** `dataM3` ve `arn` kopyalanmadığı için arayüzde yapılan piksel düzenlemesi ve varyant değişikliği, ilgili bölge bir sonraki çizimde yeni haliyle görünür. Ekrandaki karonun yenilenmesini `MosaicView.InvalidateStone` sağlar. Motor yeni bir mozaik kurarken `dataM3` ve `arn` için yeni diziler ayırır; böylece eski nesne, yerine yenisi konana kadar eski mozaiği tutarlı biçimde çizer.
7. **İş parçacığı:** Önbellekler `ConcurrentDictionary` olduğundan aynı nesneden birden çok karo aynı anda çizilebilir.
8. **İptal** ([WorkCancellation](./WorkCancellation.md)): `RenderRegion` her taş satırında `WorkCancellation.Check()` çağırır. Belirteci yalnızca dışa aktarma (`MainViewModel.ExportImageAsync`) atar; `MosaicView` karoları ve dosya boyutu tahmini belirteçsiz çalıştığı için hiç durmaz. Böylece tek bitmap'e sığan küçük bir dışa aktarma da çizim sırasında iptal edilebilir.

## Dikkat / bilinen sınırlamalar

- Ekran (`MosaicView`) canlı `dataM3`/`arn` üzerinden çizer; dışa aktarma ise `WithStoneSnapshot()` kopyasından çizdiği için aktarma sürerken yapılan piksel düzenlemeleri dosyaya girmez.
- `_catalogOf[(b, g, r)]` yalnızca `CodeFor` 2. adımda o anahtarın bulunduğunu doğruladıktan sonra okunur; adımlar değiştirilirken bu sıra korunmalıdır.
- Her nesne kendi küçültülmüş doku önbelleğini tutar (boyut başına; ekran seviyeleri ve dışa aktarma kalitesi ayrı girdiler açar); mozaik değişince yeni nesne kurulur, eskisi çöp toplayıcıya bırakılır.
- `unsafe` kod kullanır (doğrudan piksel işaretçisi).

## İlgili dosyalar

- [StoneTextureService](./StoneTextureService.md)
- [MosaicView](../Controls/MosaicView.md)
- [PixelEditService](./PixelEditService.md)
- [MosaicData](../Models/MosaicData.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
- [MosaicExporter](./MosaicExporter.md)
