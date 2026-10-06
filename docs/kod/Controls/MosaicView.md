# MosaicView

> Kaynak: `mosair/Controls/MosaicView.cs` · Güncelleme: 2026-10-06

## Amaç

Mos'tan sonra tuvalde taş dokulu mozaiği gösteren `Control`. Görüntünün tamamını hiçbir zaman oluşturmaz: yalnızca ekranda görünen kısmı, arka planda çizilen 512 piksellik karolardan (tile) ve yakınlaştırmanın gerektirdiği detayla (seviye) çizer. Çok uzaktayken taş başına 1 piksellik genel görünüm (overview) yeterlidir. Böylece görüntü boyutu sınırı kalkar; ör. 20 m'lik (1667×1667 taş) bir mozaik de gösterilebilir.

Kontrolün sanal boyutu `C·N × R·N` pikseldir (`MainViewModel.BitmapPixelWidth/Height`, `C` sütun, `R` satır, `N` = `StonePixelSize` = 100, taş fotoğraflarının kendi boyutu). Bu sayede yakınlaştırma, Ekrana Sığdır, mini harita ve tıklama koordinatları eskisi gibi çalışır.

## Nerede kullanılır

`MainWindow.axaml` içinde, `imageScroller` içindeki `Panel`'de `Image`'ın üstünde, `GridOverlay`'in altında:

```xml
<ctrl:MosaicView x:Name="mosaicView"
                 IsVisible="{Binding MosaicDone}"
                 Source="{Binding RenderSource}"
                 Overview="{Binding OverviewBitmap}"
                 StonePixelSize="{Binding StonePixelSize}"
                 ShowGrid="{Binding ShowGrid}"
                 GridColor="{Binding GridColor}"
                 PointerWheelChanged="OnImageWheel"
                 PointerMoved="OnImagePointerMoved"
                 PointerPressed="OnImagePointerPressed"
                 PointerReleased="OnImagePointerReleased"/>
```

Mos'tan önce aynı yerde yüklenen görsel (`Image`, `!MosaicDone`) görünür. `MainWindow` yapıcısı `_vm.StoneInvalidated` olayını `mosaicView.InvalidateStone`'a bağlar.

## Yapı

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `Source` | `MosaicRenderSource?` (StyledProperty) | `null` | Çizilecek mozaik ([MosaicRenderSource](../Services/MosaicRenderSource.md)) |
| `Overview` | `Bitmap?` (StyledProperty, `AffectsRender`) | `null` | Taş başına 1 piksellik genel görünüm |
| `StonePixelSize` | `int` (StyledProperty) | 40 | Tam detay N (taş başına piksel). `MainViewModel.StonePixelSize`'a bağlıdır; o artık sabit 100'dür (`ViewStonePixels`), kullanıcı ayarı yoktur. Kontrolün kendi varsayılanı 40 yalnızca bağlama yokken geçerlidir. |
| `ShowGrid` | `bool` (StyledProperty) | `false` | Izgara karolara işlensin mi |
| `GridColor` | `Color` (StyledProperty) | `Colors.Gray` | Izgara rengi |
| `TilePx` | const | 512 | Karo kenarı (piksel); bir karo `max(1, 512 / seviye)` taş kenarı kapsar |
| `CacheBudgetBytes` | const | 256 MB | Karo önbelleği üst sınırı |
| `OverviewOnlyBelow` | const | 2,5 | Taş başına ekran pikseli bunun altındaysa yalnızca genel görünüm çizilir |
| `Levels` | `int[]` | 2, 4, 8, 16, 32, 64 | Karo detay seviyeleri (taş başına piksel) |
| `Workers` | `SemaphoreSlim` (static) | `ProcessorCount − 1` (en az 1) | Aynı anda çizilen karo sayısı |
| `_cache` | `Dictionary<TileKey, Tile>` | boş | `(seviye, satır, sütun)` → Avalonia bitmap, bayt, son çizildiği kare |
| `_pending`, `_wanted` | `HashSet<TileKey>` | boş | Çizilmekte olan / son karede gereken karolar |
| `_generation` | `int` | 0 | Pikselleri değiştiren her ayarda artar; eski nesilden gelen karolar atılır |
| `_retired` | liste | boş | Önbellekten çıkarılıp ~1,5 sn sonra `Dispose` edilecek bitmap'ler |

## Public API

| Üye | Ne yapar | Kimden çağrılır |
|---|---|---|
| `Render(DrawingContext)` | Görünen kısmı çizer (aşağıdaki sıra) | Avalonia render döngüsü |
| `InvalidateStone(row, col)` | O taşı içeren bütün önbellek karolarını (her seviyede) atar ve yeniden çizim ister; `(-1, -1)` bütün önbelleği temizler | `MainWindow` (`_vm.StoneInvalidated`) |
| `HitTest(Point)` | `ICustomHitTest`: kontrolün tüm alanı tıklanabilir, böylece henüz çizilmemiş yerlerde de tıklama ve tekerlek olayları gelir | Avalonia |

## Çalışma

1. **Görünen alan:** Üstteki `ScrollViewer` bulunur; `ScrollChanged` olunca yeniden çizilir. Yalnızca görünen dikdörtgen işlenir.
2. **Seviye seçimi:** Taş başına ekran pikseli = `Bounds.Width / Cols × RenderScaling`. Seviye, `Levels` içinde N'den küçük olup bu değere eşit ya da büyük olan ilk değerdir; böyle bir değer yoksa N'nin kendisidir. Yani karo hiçbir zaman N'den daha detaylı çizilmez.
3. **Çizim sırası:**
   1. En altta genel görünüm; taş en az 1 ekran pikseliyse keskin (`None`), değilse `MediumQuality` ölçekleme.
   2. Taş başına 2,5 ekran pikselinin altındaysa ya da doku yoksa (`HasTextures` false) burada durulur.
   3. Önbellekteki başka seviyelerin görünen alana düşen karoları, kabadan inceye, yer tutucu olarak.
   4. İstenen seviyenin karoları; önbellekte olmayanlar istenir.
4. **Karo isteme:** Her karo `Task.Run` ile arka planda, `Workers` semaforu altında `MosaicRenderSource.RenderRegion` ile çizilir. Sıra gelince karo artık gerekmiyorsa (kaydırıldı, yakınlaştırıldı ya da ayar değişti) çizilmeden atlanır. Bitince UI iş parçacığında (`DispatcherPriority.Background`) Avalonia bitmap'e çevrilip önbelleğe konur.
5. **Izgara:** Karoya işlenir. Seviye = N ise eski tek parça görüntüdeki gibi tam `max(1, N/11)`; daha kaba seviyelerde `max(1, seviye/11)`, ama yalnızca seviye ≥ 8 ise (2 ve 4'te ızgara karoya işlenmez).
6. **Önbellek:** Toplam 256 MB'ı geçince en uzun süredir çizilmeyen karolar, toplam 192 MB'a inene kadar çıkarılır; o karede çizilenler korunur. Çıkarılan bitmap'ler, compositor o anda hâlâ kullanıyor olabileceği için ~1,5 sn sonra `Dispose` edilir.
7. **Temizleme:** `Source`, `StonePixelSize` (uygulamada artık değişmez), `ShowGrid` veya `GridColor` değişince ya da kontrol görsel ağaçtan çıkınca bütün karolar atılır ve nesil artar. Bu ayarlar bu yüzden anında uygulanır; ekran önce genel görünümü ve önbellekteki karoları gösterir, yeni karolar geldikçe keskinleşir.

## Ölçümler

20 m'lik mozaikte (1667×1667 taş): Mos (Optimum) 1,8 sn; karo çizimi 2–13 ms (seviye 2 ≈ 13 ms, seviye ≥ 8 ≈ 2,5 ms); genel görünüm 9 ms.

## Dikkat / bilinen sınırlamalar

- [GridOverlay](./GridOverlay.md) bu kontrolün üstünde durmaya devam eder. Izgara açıkken çizgiler hem karolarda (seviye N'de ve seviye ≥ 8'de) hem de üst katmanda bulunur; ızgara görünümü değiştirilirken iki yol birlikte düşünülmelidir.
- Daha keskin karo gelene kadar o bölge kısa bir süre genel görünümden ya da kaba karodan büyütülmüş, bulanık görünebilir.
- Seviye listesi 64'te biter; N 64'ten büyükse en yakın görünümde karo N ile çizilir.
- Karo içeriği `MosaicRenderSource`'un canlı `dataM3`/`arn` verisinden gelir; bir taş değiştiğinde ekranın güncellenmesi için `InvalidateStone` çağrılmalıdır (`MainViewModel.StoneInvalidated`).

## İlgili dosyalar

- [MosaicRenderSource](../Services/MosaicRenderSource.md), [StoneTextureService](../Services/StoneTextureService.md)
- [MainWindow](../MainWindow.md), [MainViewModel](../ViewModels/MainViewModel.md)
- [GridOverlay](./GridOverlay.md), [ImageService](../Services/ImageService.md)
