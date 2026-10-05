# StockAwareAssigner

> Kaynak: `mosair/Services/StockAwareAssigner.cs` · Güncelleme: 2026-10-06 · Durum: deneme (`stok_deneme` dalı)

## Amaç
Bitmiş bir Optimum mozaiğini, hiçbir taş stoğunu aşmayacak şekilde en az görünür değişiklikle yeniden düzenler. Stoğu yetmeyen taşın fazlası, renkçe en yakın ve stoğu olan taşlara aktarılır. Çok az kullanılan taşlar (`MinUsage` altı) da çıkarılır. Stoğu aşan ve az kullanılan taş yoksa mozaik değişmez. Ayrıntılı tasarım ve test sonuçları için [STOGA_GORE_RAPOR.md](../../STOGA_GORE_RAPOR.md).

## Nerede kullanılır
| Dosya | Kullanım |
|---|---|
| [MosaicEngine](./MosaicEngine.md) | `ApplyOptimalKWithStock` → `SolveWithMinimum` |
| [StockCompareRunner](../StockCompareRunner.md) | Çevrimdışı karşılaştırma |

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
| `MinUsage` | int | 0 | Bu sayının altında kullanılan taşlar çıkarılır (0/1 = kapalı) |
| `MinUsageFor(total)` | static | — | max(10, ⌈toplam × 0,0005⌉) |

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
5. En ucuz akış (min-cost flow, potansiyelli Dijkstra): maliyet = kenar ağırlığı × (d²(yeni) − d²(eski)). Toplam renk değişimi en küçük çözüm bulunur.
6. Yakın taşlar yetmezse sınır genişler: τ, 2τ, 4τ, sınırsız, sonra yeni tür sınırı da kalkar (`Level`).
7. Grup içinde hangi piksellerin gideceği: 3×3 komşuluk ortalaması yeni taşa en yakın olanlar önce; eşitlikte Bayer sırası.
8. `SolveWithMinimum`: sonuçta `0 < adet < MinUsage` olan taşlara kapasite 0 verilip tekrar çözülür; yeni küçük taş kalmayana kadar.

## Önemli davranışlar ve iş kuralları
- Stok sert sınırdır; ancak hiç stoklu taş kalmazsa `ShortIds` dolar.
- Eksi kapasite 0 sayılır.
- Stoğu aşan ve az kullanılan taş yoksa `Assignment` girdinin aynısıdır (birebir aynılık).

## Dikkat / bilinen sınırlamalar
- Benzerlik katalog RGB değerlerine dayanır (`Assets/colorsBas.txt`).
- Stok kaydı olmayan taşlar (`UnknownIds`) kontrol edilemez.
- Her yeni taş türü robot için bir renk değişimi daha demektir.

## İlgili dosyalar
- [MosaicEngine](./MosaicEngine.md), [OptimalPaletteService](./OptimalPaletteService.md), [StockSheetService](./StockSheetService.md), [ColorMatcher](./ColorMatcher.md)
- [MainViewModel](../ViewModels/MainViewModel.md), [StockCompareRunner](../StockCompareRunner.md)
