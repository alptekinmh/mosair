# NearestColorIndex

> Kaynak: `mosair/Services/NearestColorIndex.cs` · Güncelleme: 2026-10-07

## Amaç
Klasik Mos'un (`MosaicEngine.RunM3`) en yakın renk aramasını hızlandırır, sonucu değiştirmeden. Eski yöntem her taşı paletteki her renkle karşılaştırıyordu:

```
for k in 0..P-1: d = mesafe(q, p[k]); if (d < min) { min = d; n = k; }
```

Bu sınıf aynı cevabı verir: en küçük mesafe, eşitlikte en küçük sıra numarası. Mesafeler aynı ifadelerle (aynı işlem sırasıyla) hesaplandığı için karşılaştırılan kayan nokta değerleri de birebir aynıdır.

Dosyada iki sınıf vardır:

- **`NearestColorIndex`**: palet noktaları üzerinde ızgara tabanlı tam (yaklaşık olmayan) en yakın komşu araması.
- **`DistinctColors`**: taş görüntüsündeki farklı renkleri ve her taşın hangi renk olduğunu çıkarır; böylece arama taş başına değil, renk başına bir kez yapılır.

## Nerede kullanılır
| Dosya | Kullanım |
|---|---|
| [MosaicEngine](./MosaicEngine.md) | `RunM3` küçültülmüş resimden bir `DistinctColors` kurar. `RunM1` (katalog) ve her `ProcessM3` turu (kalan ızgara paleti) için bir `NearestColorIndex` kurulur; `NearestPerColor` her farklı renk için `Find` çağırır (paralel). |

## Yapı

### `NearestColorIndex`
| Alan | Açıklama |
|---|---|
| `_c0`, `_c1`, `_c2` | Palet noktalarının üç koordinatı, mesafenin kullandığı sırayla: klasik Mos'ta `(b, g, r)` ya da Lab ile `(L, A, B)`. |
| `_average` | true: "ortalama" mesafe `(|d0| + |d1| + |d2|) / 3.0`; false: kare mesafe `d0² + d1² + d2²`. |
| `_min0..2`, `_cell` | Izgaranın başlangıcı ve küp hücre boyu. Hücre boyu, en geniş eksen `ceil(∛(P/2))` parçaya bölünecek şekilde seçilir (hücre başına yaklaşık iki nokta). |
| `_n0..2` | Eksen başına hücre sayısı. |
| `_cellStart`, `_points` | Hücre içerikleri (CSR düzeni): `c` hücresinin noktaları `_points[_cellStart[c] .. _cellStart[c+1])`; her hücrede sıra numaraları artan sırada. |

### `DistinctColors`
| Alan | Açıklama |
|---|---|
| `PixelColor` | Taş başına (satır satır) farklı renk numarası. |
| `B`, `G`, `R` | Farklı renklerin bayt değerleri; ilk görülme sırasıyla. |
| `Count` | Farklı renk sayısı. |

## Public API
| Üye | Ne yapar |
|---|---|
| `NearestColorIndex(c0, c1, c2, average)` | Noktaları ızgaraya yerleştirir. Boş palet de kabul edilir. |
| `Find(q0, q1, q2)` → `int` | En yakın noktanın sıra numarası; eşit mesafede en küçük sıra. Boş palette 0 döner (eski döngü gibi). |
| `DistinctColors(byte[,,] data)` | `ImageService.ToByteArray` düzenindeki (BGR) taş verisinden farklı renkleri çıkarır. |

## Önemli davranışlar ve iş kuralları
- **Arama:** sorgunun hücresinden başlayıp halka halka (Chebyshev uzaklığı 0, 1, 2, …) dışa doğru hücreler taranır. `ring` halkasına başlarken, o halkadaki ve daha dıştaki her nokta sorgudan en az bir eksende `(ring − 1) · hücre` kadar uzaktır (sorgu kendi hücresinin herhangi bir yerinde ya da ızgaranın dışında olabilir). Bu alt sınır kare mesafede `gap²`, ortalama mesafede `gap / 3` olur. Alt sınır, bulunan en iyi mesafeden büyükse arama durur.
- **Güvenlik payı:** durma koşulu `bound · (1 − 1e-9) > min`'dir. Yuvarlama hiçbir zaman eski döngünün seçeceği bir noktanın atlanmasına yol açmaz. Alt sınır en iyi mesafeye eşitse arama sürer, çünkü dışarıda aynı mesafede ama daha küçük sıra numaralı bir nokta olabilir.
- **Eşitlik:** taranan her noktada `av < min || (av == min && k < best)`; hücreler hangi sırayla taranırsa taransın sonuç, eski döngünün ilk bulduğu en küçük değerle aynıdır.
- Izgara dışındaki sorgular (ör. Lab'da palet sınırlarının ötesi) en yakın kenar hücresine sıkıştırılır; alt sınır yine geçerlidir.
- Doğrulama: eski ve yeni `RunM3`, 6 görselde (en büyüğü 6000×6000 px) RGB/Lab × kare/ortalama mesafe ve `RGBInc` 5, 7, 10, 19, 32 ile toplam 182 durumda karşılaştırıldı. `dataM3`, `dataM1`, `drl.dat`, `arMA/arMB/arMBR` (tüm alanlar), katalog sayaçları ve bitmap'ler bayt bayt aynı çıktı.

## Dikkat / bilinen sınırlamalar
- Palet her `ProcessM3` turunda değiştiği için dizin her turda yeniden kurulur (17.576 nokta için milisaniyeler).
- `DistinctColors` bir `Dictionary<int,int>` kullanır; çok renkli büyük görsellerde farklı renk sayısı milyonları bulabilir (bellek: renk başına birkaç bayt + sözlük).
- Yalnızca klasik Mos kullanır; Optimum'un kendi renk başına önbelleği vardır ([OptimalPaletteService](./OptimalPaletteService.md)).

## İlgili dosyalar
- [MosaicEngine](./MosaicEngine.md)
- [ColorMatcher](./ColorMatcher.md) (`RgbToLab`)
- [WorkCancellation](./WorkCancellation.md)
