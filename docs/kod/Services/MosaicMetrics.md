# MosaicMetrics

> Kaynak: `mosair/Services/MosaicMetrics.cs` · Güncelleme: 2026-10-04

## Amaç
Üretilen mozaiğin kaynak resme ne kadar benzediğini sayısal olarak ölçer. Farklı algoritma varyantlarını (M3, WPF tarzı M3, Optimum) aynı ölçütlerle karşılaştırmak için yazılmıştır. Arayüzde kullanılmaz, yalnızca komut satırındaki karşılaştırma modunda çalışır.

## Nerede kullanılır
| Dosya | Kullanım |
|---|---|
| [CompareRunner](../CompareRunner.md) | Her varyant için `MosaicMetrics.Evaluate(src, data, R, C)` çağrılır ve `MosaicQuality` raporlanır |

Uygulamanın arayüzü (MainViewModel) bu sınıfı kullanmaz.

## Yapı

### `MosaicQuality` (readonly record struct)
| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `MeanDeltaE` | double | – | Tüm pikseller için ortalama CIE76 ΔE |
| `P95DeltaE` | double | – | ΔE'nin %95 persentili |
| `EdgeMeanDeltaE` | double | – | Kenar piksellerinde ortalama ΔE |
| `Stones` | int | – | Mozaikte kullanılan farklı renk (taş) sayısı |
| `ChromaMeanDeltaE` | double | 0 | Doygun piksellerde (kaynak kroma > 30) ortalama ΔE |
| `ChromaPixels` | int | 0 | Doygun piksel sayısı |
| `EdgeKept` | double | 1 | Güçlü kaynak kenarlarından mozaikte hâlâ farklı renkte kalanların oranı |
| `LightnessCorr` | double | 1 | Kaynak ve mozaik L kanalları arasındaki Pearson korelasyonu |

`ToString()` bu değerleri tek satırda biçimlendirir. Kenar oranı yüzde olarak, korelasyon üç ondalıkla yazılır.

## Public API
| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `Evaluate(src, mosaic, R, C)` | BGR kaynak ve mozaik dizilerini karşılaştırır ve `MosaicQuality` döndürür | CompareRunner |

## Algoritma / akış
1. Her pikselin kaynak ve mozaik rengi `ColorMatcher.RgbToLab` ile Lab'a çevrilir. Dönüşümler renk anahtarına göre önbelleklenir.
2. **ΔE**: `sqrt(ΔL² + Δa² + Δb²)` (CIE76, ağırlıksız).
3. **Ortalama**: Tüm ΔE değerlerinin aritmetik ortalaması.
4. **P95**: ΔE değerleri sıralanır ve `ceil(0.95·n) − 1` indeksindeki değer alınır.
5. **Kroma**: Kaynakta `sqrt(a² + b²) > 30` olan piksellerin ΔE ortalaması hesaplanır ve bu piksellerin sayısı tutulur.
6. **Kenar ΔE**: Kaynağın L kanalında merkezi fark ile gradyan hesaplanır: `gx = L[j+1] − L[j−1]`, `gy = L[i+1] − L[i−1]`. Sınırlarda kenar pikseli tekrarlanır. Gradyanı en yüksek %10'a giren ve sıfırdan büyük olan piksellerin ΔE ortalaması alınır.
7. **Kenar korunumu**: Yatay ve dikey komşu çiftlerden kaynakta Lab farkı `OptimalPaletteService.EdgeContrast` (12) ve üzeri olanlar sayılır. Bunlardan mozaikte iki yanı farklı renk olanların oranı hesaplanır.
8. **Açıklık korelasyonu**: Kaynak L ve mozaik L dizileri arasında Pearson korelasyonu hesaplanır. Varyanslardan biri sıfırsa sonuç 1 olur.
9. **Taş sayısı**: Mozaikteki benzersiz renklerin sayısıdır.

## Önemli davranışlar ve iş kuralları
- Her iki dizi de **BGR** düzenindedir (`[..,2]` = R).
- `Stones` değeri renge göre sayılır. Aynı RGB'ye sahip iki katalog taşı tek sayılır.
- ΔE burada ağırlıksızdır. [OptimalPaletteService](./OptimalPaletteService.md)'teki `LightnessWeight` uygulanmaz, bu yüzden sayılar o servisin eğrileriyle aynı ölçekte değildir.

## Dikkat / bilinen sınırlamalar
- Kenar tanımı iki ölçütte farklıdır. Kenar ΔE'si merkezi farkla bulunan en yüksek %10'u kullanır. Optimizer ise Sobel ve %98 persentilini kullanır. Kenar korunumu ölçütü ise optimizer ile aynı `EdgeContrast` eşiğine dayanır.
- Dizilerin `R×C` boyutunda olduğu varsayılır ve boyut kontrolü yapılmaz.

## İlgili dosyalar
- [CompareRunner](../CompareRunner.md), [OptimalPaletteService](./OptimalPaletteService.md), [ColorMatcher](./ColorMatcher.md), [MosaicEngine](./MosaicEngine.md)
