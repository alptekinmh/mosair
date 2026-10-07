# ProjectService

> Kaynak: `mosair/Services/ProjectService.cs` · Güncelleme: 2026-10-07

## Amaç

Mozaik projesini `.mos` dosyasına kaydeder ve geri açar. `.mos`, `System.Text.Json` ile yazılan bir JSON belgesidir ve WPF uygulamasıyla ortak formattır: mosair WPF'in kaydettiği projeleri açabilir, WPF'e özgü alanları kaybetmeden geri yazabilir.

Kaydetme ve açma ikişer adımdır; ağır kısım (JSON'a çevirme, diske yazma, dosyayı okuyup çözme, dizileri kurma) arka planda çalışabilir, pencere donmaz:

- **Kaydet:** `CreateSnapshot` (UI iş parçacığı; yalnızca dizi ve liste kopyaları, milisaniyeler) → `WriteSnapshot` (arka plan; JSON + dosya).
- **Aç:** `ReadProject` (arka plan; okuma, çözme, dizileri kurma, WPF aynalama) → `ApplyProject` (UI iş parçacığı; hazır veriyi global duruma atama, dosya erişimi yok).

`Save` ve `Open` bu iki adımı arka arkaya çağıran engelleyici sarmalayıcılar olarak kalır (araçlar ve testler için).

Dosya ayrıca serileştirme için kullanılan DTO sınıflarını (`ProjectData`, `RgbData`, `EditedPixelData`, `RegionData`) ve iki ara nesneyi (`ProjectSnapshot`, `LoadedProject`) içerir.

## Nerede kullanılır

| Çağıran | Kullanım |
|---|---|
| `MainViewModel.SaveProjectAsync` | `CreateSnapshot` (`CreateProjectSnapshot` üzerinden) UI iş parçacığında, `WriteSnapshot` `Task.Run` içinde; başarılıysa `CurrentFileName` çağıran tarafından güncellenir |
| `MainViewModel.SaveToDriveAsync` | Aynı ikili: anlık kopya + geçici klasöre `WriteSnapshot` (`CurrentFileName` değişmez) |
| `MainViewModel.OpenProjectAsync` | `ReadProject` `Task.Run` içinde (görsel yükleme ve taş renkli bitmap ile birlikte); `null` ya da hata ise `StatusOpenFailed` / `AlertProjectOpenFailed`; başarılıysa `ApplyProject` UI iş parçacığında |
| `MainViewModel.LoadImage` ve mozaikleştirme başlangıcı (`RunMosaicAsync`) | `ProjectService.ForgetWpfState()`; `LoadImage` ayrıca `CurrentPictureFileName = path`, `CurrentFileName = ""` yapar |
| `MainViewModel` (`StockProjectName`, `SaveToDriveAsync`), `MainWindow.axaml.cs` | `CurrentPictureFileName` / `CurrentFileName` okunur (stok sütun adı, Drive proje adı, dışa aktarma ve hızlı kayıt dosya adı) |

## Yapı

### `ProjectData` (JSON kök nesnesi)

JSON alan adları C# özellik adlarıyla birebir aynıdır (isimlendirme politikası yok). `null` değerler yazılmaz (`JsonIgnoreCondition.WhenWritingNull`), çıktı girintisizdir.

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `DataM1Flat` / `DataM1Dims` | `byte[]?` / `int[]` | `null` / boş | `MosaicData.dataM1` düzleştirilmiş hali ve boyutları `[satır, sütun, kanal]` |
| `DataM3Flat` / `DataM3Dims` | `byte[]?` / `int[]` | `null` / boş | `MosaicData.dataM3` (taş başına BGR mozaik) |
| `DataM3FFLat` / `DataM3FDims` | `byte[]?` / `int[]` | `null` / boş | `MosaicData.dataM3F` (ad bilerek `FFLat`; WPF ile uyumlu) |
| `DataM3BackupFlat` / `DataM3BackupDims` | `byte[]?` / `int[]` | `null` / boş | `MosaicData.dataM3Backup` |
| `DrlDatFlat` / `DrlDatDims` | `int[]?` / `int[]` | `null` / boş | `drl.dat` (kanal 3 = taş ID'si) |
| `ArRGBAll` | `List<RgbData>` | boş | Tüm katalog (`MosaicData.arRGBAll`) |
| `ArRGB` | `List<RgbData>` | boş | Aktif renkler (`MosaicData.arRGB`) |
| `ArMA`, `ArMB`, `ArMBR` | `List<List<RgbData>>` | boş | Bölge paletleri (`MosaicData.arMA/arMB/arMBR`) |
| `EditedPixels` | `List<EditedPixelData>` | boş | Piksel düzenleme kayıtları |
| `Regions` | `List<RegionData>` | boş | `drl.arar` bölgeleri |
| `Width`, `Height` | `double` | 0 | `MosaicEngine.width/height`; mozaik bu oturumda tam kalıba tamamlandıysa (`MosaicEngine.IsPadded`) dolgulu boyut (`dataM3` sütun/satır sayısı). WPF mozaik genişliğini `Width`'ten okur. |
| `RgbM` | `int` | 0 | `MosaicEngine.rgbM` |
| `N` | `int` | 20 | Taş başına piksel (`MosaicData.N`). WPF uyumluluğu için okunur ve geri yazılır; mosair'de görünümü ya da dışa aktarmayı etkilemez (`MainViewModel.OpenProject` görünüm için kullanmaz). `MosaicData.N` uygulama açılışında 40'tır; bir proje açılınca onun değerini alır ve sonraki kayıtlarda (yeni Mos'lar dahil) o değer yazılır. |
| `ShowGrid`, `ShowMouldLines` | `bool` | `false` | Görünüm ayarları |
| `GridColorR/G/B` | `byte` | 0 | Izgara rengi |
| `InterpolationMethod` | `int` | 0 | `InterpolationMethod` enum değeri |
| `ImageAdjust` | `int[]?` | `null` | Görsel Ayarları: `[parlaklık, kontrast, doygunluk, gama]` (her biri −100…100). Hiçbir ayar yoksa yazılmaz (`WhenWritingNull`), böylece ayarsız projelerin dosyası değişmez. WPF bu alanı bilmez ve yok sayar. Açarken `ImageAdjustments.FromArray` ile okunur. |
| `ZoomLevel` | `double` | 1 | Yakınlaştırma |
| `WidthCm` | `double` | 0 | Mozaik genişliği (cm) |
| `PictureFileName` | `string?` | `null` | Kaynak görselin yalnızca dosya adı (proje klasörüne göre) |
| `Arn` | `int[]?` | `null` | Taş başına doku varyant indeksi (`MosaicData.arn`) |
| `Source` | `string?` | `null` | mosair her zaman `"mosair"` yazar; yön düzeltmesi bu işarete bakar |
| `WpfExtra` | `Dictionary<string, JsonElement>?` | `null` | `[JsonExtensionData]`: tanınmayan (WPF'e özgü) tüm alanlar |

### `ProjectSnapshot` (kaydetme ara nesnesi)

`CreateSnapshot`'ın döndürdüğü, `WriteSnapshot`'ın yazdığı paket. Canlı durumdan bağımsızdır: diziler `Flatten3D` ile kopyalanır, paletler ve düzenlemeler `RgbData` kopyalarıdır, `Arn` klonlanır. Bu yüzden yazma sürerken kullanıcı mozaiği değiştirse de dosyaya tıklandığı anki hali yazılır.

| Ad | Tip | Açıklama |
|---|---|---|
| `Data` (internal) | `ProjectData` | Yazılacak veri (`PictureFileName` henüz boş) |
| `PictureSource` (internal) | `string?` | Yüklü görselin yolu (`inputBitmap` varsa ve `CurrentPictureFileName` doluysa); yoksa `null` |

### `LoadedProject` (açma ara nesnesi)

`ReadProject`'in hazırladığı, `ApplyProject`'in global duruma koyduğu paket.

| Ad | Tip | Açıklama |
|---|---|---|
| `Data` | `ProjectData` | Çözülen JSON |
| `FilePath` | `string` | Açılan `.mos` yolu |
| `PicturePath` | `string` | Proje klasörüne göre çözülmüş görsel yolu; dosyada ad yoksa `""` (dosya diskte olmayabilir) |
| `DataM3` | `byte[,,]` | Hazır `dataM3` (çağıran arka planda taş renkli bitmap'i bundan üretir) |
| `_m1`, `_m3`, `_m3f`, `_m3b`, `_dat` (internal) | diziler | Kurulmuş (WPF dosyasında aynalanmış) `dataM1/M3/M3F/M3Backup` ve `drl.dat` |
| `_all`, `_rgb`, `_ma`, `_mb`, `_mbr` (internal) | `rgb` listeleri | `arRGBAll`, `arRGB`, `arMA/arMB/arMBR` |
| `_edits`, `_regions` (internal) | listeler | Düzenlenen pikseller ve `drl` bölgeleri (aynalanmış) |
| `_arn` (internal) | `int[]?` | Uzunluğu uyan doku varyantları; `null` ise uygulanırken yeni rastgele varyantlar seçilir |

### `RgbData`

`rgb` modelinin JSON karşılığı; `FromRgb(rgb)` / `ToRgb()` ile çevrilir.

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `R`, `G`, `B` | `double` | 0 | Renk |
| `ID` | `int` | 0 | Taş ID'si |
| `CodeName`, `Name` | `string` | `""` | Katalog kodu ve adı |
| `BoolLeaveOut` | `bool` | `false` | Paletten hariç mi |
| `NumOfPixel` | `int` | 0 | Kullanım sayısı |
| `Dis` | `double` | 0 | Uzaklık değeri |
| `Uc` | `int` | 1 | |
| `U` | `int` | 0 | WPF orta palet sütununda gösterilen palet numarası |
| `Reg` | `int` | 0 | Bölge numarası |
| `Ri`, `Gi`, `Bi` | `double` | 0 | WPF orta sütununda `U`'nun arka plan rengi (katalog öncesi renk) |

### `EditedPixelData`

| Ad | Tip | Açıklama |
|---|---|---|
| `Y`, `X` | `int` | Taş koordinatı |
| `Source` | `RgbData` | Yerleştirilen renk |
| `Target` | `RgbData` | Tıklanan hücrenin o anki rengi |
| `Original` | `RgbData` | Hücrenin ilk (düzenleme öncesi) rengi |

### `RegionData`

| Ad | Tip | Açıklama |
|---|---|---|
| `X1`, `Y1`, `X2`, `Y2` | `int` | Bölge sınırları; sütun aralığı `[X1, X2)` |
| `RgbM` | `int` | Bölgenin renk sayısı |

### `ProjectService` (static)

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `CurrentFileName` | `string` | `""` | Son kaydedilen/açılan `.mos` yolu |
| `CurrentPictureFileName` | `string` | `""` | Kaynak görselin tam yolu |
| `_wpfExtra` | `Dictionary<string, JsonElement>?` | `null` | Açılan dosyadan alınan WPF alanları (özel) |
| `JsonOpts` | `JsonSerializerOptions` | | `WriteIndented = false`, `WhenWritingNull` (özel) |

Özel yardımcılar: `MarkAsPaletteEntry` (palet enjeksiyonu), `Flatten3D` / `Unflatten3D`, `FlattenInt3D` / `UnflattenInt3D`, `FlipHorizontalInPlace`, `FlipHorizontalIntInPlace`, `FlipRowsInPlace`.

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `CreateSnapshot(widthCm, zoomLevel, showGrid, showMouldLines, gcR, gcG, gcB, interpMethod, imageAdjust = null)` → `ProjectSnapshot` | `MosaicData`, `MosaicEngine`, `drl` ve `PixelEditService.EditedPixels` durumunu `ProjectData`'ya kopyalar (palet enjeksiyonu dahil); `imageAdjust` (`MainViewModel.ImageAdjustArray`) `ImageAdjust` alanına yazılır. Hızlıdır; UI iş parçacığında çağrılır. Dosyaya dokunmaz | `MainViewModel.CreateProjectSnapshot` (`SaveProjectAsync`, `SaveToDriveAsync`), `Save` |
| `WriteSnapshot(snapshot, filePath)` | Klasörü oluşturur, görseli (yoksa) yanına kopyalar ve `PictureFileName`'i yazar, JSON'u doğrudan bir `FileStream` ile `<dosya>.part`'a yazar, sonra `File.Move(..., overwrite: true)` ile hedefin üzerine taşır. Hata olursa `.part` silinir ve hata çağırana yükselir. Yalnızca anlık kopyayı kullanır, arka planda çalışabilir. `CurrentFileName`'i değiştirmez | `MainViewModel.SaveProjectAsync` ve `SaveToDriveAsync` (`Task.Run` içinde), `Save` |
| `Save(filePath, widthCm, zoomLevel, showGrid, showMouldLines, gcR, gcG, gcB, interpMethod)` | `CreateSnapshot` + `WriteSnapshot`, ardından `CurrentFileName = filePath` (engelleyici) | Araçlar ve testler |
| `ReadProject(filePath)` → `LoadedProject?` | Dosyayı akış olarak okuyup çözer, dizileri ve listeleri kurar, WPF dosyasında aynalar, görsel yolunu çözer. Global duruma dokunmaz; arka planda çalışabilir. Dosya yoksa veya JSON değilse `null`; G/Ç hataları (erişim yok, kilitli) çağırana yükselir | `MainViewModel.OpenProjectAsync` (`Task.Run` içinde), `Open` |
| `ApplyProject(loaded)` | Hazır veriyi global duruma koyar: `_wpfExtra`, diziler, katalog ve paletler, `arcs`, `PixelEditService.Reset()` + düzenlemeler, bölgeler, `MosaicEngine.width/height/rgbM`, `MosaicEngine.ForgetPadding()` (açılan projenin dolgusu mozaiğin sıradan bir parçasıdır), `MosaicData.N`, `arn` (ya da yeni rastgele varyantlar), `CurrentFileName`, `CurrentPictureFileName`. Dosya erişimi yok, hızlıdır | `MainViewModel.OpenProjectAsync`, `Open` |
| `Open(filePath)` → `ProjectData?` | `ReadProject` + `ApplyProject` (engelleyici); okunamazsa `null` | Araçlar ve testler |
| `ForgetWpfState()` | Saklanan WPF alanlarını (`_wpfExtra`) atar | `MainViewModel.LoadImage`, mozaikleştirme başlangıcı |
| `RgbData.FromRgb(rgb)` / `RgbData.ToRgb()` | Model ↔ DTO çevirisi | `ProjectService` |

## Önemli davranışlar ve iş kuralları

- **Yön (ayna) kuralı.** mosair dizileri aynalamadan yazar ve `Source = "mosair"` koyar. `ReadProject` sırasında `Source != "mosair"` ise (WPF dosyası) şunlar yatay aynalanır:
  - `dataM1`, `dataM3`, `dataM3F`, `dataM3Backup` (`FlipHorizontalInPlace`) ve `drl.dat` (`FlipHorizontalIntInPlace`);
  - düzenlenen piksellerin `X` değeri: `X = cols - 1 - X`;
  - bölgeler (`[x1, x2)` aralığı): `(x1, x2) = (cols - x2, cols - x1)`;
  - dosyadaki taş doku varyantları (`Arn`, piksel başına, satır satır): her satır ters çevrilir (`FlipRowsInPlace`). Dosyada varyant yoksa seçilen yeni rastgele varyantlar aynalanmaz (zaten rastgeledir; eski kod onları da aynalıyordu, sonuç açısından fark yoktur).
  
  mosair'in kaydettiği bir dosya bir sonraki açılışta tekrar aynalanmaz.
- **WPF alanlarının korunması.** `ProjectData`'da karşılığı olmayan alanlar (robot üretim konumu colorID/mouldID/cn, görüş dikdörtgeni, numOfMinRGB vb.) `[JsonExtensionData] WpfExtra` ile okunur, `_wpfExtra`'da tutulur ve `Save` sırasında aynen geri yazılır. Yeni görsel yüklenince veya yeni Mos yapılınca `ForgetWpfState()` bunları atar, çünkü artık anlamsızdırlar. `_wpfExtra` anlık kopyaya referans olarak girer; değiştirilmez, yalnızca yenisiyle değiştirilir, bu yüzden arka planda yazılırken güvenlidir.
- **Eski binary .mos.** `JsonSerializer.Deserialize` `JsonException` atarsa `ReadProject` çökmez, `null` döner; çağıran "açılamadı" uyarısını gösterir.
- **Güvenli yazma.** Dosya önce `<ad>.mos.part` olarak yazılır, sonra hedefin üzerine taşınır. Disk dolması, erişim hatası ya da uygulamanın yazma sırasında kapanması var olan projeyi bozmaz; yarım kalan iş yalnızca bir `.part` dosyası bırakabilir (hata yolunda silinir).
- **Doğrulama (2026-10-07).** Eski (tek adımlı) ve yeni kod 9 projeyle (5 gerçek proje, WPF kaynaklı aynalanan, düzenlenmiş pikselli, görseli eksik, varyantı kayıtsız) ve 2,8 milyon taşlık bir projeyle karşılaştırıldı: kaydedilen dosyalar bayt bayt aynı, açılan durum alan alan aynı (yalnızca varyantı kayıtsız dosyanın rastgele varyantları farklı; bu her açılışta zaten farklıdır). Süreler: 2,8 milyon taş (62 MB) / 6,25 milyon taş (140 MB) için kaydetme pencerede 23 / 45 ms + arka planda 0,23 / 0,35 sn; açma arka planda 1,34 / 1,72 sn + pencerede yaklaşık 1 ms. Eski kod bu sürelerin tamamında pencereyi donduruyordu.
- **Palet enjeksiyonu (yalnızca dosyada).** Düzenlenen bir pikselin `Source` rengi `ArMA[0]`'da yoksa, `MosaicData.arRGBAll`'dan bulunur ve `ArMA[0]`'a (varsa `ArMB[0]`'a da) eklenir: `BoolLeaveOut = false`, `NumOfPixel` en az 1, `MarkAsPaletteEntry` ile `U = sıra no`, `Reg = 1`, `Ri/Gi/Bi = R/G/B`. Böylece WPF bu taşı paletinde görür. Bellekteki `MosaicData.arMA` değişmez.
- **Görsel kopyalama.** `inputBitmap` varsa ve `CurrentPictureFileName` doluysa (anlık kopyada `PictureSource`), `WriteSnapshot` görseli proje klasöründe yoksa oraya kopyalar; `PictureFileName` yalnızca dosya adını tutar. `ReadProject` bu adı proje klasörüne göre çözer, `ApplyProject` `CurrentPictureFileName`'e yazar; alan yoksa `CurrentPictureFileName = ""` yapılır (stok sütunu bu ada göre seçildiği için önceki görselin adı taşınmaz).
- **Düzleştirme.** 3B diziler `Buffer.BlockCopy` ile tek boyutlu diziye çevrilir; `byte[]` JSON'da base64 olarak yazılır. `Unflatten3D` veri yoksa `byte[3,3,3]`, `UnflattenInt3D` ise `int[1,1,4]` döner (Reset varsayılanlarıyla aynı).
- `ApplyProject`, `MosaicData.arcs`'ı `arRGBAll[i].boolLeaveOut` değerlerinden yeniden kurar. `PixelEditService.Reset()` ile önceki projenin düzenlemelerini, geri al/yinele geçmişini ve düzenleme modunu sıfırlar, sonra `PixelEditService.EditedPixels`'i dosyadan doldurur.
- **Doku varyantları.** `Arn` dosyada varsa ve uzunluğu `satır × sütun` ise kullanılır. Yoksa ya da boyut uymuyorsa `StoneTextureService.PopulateRandomIndices` ile Mos sonrasındaki gibi yeni rastgele varyantlar seçilir; önceki projeden kalan dizi asla kullanılmaz.

## Dikkat / bilinen sınırlamalar

- `PixelEditRecord` kopyaları yalnızca `r/g/b/ID/codeName` taşıdığından `EditedPixels` içindeki `RgbData`'ların `Name` vb. alanları boştur.
- `MainViewModel.CreateProjectSnapshot`, `showMouldLines` için her zaman `false` gönderir.
- Kaydetme tıklandığı andaki mozaiği yazar; yazma sürerken yapılan düzenlemeler o kayda girmez (bir sonraki kayıtta girer).
- `ReadProject` çok büyük dosyada dizileri tek seferde bellekte kurar (JSON + diziler); 140 MB'lık projede bu birkaç yüz MB geçici bellek demektir.
- Görsel dosyası diskte bulunamasa da `PictureFileName` yazılır.

## İlgili dosyalar

- [PixelEditService](./PixelEditService.md)
- [MosaicEngine](./MosaicEngine.md)
- [ImageService](./ImageService.md)
- [StockSheetService](./StockSheetService.md)
- [MosaicData](../Models/MosaicData.md), [Rgb](../Models/Rgb.md), [Region](../Models/Region.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
- [MainWindow](../MainWindow.md)
