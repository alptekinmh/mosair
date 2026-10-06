# OptimalPaletteService

> Kaynak: `mosair/Services/OptimalPaletteService.cs` · Güncelleme: 2026-10-06

## Amaç
Bir resim için kaç farklı katalog taşının "yeterli" olduğunu bulur. Analiz tüm aktif katalogla başlar ve her adımda en az zarar veren taşı paletten çıkarır (açgözlü geriye eleme). Her k için kalite eğrileri kaydedilir. Ardından kalitenin tam katalogdan belirgin biçimde kötüleşmediği en küçük k seçilir. Taşların çıkarılma sırası da sonuçla birlikte döner, böylece her k için palet iç içe bir alt küme olur.

## Nerede kullanılır
| Dosya | Kullanım |
|---|---|
| [MosaicEngine](./MosaicEngine.md) | `RunOptimal` içinde `Analyze` çağrılır. `ApplyOptimalK` içinde `LightnessWeight` ile taş eşlemesi yapılır. |
| [MosaicMetrics](./MosaicMetrics.md) | `EdgeContrast` eşiğini kullanır |
| [CompareRunner](../CompareRunner.md) | `MOSAIR_LW` ortam değişkeniyle `LightnessWeight`'i değiştirir. `KKnee` ve `KThreshold` değerlerini loglar. |
| [StockAwareAssigner](./StockAwareAssigner.md) | Stoğa göre düzeltmede piksel ağırlığı için `SobelMagnitude`, `Percentile` (%98) ve `EdgeWeight` kullanılır; böylece kenar ağırlığı bu servisle aynı hesaplanır. |
| [MainViewModel](../ViewModels/MainViewModel.md) | `MosaicEngine.LastOptimalResult` üzerinden `KOptimal` ve `CandidateCount` değerlerini okur (slider aralığı ve önerilen k) |

## Yapı

### `OptimalPaletteResult` (sealed sınıf)
| Alan | Tip | Açıklama |
|---|---|---|
| `MeanByK` | double[M+1] | k taş için ağırlıklı ortalama mesafe. İndeks 0 kullanılmaz. |
| `P95ByK`, `P99ByK` | double[M+1] | Piksel mesafelerinin %95 ve %99 persentili |
| `EdgeKeptByK` | double[M+1] | Güçlü kenarlardan iki yanı farklı taş alanların oranı |
| `CandidateCount` | int | M, yani aday (aktif katalog) taş sayısı |
| `KOptimal` | int | Önerilen taş sayısı |
| `KKnee` | int | Eğrinin dirsek noktası (yalnızca bilgi amaçlı) |
| `KThreshold` | int | Toleransları sağlayan en küçük k |
| `KProtected` | int | Korumalı bir taşın ilk kez çıkarılmak zorunda kalındığı k. 0 ise hiç olmadı demektir. |
| `RemovalOrder` | List&lt;int&gt; | Aday indeksleri çıkarılma sırasıyla tutulur. Son k eleman k taşlık paleti oluşturur. |
| `Selected` | List&lt;int&gt; | `KOptimal` paletinin aday indeksleri |

### Sabitler
| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `EdgeWeight` | const double | 2.0 | Kenar piksellerinin ağırlık katkısı (1 + EdgeWeight·min(1, kenar/referans)) |
| `LightnessWeight` | static double | 3.0 | Mesafede ΔL çarpanı. Açıklık farkı renk farkından 3 kat önemli sayılır. Değiştirilebilir. |
| `MeanTolerance` | const double | 1.0 | Ortalamada tam kataloğa göre izin verilen artış |
| `P95Tolerance` | const double | 2.3 | P95'te izin verilen artış |
| `P99Tolerance` | const double | 5.0 | P99'da izin verilen artış |
| `ProtectDeltaE` | const double | 10.0 | Taş çıkarılınca sahip olduğu piksellerdeki ortalama kayma bunu aşarsa taş korunur |
| `MinProtectedShare` | const double | 0.001 | Koruma için gereken en az piksel payı (toplamın %0,1'i) |
| `MinProtectedPixels` | const int | 4 | Koruma için gereken en az piksel sayısı |
| `EdgeContrast` | const double | 12.0 | Komşu pikseller arasında "güçlü kenar" sayılmak için gereken en az Lab farkı |
| `EdgeLambda` | const double | 1.0 | Kenar cezası katsayısı (`EdgeLambda·d²`) |
| `EdgeLossTolerance` | const double | 0.02 | Korunan kenar oranında izin verilen kayıp |
| `HistBins` / `HistStep` | private const | 2000 / 0.1 | Persentil histogramı. Aralığı 0–200, adımı 0,1. |

### Yardımcı metotlar
| Metot | Erişim | Açıklama |
|---|---|---|
| `SobelMagnitude(L, R, C)` → `double[]` | internal static | L kanalında 3×3 Sobel kenar büyüklüğü; resim kenarında komşu indeksleri sınıra kenetlenir (clamp). `StockAwareAssigner` de kullanır. |
| `Percentile(values, q)` → `double` | internal static | Diziyi kopyalayıp sıralar ve q persentilini döndürür. `StockAwareAssigner` de kullanır. |
| `HistPercentile(hist, totalCount, q)` | private static | P95/P99 değerini histogramdan okur (kutu üst sınırı). |
| `RecordCurve`, `EdgeKept`, `AdvanceAlive`, `SelectK` | private static | Aşağıdaki akışta anlatılan adımlar. |

## Public API
| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `Analyze(src, R, C, candidates, onProgress, gamut)` | BGR kaynak ve aday listesiyle eleme yapar ve `OptimalPaletteResult` döndürür. `candidates` boşsa boş sonuç döner. | `MosaicEngine.RunOptimal` |

## Algoritma / akış
1. **Benzersiz renkler**: Her piksel Lab'a çevrilir. `gamut` verilmişse önce `gamut.Map` uygulanır. Benzersiz renkler U listesinde toplanır ve her pikselin hangi benzersiz renge ait olduğu kaydedilir.
2. **Kenar ağırlığı**: Açıklık (L) kanalına Sobel uygulanır (`SobelMagnitude`). Referans değer kenar büyüklüğünün %98 persentilidir. Her benzersiz rengin ağırlığı, ait olduğu piksellerin 1 + EdgeWeight·min(1, kenar/referans) değerlerinin toplamıdır.
3. **Mesafe tablosu** (paralel): Her benzersiz renk–aday çifti için `d = sqrt((ΔL·LightnessWeight)² + Δa² + Δb²)` hesaplanır. Adaylar her renk için mesafeye göre sıralanır (`order`).
4. **Güçlü kenarlar**: Yatay ve dikey komşu piksel çiftlerinden Lab farkı `EdgeContrast` ve üzeri olanlar listeye alınır. Bu farka LightnessWeight uygulanmaz. Her kenarın cezası `EdgeLambda·d²` olur.
5. k = M için eğri değerleri kaydedilir (`RecordCurve`, `EdgeKept`).
6. **Eleme döngüsü** (k = M … 2):
   1. Her canlı taş için çıkarma maliyeti hesaplanır: Σ ağırlık·(ikinci en iyi mesafe² − en iyi mesafe²). Taş bir rengin tek seçeneğiyse maliyet sonsuz olur.
   2. Taş çıkarıldığında bir güçlü kenarın iki yanı aynı taşa düşecekse kenar cezası maliyete eklenir.
   3. **Koruma**: Taşın sahip olduğu piksel sayısı max(`MinProtectedPixels`, ceil(toplam piksel·`MinProtectedShare`)) ve üzeriyse ve piksel başına ortalama kayma `ProtectDeltaE` değerini aşıyorsa taş korumalı sayılır.
   4. Kurban seçilir. Korumasız taşlar her zaman öncelikli aday olur. Aralarından maliyeti en düşük olan seçilir, eşitlikte daha az piksele sahip olan seçilir. Kurban korumalıysa ve bu ilk kez oluyorsa `KProtected = k` kaydedilir.
   5. Kurban `RemovalOrder` listesine eklenir. Her rengin en iyi ve ikinci en iyi canlı adayı güncellenir (`AdvanceAlive`).
   6. k−1 için eğri değerleri kaydedilir ve ilerleme bildirilir.
7. Sağ kalan son taş `RemovalOrder`'ın sonuna eklenir.
8. **k seçimi** (`SelectK`):
   - `KThreshold`: Aşağıdaki dört koşulu birlikte sağlayan en küçük k.
     - `MeanByK[k] ≤ MeanByK[M] + MeanTolerance`
     - `P95ByK[k] ≤ P95ByK[M] + P95Tolerance`
     - `P99ByK[k] ≤ P99ByK[M] + P99Tolerance`
     - `EdgeKeptByK[k] ≥ EdgeKeptByK[M] − EdgeLossTolerance`
   - `KKnee`: Normalize edilmiş eğride `1 − x − y` değerini en büyük yapan k (Kneedle).
   - `KOptimal = max(KThreshold, KProtected)`.
9. `Selected`, `RemovalOrder` listesinin son `KOptimal` elemanıdır.

### Eğri ölçüleri (`RecordCurve`)
- **Ortalama**: Kenar ağırlıklı ortalama, Σ ağırlık·d / Σ ağırlık.
- **P95/P99**: Piksel sayısıyla doldurulan 0,1 adımlı histogramdan okunur. Sonuç ilgili histogram kutusunun üst sınırıdır.

## Önemli davranışlar ve iş kuralları
- Mesafeler ve tolerans eşikleri **saf CIE76 ΔE değildir**. ΔL `LightnessWeight` (3.0) ile çarpılır ve ortalama kenar ağırlıklıdır. Bu yüzden tolerans sayıları bu ölçeğe göre ayarlanmıştır. [MosaicMetrics](./MosaicMetrics.md)'teki ΔE ile doğrudan karşılaştırılamaz.
- `LightnessWeight` static ve yazılabilirdir. Değeri `MosaicEngine.ApplyOptimalK` içindeki eşlemeyi de etkiler. `StockAwareAssigner` bunu kullanmaz; kendi `StockAwareOptions.LightnessWeight` (varsayılan 1.0) değeriyle çalışır. CompareRunner bu değeri `MOSAIR_LW` ortam değişkeniyle değiştirebilir.
- `KKnee` önerilen k'yı etkilemez. Yalnızca CompareRunner loglarında görünür.
- Eleme sırası sabit olduğu için kullanıcı slider ile k'yı değiştirdiğinde analiz yeniden çalıştırılmaz.
- İlerleme, eleme adımlarının oranı olarak bildirilir.

## Dikkat / bilinen sınırlamalar
- Bellek kullanımı U×M'dir (`float` mesafe + `int` sıra). Çok renkli büyük resimlerde bu tablo büyür.
- Eleme döngüsü her adımda tüm benzersiz renkleri ve kenarları dolaşır. Karmaşıklık yaklaşık O(M·(U+E+M))'dir.
- Histogram 200'de doyar. Bu değerin üzerindeki mesafeler son kutuya düşer.
- `Percentile` tüm kenar dizisini kopyalayıp sıralar.
- `SobelMagnitude` ve `Percentile` `internal` olduğundan imzaları değişirse `StockAwareAssigner` da güncellenmelidir.

## İlgili dosyalar
- [MosaicEngine](./MosaicEngine.md), [StockAwareAssigner](./StockAwareAssigner.md), [ColorMatcher](./ColorMatcher.md) (`RgbToLab`), [GamutMapper](./GamutMapper.md), [MosaicMetrics](./MosaicMetrics.md)
- [Rgb](../Models/Rgb.md), [CompareRunner](../CompareRunner.md), [MainViewModel](../ViewModels/MainViewModel.md), [Arayüz kılavuzu](../../ARAYUZ.md)
