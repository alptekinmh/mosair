# GamutMapper

> Kaynak: `mosair/Services/GamutMapper.cs` · Güncelleme: 2026-10-06

## Amaç
Resmin Lab açıklık aralığını ve kroma değerini taş kataloğunun üretebildiği aralığa sıkıştırır. Ton (hue) korunur. Böylece katalog dışında kalan farklı kaynak renkleri aynı sınır taşına yığılmaz ve birbirinden ayrı kalır. Dönüşüm yalnızca sıkıştırma yönündedir. Zaten katalog gamı içinde kalan bir resim değişmeden geçer.

> **Durum: şu an etkin değil.** Sınıfa tek giriş yolu `MosaicEngine.RunOptimal(..., useGamut: true)` çağrısıdır. Ancak hiçbir çağıran `useGamut` için true vermez: MainViewModel, CompareRunner ve StockCompareRunner varsayılan false değeriyle çağırır. `MosaicEngine.LastGamut` da hiçbir yerde okunmaz. Kod deneysel olarak duruyor.

## Nerede kullanılır
| Dosya | Kullanım |
|---|---|
| [MosaicEngine](./MosaicEngine.md) | `RunOptimal` içinde `useGamut` true ise `GamutMapper.Build` çağrılır. Sonuç `_optGamut` olarak saklanır; `Analyze`, `ApplyOptimalK` ve `ApplyOptimalKWithStock` (stoğa göre düzeltme) bu eşleyiciyi kullanır. `FixCurrentMosaicToStock` her zaman `null` geçer. |
| [OptimalPaletteService](./OptimalPaletteService.md) | `Analyze` içinde her benzersiz pikselin Lab değeri `gamut.Map` ile dönüştürülür |
| [StockAwareAssigner](./StockAwareAssigner.md) | `SolveWithMinimum` / `Solve` içinde kaynak piksel Lab değerleri `gamut.Map` ile dönüştürülür (Optimum ile aynı) |

## Yapı
`GamutMapper` sealed bir sınıftır. Kurucusu private'tır, örnekler yalnızca `Build` ile oluşturulur.

| Alan | Tip | Açıklama |
|---|---|---|
| `_srcL0`, `_srcL1` | double | Resmin L aralığı (%1 ve %99 persentilleri) |
| `_dstL0`, `_dstL1` | double | Hedef L aralığı |
| `_chromaScale` | double | a ve b kanallarının çarpanı (≤ 1) |

Özel yardımcı `Pct(list, q)` listeyi kopyalayıp sıralar ve q persentilini döndürür.

## Public API
| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `Build(src, R, C, catalog)` | BGR resim ve katalogdan eşleyiciyi kurar | `MosaicEngine.RunOptimal` (yalnızca `useGamut` true ise) |
| `Map(lab)` | Lab değerini sıkıştırılmış Lab değerine çevirir | `OptimalPaletteService.Analyze`, `MosaicEngine.ApplyOptimalK`, `StockAwareAssigner` |
| `ToString()` | Tanı amaçlı özet döndürür (`L[..]->[..] chroma×..`) | Çağıran yok |

## Algoritma / akış
1. Resmin tüm pikselleri için L ve kroma (`sqrt(a² + b²)`) toplanır. Aynı değerler katalog taşları için de toplanır.
2. Kaynak aralığı: `sL0` = resim L'sinin %1 persentili, `sL1` = %99 persentili.
3. Katalog aralığı: `dL0` = katalog L'sinin en küçük değeri, `dL1` = en büyük değeri.
4. Hedef aralık: `tL0 = max(sL0, dL0)`, `tL1 = min(sL1, dL1)`. Bu aralık 1'den darsa hedef aralık kaynak aralığının kendisi olur ve sıkıştırma yapılmaz.
5. Kroma ölçeği: Resim kromasının %95 persentili katalogunkinden büyükse ölçek katalog kroması / resim kroması olur. Aksi halde 1'dir.
6. `Map` iki dönüşüm uygular:
   - L değeri `[sL0, sL1]` aralığında 0–1'e sıkıştırılarak normalize edilir ve `[tL0, tL1]` aralığına doğrusal olarak taşınır.
   - a ve b değerleri kroma ölçeğiyle çarpılır.

## Önemli davranışlar ve iş kuralları
- Kaynak aralığın dışındaki L değerleri hedef aralığın uçlarına kırpılır.
- Ton açısı değişmez, çünkü a ve b aynı katsayıyla ölçeklenir.

## Dikkat / bilinen sınırlamalar
- Şu an hiçbir akışta etkin değildir (yukarıdaki not).
- Katalog boşsa `Pct` boş dizide `Math.Clamp(…, 0, -1)` çağırır ve bu çağrı hata fırlatır.
- `Build` her piksel için Lab dönüşümünü önbelleksiz yapar.

## İlgili dosyalar
- [MosaicEngine](./MosaicEngine.md), [OptimalPaletteService](./OptimalPaletteService.md), [StockAwareAssigner](./StockAwareAssigner.md), [ColorMatcher](./ColorMatcher.md), [Rgb](../Models/Rgb.md)
