# WorkCancellation

> Kaynak: `mosair/Services/WorkCancellation.cs` · Güncelleme: 2026-10-06

## Amaç
Durum çubuğundaki **İptal** düğmesinin (ve Esc'nin) belirtecini motora taşır. Belirteç `AsyncLocal<CancellationToken>` içinde tutulur; `Task.Run` içinde atanınca o işin `Parallel.For` döngülerine de kendiliğinden geçer. Böylece motorun uzun döngüleri, her metoda ayrı bir `CancellationToken` parametresi eklemeden kontrol noktalarında durabilir. İptal edilebilir bir işin dışında belirteç `CancellationToken.None`'dır ve `Check()` hiçbir şey yapmaz; bu yüzden aynı kod `CompareRunner` gibi araçlarda değişmeden çalışır.

## Nerede kullanılır
| Dosya | Kullanım |
|---|---|
| [MainViewModel](../ViewModels/MainViewModel.md) | `RunMosaicAsync`, `ApplyOptimalKAsync`, `FixToStockAsync` ve `ExportImageAsync` `Task.Run` içinde `Token = cts.Token` atar; iptalden sonra işi düz biçimde yeniden kurmadan önce `Token = default` yapar. |
| [OptimalPaletteService](./OptimalPaletteService.md) | `Analyze`: renk ve kenar geçişlerinde satır başına, paralel mesafe tablosunda benzersiz renk başına, eleme döngüsünde adım başına `Check()`. |
| [MosaicEngine](./MosaicEngine.md) | `RunM3`: azaltma turu başına; `RunM1` ve `ProcessM3` paralel döngülerinde satır başına `Check()`. |
| [StockAwareAssigner](./StockAwareAssigner.md) | En az kullanım turu, paralel seviye, en ucuz akıştaki artırma yolu ve piksel dağıtımındaki grup başına `Check()`. |
| [MosaicExporter](./MosaicExporter.md) | Akışla PNG ve büyük JPEG'de taş satırı başına `Check()`. |
| [MosaicRenderSource](./MosaicRenderSource.md) | `RenderRegion`'da taş satırı başına `Check()`. Belirteci yalnızca dışa aktarma atar; ekran karoları ve dosya boyutu tahmini etkilenmez. |

## Yapı
`public static class WorkCancellation`

| Ad | Tip | Açıklama |
|---|---|---|
| `CurrentToken` | `static readonly AsyncLocal<CancellationToken>` (private) | Bu mantıksal iş parçacığının belirteci. |

## Public API
| Üye | Ne yapar | Kimden çağrılır |
|---|---|---|
| `Token` (get/set) | Geçerli mantıksal iş parçacığının belirtecini okur/atar. Atama yalnızca o akışı ve ondan başlayan görevleri etkiler; çağıranın (UI iş parçacığının) değeri değişmez. | MainViewModel |
| `Check()` | `CurrentToken.Value.ThrowIfCancellationRequested()`: iptal istendiyse `OperationCanceledException` fırlatır. | Yukarıdaki servisler |

## Algoritma / akış
1. `MainViewModel.BeginCancellable()` yeni bir `CancellationTokenSource` kurar (`CanCancel` true, düğme görünür).
2. İş `Task.Run(() => { WorkCancellation.Token = cts.Token; … })` içinde çalışır. `AsyncLocal` değeri `Parallel.For`'un işçi görevlerine de akar.
3. Kullanıcı İptal'e ya da Esc'ye basar → `MainViewModel.CancelWork()` → `cts.Cancel()`, durum "İptal ediliyor...".
4. İlk kontrol noktasında `Check()` `OperationCanceledException` fırlatır. `Parallel.For` içinden gelirse `AggregateException` ile sarılır; `MainViewModel.IsCancellation` ikisini de iptal sayar.
5. İşi başlatan metot sonucu belirler (önceki mozaiği koru, yarım mozaiği temizle, düzeltmesiz bırak, yarım dosyayı sil); ayrıntı [MainViewModel](../ViewModels/MainViewModel.md) **İptal** bölümünde.
6. `EndCancellable(cts)` kaynağı kapatır, düğme gizlenir.

## Önemli davranışlar ve iş kuralları
- Kontrol noktaları yalnızca okuma yapar; sonucu değiştirmez. Optimum ve klasik Mos için karşılaştırma aracıyla iptalsiz çıktının birebir aynı kaldığı doğrulandı.
- Kontrol noktaları seyrek tutulmuştur (satır, tur, grup başına), bu yüzden maliyeti ölçülemeyecek kadar küçüktür.
- Ölçülen durma süreleri (tıklamadan işin durmasına kadar): Optimum 4000×4000 taş 8 ms (önceki mozaik değişmeden), klasik 2000×2000 125 ms, stoğa göre düzeltme 1200×1200 510 ms, akışla yazılan 20 m PNG 34 ms (yarım dosya silindi).

## Dikkat / bilinen sınırlamalar
- Kontrol noktası olmayan adımlar durdurulamaz: `MosaicEngine.ApplyOptimalK`, mozaiğin yeniden kurulması, doku hazırlığı ve dışa aktarmada çizimden sonraki kodlama adımı (tek bitmap'te ve büyük JPEG'de). Bu adımlarda basılan iptal etkisiz kalır; iş normal biter.
- Belirteç `Task.Run` içinde atanmalıdır. UI iş parçacığında atanırsa sonraki bütün işlere sızar.
- Yeni bir uzun döngü eklenirken iptal edilmesi isteniyorsa `WorkCancellation.Check()` eklenmeli ve döngünün paylaşılan veriyi değiştirip değiştirmediği çağıranın iptal yolunda dikkate alınmalıdır.

## İlgili dosyalar
- [MainViewModel](../ViewModels/MainViewModel.md), [MainWindow](../MainWindow.md) (İptal düğmesi, Esc)
- [MosaicEngine](./MosaicEngine.md), [OptimalPaletteService](./OptimalPaletteService.md), [StockAwareAssigner](./StockAwareAssigner.md), [MosaicExporter](./MosaicExporter.md)
- [Arayüz kılavuzu](../../ARAYUZ.md)
