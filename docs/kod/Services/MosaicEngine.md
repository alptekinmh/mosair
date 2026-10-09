# MosaicEngine

> Kaynak: `mosair/Services/MosaicEngine.cs` · Güncelleme: 2026-10-07

## Amaç
Mozaik üretiminin ana motoru. Yüklenen resmi taş ızgarası boyutuna küçültür ve her taş hücresine katalogdaki bir taş rengini atar. İki yolu vardır:

- **Optimum** (`RunOptimal` / `ApplyOptimalK`): Palet seçimini [OptimalPaletteService](./OptimalPaletteService.md)'e bırakır ve yalnızca katalog taşlarıyla çalışır.
- **Klasik M1/M3** (`RunM3`): WPF sürümünden taşınan yoldur. Önce katalog dışı bir RGB ızgara paleti kurar ve bunu iteratif olarak azaltır. Kalan renkleri en sonda katalog taşlarına eşler.

Her iki yolun ardından isteğe bağlı bir **stoğa göre düzeltme** adımı çalışabilir (`ApplyOptimalKWithStock`, `FixCurrentMosaicToStock`). Bu adım iki algoritmayı da değiştirmez; bitmiş mozaiği [StockAwareAssigner](./StockAwareAssigner.md) ile stoğu aşmayacak şekilde yeniden düzenler.

Sonuçların hepsi [MosaicData](../Models/MosaicData.md) ve `drl` statik alanlarına yazılır. Sınıfın kendisi de tamamen statiktir ve global durum tutar.

## Nerede kullanılır
| Dosya | Kullanım |
|---|---|
| [MainViewModel](../ViewModels/MainViewModel.md) | `Reset`, `LoadImage`, `CalculateDimensions`, `RunOptimal`, `RunM3`, `ApplyOptimalK` (slider, 350 ms debounce), `LastOptimalResult`, `width`/`height` okuma. "Stoğa göre" açıkken `ApplyOptimalKWithStock` (Optimum), `FixCurrentMosaicToStock` (klasik Mos ve Stok Kontrol öncesi düzeltme), `LastStockResult` (rapor) ve `LastRunPool` (düzeltme yapılabilir mi kontrolü) |
| [CompareRunner](../CompareRunner.md) | `--compare` modu: `RunM3` (WPF ve yeni silme stiliyle), `RunOptimal`, `GetSourceStoneData`, `WpfStyleRemoval` |
| [StockCompareRunner](../StockCompareRunner.md) | Stoğa göre karşılaştırma aracı: `RunOptimal`, `RunM3`, `ApplyOptimalKWithStock`, `FixCurrentMosaicToStock`, `LastStockResult` |
| [ProjectService](./ProjectService.md) | Proje kaydedip açarken `width`, `height`, `rgbM` alanlarını yazar ve okur |

`CloneRgb`, `CloneList` ve `CloneNestedList` public olsa da yalnızca bu dosyanın içinde kullanılır. [PixelEditService](./PixelEditService.md)'in kendine ait private bir `CloneRgb` metodu vardır.

## Yapı

### `DimensionResult` (sınıf)
`CalculateDimensions` bu sınıfı döndürür.

| Alan | Tip | Açıklama |
|---|---|---|
| `WidthCm`, `HeightCm` | double | Taş katına yuvarlanmış gerçek ölçü (cm) |
| `AreaM2` | double | Alan (m²) |
| `StoneColumns`, `StoneRows`, `Stones` | int | Taş ızgarası ve toplam taş sayısı |
| `MouldColumns`, `MouldRows`, `Moulds` | int | 26×26 taşlık kalıp sayıları |
| `OriginalWidth`, `OriginalHeight` | int | Kaynak resmin piksel boyutu |

### `MosaicEngine` statik alanları
| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `width`, `height` | double | – | Taş ızgarası (sütun, satır) |
| `wi`, `he` | int | – | Kalıp sütun ve satır sayısı (yukarı yuvarlanmış) |
| `actualWidth`, `actualHeight` | double | – | Gerçek ölçü (mm) |
| `rate` | double | – | Resmin yükseklik/genişlik oranı |
| `excessiveW`, `excessiveH` | double | – | Kalıp sayısının ondalık artığı. Dışarıda okunmaz. |
| `rgbM` | int | 3 | Hedef renk sayısı. Optimum yolda k değerini tutar. |
| `RGBInc` | int | 19 | M3 başlangıç paletinin RGB adımı |
| `minRGBInc` | int | 2 | Her iterasyonda silme eşiğine eklenen artış |
| `numOfMinRGB` | int | – | Silme eşiği. Bu sayıdan az piksele sahip renkler silinir. |
| `boolLab`, `boolAv` | bool | – | M1/M3'te Lab uzayı ve ortalama mutlak fark seçenekleri |
| `interpolationMethod` | `InterpolationMethod` | `Area` | Küçültme yöntemi |
| `penW` | int | 1 | Kullanılmıyor |
| `WpfStyleRemoval` | bool | false | Yalnızca karşılaştırma içindir. WPF'teki sınırsız renk silmeyi taklit eder. |
| `LastOptimalResult` | `OptimalPaletteResult?` | null | Son optimum analizi (salt okunur) |
| `LastGamut` | `GamutMapper?` | null | Son gamut eşleyicisi. Kimse okumuyor. |
| `LastRunPool` | `List<rgb>?` (salt okunur) | null | Son mozaik üretilirken seçilebilir olan taşlar (`MosaicData.arRGB` kopyası). `RunM3` başında, `RunOptimal`'da ise analiz bittikten sonra atanır (iptal edilen Optimum eski değeri bozmaz), `Reset()` temizler. Stok düzeltmesinde ikame taşlar yalnızca bu havuzdan seçilir; böylece kullanıcının katalog seçimi ve Stok Çek ile kapatılan taşlar korunur. |
| `LastStockResult` | `StockAwareResult?` (salt okunur) | null | Son stok düzeltmesinin sonucu. `FixToStock` başında null yapılır, çözümden sonra atanır; `Reset()` temizler. Düzeltme yapılamadıysa (eşleşmeyen piksel) null kalır. |
| `_optSrc`, `_optCandidates`, `_optGamut` | private | null | `ApplyOptimalK` için önbellek |

### `drl.dat[R, C, 4]` kanalları
| İndeks | Anlam |
|---|---|
| 0, 1 | Bayrak. `CreateSingleRegion` her ikisini de 1 yapar. `ProcessM3` en az birinin 1 olmasını bekler. |
| 2 | Bölge numarası (1'den başlar). Şu an her zaman 1'dir. |
| 3 | Önce renk numarası `u`, sonra `uc`. `RunM3` bittiğinde katalog `ID` olur. `ApplyOptimalK` buraya doğrudan `ID` yazar. |

## Public API
| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `LoadImage(path)` | `ImageService.LoadImage` ile resmi yükler ve `MosaicData.inputBitmap`'e koyar | MainViewModel, CompareRunner |
| `CalculateDimensions(widthCm, rows = null)` | Taş ve kalıp ızgarasını hesaplar, statik alanları doldurur, `DimensionResult` döndürür. `rows` verilirse taş satırı (`height`) görselin oranından değil ondan alınır (mosair'in dışa aktardığı görselin ölçü etiketi); verilmezse eskisi gibi. | MainViewModel, CompareRunner |
| `GetSourceStoneData()` | Kaynağı taş ızgarasına küçültür ve BGR `byte[,,]` döndürür | CompareRunner |
| `RunOptimal(interpMethod, onProgress, prepareTextures, useGamut)` | Optimum analizini yapar ve önerilen k ile mozaiği üretir. Analiz iptal edilebilir ve paylaşılan hiçbir şeyi değiştirmeden önce çalışır; iptalde `OperationCanceledException` fırlar, önceki mozaik olduğu gibi kalır. | MainViewModel, CompareRunner |
| `ApplyOptimalK(k, prepareTextures)` | Son analizden k taşlık alt kümeyi uygular (yeniden analiz yapmaz) | MainViewModel |
| `RunM3(targetColors, rgbIncrement, useLab, useAverage, interpMethod, onProgress, prepareTextures)` | Klasik M1/M3 hattını çalıştırır. İptal edilebilir; paylaşılan veriyi giderek değiştirdiği için iptalde yarım kalan mozaiği çağıran temizler (`MainViewModel.ClearMosaic`). | MainViewModel, CompareRunner |
| `ApplyOptimalKWithStock(k, capacityOfId, familyOfId, options, prepareTextures)` | `ApplyOptimalK(k, prepareTextures: false)` çalıştırır, ardından sonucu stoğa göre düzeltir (`FixToStock`). Dokular her durumda (`prepareTextures` true ise) hazırlanır. Hiçbir taş stoğu aşmıyorsa mozaik `ApplyOptimalK`'nın ürettiğinin aynısıdır. `MosaicData.reducedBitmap` döndürür. | MainViewModel (`ApplyOptimalKFor`), StockCompareRunner |
| `FixCurrentMosaicToStock(capacityOfId, familyOfId, options, prepareTextures)` → `bool` | Ekrandaki mozaiği (klasik Mos sonucu da olabilir) stoğa göre düzeltir. `LastRunPool` veya `inputBitmap` yoksa ya da kaynak boyutu `dataM3` ile uyuşmuyorsa `false` döndürür. Gamut kullanmaz. Dokular yalnızca mozaik değiştiyse yeniden hazırlanır. | MainViewModel (`FixClassicMosaicToStock`), StockCompareRunner |
| `Reset()` | Tüm mozaik durumunu, bitmap'leri, doku ve piksel düzenleme durumunu temizler; `LastOptimalResult`, `LastRunPool`, `LastStockResult`, Optimum önbelleğini ve dolgu bilgisini (`ForgetPadding`) de sıfırlar | MainViewModel, CompareRunner, StockCompareRunner |
| `MouldStones` (sabit, 26) · `UpToMould(n)` · `PaddingCount(rows, cols)` | Kalıp kenarındaki taş sayısı; `n`'yi 26'nın bir üst katına yuvarlar (tam katsa aynen); R × C mozaiği tam kalıba tamamlamak için gereken dolgu taşı sayısı (`UpToMould(R)·UpToMould(C) − R·C`, tam kalıpsa 0) | MainViewModel (`PadResult`, `PadPreview`, `UpdateDimensions`) |
| `UnpaddedRows`, `UnpaddedCols`, `FillerId`, `FillerCode`, `IsPadded` | Bu oturumda eklenen dolgunun bilgisi: dolgudan önceki boyut ve dolgu taşı. Dolgu yoksa 0 / `""` / false. | MainViewModel, ProjectService |
| `ForgetPadding()` | Dolgu bilgisini siler (dizilere dokunmaz). `CreateSingleRegion` (sıfırdan kurulan her mozaik), `Reset` ve `ProjectService.ApplyProject` çağırır: açılan projenin dolgusu artık yalnızca kaydedilmiş mozaiğin bir parçasıdır. | Dosya içi, ProjectService |
| `ImageColours(bmp)` → Lab listesi | Görselin renkleri (aşağıda) | MainViewModel (`PadResult`, `UpdatePadPreview`) |
| `ChooseFiller(colours, excludeUsed)` → `rgb?` | Dolgu taşını seçer (aşağıda). Uygun taş yoksa null. | MainViewModel (`PadResult`, `UpdatePadPreview`) |
| `PadToMoulds(filler)` → `SKBitmap` | Mozaiği sağa ve alta tam kalıba tamamlar, yeni taş renkleri bitmap'ini döndürür (aşağıda). Zaten tam kalıpsa hiçbir şey değiştirmeden `reducedBitmap`'i döndürür. | MainViewModel (`PadResult`) |
| `RemovePadding()` | Dolguyu geri alır (sol üstten kırpma). Dolgu yoksa hiçbir şey yapmaz. | MainViewModel (`FixToStockAsync`) |
| `CloneRgb`, `CloneList`, `CloneNestedList` | `rgb` nesnelerini ve listelerini derin kopyalar | Dosya içi |

## Algoritma / akış

### Boyut hesabı (`CalculateDimensions`)
1. Bir sıradaki taş sayısı `Convert.ToInt32(widthCm*10/12)` ile bulunur (taş 12 mm). `actualWidth` bu sayının 12 katıdır.
2. Kalıp sütunu taş sütun sayısının 26'ya bölünüp yukarı yuvarlanmasıyla (`wi`) olarak hesaplanır (kalıp 26×26 taş).
3. `rate` = resim yüksekliği / resim genişliği bulunur. Ardından `height`, `width*rate` değerinin yuvarlanmasıyla, `actualHeight` ise `height*12` olarak hesaplanır.
4. Kalıp satırı `he`, `actualWidth*rate / (12*26)` değerinin yukarı yuvarlanmasıyla bulunur. Bu, yuvarlanmamış yükseklikten hesaplanır.

### Optimum yol (`RunOptimal` → `ApplyOptimalK`)
1. Kaynak `(int)width × (int)height` boyutuna yerel bir bitmap'e küçültülür ve BGR diziye çevrilir.
2. Adaylar aktif katalogdur (`MosaicData.arRGB` kopyası). `useGamut` true ise `GamutMapper.Build` çağrılır. Varsayılan false'tur.
3. `OptimalPaletteService.Analyze` çağrılır. İlerleme bu adımda %0–90 aralığına ölçeklenir. Analiz iptal edilirse (ya da hata verirse) yerel bitmap serbest bırakılır ve istisna yeniden atılır; bu noktaya kadar paylaşılan hiçbir şey değişmemiştir.
4. Analiz bittikten sonra paylaşılan alanlar atanır: `reducedBitmap` (yerel bitmap ile değiştirilir), `LastRunPool`, `LastGamut`, `LastOptimalResult` ve `_optSrc`/`_optCandidates`/`_optGamut` önbelleği.
5. `ApplyOptimalK(result.KOptimal)` çağrılır:
   1. `k` değeri [1, M] aralığına sıkıştırılır. `PixelEditService.Reset()` çağrılır, `rgbM = k` yapılır ve tek bölge kurulur.
   2. Seçilen taşlar `RemovalOrder` listesinin son k elemanıdır. Böylece farklı k değerleri iç içe alt kümeler verir.
   3. Her benzersiz kaynak rengi için Lab mesafesi hesaplanır ve en yakın taş seçilir (renk başına önbelleklenir). Gamut varsa önce Lab değeri `gamut.Map` ile eşlenir. Mesafe şu formülle bulunur: `(ΔL·LightnessWeight)² + Δa² + Δb²`.
   4. `dataM3` doldurulur ve `drl.dat[..,3] = ID` yazılır. Kullanılan taşlar `ID` sırasına dizilir, ardından `u`, `uc`, `ri/gi/bi` ve `dis` atanır.
   5. `dataM1 = dataM3` yapılır, `arMB` ve `arMA` ile yedek (`BackupM3`) güncellenir. `reducedBitmap` ve `exportBitmap` üretilir.
   6. `prepareTextures` true ise taş dokuları hazırlanır: `StoneTextureService.PopulateRandomIndices` her taşa rastgele bir varyant verir, `LoadTextures` paletteki taşların orijinal dokularını yükler. Dokular burada küçültülmez; küçültme ve çizim, ekranda ve dışa aktarmada gerekince [MosaicRenderSource](./MosaicRenderSource.md) içinde yapılır.

### Klasik M1/M3 yolu (`RunM3`)
1. Parametreler statik alanlara yazılır. Resim taş boyutuna **bir kez** küçültülür; bayt dizisi ve farklı renkleri ([`DistinctColors`](./NearestColorIndex.md)) çalışma boyunca saklanır. Önceden her M1/M3 turunda resim baştan küçültülüyordu; küçültme her seferinde aynı sonucu verdiği için bu gereksizdi. `QueryCoordinates` her farklı renk için mesafede kullanılacak koordinatları bir kez hesaplar: `(b, g, r)` ya da `boolLab` ise `RgbToLab(r, g, b)`. `CreateSingleRegion` tüm resmi kapsayan tek bir `drl` bölgesi kurar ve `drl.dat`'ı doldurur (WPF'ten kalan taş başına konum listesi artık kurulmaz; bkz. [Region](../Models/Region.md)). `InitM3` çağrılır.
2. **Katalog dışı başlangıç paleti** (`GenerateInitialPalette`): `0, RGBInc, 2·RGBInc, … < 255` adımlarıyla tüm R×G×B kombinasyonları oluşturulur. Bu renklerin katalogla ilgisi yoktur. `RGBInc` = 19 için 14³ = 2744 renk çıkar.
3. **M1** (`RunM1`, paralel): Her piksel aktif katalogdaki en yakın renge atanır ve `dataM1`'e yazılır. Mesafe iki seçeneğe bağlıdır. Uzay `boolLab` ise Lab, değilse RGB'dir. Ölçü `boolAv` ise ortalama mutlak fark, değilse kare toplamıdır. En yakın renk her **farklı** piksel rengi için bir kez, [`NearestColorIndex`](./NearestColorIndex.md) ile bulunur (`NearestPerColor`) ve o renkteki tüm piksellere dağıtılır. Seçim, her pikseli her katalog rengiyle karşılaştıran eski döngüyle aynıdır: en küçük mesafe, eşitlikte en küçük sıra. Katalog renklerinin `numOfPixel` sayaçları da bu adımda güncellenir (iş parçacığı başına sayılıp toplanır). M1 sonucu ayrıca bitmap'e çevrilmez; `reducedBitmap` en sonda `dataM3`'ten üretilir.
4. `CopyM1ToM3` çağrılır. M1 sonucu `dataM3` için başlangıç olur.
5. **M3 azaltma döngüsü** (bölge başına, `ar3.Count > dr.rgbM` olduğu sürece):
   1. `minRGBInc` palet boyutuna göre belirlenir: 2000 ve üzeri için 5, 1000'den büyükse 2, aksi halde 1.
   2. `RemoveMinimalColors` çağrılır. `numOfPixel < numOfMinRGB` olan renkler silinir. Silme sonrası hedeften az renk kalacaksa renkler piksel sayısına göre sıralanır ve ilk `rgbM` tanesi tutulur. `WpfStyleRemoval` açıkken bu alt sınır uygulanmaz, yalnızca paletin boşalması engellenir.
   3. `ProcessM3` çağrılır. Bölgedeki her piksel kalan ızgara paletinin en yakın rengine atanır (paralel). Mesafe ve arama M1'deki gibidir: palet için her turda yeni bir `NearestColorIndex` kurulur, en yakın renk farklı renk başına bir kez bulunur. Sayaçlar iş parçacığı başına sayılıp toplanır; `n` alanı yalnızca kullanılan girdilere yazılır (eskisi gibi). Ara sonuç bitmap'e çevrilmez.
   4. `numOfMinRGB += minRGBInc` yapılır ve ilerleme `rgbM / ar3.Count · 100` olarak bildirilir.
   5. İptal kontrol noktaları: her azaltma turunun başında, en yakın renk aramasında her 4096 renkte bir ve arama bitince, ayrıca `RunM1` ve `ProcessM3`'ün paralel piksel döngülerinde satır başına (`WorkCancellation.Check`).
   6. Hız: büyük mozaikte süreyi belirleyen bu aramaydı (varsayılan `RGBInc` = 10 ile palet 26³ = 17.576 renk). 6000×6000 px görselde, 28 iş parçacıklı bir bilgisayarda, tek başına çalıştırılarak ölçüldü (eski → yeni): 1.000 × 1.000 taş 6–15 sn → 1,3 sn; 20 m (1.667 × 1.667 ≈ 2,8 milyon taş) 37–46 sn → 1,8–2 sn; 2.500 × 2.500 taş (6,25 milyon) 58–60 sn → 2,5–3 sn. Sonuç bayt bayt aynıdır (RGB/Lab, ortalama/kare, `RGBInc` 5–32, 182 durumda karşılaştırıldı).
6. `AssignColorNumbers`: Kalan renklere 1'den başlayarak `u` numarası verilir ve bölge numarası atanır. `drl.dat[..,3]` alanına `u` yazılır (satırlar paralel).
7. `arMB = arMA` kopyası alınır.
8. **Katalog eşleme (Bölüm 2)**: Her ızgara rengi için `ColorMatcher.FindCatalogDistances` ve `SelectNearest` çağrılır. Bunlar RGB kare mesafesiyle en yakın katalog taşını bulur. Rengin `r,g,b`, `ID`, `codeName` ve `name` alanları bu taşınkiyle değiştirilir. `dataM3` pikselleri `(b,g,r,reg,u)` anahtarıyla güncellenir.
9. **Aynı renkleri birleştirme (Bölüm 3)**: RGB'si aynı olan girdilere ortak bir `uc` verilir. `drl.dat[..,3]` alanına `uc` yazılır, sonra `u = uc` yapılır.
10. **Katalog koduna göre toplama (Bölüm 4)**: Katalog sırasıyla gezilir. `arMB[0]` içinde aynı `codeName`'e sahip girdiler tek girdide toplanır ve `numOfPixel` değerleri eklenir. Sıfır pikselli girdiler atılır.
11. **ID yazımı (Bölüm 5)**: `drl.dat[..,3]` alanına katalog `ID` yazılır ve `u` değerleri 1'den yeniden numaralanır.

    Bölüm 2, 3 ve 5'teki piksel geçişleri satır satır paraleldir: her taş yalnızca kendi değerlerini okuyup yazar ve sözlükler bu sırada yalnızca okunur, bu yüzden sonuç sıralı çalışmayla aynıdır. Bölümlerin arasında, tek bölge ve farklı renkler kurulduktan sonra ve yedeklemeden önce de `WorkCancellation.Check()` vardır; döngü sonrası adımlarda da İptal en geç birkaç yüz milisaniyede durdurur.
12. `arMA` yedeklenir ve `BackupM3` çağrılır. Bitmap'ler üretilir, dokular hazırlanır (`PopulateRandomIndices` + `LoadTextures`) ve ilerleme 100 olarak bildirilir.

### Stoğa göre düzeltme (`FixToStock`, private)
1. `LastStockResult = null` yapılır.
2. Her pikselin hangi havuz taşında olduğu bulunur: `drl.dat[..,3]` değeri havuzdaki bir taşın `ID`'si ise ve o taşın rengi `dataM3`'teki piksel rengiyle aynıysa bu taş alınır (Optimum'dan sonra her zaman böyledir). Değilse piksel rengiyle eşleşen havuz taşı aranır (klasik Mos `drl.dat`'ta ara numaralar bırakabilir).
3. Renk eşleşmesi yoksa ya da havuzda aynı RGB'li iki taş varsa (renkten ayırt edilemez) işlem tahmin yürütmeden `false` döndürür: yanlış taş ID'si stoğu yanlış taşlar arasında taşırdı.
4. `StockAwareAssigner.SolveWithMinimum` çağrılır ve sonuç `LastStockResult`'a yazılır.
5. Sonuç değiştiyse `PixelEditService.Reset()` ve `RebuildFromAssignment` çağrılır: `dataM3`, `drl.dat[..,3] = ID`, `ID` sırasına dizilmiş palet (`u`, `uc`, `ri/gi/bi`, `dis`), `rgbM`, `dataM1`, `arMB`/`arMA`, `BackupM3` ve `reducedBitmap` `ApplyOptimalK`'daki gibi yeniden kurulur. `exportBitmap` yeni bir kopyayla değiştirilir; eskisi ekranda olabileceği için burada (worker thread'de) dispose edilmez.
6. Dokular: `prepareTextures` true ise ve (`texturesAlways` ya da sonuç değiştiyse) `StoneTextureService` ile yeniden hazırlanır (`PopulateRandomIndices` + `LoadTextures`).

### Tam kalıba tamamlama (dolgu)
Robot (WPF) yalnızca 26 × 26 taşlık (31,2 cm) tam kalıpları üretebilir. **Kalıp Dolgu** açıkken (`MainViewModel.UsePadding`) MainViewModel her Mos'tan (klasik ya da Optimum), Optimum taş sayısı değişiminden ve stok düzeltmesinden sonra son adım olarak mozaiği sağa ve alta, bir üst tam kalıba kadar tek bir dolgu taşıyla doldurur. Mos algoritmaları dolguyu hiç görmez: dolgu sonuca en son eklenir. Görsel sol üstte kaldığı için görsel kısmındaki tüm taş koordinatları değişmez. Mos'tan önce MainViewModel görseli aynı taşın rengiyle dolgulu gösterir (yalnızca ekranda; `inputBitmap` değişmez, bu yüzden Mos sonuçları aynı kalır).

**`ImageColours(bmp)`:** Görsel en çok 128 × 128 noktalık bir ızgarada örneklenir (adım `max(1, boyut/128)`, hücre ortası); kanal başına 16 düzeye yuvarlanınca aynı çıkan renkler bir kez sayılır. Her rengin Lab değeri döner.

**`ChooseFiller(colours, excludeUsed)`:**
1. `excludeUsed` ise kullanılan taşlar elenir: `arMA[0]` içinde `numOfPixel > 0` olan ID'ler (Mos'tan sonra; önizlemede false).
2. Adaylar: `arRGBAll` içindeki, `ID > 0` ve `codeName`'i boş olmayan taşlar; seçili olup olmamaları (`boolLeaveOut`) ve stokları önemsizdir.
3. Her aday için `colours`'a en küçük Lab ΔE76 uzaklığı hesaplanır; bu değeri en büyük olan aday seçilir (eşitlikte katalogda önce gelen). Böylece dolgu görseldeki hiçbir renge benzemez ve gerçek bir taşın rengidir.

**`PadToMoulds(filler)`:** `R × C` → `UpToMould(R) × UpToMould(C)`:
- `dataM3` büyütülür; yeni hücreler dolgu taşının BGR rengidir. `dataM1` ve `dataM3Backup` yalnızca `R × C` boyutundaysa aynı şekilde büyütülür (`dataM3F` 3×3×3 yer tutucusu dokunulmaz).
- `drl.dat` `[R2, C2, 4]` olur; yeni hücreler `1, 1, 1, filler.ID`.
- `arn`: görsel kısmındaki doku varyantları korunur (uzunluk `R·C` ise), dolgu hücreleri `PopulateRandomIndices` gibi rastgele 1–15.
- `drl.arar` bölgelerinin `x2 == C` / `y2 == R` olan kenarları `C2` / `R2` yapılır.
- `arMA[0]`, `arMB[0]` ve `arMBR[0]`'a (varsa) dolgu taşının bir kopyası eklenir: `numOfPixel` = dolgu taşı sayısı, `reg = 1`, `boolLeaveOut = false`, `stokYetersiz = false`, `u = uc =` listedeki eleman sayısı + 1, `ri/gi/bi = r/g/b`, `dis = (r+g+b)/3`.
- `UnpaddedRows/Cols`, `FillerId/Code` yazılır; `reducedBitmap` dolgulu `dataM3`'ten yeniden üretilir (`Swap`), `exportBitmap` yeni kopyayla değiştirilir (eskisi ekranda olabileceği için dispose edilmez, `RebuildFromAssignment`'taki gibi).

**`RemovePadding()`:** Stok düzeltmesi mozaiği Mos'un ürettiği boyutta görmelidir (`FixCurrentMosaicToStock` kaynakla `dataM3` boyutunu karşılaştırır). Bu yüzden Stok Kontrol'deki düzeltmeden önce dolgu kaldırılır: `dataM3`, `dataM1`, `dataM3Backup`, `drl.dat` ve `arn` sol üstteki `UnpaddedRows × UnpaddedCols` alana kırpılır, bölge kenarları geri alınır, `FillerId`'li palet girdisinin `numOfPixel`'inden dolgu taşı sayısı düşülür (0 ya da altına inerse girdi silinir; piksel düzenlemeyle görselin içine konmuş dolgu taşları sayıda kalır), dolgu bilgisi silinir ve `reducedBitmap` yeniden üretilir. Düzeltmeden sonra MainViewModel dolguyu yeniden ekler (dolgu taşı yeniden seçilir).

**Doğrulama:** Mos sonuçları dolgu eklenmeden önceki kodla aynıdır (182 durumluk karşılaştırma). Dolgu 6 görselde (kare, dikey, yatay; zaten tam kalıp olan boyutlar dahil) denendi: boyutlar 26'nın katı, görsel kısmı birebir aynı, dolgu hücrelerinin hepsi dolgu taşı, sayılar toplam taş sayısını veriyor, kaydet/aç aynı, `RemovePadding` mozaiği tam eski haline getiriyor.

## Önemli davranışlar ve iş kuralları
- Stok düzeltmesi Optimum ve klasik Mos algoritmalarını değiştirmez; yalnızca sonuca uygulanır. Hiçbir taş stoğu aşmıyorsa mozaik birebir aynı kalır.
- En az kullanım kuralı uygulamada kapalıdır: MainViewModel `new StockAwareOptions()` (`MinUsage = 0`) gönderir.
- Taş 12 mm, kalıp 26×26 taştır (`MouldStones`). Bu değerler koda sabit olarak gömülüdür.
- `width`/`height` her zaman görselin taş sayısıdır; dolgu yalnızca dizileri büyütür. Dolgulu mozaiğin boyutunu `dataM3.GetLength(..)` verir (MainViewModel'in tıklama ve ekran boyutu hesapları ve `ProjectService.CreateSnapshot` bunu kullanır).
- `TargetColors` değerini `RunM3`'ü çağıran taraf belirler. MainViewModel bu değeri toplam taş sayısının yaklaşık %10'u olarak hesaplar, 10'un katına aşağı yuvarlar, en az 2 yapar ve palet boyutunun altında tutar.
- Optimum yolda çıktı her zaman katalog taşlarından oluşur. M3 yolunda ara renkler katalog dışıdır ve ancak Bölüm 2'de katalog taşlarına çevrilir.
- `ApplyOptimalK` yeniden analiz yapmaz. `_optSrc` ve `_optCandidates` önbelleğini kullandığı için slider hızlı tepki verir. `Reset()` bu önbelleği temizler.
- `RunM3` `LastOptimalResult`'ı sıfırlamaz. MainViewModel bunu `_lastRunOptimal` bayrağıyla ayırt eder.
- `rgb` kanal sırası dizilerde **BGR**'dir (`[..,0]=B, [..,2]=R`).
- **İptal** ([WorkCancellation](./WorkCancellation.md)): Optimum'da yalnız analiz (`OptimalPaletteService.Analyze`), klasikte `RunM3`'ün azaltma döngüsü ve paralel satır döngüleri, stoğa göre düzeltmede `StockAwareAssigner` iptal edilebilir. Kontrol noktaları sonucu değiştirmez (Optimum ve klasik için karşılaştırma aracıyla aynı çıktı doğrulandı). `ApplyOptimalK`, `RebuildFromAssignment` ve doku hazırlığında kontrol noktası yoktur; iptal bu adımlarda etkisizdir. Stoğa göre düzeltme `SolveWithMinimum` içinde iptal edilirse `FixToStock` paylaşılan veriye henüz yazmamıştır, ama `LastStockResult` null kalır; Optimum'da `ApplyOptimalKWithStock`'un ilk adımı (`ApplyOptimalK`) ise uygulanmış olur.

## Dikkat / bilinen sınırlamalar
- Bölüm 2'deki katalog eşleme `boolLab` seçeneğine bakmaz ve her zaman RGB kare mesafesi kullanır.
- Bölge desteği altyapıda var (`drl.arar`, `reg`), ancak her zaman tek bölge kurulur. Bölüm 4 yalnızca `arMB[0]`'ı işler.
- `GenerateInitialPalette` döngü sınırı `< 255`'tir, MainViewModel ise palet boyutunu `RgbIncrement` için ceil(256/adım)³ olarak hesaplar. Bazı adımlarda bu iki değer farklı çıkar.
- `RunM3` döngüsünde her iterasyonda yeni bir `reducedBitmap` oluşturulur. Bütün atamalar `Swap(old, replacement)` üzerinden yapılır; önceki bitmap hemen `Dispose` edilir, böylece döngü yerel bellek biriktirmez. `exportBitmap` ise ekranda okunabildiği için motor tarafından değil, iş bittikten sonra UI iş parçacığında `MainViewModel.DisposeIfReplaced` ile serbest bırakılır.
- Katalogda RGB'si aynı ama `codeName`'i farklı iki taş varsa Bölüm 3–5'teki `(b,g,r,reg,u)` anahtarları çakışır.
- `ProcessM1` private'tır ve hiçbir yerden çağrılmaz (ölü kod). `penW`, `excessiveW`, `excessiveH` ve `LastGamut` da hiçbir yerde okunmaz.
- Statik global durum nedeniyle aynı anda iki mozaik işlenemez.
- Dolgu bilgisi (`IsPadded` vb.) yalnızca bu oturumda dolgu eklenmiş mozaik için tutulur; açılan bir projede dolgu sıradan mozaik taşlarıdır ve `RemovePadding` hiçbir şey yapmaz.
- `FixCurrentMosaicToStock`, mozaik bu oturumda üretilmediyse (ör. açılan projede `LastRunPool` null) çalışmaz. Piksel düzenlemeyle (`PixelEditService`) havuz dışı bir taş konmuşsa renk eşleşmesi bulunamaz ve `false` döner.
- `ApplyOptimalKWithStock`, `ApplyOptimalK`'nın Optimum önbelleğine (`_optSrc`, `_optCandidates`, `_optGamut`) dayanır; önce `RunOptimal` çalışmış olmalıdır.
- Aktif katalog boşken `ApplyOptimalK` `Math.Clamp(k, 1, 0)` nedeniyle hata verir. MainViewModel boş katalogda işlemi önceden durdurur.

## İlgili dosyalar
- [OptimalPaletteService](./OptimalPaletteService.md), [StockAwareAssigner](./StockAwareAssigner.md), [ColorMatcher](./ColorMatcher.md), [GamutMapper](./GamutMapper.md), [ColorCatalogService](./ColorCatalogService.md)
- [ImageService](./ImageService.md), [StoneTextureService](./StoneTextureService.md), [PixelEditService](./PixelEditService.md), [ProjectService](./ProjectService.md), [MosaicMetrics](./MosaicMetrics.md)
- [Rgb](../Models/Rgb.md), [Region](../Models/Region.md), [MosaicData](../Models/MosaicData.md)
- [MainViewModel](../ViewModels/MainViewModel.md), [CompareRunner](../CompareRunner.md), [StockCompareRunner](../StockCompareRunner.md), [Arayüz kılavuzu](../../ARAYUZ.md)
