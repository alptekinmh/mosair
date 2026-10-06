# GridOverlay

> Kaynak: `mosair/Controls/GridOverlay.cs` · Güncelleme: 2026-10-06

## Amaç

Tuval üzerindeki mozaik görüntüsünün üstüne, taş sınırlarını gösteren ızgara çizgilerini çizen hafif bir `Control`. Bitmap'e dokunmaz; yalnızca ekran koordinatlarında çizgi çizer, bu yüzden yakınlaştırmada keskin kalır.

## Nerede kullanılır

`MainWindow.axaml` içinde, `imageScroller` içindeki `Panel`'de `Image` (Mos'tan önce) ve [MosaicView](./MosaicView.md) (Mos'tan sonra) kontrollerinin üstünde:

```xml
<ctrl:GridOverlay ShowGrid="{Binding ShowGrid}"
                  StoneSize="{Binding StonePixelSize}"
                  BitmapWidth="{Binding BitmapPixelWidth}"
                  BitmapHeight="{Binding BitmapPixelHeight}"
                  StoneColumns="{Binding StoneColumns}"
                  StoneRows="{Binding StoneRows}"
                  GridColor="{Binding GridColor}"
                  IsHitTestVisible="False"/>
```

`IsHitTestVisible="False"` sayesinde fare olayları alttaki `Image`'a veya `MosaicView`'a geçer (piksel düzenleme, kaydırma).

## Yapı

Tüm özellikler `StyledProperty`'dir ve `AffectsRender` ile kayıtlıdır; herhangi biri değişince kontrol yeniden çizilir.

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `ShowGrid` | `bool` | `false` | Izgara görünür mü. |
| `StoneSize` | `int` | 20 | Bir taşın bitmap içindeki piksel boyutu (`MainViewModel.StonePixelSize` = `MosaicData.N`). |
| `BitmapWidth` | `int` | 0 | Tuvaldeki görüntünün piksel genişliği: Mos'tan önce görselin genişliği, Mos'tan sonra taş dokulu görüntünün sanal genişliği `C·N` (`MainViewModel.BitmapPixelWidth`). |
| `BitmapHeight` | `int` | 0 | Tuvaldeki görüntünün piksel yüksekliği: Mos'tan önce görselin yüksekliği, Mos'tan sonra sanal yükseklik `R·N` (`MainViewModel.BitmapPixelHeight`). |
| `StoneColumns` | `int` | 0 | Taş sütun sayısı (bağlı, ama `Render` içinde kullanılmıyor). |
| `StoneRows` | `int` | 0 | Taş satır sayısı (bağlı, ama `Render` içinde kullanılmıyor). |
| `GridColor` | `Color` | `Colors.Gray` | Çizgi rengi. |

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `Render(DrawingContext context)` | Izgarayı çizer (aşağıdaki kurallar). | Avalonia render döngüsü |
| `OnAttachedToVisualTree` / `OnDetachedFromVisualTree` | Üstteki `ScrollViewer`'ı bulur ve `ScrollChanged` olayına abone olur / aboneliği bırakır; kaydırma olunca `InvalidateVisual`. | Avalonia |

Özelliklerin CLR sarmalayıcıları (`ShowGrid`, `StoneSize` …) XAML bağlamaları için vardır.

## Önemli davranışlar ve iş kuralları

1. `ShowGrid` kapalıysa veya `StoneSize`, `BitmapWidth`, `BitmapHeight` sıfır/negatifse hiçbir şey çizilmez.
2. Ölçek: `scaleX = Bounds.Width / BitmapWidth`, `scaleY = Bounds.Height / BitmapHeight`. Bir taşın ekrandaki boyu `cellScreen = StoneSize * min(scaleX, scaleY)`.
3. `cellScreen < 3` piksel ise (çok uzaklaştırılmış) çizim yapılmaz.
4. **Seyreltme:** Ekrandaki çizgi aralığı en az 20 piksel olana kadar `skip` değeri 2 ile çarpılır; yani uzaklaştırıldığında her 2., 4., 8. … taş çizgisi çizilir.
5. Çizgi kalınlığı: etkin hücre ≥ 40 piksel ise 2, değilse 1.
6. Çizgiler bitmap pikseli cinsinden `StoneSize * skip` adımlarla hesaplanır ve `Math.Round` ile tam piksele yuvarlanır. Kenar çizgileri (0 ve tam genişlik) çizilmez.
7. **Yalnızca görünen kısım:** Çizgiler, `ScrollViewer`'ın görünen alanıyla kesişen bölgede ve yalnızca o alanın uzunluğunda çizilir (`VisibleRect`). Yakınlaştırılmış büyük bir mozaikte (ör. 20 m) ekran dışındaki on binlerce çizgi çizilmez; kaydırma olunca katman yeniden çizilir. `ScrollViewer` bulunamazsa bütün alan kullanılır.

## Dikkat / bilinen sınırlamalar

- `StoneColumns` ve `StoneRows` bağlanmış olsa da çizimde kullanılmaz; ızgara tamamen `StoneSize` ve bitmap boyutundan türetilir. `BitmapWidth` taş boyunun tam katı değilse son sütun/satır çizgileri kayabilir.
- Izgara açıkken [MosaicView](./MosaicView.md) de ızgarayı taş dokulu karoların içine işler (seviye N'de ve seviye ≥ 8'de). Bu yüzden ızgara hem karolarda hem bu katmanda bulunabilir; görsel tutarlılık değiştirilirken iki yol birlikte düşünülmelidir.
- Seyreltme nedeniyle uzaklaştırılmış görünümde tüm taş sınırları görünmez; bu bilinçli bir performans/okunabilirlik tercihidir.

## İlgili dosyalar

- [MainWindow](../MainWindow.md)
- [MosaicView](./MosaicView.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
- [StoneTextureService](../Services/StoneTextureService.md)
- [MosaicData](../Models/MosaicData.md)
