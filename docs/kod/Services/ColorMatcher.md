# ColorMatcher

> Kaynak: `mosair/Services/ColorMatcher.cs` · Güncelleme: 2026-10-06

## Amaç
Renk uzayı dönüşümü ve katalog eşleme için küçük yardımcı fonksiyonlar sunan statik bir sınıf. sRGB'den CIE Lab'a dönüşüm (D65) yapar. Bir rengi aktif katalogdaki en yakın taşa RGB mesafesiyle eşler.

## Nerede kullanılır
| Dosya | Kullanım |
|---|---|
| [MosaicEngine](./MosaicEngine.md) | `RgbToLab` (M1/M3 ve optimum eşleme). `RunM3` Bölüm 2'de `ResetUseOnce`, `FindCatalogDistances` ve `SelectNearest` çağrılır. |
| [OptimalPaletteService](./OptimalPaletteService.md) | `RgbToLab` |
| [MosaicMetrics](./MosaicMetrics.md) | `RgbToLab` |
| [GamutMapper](./GamutMapper.md) | `RgbToLab` |
| [StockAwareAssigner](./StockAwareAssigner.md), [StockCompareRunner](../StockCompareRunner.md) | `RgbToLab` (stoğa göre düzeltmede piksel ve taş renkleri) |

## Yapı
Statik sınıftır ve alanı yoktur. Mesafe sonuçları `cooo` modeliyle taşınır: `av` alanı mesafeyi, `n` alanı katalog indeksini tutar (bkz. [Rgb](../Models/Rgb.md)).

## Public API
| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `RgbToLab(r, g, b)` | 0–255 aralığındaki RGB değerini `(L, A, B)` tuple'ına çevirir | MosaicEngine, OptimalPaletteService, MosaicMetrics, GamutMapper, StockAwareAssigner, StockCompareRunner |
| `CalculateDistance(ra, raa, k)` | İki `rgb` arasındaki RGB kare mesafesini `cooo(av, k)` olarak döndürür | `FindCatalogDistances` |
| `FindCatalogDistances(ra)` | `MosaicData.arRGB` içinde `boolUseOnce` false olan her taşa olan mesafeyi listeler | `MosaicEngine.RunM3` |
| `SelectNearest(aro)` | Listeden en küçük `av` değerine sahip girdinin katalog indeksini döndürür | `MosaicEngine.RunM3` |
| `ResetUseOnce()` | Aktif katalogdaki tüm `boolUseOnce` bayraklarını false yapar | `MosaicEngine.RunM3` |

## Algoritma / akış

### `RgbToLab`
1. Kanallar 255'e bölünür. sRGB gama açılır: değer `> 0.04045` ise `((v + 0.055)/1.055)^2.4`, değilse `v/12.92`. Sonuç 100 ile çarpılır.
2. Sabit 3×3 matrisle XYZ'ye çevrilir.
3. D65 beyaz noktasına (95.047, 100.0, 108.883) bölünür.
4. `f(t)` uygulanır: `t > 0.008856` ise `t^(1/3)`, değilse `7.787·t + 16/116`.
5. `L = 116·xyz[1] − 16`, `A = 500·(xyz[0] − xyz[1])`, `B = 200·(xyz[1] − xyz[2])`. Burada `xyz` dizisi 4. adımdan sonraki değerleri tutar.

### Katalog eşleme
1. `FindCatalogDistances` her uygun katalog taşı için `ΔR² + ΔG² + ΔB²` değerini hesaplar.
2. `SelectNearest` başlangıç minimumunu 10000 alır ve bundan küçük olan en küçük mesafeyi seçer.

## Önemli davranışlar ve iş kuralları
- Katalog eşleme her zaman **RGB** mesafesiyle yapılır, Lab kullanılmaz.
- `SelectNearest`'in döndürdüğü indeks `MosaicData.arRGB` (aktif katalog) içindir, `arRGBAll` için değildir.

## Dikkat / bilinen sınırlamalar
- `SelectNearest` başlangıç minimumu 10000'dir. Bu, Öklid mesafesinde 100'e karşılık gelir. Tüm katalog taşları bundan uzaksa fonksiyon sessizce **0 indeksini**, yani aktif katalogdaki ilk taşı döndürür.
- `boolUseOnce` hiçbir yerde true yapılmıyor. Bu yüzden `ResetUseOnce` ve `FindCatalogDistances` içindeki filtre fiilen etkisizdir (WPF'ten kalan mantık).
- `RgbToLab` her çağrıda iki küçük dizi ayırır. Sık çağrılan yerler sonucu kendileri önbellekler.

## İlgili dosyalar
- [MosaicEngine](./MosaicEngine.md), [StockAwareAssigner](./StockAwareAssigner.md), [OptimalPaletteService](./OptimalPaletteService.md), [MosaicMetrics](./MosaicMetrics.md), [GamutMapper](./GamutMapper.md), [ColorCatalogService](./ColorCatalogService.md)
- [Rgb](../Models/Rgb.md), [MosaicData](../Models/MosaicData.md)
