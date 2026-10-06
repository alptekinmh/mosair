# StockAwareAssigner

> Kaynak: `mosair/Services/StockAwareAssigner.cs` · Güncelleme: 2026-10-06

## Amaç
"Stoğa göre" özelliğinin çözücüsü (v1.3.1). Bitmiş bir mozaiği (Optimum ya da klasik Mos), hiçbir taş stoğunu aşmayacak şekilde en az görünür değişiklikle yeniden düzenler. Stoğu yetmeyen taşın fazlası, renkçe en yakın ve stoğu olan taşlara aktarılır. İsteğe bağlı olarak çok az kullanılan taşlar (`MinUsage` altı) da çıkarılabilir; uygulamada bu kural **kapalıdır** (`MinUsage = 0`). Stoğu aşan taş yoksa mozaik değişmez. Ayrıntılı tasarım ve test sonuçları için [STOGA_GORE_RAPOR.md](../../STOGA_GORE_RAPOR.md).

## Nerede kullanılır
| Dosya | Kullanım |
|---|---|
| [MosaicEngine](./MosaicEngine.md) | `ApplyOptimalKWithStock` (Optimum) ve `FixCurrentMosaicToStock` (klasik Mos, Stok Kontrol öncesi düzeltme), ikisi de `FixToStock` → `SolveWithMinimum` |
| [MainViewModel](../ViewModels/MainViewModel.md) | `StockOptions()` → `new StockAwareOptions()` (`MinUsage = 0`); sonucu `MosaicEngine.LastStockResult` üzerinden raporlar |
| [StockCompareRunner](../StockCompareRunner.md) | Çevrimdışı karşılaştırma; en az kullanım kuralını yalnızca `MOSAIR_MINUSAGE=1` ile açar |

## Yapı
### `StockAwareOptions`
| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `MaxExtraStones` | int | 2 | Mozaiğin önce kullanmadığı en fazla yeni taş türü (ikizler hariç) |
| `LightnessWeight` | double | 1.0 | İkame ölçüsünde ΔL çarpanı; 1 = düz ΔE76 |
| `SimilarityTolerance` | double | 10.0 | Arama genişlemeden önceki en büyük taş–taş ΔE |
| `SameFamilyFactor` | double | 0.85 | Aynı taşın başka yüzeyine geçiş maliyet çarpanı |
| `NewStoneGain` | double | 0.8 | Yeni tür ancak toplam değişimi en az %20 azaltıyorsa |
| `MaxCandidates` | int | 8 | Taş başına değerlendirilen en yakın aday sayısı |
| `GroupStep` | double | 2.0 | Birlikte taşınan renk grubunun Lab adımı |
| `TwinTolerance` | double | 3.0 | "İkiz" sayılan en büyük ΔE; en yakın tek ikiz her zaman kullanılabilir |
| `MinUsage` | int | 0 | Bu sayının altında kullanılan taşlar çıkarılır (0/1 = kapalı). Uygulamada kapalıdır (0); yalnızca karşılaştırma aracı `MOSAIR_MINUSAGE=1` ile açar |
| `MinUsageFor(total)` | static | — | max(10, ⌈toplam × 0,0005⌉) (78×78 → 10, ~111.000 taş → 56); yalnızca `StockCompareRunner` kullanır |

### `StockAwareResult`
`Changed`, `Assignment` (piksel başına havuz indeksi), `CountBefore` / `CountAfter` / `Capacity` (taş ID'sine göre), `AddedIds`, `ShortIds` (hâlâ yetmeyen), `UnknownIds` (stok kaydı olmayan), `SmallRemovedIds` / `SmallKeptIds`, `Moves` (`StockMove`: `FromId`, `ToId`, `Count`), `MovedPixels`, `MeanShift`, `Level` (0–4 arama genişliği), `MinUsage`.

## Public API
| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `SolveWithMinimum(src, R, C, assign, pool, capacityOfId, familyOfId, opt, gamut)` | Stok sınırı + en az kullanım kuralı (turlar halinde); genel sonucu raporlar | `MosaicEngine.ApplyOptimalKWithStock` |
| `Solve(...)` | Tek tur: stoğu aşan taşların fazlasını aktarır | `SolveWithMinimum` |

`capacityOfId(id)`: o taştan kullanılabilecek adet; `null` = tabloda kayıt yok (sınırsız sayılır, ikame olarak kullanılmaz).

## Algoritma / akış
1. Piksel renkleri Lab'a çevrilir; kenar ağırlığı Optimum'daki gibi Sobel ile hesaplanır (`OptimalPaletteService.SobelMagnitude`, `EdgeWeight`).
2. Stoğu aşan taşlar bulunur; yoksa hiçbir şey değişmez.
3. Bu taşların pikselleri benzer renk gruplarına ayrılır.
4. Adaylar: stoğu boşta olan, benzerlik sınırı içindeki en yakın taşlar (palet + ikizler; gerekirse en fazla `MaxExtraStones` yeni tür).
5. En ucuz akış (min-cost flow, potansiyelli Dijkstra): maliyet = kenar ağırlığı × (d²(yeni) − d²(eski)). Toplam renk değişimi en küçük çözüm bulunur. Dijkstra, havuza (sink) giden yol kesinleşince durur; kenarlar düz dizilerde tutulur.
6. Yakın taşlar yetmezse sınır genişler: τ, 2τ, 4τ, sınırsız, sonra yeni tür sınırı da kalkar (`Level`).
   - Seviyeler birbirinden bağımsızdır ve **paralel** çalışır. Fazlanın tamamını yerleştiren en düşük seviye seçilir; daha düşük bir seviye başarılı olunca üstteki seviyeler durur.
   - Bir seviyede B planı (yeni türler serbest) fazlanın bir kısmını yerleştiremiyorsa o seviye başarısız sayılır ve yeni türleri tek tek eleme döngüsü (her adım bir tam çözüm) atlanır. Eleme ve A planı yalnızca hedef azaltır, bu yüzden sonuç değişmez.
   - Hiçbir seviye fazlanın tamamını yerleştiremezse (stok genel olarak yetmiyorsa) eski tam arama yapılır: en az eksik bırakan en erken seviye seçilir.
7. Grup içinde hangi piksellerin gideceği: 3×3 komşuluk ortalaması yeni taşa en yakın olanlar önce; eşitlikte Bayer sırası.
8. `SolveWithMinimum`: `MinUsage` > 1 ise sonuçta `0 < adet < MinUsage` olan taşlara kapasite 0 verilip tekrar çözülür; yeni küçük taş kalmayana kadar. Uygulamada `MinUsage = 0` olduğu için tek tur çalışır.

## Önemli davranışlar ve iş kuralları
- Stok sert sınırdır; ancak hiç stoklu taş kalmazsa `ShortIds` dolar.
- Eksi kapasite 0 sayılır.
- Hız (paralel seviyeler, `skipHopeless`, sink'te duran Dijkstra, düz dizili ağ): 300 cm genişlikte 125.000 taşlık bir test görselinde (st1.jpg; 10 taş stoğu aşıyor, 87.000 taş taşınıyor) düzeltme 86 sn'den 9 sn'ye, 7.jpg'de 249 sn'den yaklaşık 26–44 sn'ye indi. Sonuçlar bayt bayt aynı kaldı (karşılaştırma aracıyla doğrulandı).
- Stoğu aşan ve az kullanılan taş yoksa `Assignment` girdinin aynısıdır (birebir aynılık).
- **İptal** ([WorkCancellation](./WorkCancellation.md)`.Check()`): `SolveWithMinimum`'da her en az kullanım turunun başında, `Solve`'da paralel seviye başına, en ucuz akışta (`MinCostMove`) her artırma yolunda (augmenting path), piksel dağıtımında grup başına. İptalde `OperationCanceledException` (paralel seviyelerde `AggregateException` içinde) fırlar; çözücü yalnız kendi dizilerini kullandığı için mozaik değişmez, sonucu çağıran (`MainViewModel`) belirler. Ölçüm: 1200×1200 taşlık düzeltme tıklamadan 510 ms sonra durdu.

## Dikkat / bilinen sınırlamalar
- Benzerlik katalog RGB değerlerine dayanır (`Assets/colorsBas.txt`).
- Stok kaydı olmayan taşlar (`UnknownIds`) kontrol edilemez.
- Her yeni taş türü robot için bir renk değişimi daha demektir.

## İlgili dosyalar
- [MosaicEngine](./MosaicEngine.md), [OptimalPaletteService](./OptimalPaletteService.md), [StockSheetService](./StockSheetService.md), [ColorMatcher](./ColorMatcher.md)
- [MainViewModel](../ViewModels/MainViewModel.md), [StockCompareRunner](../StockCompareRunner.md)
