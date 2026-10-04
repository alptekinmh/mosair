# ProjectService

> Kaynak: `mosair/Services/ProjectService.cs` · Güncelleme: 2026-10-04

## Amaç

Mozaik projesini `.mos` dosyasına kaydeder ve geri açar. `.mos`, `System.Text.Json` ile yazılan bir JSON belgesidir ve WPF uygulamasıyla ortak formattır: mosair WPF'in kaydettiği projeleri açabilir, WPF'e özgü alanları kaybetmeden geri yazabilir.

Dosya ayrıca serileştirme için kullanılan DTO sınıflarını (`ProjectData`, `RgbData`, `EditedPixelData`, `RegionData`) içerir.

## Nerede kullanılır

| Çağıran | Kullanım |
|---|---|
| `MainViewModel.SaveProject` | `ProjectService.Save(...)` |
| `MainViewModel.OpenProject` | `ProjectService.Open(...)`; `null` dönerse `StatusOpenFailed` / `AlertProjectOpenFailed` gösterilir |
| `MainViewModel.LoadImage` ve mozaikleştirme başlangıcı | `ProjectService.ForgetWpfState()` |
| `MainViewModel` (`StockProjectName`), `MainWindow.axaml.cs` | `CurrentPictureFileName` / `CurrentFileName` okunur (stok sütun adı, dışa aktarma dosya adı) |

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
| `Width`, `Height` | `double` | 0 | `MosaicEngine.width/height` |
| `RgbM` | `int` | 0 | `MosaicEngine.rgbM` |
| `N` | `int` | 20 | Taş başına piksel (`MosaicData.N`) |
| `ShowGrid`, `ShowMouldLines` | `bool` | `false` | Görünüm ayarları |
| `GridColorR/G/B` | `byte` | 0 | Izgara rengi |
| `InterpolationMethod` | `int` | 0 | `InterpolationMethod` enum değeri |
| `ZoomLevel` | `double` | 1 | Yakınlaştırma |
| `WidthCm` | `double` | 0 | Mozaik genişliği (cm) |
| `PictureFileName` | `string?` | `null` | Kaynak görselin yalnızca dosya adı (proje klasörüne göre) |
| `Arn` | `int[]?` | `null` | Taş başına doku varyant indeksi (`MosaicData.arn`) |
| `Source` | `string?` | `null` | mosair her zaman `"mosair"` yazar; yön düzeltmesi bu işarete bakar |
| `WpfExtra` | `Dictionary<string, JsonElement>?` | `null` | `[JsonExtensionData]`: tanınmayan (WPF'e özgü) tüm alanlar |

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

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `Save(filePath, widthCm, zoomLevel, showGrid, showMouldLines, gcR, gcG, gcB, interpMethod)` | `MosaicData`, `MosaicEngine`, `drl` ve `PixelEditService.EditedPixels` durumunu `ProjectData`'ya kopyalar, JSON olarak yazar, `CurrentFileName`'i günceller | `MainViewModel.SaveProject` |
| `Open(filePath)` → `ProjectData?` | JSON'u okur, tüm global durumu geri yükler, gerekirse yatay aynalar; dosya yoksa veya JSON değilse `null` | `MainViewModel.OpenProject` |
| `ForgetWpfState()` | Saklanan WPF alanlarını (`_wpfExtra`) atar | `MainViewModel.LoadImage`, mozaikleştirme başlangıcı |
| `RgbData.FromRgb(rgb)` / `RgbData.ToRgb()` | Model ↔ DTO çevirisi | `ProjectService` |

## Önemli davranışlar ve iş kuralları

- **Yön (ayna) kuralı.** mosair dizileri aynalamadan yazar ve `Source = "mosair"` koyar. `Open` sırasında `Source != "mosair"` ise (WPF dosyası) şunlar yatay aynalanır:
  - `dataM1`, `dataM3`, `dataM3F`, `dataM3Backup` (`FlipHorizontalInPlace`) ve `drl.dat` (`FlipHorizontalIntInPlace`);
  - düzenlenen piksellerin `X` değeri: `X = cols - 1 - X`;
  - bölgeler (`[x1, x2)` aralığı): `(x1, x2) = (cols - x2, cols - x1)`;
  - taş doku varyantları `MosaicData.arn` (piksel başına, satır satır): her satır ters çevrilir (`FlipRowsInPlace`).
  
  mosair'in kaydettiği bir dosya bir sonraki açılışta tekrar aynalanmaz.
- **WPF alanlarının korunması.** `ProjectData`'da karşılığı olmayan alanlar (robot üretim konumu colorID/mouldID/cn, görüş dikdörtgeni, numOfMinRGB vb.) `[JsonExtensionData] WpfExtra` ile okunur, `_wpfExtra`'da tutulur ve `Save` sırasında aynen geri yazılır. Yeni görsel yüklenince veya yeni Mos yapılınca `ForgetWpfState()` bunları atar, çünkü artık anlamsızdırlar.
- **Eski binary .mos.** `JsonSerializer.Deserialize` `JsonException` atarsa `Open` çökmez, `null` döner; çağıran "açılamadı" uyarısını gösterir.
- **Palet enjeksiyonu (yalnızca dosyada).** Düzenlenen bir pikselin `Source` rengi `ArMA[0]`'da yoksa, `MosaicData.arRGBAll`'dan bulunur ve `ArMA[0]`'a (varsa `ArMB[0]`'a da) eklenir: `BoolLeaveOut = false`, `NumOfPixel` en az 1, `MarkAsPaletteEntry` ile `U = sıra no`, `Reg = 1`, `Ri/Gi/Bi = R/G/B`. Böylece WPF bu taşı paletinde görür. Bellekteki `MosaicData.arMA` değişmez.
- **Görsel kopyalama.** `inputBitmap` varsa ve `CurrentPictureFileName` doluysa, görsel proje klasöründe yoksa oraya kopyalanır; `PictureFileName` yalnızca dosya adını tutar. `Open` bu adı proje klasörüne göre çözer; alan yoksa `CurrentPictureFileName = ""` yapılır (stok sütunu bu ada göre seçildiği için önceki görselin adı taşınmaz).
- **Düzleştirme.** 3B diziler `Buffer.BlockCopy` ile tek boyutlu diziye çevrilir; `byte[]` JSON'da base64 olarak yazılır. `Unflatten3D` veri yoksa `byte[3,3,3]`, `UnflattenInt3D` ise `int[1,1,4]` döner (Reset varsayılanlarıyla aynı).
- `Open`, `MosaicData.arcs`'ı `arRGBAll[i].boolLeaveOut` değerlerinden yeniden kurar. `PixelEditService.Reset()` ile önceki projenin düzenlemelerini, geri al/yinele geçmişini ve düzenleme modunu sıfırlar, sonra `PixelEditService.EditedPixels`'i dosyadan doldurur.
- **Doku varyantları.** `Arn` dosyada varsa ve uzunluğu `satır × sütun` ise kullanılır. Yoksa ya da boyut uymuyorsa `StoneTextureService.PopulateRandomIndices` ile Mos sonrasındaki gibi yeni rastgele varyantlar seçilir; önceki projeden kalan dizi asla kullanılmaz.

## Dikkat / bilinen sınırlamalar

- `PixelEditRecord` kopyaları yalnızca `r/g/b/ID/codeName` taşıdığından `EditedPixels` içindeki `RgbData`'ların `Name` vb. alanları boştur.
- `MainViewModel.SaveProject`, `showMouldLines` için her zaman `false` gönderir.
- Görsel dosyası diskte bulunamasa da `PictureFileName` yazılır.

## İlgili dosyalar

- [PixelEditService](./PixelEditService.md)
- [MosaicEngine](./MosaicEngine.md)
- [ImageService](./ImageService.md)
- [StockSheetService](./StockSheetService.md)
- [MosaicData](../Models/MosaicData.md), [Rgb](../Models/Rgb.md), [Region](../Models/Region.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
- [MainWindow](../MainWindow.md)
