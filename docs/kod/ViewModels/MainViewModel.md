# MainViewModel
> Kaynak: `mosair/ViewModels/MainViewModel.cs` · Güncelleme: 2026-10-08

## Amaç
Ana pencerenin tek view model'idir: görsel yükleme, mozaikleme (klasik M3 ve Optimum), taş dokulu görüntünün (RS) çizim kaynağı ve genel görünümü (`RenderSource`, `OverviewBitmap`; çizimi `MosaicView` yapar), zoom/navigator, renk kataloğu seçimi, piksel düzenleme, taş varyantı seçimi, proje aç/kaydet, dışa aktarma ve Google Sheet stok işlemlerinin (stoğa göre mozaik, "Stoğa göre" dahil) UI tarafındaki durumunu tutar. Ağır işi servislere (`MosaicEngine`, `StoneTextureService`, `ProjectService`, `StockSheetService`, `PixelEditService`) devreder; kendisi durum, iş sırası ve kullanıcıya gösterilen metinlerden sorumludur. Dosyada ayrıca katalog/palet/atama listelerinin satır modelleri (`ColorItem`, `PaletteItem`, `AssignedItem`, `StoneThumbItem`) bulunur.

## Nerede kullanılır
| Dosya | Kullanım |
|---|---|
| `mosair/MainWindow.axaml` | `DataContext` olarak bağlanır; `CatalogColors`, `PaletteColors`, `AssignedColors`, `PropStoneThumbs`, durum çubuğu, Optimum kaydırıcısı, stok menüsü vb. binding'ler. `ColorItem` alanları katalog satırı ve tooltip'te (`StockShort`, `StockKgText`, `RemainingText`, `TooltipBitmap`, `ThumbnailBitmap`), `AssignedItem` alanları "Atanan" tablosunda kullanılır. |
| `mosair/MainWindow.axaml.cs` | `_vm` alanı üzerinden metotları çağırır (`LoadImage`, `RunMosaicAsync`, `OpenProjectAsync`, `SaveProjectAsync`, `CanSaveProject`, `ExportImageAsync`, `QuickExportExtension`, `ExportChoiceLabel`, `RefreshExportEstimatesAsync`, stok metotları, `UndoPixelEdit`/`RedoPixelEdit`, `SelectStone`, `FitToWindow`, `UpdateNavigator` ...) ve `ShowAlert`, `ShowConfirm`, `ShowStockSettings`, `ShowDriveSettings`, `ShowDriveOpen`, `OpenUrl` delegelerini atar; Google Drive için `SaveToDriveAsync`, `OpenFromDriveAsync`, `ConfigureDriveAsync` çağırır; `StoneInvalidated`, `ExportEstimatesChanged` ve `FileSaved` olaylarına abone olur. |

## Yapı

### `ColorItem` (`INotifyPropertyChanged`)
Katalog listesindeki bir taş rengi.

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `Index` | `int` | 0 | `MosaicData.arRGBAll` içindeki sıra. |
| `R`, `G`, `B` | `byte` | 0 | Taşın RGB değeri. |
| `CodeName`, `Name` | `string` | `""` | Taş kodu ve adı. |
| `ID` | `int` | 0 | Taş kimliği (stok sayfası bu ID ile eşleşir). |
| `IsExcluded` | `bool` | false | Mozaikten hariç mi; değişince `IsSelected` da bildirilir. |
| `IsSelected` | `bool` | — | `!IsExcluded`; checkbox'a bağlanır. |
| `StockShort` | `bool` | false | Stok kontrolünde tahmini kalan negatifse true (kırmızı nokta). |
| `StockKg` | `double?` | null | Eldeki stok (kg); "Stok çek" ve "Stok kontrol" doldurur. |
| `RemainingKg` | `double?` | null | Tahmini kalan (kg); yalnız "Stok kontrol" doldurur. |
| `StockKgVisible`, `RemainingVisible`, `StockArrowVisible` | `bool` | — | Tooltip stok satırı parçalarının görünürlüğü (ok yalnız iki değer de varsa). |
| `StockKgText`, `RemainingText` | `string` | — | `"0.##"` biçimli kg metni; tek değer varsa " kg" / `StockKgLeft` eki eklenir. |
| `StockKgBrush`, `RemainingBrush` | `IBrush` | — | Değer ≤ 0 ise kırmızı (`#E53935`), değilse yeşil (`#4ECB71`). |
| `TooltipHeader` | `string` | — | `"{CodeName}  {Name}  {R} {G} {B}"`. |
| `ColorBrush` | `IBrush` | — | Taş renginden düz fırça. |
| `ThumbnailBitmap` | `Bitmap?` | — | İlk erişimde `StoneTextureService.LoadSingleThumbnail(CodeName, 16, 14)` ile tembel yüklenir. |
| `TooltipBitmap` | `Bitmap?` | — | İlk erişimde `BuildTooltipBitmap` ile tembel üretilir: `LoadTooltipImages(CodeName, 60)` görsellerinden 4 sütunlu, 62 px hücreli ızgara. |

### `PaletteItem`
"Eşleşen" sütunundaki renk kutusu: `Index`, `R`, `G`, `B`, `ColorBrush`.

### `AssignedItem`
"Atanan" tablosunun satırı.

| Ad | Tip | Açıklama |
|---|---|---|
| `Num` | `int` | Katalogda seçili taşlar arasındaki sıra numarası. |
| `ID`, `CodeName` | `int`, `string` | Taş kimliği/kodu. |
| `PixelCount` | `int` | Bu taşla doldurulan piksel (taş) sayısı. |
| `R`, `G`, `B` | `byte` | Renk. |
| `RowBrush` | `IBrush` | Satır arka planı. |
| `TextBrush` | `IBrush` | Parlaklık (`0.299R+0.587G+0.114B`) > 140 ise siyah, değilse beyaz yazı. |


### `StoneThumbItem` (`INotifyPropertyChanged`)
Özellikler panelindeki taş varyantı küçük resmi: `Index` (0 tabanlı varyant), `DisplayIndex` (1 tabanlı), `Thumbnail` (44×44), `IsSelected` (değişince bildirilir).

### `MainViewModel` (`INotifyPropertyChanged`)

**UI delegeleri** (MainWindow atar)

| Ad | Tip | Açıklama |
|---|---|---|
| `ShowAlert` | `Func<string, string, Task>?` | Uyarı diyaloğu; `Alert` bunu UI thread'e post eder. |
| `ShowConfirm` | `Func<string, string, Task<bool>>?` | Onay diyaloğu; null ise onay "hayır" sayılır. |
| `ShowStockSettings` | `Func<StockSheetService.Config, Task<StockSheetService.Config?>>?` | Stok ayarları diyaloğu. |
| `ShowDriveSettings` | `Func<DriveService.Config, Task<DriveService.Config?>>?` | Google Drive ayarları diyaloğu ([DriveSettingsDialog](../Controls/DriveSettingsDialog.md)). |
| `ShowDriveOpen` | `Func<DriveService.Config, Task<DriveService.DriveFile?>>?` | Drive ayarlarıyla [DriveOpenDialog](../Controls/DriveOpenDialog.md) proje tarayıcısı (listeyi ve önizlemeleri kendisi yükler); seçilen dosya ya da `null`. |
| `OpenUrl` | `Func<string, Task>?` | Tarayıcıda URL açma. |

**Görsel & zoom**

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `DisplayBitmap` | `Bitmap?` | null | Yüklenen (ayarlanmış) görsel; tuvalde `ShowSourceView` iken görünür: Mos'tan önce ve Anlık Mos kapalıyken bir ayar değişikliğinden sonra (eskimiş mozaiğin yerine). Değişince `NavBitmap` da bildirilir. |
| `RenderSource` | `MosaicRenderSource?` | null | `MosaicView`'ın çizdiği mozaik anlık görüntüsü (`StoneTextureService.CreateRenderSource`); `RefreshMosaicView` kurar, `LoadImage` null yapar. |
| `OverviewBitmap` | `Bitmap?` | null | Taş başına 1 piksellik genel görünüm (`RenderOverview`): uzaktan görünüm, karo gelene kadar yer tutucu ve gezgin. |
| `NavBitmap` | `Bitmap?` | — | Gezgin görüntüsü: Mos'tan sonra (ve `OverviewBitmap` varsa) genel görünüm, değilse `DisplayBitmap`. Mozaik yerine görsel gösterilirken (`_showingRaw`) yine `DisplayBitmap`. |
| `StoneInvalidated` | `event Action<int, int>?` | — | Tek taş değişti (satır, sütun): piksel düzenleme, geri al/yinele, varyant seçimi. `MainWindow` bunu `MosaicView.InvalidateStone`'a iletir. |
| `SavedFileKind` | `enum` | — | Yazılan dosyanın türü: `Export` (mosairEXPORT / mosairEXPORT As), `Screenshot`, `Project`. |
| `FileSaved` | `event Action<string, SavedFileKind>?` | — | Bir dosya başarıyla yazıldı (tam yol, tür). `ExportImageAsync`, `SaveScreenshotAsync` ve `SaveProjectAsync` başarı durumunu yazdıktan hemen sonra tetikler; iptal ya da hatada tetiklenmez. Drive'a kayıt tetiklemez. `MainWindow` sağ altta kaydedilen dosya bildirimini gösterir. |
| `ZoomLevel` | `double` | 1 | `[MinZoomLevel, 20]` aralığına kırpılır; `ImageDisplayWidth/Height`, `SourceViewWidth/Height` ve `ZoomInfo`'yu bildirir. |
| `MinZoomLevel` | `double` | 1.0 | `FitToWindow` sığdırma zoom'una (en az 0.001) ayarlar. |
| `ImageDisplayWidth`, `ImageDisplayHeight` | `double` | — | Görüntünün piksel boyutu (`BitmapPixelWidth/Height`, yuvarlanmadan) × zoom. |
| `BitmapPixelWidth`, `BitmapPixelHeight` | `int` | 1 | Tuvaldeki görüntünün piksel boyutu: Mos'tan önce yüklenen görselin boyutu (dolgulu önizlemede `PadScaleX/Y` ile tam kalıba büyütülmüş), Mos'tan sonra taş dokulu görüntünün sanal boyutu `C·100 × R·100` (`FinishMosaic`, `OpenProjectAsync`). |
| `StoneColumns`, `StoneRows` | `int` | — | Mozaik varken (`MosaicDone`) mozaiğin kendi boyutu (`dataM3` sütun/satır sayısı: tam kalıba tamamlanmış ya da açılmış projenin boyutu), Mos'tan önce `MosaicEngine.width` / `MosaicEngine.height` (GridOverlay için). |
| `ZoomInfo` | `string` | — | `"Zoom={zoom}  {w}x{h}"`. Zoom 0,1'in altındaysa en fazla 3 ondalık (`0.###`), değilse 1 ondalık. |
| `DocumentTitle` | `string` | "" | Başlık çubuğunda gösterilen ad: `ProjectService.CurrentFileName` (kaydedilmiş/açılmış proje) varsa onun dosya adı, yoksa `CurrentPictureFileName` (yüklenen görsel). `LoadImage`, `OpenProjectAsync` (açılınca) ve başarılı `SaveProjectAsync` sonrası bildirilir. |
| `NavViewLeft`, `NavViewTop`, `NavViewWidth`, `NavViewHeight` | `double` | 0 | Navigator küçük resmindeki görünüm dikdörtgeni. |
| `ImageLoaded` | `bool` | false | Görsel yüklü mü; `CanRunMosaic`'i etkiler. Değişince `ShowImageInfo` ve `ShowNoImageHint` de bildirilir. |

**Mozaik ayarları**

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `WidthCm` | `double` | 93.6 | cm kutusu; 0.01'den küçük değişiklik yok sayılır. `UpdateDimensions` yazar: Kalıp Dolgu kapalıyken görselin genişliği (1.2 cm katı), açıkken dolgulu genişlik `UpToMould(taş)·1.2`. Görselin kendi genişliği `_imageWidthCm`'dedir (Mos, proje kaydı); `_shownWidthCm` kutuya son yazılan değerdir. |
| `HeightCm` | `double` | 0 | `MosaicEngine.CalculateDimensions` sonucundan (salt okunur). |
| `TargetColors` | `int` | 15 | M3 renk sayısı; `RunMosaicAsync` her çalışmada yeniden hesaplar. |
| `RgbIncrement` | `int` | 10 | M3 palet adımı. |
| `UseLab`, `UseAverage` | `bool` | false | M3 seçenekleri. |
| `SelectedInterpolation` | `InterpolationMethod` | `Area` | Ölçekleme yöntemi; `InterpolationMethods` (statik) tüm enum değerleri. |
| `ShowGrid` | `bool` | true | Derz/ızgara. `MosaicView` ve `GridOverlay` bağlamadan anında yeniden çizer; başka bir şey yapılmaz. |
| `StonePixelSize` | `int` (salt okunur) | 100 | Ekrandaki taş dokulu görüntünün tam detayı (taş başına piksel) = `ViewStonePixels` (`const`, 100: taş fotoğraflarının kendi boyutu). Yakınlaştırınca dokular bu boyutta çizilir; uzaklaştırınca `MosaicView` daha kaba seviyeler kullanır. Ayarı yoktur (eski Detay kaydırıcısı ve Araçlar → Detay Seviyesi kaldırıldı); dışa aktarma kalitesi listede ayrıca seçilir. `MosaicView.StonePixelSize` ve `GridOverlay.StoneSize` buna bağlıdır. `MosaicData.N`'ye yazmaz. |
| `GridColor` | `Color` | (128,128,128) | Derz rengi; yalnızca bildirim yapar, yeniden üretim yoktur. `MosaicView` ve `GridOverlay` bağlamadan yeniden çizer. |
| `GridColorPresets` | `Color[]` (statik) | 12 renk | Ana renk paleti. |
| `GridColorShades` | `Color[]` | boş | `SelectMainColor` ile üretilen 7 ton (siyah → renk → beyaz). |

**İşlem durumu & komut uygunluğu**

| Ad | Tip | Açıklama |
|---|---|---|
| `Progress` | `int` | İlerleme yüzdesi. |
| `IsProcessing`, `IsExporting` | `bool` | Değişince `CanRunMosaic`, `CanExport` ve `IsBusy` bildirilir. |
| `IsBusy` | `bool` (salt okunur) | `IsProcessing \|\| IsStockBusy \|\| IsExporting \|\| IsDriveBusy \|\| IsSavingProject`; durum çubuğundaki `ActivityWave` dalgasını sürer. Beş bayraktan biri değişince bildirilir. |
| `IsSavingProject` | `bool` | Proje dosyası arka planda yazılırken true (private set); değişince `CanSaveProject` ve `IsBusy` bildirilir. |
| `CanSaveProject` | `bool` (salt okunur) | `MosaicDone && !IsSavingProject`. Dosya menüsündeki Proje Kaydet / Farklı Kaydet, toolbar'daki iki kaydet düğmesi ve Ctrl/⌘+S, Ctrl/⌘+Shift+S buna bağlıdır; bir kayıt sürerken ikincisi başlatılamaz. `MosaicDone` değişince de bildirilir. |
| `MosaicDone` | `bool` | Değişince `CanExport`, `OptimalAvailable`, `NavBitmap`, `ShowStoneHint`, `CanSaveProject`, `ShowMosaicView` ve `ShowSourceView` bildirilir. |
| `CanRunMosaic` | `bool` | `ImageLoaded && !IsProcessing && !IsExporting`. |
| `CanExport` | `bool` | `(MosaicDone || (ImageLoaded && inputBitmap != null)) && !IsProcessing && !IsExporting`: Mos'tan önce görsel dışa aktarılır. `ImageLoaded` değişince de bildirilir. |
| `CanCancel` | `bool` (salt okunur) | `_workCts != null && !_workCts.IsCancellationRequested`: iptal edilebilir bir iş sürüyor ve henüz iptal istenmedi. Durum çubuğundaki İptal düğmesini gösterir, Esc'yi açar. `BeginCancellable`, `EndCancellable` ve `CancelWork` bildirir. |
| `_workCts` | `CancellationTokenSource?` | Süren iptal edilebilir işin kaynağı (Mos, taş sayısı kaydırıcısının uygulaması, Stok Kontrol öncesi düzeltme, dışa aktarma). Tek alan vardır; yeni iş başlarsa öncekinin yerine geçer. |
| `_stockCheckCancelled` | `bool` | `FixToStockAsync` iptal edilince true; `CheckStockAsync` bunu görünce tabloya yazmadan durur. |

**Dışa aktarma**

| Ad | Tip | Açıklama |
|---|---|---|
| `ExportQualities` | `static readonly int[]` | Listede sunulan görüntü kaliteleri: taş başına piksel 10, 20, …, 100. Kullanıcıya "N" olarak değil, piksel boyutu ve dosya boyutuyla gösterilir. |
| `DefaultExportQuality` | `const int` | 40. `Ctrl/⌘+E`'nin ve `quality` verilmeden çağrılan `ExportImageAsync`'in kalitesi; listede bu seçeneğin sonuna `ExportDefaultQuality` ("(varsayılan)") eklenir. Eski Detay ayarının varsayılanıdır; ekrandaki görünümle ilgisi yoktur. |
| `_exportEstimates` | `Dictionary<(int n, bool jpeg), long>` | Tahmini dosya boyutları (bayt) önbelleği. |
| `_exportEstimatesKey` | `(int version, bool grid, Color color)` | Önbelleğin geçerli olduğu mozaik sürümü (`RenderSource.Version`), ızgara ve derz rengi; değişince önbellek temizlenir. |
| `_estimatingExport` | `bool` | Tahmin döngüsü çalışıyor mu (aynı anda tek döngü). |
| `ExportEstimatesChanged` | `event Action?` | Yeni bir tahmin hazır olunca (UI iş parçacığında) yayınlanır; `MainWindow` seçenek metinlerini yeniler (`UpdateExportChoiceTexts`). |

**Optimum**

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `UseOptimal` | `bool` | false | Mos'un Optimum (`MosaicEngine.RunOptimal`) mi M3 (`RunM3`) mü çalıştıracağı. |
| `OptimalAvailable` | `bool` | — | `MosaicDone && _lastRunOptimal`; taş sayısı kaydırıcısını açar. |
| `OptimalK` | `int` | 0 | Seçili taş sayısı; değişince (bastırılmadıysa) `ScheduleOptimalApply`. |
| `OptimalKMax` | `int` | 1 | `LastOptimalResult.CandidateCount`. |
| `OptimalKSuggested` | `int` | 0 | `LastOptimalResult.KOptimal`. |
| `_optimumUserSelection` | `bool[]?` | null | Son Optimum çalıştırmasından önceki kullanıcı katalog seçimi (`boolLeaveOut` dizisi). |
| `_optimumAutoSelection` | `bool[]?` | null | Optimum sonrası uygulamanın uyguladığı "yalnız kullanılan taşlar" seçimi. |
| `_suppressOptimalApply` | `bool` | false | `RunMosaicAsync` içinde `OptimalK` atanırken yeniden uygulamayı engeller. |

**Katalog & stok**

| Ad | Tip | Açıklama |
|---|---|---|
| `CatalogColors` | `ObservableCollection<ColorItem>` | Tüm katalog. |
| `PaletteColors` | `ObservableCollection<PaletteItem>` | Mozaikte kullanılan renkler. |
| `AssignedColors` | `ObservableCollection<AssignedItem>` | Kullanılan taşlar ve piksel sayıları. |
| `IsStockBusy` | `bool` | Stok işlemi sürerken true (private set); değişince `CanUseStock` ve `IsBusy` bildirilir. |
| `CanUseStock` | `bool` | `!IsStockBusy`; stok menü/butonlarını kilitler. |
| `IsDriveBusy` | `bool` | Google Drive işlemi (kaydetme, indirme) sürerken true (private set); değişince `CanUseDrive` ve `IsBusy` bildirilir. Drive'dan Aç penceresi açıkken (listeyi pencere kendisi okur) false'tur. |
| `CanUseDrive` | `bool` | `!IsDriveBusy`; Dosya → Google Drive menüsünü, toolbar'daki Drive ikonunu ve okunu kilitler. |
| `UseStockAware` | `bool` | Varsayılan `false` ("Stoğa göre" onay kutusu). Kalıcı saklanmaz; her açılışta kapalı başlar. Değişince `StockAwareTip` de bildirilir. |
| `UsePadding` | `bool` | "Kalıp Dolgu" (katalog üstündeki düğme, Araçlar menüsü). Varsayılan `false`; kalıcı saklanmaz. Değişince önce `UpdateDimensions()` (kutu görsel genişliği ↔ dolgulu genişlik), sonra `ApplyPaddingChoiceAsync()` çağrılır. Kapalıyken `PadResult` hiçbir şey yapmaz ve önizleme yoktur. |
| `ShowPadBackground` | `bool` | Görsel gösterilirken (`ShowSourceView`) dolgu alanı boyansın mı: Mos'tan önce `PadPreview`, ya da dolgulu bir mozaiğin yerine görsel gösterilirken. |
| `PadBrush` | `IBrush` | Dolgu alanının rengi: önizlemede `_previewFiller`, dolgulu mozaikte `FillerId`'li katalog taşı; yoksa saydam. |
| `StockAwareTip` | `string` | `TipStockAware` metni; son stoğa göre çalışmanın raporu (`_stockAwareReport`) varsa altına eklenir. |
| `_loadedStock` | `Dictionary<int, StockSheetService.StoneStock>?` | Açılışta ve her görsel/proje yüklemesinde `RefreshStockAsync` ile okunan stok; stoğa göre Mos bunu kullanır. Görsel/proje yüklenince önce null yapılır. |
| `_stockOnHand` | `Dictionary<int, StockSheetService.StoneStock>?` | Son stoğa göre Mos'un veya Stok Kontrol düzeltmesinin kullandığı stok; taş sayısı kaydırıcısı aynı stoğa uyar. Yeni Mos, yeni görsel ve proje açma temizler. |
| `_stockAwareReport` | `string` | Son stoğa göre çalışmanın tam raporu (taşınan taşlar, eklenenler, yetersizler); `SetStockAwareReport` yazar. |
| `_mosaicMadeThisSession` | `bool` | Ekrandaki mozaik bu oturumda Mos (Optimum veya klasik) ile üretildiyse true; görsel yükleme ve proje açma false yapar. `FixToStockAsync` bunu ister. |

**Piksel düzenleme**

| Ad | Tip | Açıklama |
|---|---|---|
| `IsPixelEditActive`, `IsSourcePixelMode`, `IsTargetPixelMode` | `bool` | `PixelEditService` durumunun UI yansıması. |
| `EditedPixelCount` | `int` | Düzenlenmiş piksel sayısı; `EditedPixelCountText`'i (`PropEditedFmt`) bildirir. |
| `_stoneUndoStack`, `_stoneRedoStack` | `Stack<(arnIndex, oldStoneIndex, newStoneIndex, codeName, pixelY, pixelX)>` | Taş varyantı değişikliklerinin geri al/yinele yığınları. |

**Özellikler paneli**

| Ad | Tip | Açıklama |
|---|---|---|
| `SmoothMouse` | `bool` | Yumuşak fare hareketi: tekerlekle zoom yeni değere kısa bir geçişle varır, sağ tuşla kaydırma bırakılınca süzülür (varsayılan true, kalıcı değil). Aynı değer atanırsa bildirim yapılmaz. Animasyonun kendisi `MainWindow`'dadır. **Görünüm → Yumuşak Fare Hareketi**'nin onay işareti buna bağlıdır. |
| `WatchNewImages` | `bool` | İndirilenler / Masaüstüne gelen yeni JPEG/PNG için bildirim gösterilsin mi (varsayılan true, kalıcı değil). Aynı değer atanırsa bildirim yapılmaz; değişince `MainWindow.ApplyWatchNewImages` izleyiciyi başlatır/durdurur. **Dosya → Yeni Görselleri Bildir**'in onay işareti buna bağlıdır. |
| `IsPropertiesPanelOpen` | `bool` | Özellikler paneli açık mı (varsayılan false: her açılışta kapalı; kalıcı değil). `HasSelection` true olunca (taş seçildi) true yapılır. Aynı değer atanırsa bildirim yapılmaz; değişince `MainWindow.ApplyPropertiesPanel` sütunları ayarlar. Görünüm menüsündeki onay işareti buna bağlıdır. |
| `LightParams` | `ObservableCollection<AdjustParam>` | Görsel Ayarları **Işık** sekmesinin 8 satırı: Pozlama (−200…200, `Divisor` 100 → ±2,00 EV), Parlaklık, Kontrast, Parlak Alanlar, Gölgeler, Beyazlar, Siyahlar, Gama (−100…100). Pozlama izinde siyahtan beyaza geçiş, diğerlerinde düz iz ([AdjustParam](AdjustParam.md)). |
| `ColorParams` | `ObservableCollection<AdjustParam>` | **Ton/Doygunluk** sekmesinin 3 satırı: Ton (−180…180), Doygunluk, Açıklık (−100…100), seçili renk aralığının değerlerini gösterir; Renklendir açıkken Ton 0…360, Doygunluk 0…100 (varsayılan 25), Açıklık −100…100 olur ve Renklendir değerlerini gösterir. |
| `AdjustRanges` | `ObservableCollection<AdjustRange>` | 7 renk aralığı yuvarlağı (Ana gökkuşağı geçişi, diğerleri kendi rengi); `IsSelected` seçili olanda, `IsUsed` kendi ayarı olan aralıkta (Ana hariç) true. |
| `AdjustTab` | `int` | 0 = Işık, 1 = Ton/Doygunluk (kalıcı değil, başta 0); `IsLightTab` / `IsColorTab` sekme düğmelerinin iki yönlü bağlandığı bool'lar. |
| `AdjColorize` | `bool` | Renklendir kutusu; değişince `CanPickRange` bildirilir, renk satırları yeniden doldurulur (`SyncColorParams`), ayar uygulanır. |
| `CanPickRange` | `bool` | `!Colorize`; renk aralığı yuvarlakları buna göre etkin. |
| `IsAdjusted` | `bool` | `!_adjust.IsNeutral`; sütun başlığındaki ve şeritteki mavi nokta ile Sıfırla düğmesinin etkinliği buna bağlıdır. |
| `AdjustSettingsForSave` | `ImageAdjustSettings?` | Proje dosyasına (`ProjectData.Adjust`) yazılacak bağımsız kopya; ayar yoksa `null`. |
| `IsAdjustPanelOpen` | `bool` | Görsel Ayarları **sütunu** açık mı (varsayılan true, kalıcı değil); **Görünüm → Görsel Ayarları**'nın onay işareti. Değişince MainWindow sütunu 24 px şeride indirir ya da önceki genişliğine açar (`ApplyAdjustPanel`). |
| `CanAdjust` | `bool` | `ImageLoaded && !IsExporting && (!IsProcessing \|\| LiveMos)`; kaydırıcılar buna göre etkin. Anlık Mos açıkken kendi Mos'u sürerken de kullanılabilir (sürükleme kesilmez; değişiklik sıraya girer). `ImageLoaded`, `IsProcessing`, `IsExporting` ve `LiveMos` değişince bildirilir. |
| `LiveMosAllColors`, `LiveMosSelectedColors` | `bool` | Anlık Mos'un renkleri: tüm katalog taşları ya da kullanıcının seçimi (`LiveMosSelectedColors` = `!LiveMosAllColors`; başta seçili renkler, kalıcı değil). Değişince Anlık Mos açık ve mozaik varsa `RunLiveMosAsync()`. |
| `_userCatalogSelection` (private) | `bool[]?` | Kullanıcının katalog işaretleri (`boolLeaveOut`): katalog yüklenince, `SyncColorExclusion` ve `SetAllColors` sonrası `RememberUserSelection()` ile alınır; `ApplyStockSelection` kararlarını da işler. Mos'tan sonraki `FilterCatalogByUsedColors` daraltması buna girmez. |
| `LiveMos` | `bool` | **Anlık Mos** (sekmelerin altındaki onay kutusu, Araçlar menüsü). Başta false, kalıcı değil. Değişince `CanAdjust` bildirilir; açılırken ekranda eskimiş mozaik yerine görsel gösteriliyorsa (`_showingRaw`) `RunLiveMosAsync()` başlatılır. |
| `LiveMosBusy` | `bool` | Ekrandaki bir mozaiğin yerine Anlık Mos'un Mos'u hazırlanıyor (`RunLiveMosAsync` içinde, başlarken `MosaicDone && !_showingRaw` ise true, bitince false). Pencere bu sürede önceki mozaiğin donmuş görüntüsünü gösterir (`MainWindow.FreezeMosaicView`). |
| `ShowMosaicView` / `ShowSourceView` | `bool` | `MosaicDone && !_showingRaw` / tersi. Tuvalde `MosaicView` ile görsel (`sourceImage`) arasında seçim yapar. |
| `SourceViewWidth`, `SourceViewHeight` | `double` | Görselin ekrandaki boyutu: normalde `ImageDisplayWidth/Height`; Mos'tan önce dolgulu önizlemede (`PadPreview`) görselin kendi boyutu × zoom; kalıba tamamlanmış bir mozaiğin yerine gösterilirken yalnızca dolgusuz kısım (`ImageDisplayWidth × UnpaddedCols / dataM3 sütun sayısı`, yükseklik için de aynı); görsel sol üste hizalanır, dolgu alanında `PadBrush` görünür. |
| `HasSelection` | `bool` | Bir piksel (taş) seçili mi. Değişince `ShowImageInfo` ve `ShowStoneHint` bildirilir; true olunca `IsPropertiesPanelOpen = true` (kapalı panel açılır). `LoadImage`, `OpenProjectAsync` ve `ClearSelection` false yapar; `OnImagePressed` true. |
| `ShowImageInfo` | `bool` | `ImageLoaded && !HasSelection`: görsel bilgileri bölümü görünür. |
| `ShowNoImageHint` | `bool` | `!ImageLoaded`: resim simgesi ve "Görsel yüklendiğinde…" (`PropNoImage`) ipucu. |
| `ShowStoneHint` | `bool` | `MosaicDone && !HasSelection`: "Taş bilgileri için…" (`PropStoneHint`) ipucu. |
| `ImageAccent` | `Color?` | Yüklenen görselin ortalama rengi (`AverageColor`: eşit aralıklı ~10.000 pikselin ortalaması, `MosaicData.inputBitmap`'ten); görsel yoksa null. `UpdateImageInfo` başında hesaplanır. MainWindow tuvali ve ölçü bölümünü bu rengin sakin bir tonuyla boyar (`ApplyImageTint`). Private set; değişince bildirilir. |
| `ImageInfoName` | `string` | `ProjectService.CurrentPictureFileName`'in dosya adı (private set; bundan sonrakiler de). |
| `ImageInfoFound` | `bool` | Görsel dosyası diskte var ve `MosaicData.inputBitmap` dolu mu. false iken önizleme, rozet ve Ayrıntılar kartı gizlenir, kırmızı `PropImageMissing` görünür. |
| `ImageInfoThumb` | `Bitmap?` | Önizleme kartının resmi: `MakeThumb(bmp, 360)` (en çok 360 px genişlik). Değiştirilirken eskisi dispose edilir. |
| `ImageInfoType` | `string` | Dosya uzantısı büyük harfle (`JPG`, `PNG`…); önizlemedeki rozet. |
| `ImageInfoSize` | `string` | Dosya boyutu (`FormatFileSize`). |
| `ImageInfoResolution` | `string` | `G × Y px`. |
| `ImageInfoMegapixels` | `string` | `x.x MP`. |
| `ImageInfoAspect` | `string` | En-boy oranı (`AspectText`). |
| `ImageInfoDate` | `string` | Dosyanın son değiştirilme zamanı (`LastWriteTime.ToString("g")`, bölge ayarına göre). |
| `PropStoneName`, `PropStoneId` | `string` | Taş kodu, `#ID`. |
| `PropPixelCoord`, `PropMouldCoord` | `string` | `Y: .. X: ..` ve kalıp içi `yi: .. xi: ..`. |
| `PropRgbInfo`, `PropColorBrush` | `string`, `IBrush` | Piksel rengi. |
| `PropTextureBitmap` | `Bitmap?` | Seçili varyantın 80×80 dokusu. |
| `SelectedStoneIndex` | `int` | `MosaicData.arn` içindeki varyant indeksi; `SelectedStoneText` (`PropStoneFmt`) bildirilir. |
| `PropStoneThumbs` | `ObservableCollection<StoneThumbItem>` | 1–16 numaralı varyant dokuları. |

**Durum çubuğu & bilgi metinleri**

| Ad | Tip | Açıklama |
|---|---|---|
| `StatusText` | `string` | Başlangıç `Loc.Get("StatusReady")`. |
| `ElapsedTime` | `string` | `m:ss.ff` süre. |
| `UsedColorInfo` | `string` | `StatusUsedColors` (toplam / kullanılan taş). |
| `DimensionInfo` | `string` | Boyut/alan metni (XAML'e bağlı değil). |
| `DimensionHeight`, `DimensionArea` | `string` | Sol paneldeki satırın yüksekliği ve alanı, genişlik kutusuyla aynı biçimde (invariant, nokta): `"93.6"`, `"0.88 m²"`. |
| `InfoStoneCols`, `InfoStoneRows`, `InfoStoneTotal`, `InfoMouldCols`, `InfoMouldRows`, `InfoMouldTotal` | `string` | Sol paneldeki taş/kalıp kartının sayıları (ayrı ayrı, kart onları hizalı sütunlara koyar); toplamlar `N0` ile binlik ayraçlı. `MouldInfo` tam kalıp sayısını taş satır ve sütunlarından hesaplar (`UpToMould(n) / 26`). Eski `DimensionSize` ve `OriginalInfo` (orijinal piksel ölçüsü) kaldırıldı; piksel ölçüsü Özellikler panelinde. |
| `PixelCoordInfo`, `PixelDetailInfo`, `PixelColorInfo`, `PixelScaleInfo` | `string` | Piksel bilgisi; `PixelDetailInfo`/`PixelColorInfo` `OnImagePressed`'de yazılır, hiçbiri XAML'e bağlı değil. |

**Dil**: ViewModel'in ürettiği metinler `RefreshLocalized` ile yenilenir.

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `MainViewModel()` | `ColorCatalogService.LoadDefaultCatalog` + `RefreshCatalogList`; hata `StatusText`'e yazılır. Dil aboneliği. Sonunda `InitAdjustPanel()`. | MainWindow |
| `LoadImage(path)` | Yeni görsel: `MosaicData.N` 40'a döner (proje dosyasına WPF için yazılan değer; önce açılan bir projenin değeri yeni mozaiğe taşınmaz), `StartNewContent()`, `ProjectService` dosya adlarını ayarlar, `ForgetWpfState`, `_stockOnHand`/`_mosaicMadeThisSession`/stok raporunu sıfırlar, `MosaicEngine.Reset`, mozaik/düzenleme/undo durumunu ve `StoneTextureService`'i sıfırlar, görseli yükler (`MosaicData.sourceBitmap` = yüklenen görüntü, ayarlar `SetAdjustSettings(new ImageAdjustSettings())` ile uygulamadan sıfırlanır: her yeni görsel ayarsız başlar; `SetShowingRaw(false)`), zoom = 2, `UpdateImageInfo`, `UpdateDimensions`, `AutoSelectGridColor`; başarılıysa `_loadedStock = null` ve arka planda `RefreshStockAsync()`. | MainWindow (menü, sürükle-bırak) |
| `UpdateDimensions()` | Kutu kullanıcı tarafından değiştirildiyse (`WidthCm ≠ _shownWidthCm`) `_imageWidthCm = WidthCm`, değilse görselin genişliği korunur. `_imageWidthCm`'yi 1.2 cm katına yuvarlar (en az 2 taş), görsel genişliğinden (1 px = 1 taş) büyükse kırpar ve `AlertResolutionTitle` uyarısı verir. Kutuya Kalıp Dolgu açıksa `UpToMould(taş)·1.2`, kapalıysa görselin genişliği yazılır. `CalculateDimensions(_imageWidthCm)`; ölçü satırı (`DimensionHeight`, `DimensionArea`, `HeightCm`) Kalıp Dolgu açıksa dolgulu yükseklik/alandır, taş/kalıp metinleri güncellenir. | MainWindow, `LoadImage`, `OpenProjectAsync`, `RefreshLocalized`, `UsePadding` |
| `RunMosaicAsync()` | Mos: önce `FlushAdjustmentsAsync()` (bekleyen Görsel Ayarları uygulanır; ardından `CanRunMosaic` yeniden denetlenir), `StartNewContent()`, stok işaretlerini temizler, Optimum taban seçimini hazırlar, `SetActiveColors`, `TargetColors` hesaplar, arka planda `RunOptimal` veya `RunM3`, sonra Optimum kaydırıcısını kurar ve `FinishMosaic`. `UseStockAware` açıksa ve stok ayarında Sheet ID varsa sonuç Mos içinde stoğa uydurulur (Sheet ID yoksa düz Mos, not gösterilmez): `_loadedStock` yoksa önce `RefreshStockAsync()` beklenir; Optimum'da `RunOptimal(prepareTextures: stock == null)` ardından `ApplyOptimalKFor(KOptimal, stock)`, klasikte `RunM3` ardından `FixClassicMosaicToStock(stock)`. Başarılıysa `_stockOnHand = stock` ve `ShowStockAwareResult`; stok okunamadıysa düz Mos + `StockAwareNoStock` notu, klasik düzeltme yapılamadıysa `StockAwareCannotFix` uyarısı. Son adım olarak (işçi iş parçacığında, iptal belirteci kapatılarak) `PadResult` mozaiği tam kalıba tamamlar; dolgu taşı stoğa bakmadan seçilir (stok yalnızca etiket için: `stock ?? _loadedStock`). Sonuç notu `ShowPadNote` ile durumun sonuna eklenir. Bitince sürüm değişmişse sonuç atılır; değişmemişse `_mosaicMadeThisSession = result != null`. İptal edilebilir (`BeginCancellable`; ayrıntı aşağıda **İptal**): Optimum analizde iptal edilirse önceki mozaik kalır (`StatusMosCancelled`), klasik Mos iptal edilirse yarım mozaik `ClearMosaic` ile kaldırılır (`StatusMosCancelledCleared`), stoğa göre düzeltme iptal edilirse mozaik düzeltmesiz kalır (`StatusStockFitCancelled` eki). | MainWindow |
| `InitAdjustPanel()` (private) | Görsel Ayarları satırlarını (`LightParams`, `ColorParams`; `Changed = OnAdjustParamChanged`) ve 7 renk aralığını kurar, Ana aralığı seçer; dil değişince satır ve aralık adlarını yeniler. | Yapıcı |
| `SelectAdjustRange(index)` | Seçili renk aralığını değiştirir (`IsSelected`) ve renk satırlarını o aralığın değerleriyle doldurur. | MainWindow (`OnAdjustRangeClick`), `InitAdjustPanel` |
| `SyncAdjustParams()` / `SyncColorParams()` (private) | `_adjust`'taki değerleri satırlara `SetSilently` ile yazar (hiçbir şey uygulanmaz). Renk satırlarında Renklendir'e göre aralık ve varsayılanı da değiştirir; iz renklerini (`UpdateColorTracks`: Ton renk çemberi, Doygunluk griden aralık rengine, Açıklık siyah→renk→beyaz) ve aralıkların `IsUsed` noktalarını günceller. | Sıfırlama, proje/görsel yükleme, aralık ve Renklendir değişimi |
| `OnAdjustParamChanged(p)` (private) | Değişen satırın değerini `_adjust`'ın ilgili alanına yazar (renk satırlarında seçili aralığa ya da Renklendir alanlarına; Renklendir'de iz renkleri de yenilenir, aralıkta `IsUsed` güncellenir), sonra `AdjustChanged()`. | `AdjustParam.Changed` |
| `AdjustChanged(apply = true)` (private) | `_adjustVersion`'ı artırır, `IsAdjusted`'ı bildirir; `apply` ise `ScheduleAdjust()`. | Satır değişimi, Renklendir, sıfırlama, `SetAdjustSettings` (`apply: false`) |
| `SetAdjustSettings(settings)` (private) | Bekleyen ayarı iptal eder, `_adjust = settings.Clone()`, satırları doldurur; uygulamaz. | `LoadImage` (nötr), `OpenProjectAsync` (projenin ayarı) |
| `ResetAdjustments()` | `_adjust` yeni (nötr) ayar olur, satırlar doldurulur, `AdjustChanged()` (iki sekme ve Renklendir dahil hepsi sıfırlanır). | MainWindow (`OnAdjustReset`) |
| `ScheduleAdjust()` (private, async void) | Önceki bekleyişi iptal eder, 150 ms bekler (kaydırıcı sürüklenirken her adımda hesaplanmasın), sonra `_adjustTask = ApplyAdjustmentsAsync()`; bitince `LiveMos` açıksa `RunLiveMosAsync()`. Değişiklik bir sürükleme sırasında geldiyse (`_adjustDragging`) Mos burada yapılmaz, `_changedWhileDragging` işaretlenir. | `AdjustChanged` |
| `SetAdjustDragging(dragging)` | Görsel Ayarları kaydırıcılarından birinin sürüklenmesi başladı/bitti. Bitişte sürükleme sırasında değişiklik olduysa ve `LiveMos` açıksa tek bir `RunLiveMosAsync()` (Mos, bekleyen ayarı önce uygular). | `MainWindow.OnAdjustSliderDragging` (`AdjustSlider.DraggingChanged`) |
| `RunLiveMosAsync()` (private) | Anlık Mos: `LiveMos` kapalıysa ya da görsel yoksa çıkar. Mos veya dışa aktarma sürüyorsa `_liveMosPending = true` yapıp çıkar. Sonra renkleri kurar: `LiveMosAllColors` ise bütün katalog işaretli, değilse `_userCatalogSelection`'ın kopyası (`ApplyCatalogSelection`; `RunMosaicAsync` etkinleştirir). Değilse `LiveMosBusy` ayarlanır ve `RunMosaicAsync(keepK)` (bitince `LiveMosBusy = false`); `keepK`: Optimum işaretli, önceki mozaik Optimum'la yapılmış ve taş sayısı kaydırıcıyla önerilenden farklı seçilmişse o sayı (yeni analizde de korunur), değilse null; o ana kadar mozaik yoktuysa (`first`) ardından `FitToWindow(_lastViewportWidth, _lastViewportHeight)` (ilk mozaik sığdırılır, sonrakiler görünümü korur). Bu arada yeni bir değişiklik sıraya girdiyse (`_liveMosPending`) bir Mos daha yapar. Mos o anki seçimlerle yapılır (Optimum, Stoğa göre, Kalıp Dolgu). | `ScheduleAdjust`, `LiveMos` |
| `SetShowingRaw(value)` (private) | `_showingRaw`'u ayarlar; `ShowMosaicView`, `ShowSourceView`, `SourceViewWidth/Height`, `ShowPadBackground`, `PadBrush` ve `NavBitmap`'i bildirir. | `ApplyAdjustmentsAsync` (true), `FinishMosaic`, `LoadImage`, `OpenProjectAsync`, `ClearMosaic` (false) |
| `FlushAdjustmentsAsync()` (private) | Henüz beklemede olan bir ayar varsa beklemeyi iptal edip hemen uygular; süren `_adjustTask`'ı bekler. | `RunMosaicAsync` (Mos son ayarla çalışsın) |
| `ApplyAdjustmentsAsync()` (private) | `MosaicData.sourceBitmap` yoksa çıkar. Ayarın kopyası (`_adjust.Clone()`) ve `_adjustVersion` alınır. Arka planda ayar nötrse kaynağın kendisini, değilse `ImageAdjustService.Apply(source, a)` sonucunu ve ekran için `ImageService.ToAvaloniaBitmap`'i hazırlar (hata → `StatusError`). Bu arada içerik (`_contentVersion`), ayar (`_adjustVersion`) ya da kaynak değiştiyse sonuçları atar. Değilse `MosaicData.inputBitmap = ayarlı`; önceki ayarlı kopya, bir iş sürmüyorsa dispose edilir (sürüyorsa çöp toplayıcıya bırakılır). `DisplayBitmap` yenilenir; `MosaicDone` ve Anlık Mos kapalıysa `SetShowingRaw(true)` (eskimiş mozaik yerine ayarlanmış görsel gösterilir) ve durum `AdjNeedsMos` ("…Mos'a basın"). | `ScheduleAdjust`, `FlushAdjustmentsAsync`, `OpenProjectAsync` |
| `CancelWork()` | `CanCancel` değilse hiçbir şey yapmaz. Yoksa `_workCts.Cancel()`, `CanCancel` bildirilir (düğme gizlenir), durum `StatusCancelling` ("İptal ediliyor..."). İş bir sonraki kontrol noktasında (`WorkCancellation.Check`) `OperationCanceledException` ile durur; sonrasında ne olacağını işi başlatan metot belirler (aşağıda **İptal**). | MainWindow (`OnCancelWork`, Esc) |
| `CreateProjectSnapshot()` (private) → `ProjectService.ProjectSnapshot` | `ProjectService.CreateSnapshot(WidthCm, ZoomLevel, ShowGrid, false, derz rengi R/G/B, interpolasyon, AdjustSettingsForSave)`: mozaiğin o anki hâlinin kopyası (UI iş parçacığında, milisaniyeler). | `SaveProjectAsync`, `SaveToDriveAsync` |
| `SaveProjectAsync(filePath)` → `Task<bool>` | Bir kayıt zaten sürüyorsa `false`. Önce `FlushAdjustmentsAsync()`: bekleyen son kaydırıcı değişikliği uygulanır (kaydedilen ayarlı görsel güncel olsun). `_contentVersion`'ı not eder, `CreateProjectSnapshot()` (hata → `ReportSaveFailed`, `false`). `IsSavingProject = true`, durum `StatusSavingProject` ("Proje kaydediliyor: <ad>"), `ProjectService.WriteSnapshot` `Task.Run` ile arka planda (JSON + `.part` dosyası + taşıma; pencere donmaz, dalga döner). Hata → `ReportSaveFailed`, `false`; `finally` içinde `IsSavingProject = false`. Başarıda, arada başka görsel ya da proje açılmadıysa (`_contentVersion` aynıysa) `ProjectService.CurrentFileName = filePath`, ayrıca anlık kopyanın `OriginalMovedTo`'su doluysa (görselin kendi klasörüne ayarlı kayıt) `ProjectService.CurrentPictureFileName = OriginalMovedTo` (oturum `orijinal/<ad>`'daki orijinalle sürer); durum `StatusSaved`, `DocumentTitle` bildirilir, `FileSaved(filePath, Project)`, `true`. Kayıt tıklandığı andaki mozaiği yazar; yazma sürerken yapılan düzenlemeler o kayda girmez. | MainWindow (`OnSaveProject`, Farklı Kaydet) |
| `ReportSaveFailed(ex)` | Durum çubuğuna hata yazar, `AlertSaveFailed` uyarısını gösterir. | `SaveProjectAsync`, MainWindow |
| `OpenProjectAsync(filePath)` → `Task<bool>` | `StartNewContent()`, durum `StatusOpeningProject` ("Proje açılıyor: <ad>"), `IsProcessing = true`. `Task.Run` içinde: `ProjectService.ReadProject` (okuma, çözme, diziler, WPF aynalama), projenin görseli diskte varsa `ImageService.LoadImage`, `dataM3`'ten taş renkli export bitmap (`ImageService.FromByteArray`). Her türlü istisna (erişim yok, kilitli dosya) "açılamadı" sayılır. Ardından `IsProcessing = false`. Bu arada başka görsel ya da proje açıldıysa (`_contentVersion` değiştiyse) hazırlanan bitmap'ler dispose edilir ve `false` döner. Okunamadıysa `StatusOpenFailed` + `AlertProjectOpenFailed`, `false`. Sonra UI iş parçacığında `ProjectService.ApplyProject` (milisaniyeler), görsel yüklendiyse `sourceBitmap` ve `inputBitmap` olur (yüklenemediyse önceki görsel kalır, eskisi gibi), projedeki Görsel Ayarları `SetAdjustSettings(data.Adjust ?? ImageAdjustSettings.FromLegacy(data.ImageAdjust))` ile geri yüklenir (ilk sürümün 4 sayılık `ImageAdjust` dizisi de okunur) ve görsel yüklendiyse ve bir ayar varsa ayarlı kopya arka planda `ApplyAdjustmentsAsync` ile hazırlanır, ayarları uygular, hazır export bitmap'i kullanır, sanal boyutu `C·100 × R·100` yapar (projedeki `N` görünümü değiştirmez; `ProjectService` onu yalnızca `MosaicData.N`'ye alır ve kayıtta geri yazar), taş seçimini bırakır (`HasSelection = false`) ve `UpdateImageInfo` ile görsel bilgilerini yeniler, kataloğu kullanılan taşlara filtreler. Ardından `StoneTextureService.Reset` + `RefreshMosaicView` ile önce taş renklerini gösterir (`StatusGeneratingRs`), dokuları arka planda `LoadTextures` ile yükler (hata yutulur, görünüm renklerle kalır), sürüm değişmemişse `RefreshMosaicView` ve `FitToWindow`. RS bütün olarak üretilmez. `_stockOnHand`, `_mosaicMadeThisSession` ve stok raporu sıfırlanır; sonunda `_loadedStock = null` ve `RefreshStockAsync()`. Proje açıldıysa (doku yüklemesi sırasında başka içerik açılsa bile) `true` döner. Görsel: proje `OriginalPictureFileName` taşıyorsa ve `orijinal/<ad>` diskte varsa temel o görseldir (`CurrentPictureFileName` ona çevrilir, `Adjust` yeniden uygulanır); alan var ama kopya yoksa (ör. Drive) proje yanındaki ayarlı görsel temel alınır ve ayarlar sıfırlanır; alan yoksa eskisi gibi (`Adjust ?? FromLegacy(ImageAdjust)`). | MainWindow (`OnOpenProject`; `true` dönerse `FitToWindow`), `OpenFromDriveAsync` |
| `SaveScreenshotAsync(bgra, width, height, bgR, bgG, bgB, path)` | Önce `NewImageWatcher.Ignore(path)` (kaydedilen dosya "yeni görsel" diye bildirilmez). Ekran görüntüsü: ekrandan gelen ön çarpımlı BGRA pikselleri tuval rengi üzerine koyar (saydam kenarlar tuval rengini alır), PNG olarak arka planda yazar; durum `StatusScreenshotSaved` ve `FileSaved(path, Screenshot)`, hata → `StatusError` + uyarı. | MainWindow (`OnScreenshot`) |
| `ExportImageAsync(path, quality = null)` | Önce `NewImageWatcher.Ignore(path)` (dışa aktarılan dosya, Masaüstüne de kaydedilse, "yeni görsel" diye bildirilmez). Mozaik yoksa ve görsel varsa `ExportSourceImageAsync(path)` (kalite yok sayılır) ve döner. `quality` = seçilen görüntü kalitesi (taş başına piksel n); verilmezse `DefaultExportQuality` (40). Dışa aktarma sürüyorsa hiçbir şey yapmaz; `MosaicDone` ve `RenderSource` yoksa `AlertExportNoMosaic`. `.jpg/.jpeg` → JPEG, diğerleri PNG. Görüntü boyutu `MosaicExporter.ImageSize(RenderSource, n)`. İki kontrol: (1) JPEG istenip bir kenar `MosaicExporter.JpegMaxSide`'ı (65.535) aşıyorsa `ExportJpegTooLarge` uyarısı (PNG ya da daha düşük bir görüntü kalitesi önerir), dosya yazılmaz; (2) JPEG tek bitmap'e sığmıyorsa (`ImageService.MaxBitmapPixels`) ve tamponu (`MosaicExporter.JpegMemoryBytes`) `GC.GetGCMemoryInfo().TotalAvailableMemoryBytes`'ın yarısından büyükse `ExportJpegMemoryConfirm` onayı sorulur (`FormatBytes` ile gereken ve kullanılabilir bellek); **Evet** devam eder, **Hayır** vazgeçer. Sonra `RenderSource.WithStoneSnapshot()` alınır, durum `StatusExporting`, arka planda `MosaicExporter.Export(src, path, jpeg, n, grid, gw, gc, …)` dosyayı yazar. Taş başına piksel her zaman seçilen n'dir, kendiliğinden düşürülmez. Görüntü `ImageService.MaxBitmapPixels`'tan büyükse yüzde değiştikçe durum `StatusExportingPct` olur. Bitince `StatusSaved`; hata olursa `StatusError` ve `AlertExportTitle` + `AlertErrorBody` uyarısı. İptal edilebilir: `Task.Run` içinde `WorkCancellation.Token` atanır, `MosaicExporter` büyük görüntülerde taş satırı başına denetler; iptalde yarım dosya `MosaicExporter.Export` tarafından silinir ve durum `StatusExportCancelled` olur (uyarı açılmaz). İptal istendikten sonra yüzde güncellemesi "İptal ediliyor..." yazısının üzerine yazmaz. Izgara: açıksa `max(1, n/11)` (seçilen n'den). Başarıyla bitince durum `StatusSaved` ve `FileSaved(path, Export)`. | MainWindow (`ExportQuickAsync`, `ExportAsAsync`) |
| `ImageExportSize()` (private) · `ExportSourceImageAsync(path)` (private) | Mos'tan önce dışa aktarma: boyut `inputBitmap` × `PadScaleX/Y` (Kalıp Dolgu önizlemesinde tam kalıba büyümüş). İşçi iş parçacığında bu boyutta bir bitmap dolgu taşı rengiyle (`_previewFiller`; dolgu yoksa saydam, JPEG'de beyaz) temizlenir, `inputBitmap` sol üste çizilir; `.png` uzantısı PNG, değilse JPEG (95). Izgara yok. `IsExporting` altında; durum `StatusSaved` + `FileSaved(Export)`, hata `AlertExportTitle`. | `ExportImageAsync`, `ExportChoiceLabel` |
| `QuickExportExtension(n)` → `string` | mosairEXPORT'un dosya uzantısı: `MosaicExporter.QuickExportUsesJpeg(ImageSize(RenderSource, n))` ise `"jpeg"`, değilse `"png"` (JPEG mümkün değil ya da tamponu kullanılabilir belleğin yarısını aşıyor). `RenderSource` ya da mozaik yoksa `"jpeg"`. | MainWindow (`ExportQuickAsync`) |
| `ExportChoiceLabel(n, saveAs)` → `string` | Listedeki bir kalite seçeneğinin metni; "N" gösterilmez. Mos'tan önce `ExportChoiceImage` ("Görsel: W × H px …", `ImageExportSize`). `ExportDimsPx` (ör. "13.333 × 23.688 px") + tahmini boyut: mosairEXPORT için `ExportChoiceQuick` ("… px · JPEG ≈ 420 MB"; biçim `QuickExportUsesJpeg`'e göre), mosairEXPORT As için `ExportChoiceAs` ("… px · JPEG ≈ … · PNG ≈ …") ya da JPEG mümkün değilse `ExportChoiceAsPngOnly` ("… px · PNG ≈ … (bu boyutta JPEG olmaz)"). Tahmin önbellekte yoksa `ExportEstimating` ("hesaplanıyor…"). `n == DefaultExportQuality` ise sonuna `ExportDefaultQuality` ("(varsayılan)") eklenir. `RenderSource` yoksa `""`. | MainWindow (`UpdateExportChoiceTexts`) |
| `RefreshExportEstimatesAsync()` | Mozaik yoksa döner. Önbellek anahtarı (`RenderSource.Version`, `ShowGrid`, `GridColor`) değiştiyse `_exportEstimates`'i temizler. Başka bir tahmin sürüyorsa (`_estimatingExport`) döner. Yoksa `ExportQualities` sırasıyla (en küçük kaliteden başlayarak) eksik her (n, JPEG/PNG) tahminini arka planda `MosaicExporter.EstimateBytes` ile hesaplar (JPEG mümkün değilse JPEG atlanır), her sonuçtan sonra `ExportEstimatesChanged` yayınlar. Mozaik ya da anahtar bu sırada değişirse durur. Hatalar yutulur (tahmin yalnızca ipucudur; seçenek "hesaplanıyor…" kalır). | MainWindow (`RefreshExportChoices`) |
| `RefreshLocalized()` | Dil değişiminden sonra yerelleştirilmiş metinleri yeniler, `StatusText` = `StatusReady`. | MainWindow |
| `FitToWindow(w, h)` | Viewport'u hatırlar, sığdırma zoom'unu hesaplar (kenar payı 16 px), `MinZoomLevel` ve `ZoomLevel`'i ayarlar (en fazla 10). | MainWindow, `OpenProjectAsync` |
| `UpdateNavigator(viewportW, viewportH, offsetX, offsetY, navSize)` | `NavView*` dikdörtgenini hesaplar. `navSize` mini haritanın kenarlık içindeki boyutudur; küçük resim `Stretch=Uniform` ile ortalandığı için dikdörtgen resmin gerçek konumuna (`NavThumb`) göre kaydırılır, tuvaldeki 4 px kenar boşluğu (`CanvasMargin`) düşülür ve ekranda görünen kısım resim sınırlarına kırpılır. | MainWindow |
| `NavigatorTarget(x, y, navSize, viewportW, viewportH)` → `(x, y)?` | Mini haritadaki noktayı görünümün ortasına getiren kaydırma konumu (aynı `NavThumb` hesabıyla). | MainWindow (`NavigateFromNav`) |
| `SyncColorExclusion(item)` | `ColorCatalogService.SetLeaveOut` ile tek taşın hariç durumunu modele yazar. | MainWindow (checkbox) |
| `SetAllColors(excluded)` | Tüm kataloğu seçer / bırakır, `SetActiveColors`. | MainWindow |
| `SelectMainColor(c)` | `GridColorShades`'i 7 tonla doldurur. | MainWindow, `AutoSelectGridColor` |
| `OnImagePressed(...)` | Mozaik yoksa ya da yerine görsel gösteriliyorsa (`_showingRaw`) hiçbir şey yapmaz. Tıklanan taşı bulur (ID önce `drl.dat`, sonra `arMA[0]`, sonra `arRGB` renk eşleşmesi), kalıp koordinatını hesaplar, özellikler panelini doldurur; orta tuş piksel düzenleme modunu değiştirir; düzenleme açıksa kaynak/hedef piksel adımını yürütür (hedef adımından sonra `InvalidateStone(y, x)`). Ölçek ve sınırlar mozaiğin kendi boyutundan (`dataM3`) alınır, dolgu dahil. Düzenleme açıkken dolgu alanına (`InPadding`) tıklanırsa düzenleme yapılmaz, durum `PadNoEdit` olur (taş bilgisi yine gösterilir). | MainWindow |
| `OnImagePointerMoved(...)` | Boş gövde. | MainWindow |
| `SelectStone(stoneIndex)` | Seçili pikselin `MosaicData.arn` varyantını değiştirir, undo yığınına ekler, redo'yu temizler, `StoneInvalidated(y, x)`. | MainWindow (varyant tıklama) |
| `TogglePixelEditMode()` | `PixelEditService.TogglePixelEditMode` ve durum senkronu (mozaik yoksa yok sayılır). | MainWindow, `OnImagePressed` |
| `SetSourceFromCatalog(item)` | Katalog taşını piksel düzenleme kaynağı yapar, hedef moda geçer. | MainWindow |
| `UndoPixelEdit()` / `RedoPixelEdit()` | Önce taş varyantı yığını (`StoneInvalidated`), boşsa `PixelEditService.UndoLastEdit` / `RedoLastEdit` ve `InvalidateStone(PixelEditService.LastChanged)`. | MainWindow (kısayollar) |
| `OpenStockSheetAsync()` | Ayarlı Sheet'i `StockSheetService.SheetUrl` ile tarayıcıda açar. | MainWindow |
| `ConfigureStockAsync()` | Stok ayar diyaloğunu açar, `StockSheetService.SaveConfig`. | MainWindow |
| `ConfigureDriveAsync()` | `ShowDriveSettings` yoksa döner. `DriveService.LoadConfig()` ile diyaloğu açar; sonuç `null` değilse `DriveService.SaveConfig` ve durum `DriveSettingsSaved`. | MainWindow (`OnDriveSettings`), `DriveConfigOrAsk` |
| `SaveToDriveAsync()` | Drive işlemi sürüyorsa döner. `MosaicDone` değilse `AlertDriveTitle` + `DriveNoMosaic` uyarısı. `DriveConfigOrAsk()` null ise döner. Dosya adı Ctrl+S ile aynı kuralla: `ProjectService.CurrentPictureFileName`'in uzantısız adı + `.mos`, görsel adı yoksa `mosair_project.mos`. `IsDriveBusy = true`, durum `StatusDriveSaving`. Proje normal biçimde tek seferlik bir klasöre, `DriveService.CacheDir/upload_<guid>/<ad>`'a yazılır: `CreateProjectSnapshot()` UI iş parçacığında mozaiğin kopyasını alır (`SaveProjectAsync` ile aynı değerler), `ProjectService.WriteSnapshot` `Task.Run` ile arka planda yazar; büyük projede pencere donmaz. `WriteSnapshot` `ProjectService.CurrentFileName`'e dokunmadığı için başlık çubuğundaki ad ve açık dosya değişmez. Proje baytları ve `WriteSnapshot`'ın projenin yanına kopyaladığı orijinal görsel (`CurrentPictureFileName`'in dosya adı; kopya yoksa görsel gönderilmez) okunup `DriveService.SaveAsync(config, <görsel adı>, <ad>, proje, görsel adı, görsel)` ile gönderilir: Drive'da mosairPROJECT gibi `<görsel adı>/<görsel adı>.mos` + görsel (script görseli farklıysa yeniler). Gönderme başarılı da olsa hata da verse geçici klasör `finally` içinde silinir (hata yutulur). Başarıda durum `StatusDriveSaved` (`<görsel adı>/<ad>`). Hata → `DriveFailed` hem durum çubuğuna hem uyarıya. Sonunda `IsDriveBusy = false`. İptal edilemez. Anlık kopyadan önce `FlushAdjustmentsAsync()`; Görsel Ayarları kullanılıyorsa geçici klasöre yazılan ve gönderilen görsel ayarlı görseldir; anlık kopyada ayarlı görsel varsa (`snapshot.HasAdjustedImage`) geçici klasörün `orijinal/<görsel adı>` dosyası da okunup `SaveAsync`'in `originalName`/`original` parametreleriyle gönderilir. Drive'dan açılınca proje bu orijinalle ve kayıtlı ayarlarla açılır. | MainWindow (`OnDriveSave`) |
| `OpenFromDriveAsync()` → `bool` | Bir proje açıldıysa `true`, aksi hâlde (iptal, hata, ayar yok) `false` döner. Drive işlemi sürüyorsa ya da `ShowDriveOpen` yoksa `false`. `DriveConfigOrAsk()` null ise `false`. `ShowDriveOpen(config)`: klasörü okuma, listeleme ve önizlemeler pencerenin içinde yapılır (hatalar da orada gösterilir). İptalde `false`. Seçilince `IsDriveBusy = true`, durum `StatusDriveDownloading`, `DriveService.DownloadAsync` projeyi `CacheDir/<proje klasörü>/<ad>`'a, orijinal görseli de yanına indirir. Hata → `DriveFailed` (durum + uyarı), proje açılmaz, `false`. Başarıda `IsDriveBusy = false`, `await OpenProjectAsync(path)` (yerel bir `.mos` gibi; okuma da arka planda) ve onun sonucu döner. İptal edilemez. | MainWindow (`OnDriveOpen`; `true` dönerse `FitToWindow`) |
| `LoadStockOnStartupAsync()` | `RefreshStockAsync()` çağırır. | MainWindow (`Opened`) |
| `RefreshStockAsync()` | `SheetId` ayarlı değilse hiçbir şey yapmaz. Ayarlı tablodan proje adıyla stoğu okur (`FetchOnHandAsync`; bu mozaiğin kendi sütunu diğer mozaiklerin payına katılmaz), `_loadedStock`'a ve eşleşen `ColorItem.StockKg`'ya yazar; seçim ve kırmızı noktalar değişmez. Okuma sürerken başka görsel yüklendiyse (`StockProjectName()` değiştiyse) sonucu atar. Sonuç (`StockLoadedOnStart`) veya hata (`StockLoadOnStartFailed`) `AppendStartupStatus` ile durum çubuğuna not olarak eklenir. | `LoadStockOnStartupAsync`, `LoadImage`, `OpenProjectAsync`, `RunMosaicAsync` |
| `FetchStockAsync(markOnly = false)` | Eldeki stoğu çeker (`StockSheetService.FetchStockAsync`), `StockKg` yazar. `markOnly` false ("Stok Çek"): Sheet'te stok ≤ 0 olan taşları hariç tutar, > 0 olanları dahil eder (`ApplyStockSelection`), durum `StockFetched`. `markOnly` true (yalnız işaretle): seçime dokunmaz, stok ≤ 0 olan taşlara yalnızca kırmızı nokta (`stokYetersiz`/`StockShort`), durum `StockFetchedMarked`. | MainWindow (`OnStockFetch`, `OnStockFetchMark`) |
| `CheckStockAsync()` | `UseStockAware` açıksa önce `FixToStockAsync` ile mozaiği stoğa uydurur; sonra mozaikteki taş sayılarını (`arMA[0]`) proje adıyla Sheet'e tek seferde gönderir, yetersiz taşları işaretler, `RemainingKg`/`StockKg` yazar. Düzeltme çalıştıysa kırmızı noktalar ve kalan kg, Sheet'ten geri okunan "Tahmini Kalan" (Google henüz yeniden hesaplamamış olabilir) yerine düzeltmede okunan stoktan (`ShowStockMarks`) gelir ve durum `StockAwareWritten` / `StockCountsWritten` ile biter. Düzeltme çalışamadıysa (neden diyalogda gösterilir) normal kontrol yine yapılır (`StockCheckOk` / `StockCheckShort`). Düzeltme iptal edildiyse (`_stockCheckCancelled`) tabloya hiçbir şey yazılmaz, durum `StatusStockCheckCancelled`. Tabloya yazma adımının kendisi iptal edilemez. | MainWindow |
| `ClearStockOneAsync()` / `ClearStockAllAsync()` | Onaydan sonra bu projenin / tüm projelerin sütununu temizler. | MainWindow |
| `AddStockAsync()` | Onaydan sonra `StockSheetService.AddStockAsync`. | MainWindow |

**Önemli private metotlar**

| Metot | Ne yapar |
|---|---|
| `RunStockAction(action, doneMessage)` | `IsStockBusy` kilidi; hata → `StatusError` + `StockErrFmt` uyarısı. |
| `TryGetStockConfig(needScript, out config)` | `SheetId` (gerekirse `ScriptUrl`) yoksa `StockNotConfigured` uyarısı, false. |
| `DriveConfigOrAsk()` → `DriveService.Config?` | Ayarlar `DriveService.IsConfigured` ise onları döndürür. Değilse `AlertDriveTitle` + `DriveNotConfigured` uyarısını `ShowAlert` ile gösterip kapatılmasını bekler (`await`), sonra `ConfigureDriveAsync` ile ayar penceresini açar (iki pencere üst üste açılmaz), ayarları yeniden okur; hâlâ eksikse (iptal) `null`. |
| `StockProjectName()` | Sheet sütun başlığı: önce resim dosya adı, yoksa proje dosya adı (uzantısız). |
| `ApplyStockSelection(leaveOutForId)` | Stok kararını (true/false/null) kataloğa ve `_optimumUserSelection`'a uygular; seçim önceden "otomatik" seçimle aynıysa `_optimumAutoSelection`'ı da günceller. |
| `CaptureCatalogSelection()` / `ApplyCatalogSelection(leaveOut)` | `boolLeaveOut` + `MosaicData.arcs` + `ColorItem.IsExcluded` senkronu. |
| `RestoreOptimumUserSelectionIfUntouched()` | Seçim hâlâ otomatik seçimse kullanıcının Optimum öncesi seçimini geri yükler. |
| `ScheduleOptimalApply()` / `ApplyOptimalKAsync(k)` | 350 ms debounce sonra `ApplyOptimalKFor(k, stock)`, `PadResult` (dolgu stoğu: `stock ?? _stockOnHand ?? _loadedStock`) + `FinishMosaic` ve en sonda `ShowPadNote`. `UseStockAware` açık ve `_stockOnHand` doluysa yeni taş sayısı da aynı stoğa uydurulur; `ShowStockAwareResult` ve `StockAwareRecheck` notu (Sheet sütunu hâlâ son Stok Kontrol sayılarını tutar). İptal edilebilir: stoğa uydurma iptal edilirse yeni taş sayısı düz `MosaicEngine.ApplyOptimalK(k)` ile kurulur ve duruma `StatusStockFitCancelled` eklenir. |
| `BeginCancellable()` → `CancellationTokenSource` | Yeni kaynak oluşturur, `_workCts`'e atar, `CanCancel` bildirir. Çağıran, `Task.Run` içinde `WorkCancellation.Token = cts.Token` atar. |
| `EndCancellable(cts)` | `_workCts` hâlâ bu kaynaksa null yapar ve `CanCancel` bildirir; kaynağı her durumda `Dispose` eder. İşin `finally` bloğunda çağrılır. |
| `IsCancellation(e)` (static) | `OperationCanceledException` ya da (`Parallel.For`'un sardığı) içindekilerin hepsi `OperationCanceledException` olan `AggregateException` ise true. `catch … when` filtrelerinde kullanılır. |
| `ClearMosaic()` | İptal edilen klasik Mos'tan sonra: `MosaicEngine.Reset`, `RenderSource` ve `OverviewBitmap` null, `MosaicDone` false, `_lastRunOptimal` ve `_mosaicMadeThisSession` false, taş varyantı geri-al/yinele yığınları boş, `EditedPixelCount` 0, `SetShowingRaw(false)`. Tuvalde yüklenen görsel yeniden görünür. |
| `DisposeIfReplaced(old, current)` | Mos / Optimum taş sayısı değişiminden sonra motorun değiştirdiği eski `exportBitmap`'i UI iş parçacığında serbest bırakır. |
| `ClearSelection()` | `HasSelection = false`: taş bilgilerinden görsel bilgilerine döner. MainWindow `OnClearSelection`'dan çağrılır. |
| `UpdateImageInfo()` (private) | Önce `ImageAccent = AverageColor(MosaicData.inputBitmap)` (görsel yoksa null). Eski `ImageInfoThumb`'ı dispose eder. `ImageInfoName`'i `ProjectService.CurrentPictureFileName`'den alır. Dosya diskte ve `MosaicData.inputBitmap` doluysa `ImageInfoFound = true` ve tür, boyut, tarih, çözünürlük, megapiksel, en-boy oranı, önizleme (`MakeThumb`) doldurulur; değilse `ImageInfoFound = false` ve metin alanları boşaltılır. `LoadImage` (görsel yüklendikten sonra) ve `OpenProjectAsync` (proje açılınca) çağırır. UI iş parçacığında, eş zamanlı çalışır. |
| `AspectText(w, h)` (private static) | Genişlik ve yüksekliği en büyük ortak bölenle sadeleştirir; iki sayı da 32 ya da daha küçükse `3:2` biçiminde, değilse `1.47:1` biçiminde (`G/Y`, iki ondalık) döndürür. |
| `MakeThumb(bmp, maxWidth)` (private static) | `SKBitmap.Resize` (doğrusal süzme + mipmap) ile en çok `maxWidth` genişliğe küçültüp Avalonia `Bitmap`'e çevirir (`ImageService.ToAvaloniaBitmap`); büyütmez. Hata olursa `null`. |
| `FormatFileSize(bytes)` (private static) | Görsel dosyası boyutu: 1 GB ve üstü `SizeGB` (iki ondalık), 1 MB ve üstü `SizeMB` (bir ondalık), altı `SizeKB` (en az 1). |
| `FormatBytes(bytes)` (static) | Dosya/bellek boyutu metni: 1 GB ve üstü `SizeGB` (bir ondalık, ör. "2.8 GB"), altı `SizeMB` (tam sayı, en az 1). Ondalık ayırıcı ve binlik ayırıcılar işletim sisteminin bölge ayarına göredir. |
| `RefreshMosaicView()` | Mozaik varsa `StoneTextureService.CreateRenderSource()` ile yeni `RenderSource` kurar ve `OverviewBitmap`'i üretir. `MosaicView` kaynak değişince bütün karoları atar. |
| `RefreshOverview()` | Yalnızca `OverviewBitmap`'i mevcut `RenderSource`'tan yeniden üretir. |
| `InvalidateStone(row, col)` | `RefreshOverview()` + `StoneInvalidated(row, col)` (renk değiştiren düzenlemeler için). |
| `FinishMosaic(result, elapsed)` | `result` varsa sanal boyutu `C·100 × R·100` yapar (`result` taş başına 1 piksel), `SetShowingRaw(false)` (mozaik yeniden gösterilir) ve `SourceViewWidth/Height`'ı bildirir; mozaik yeniden kurulduğu için `_stoneUndoStack`/`_stoneRedoStack`'i temizler ve `EditedPixelCount`'u günceller; `MosaicDone`, `FilterCatalogByUsedColors`, Optimum ise `_optimumAutoSelection` yakalar, `RefreshMosaicView`, `UsedColorInfo` ve `StatusCompleted`. |
| `StartNewContent()` → `int` | Bekleyen kaydırıcı uygulamasını (`_optimalApplyCts`) iptal eder, `_contentVersion`'ı artırıp döndürür. |
| `FixToStockAsync(config, projectName)` → stok veya null | Stok Kontrol öncesi düzeltme. Mozaik bu oturumda Mos ile üretilmemişse (`_mosaicMadeThisSession` false veya `MosaicEngine.LastRunPool` null) `StockAwareNeedsMos`; stok okunamazsa `StockAwareReadFailed` (diyalog, null döner). Kapasiteyi aşan taş yoksa `StockAwareOk` ile stoğu döndürür. Varsa (piksel düzenlemesi varsa `StockAwareEditsConfirm` onayından sonra) `StartNewContent()`, arka planda Optimum için `ApplyOptimalKFor(OptimalK, stock)`, klasik için `FixClassicMosaicToStock`; bunlardan önce `MosaicEngine.RemovePadding()` ile dolgu kaldırılır (düzeltme Mos'un ürettiği boyutla çalışır), sonra her durumda (başarı, iptal, düzeltilemedi) `PadResult(result, stock)` ile yeniden eklenir; dolgu taşı yeniden seçilir. Sürüm değiştiyse sonucu atar; klasik düzeltme olmazsa `FinishMosaic` (yeniden eklenen dolgu ekrana gelsin diye) ve `StockAwareCannotFix`. Başarıda `FinishMosaic` + `ShowStockAwareResult`. `_stockOnHand` okunan stoğa ayarlanır. İptal edilebilir: iptalde mozaik eski hâline döner (Optimum aynı taş sayısında `ApplyOptimalK(k)` ile yeniden kurulur, taş varyantları yeniden rastgele seçilir; klasikte mozaik zaten değişmemiştir), iki durumda da dolgu yeniden eklenip `FinishMosaic` + `ShowPadNote` çağrılır, `_stockCheckCancelled = true` ve null döner. |
| `StockOptions()` | `new StockAwareOptions()`; `MinUsage = 0`, yani az kullanılan taşları düşürme kuralı uygulamada **kapalıdır**. |
| `ApplyOptimalKFor(k, stock)` | İşçi iş parçacığında: `stock` null ise `MosaicEngine.ApplyOptimalK(k)`, değilse `ApplyOptimalKWithStock(k, kapasite, aile adı, StockOptions())`. |
| `PadResult(result, stock)` → `SKBitmap?` | İşçi iş parçacığında, mozaik kurulduktan sonra. `result` null ya da `UsePadding` kapalıysa `result` aynen döner (not yok). Gereken dolgu `PaddingCount(dataM3 boyutu)`; 0 ise `result` aynen döner. Dolgu taşı `MosaicEngine.ChooseFiller(ImageColours(inputBitmap), excludeUsed: true)`: Mos'un kullandığı (ayarlı) görselin renklerine en uzak, mozaikte kullanılmayan katalog taşı; stok aranmaz. Bulamazsa dolgu yapılmaz, not `PadNoStone` + diyalog. Bulursa `MosaicEngine.PadToMoulds(filler)`, `StoneTextureService.EnsureTextures`, not `PadDone` (`FillerLabel`: "#ID Kod Ad", stok varsa stok satırından, yoksa "#ID kod"). Dolgulu bitmap'i döndürür. Çağıranlar stoğu yalnızca etiket için verir (`stock ?? _loadedStock`). |
| `PadPreview` (private) · `PadScaleX/Y` | Mos'tan önce görsel dolgulu mu gösteriliyor: `UsePadding`, görsel yüklü, `!MosaicDone`, `_viewIsImage` (görünüm henüz görselin boyutunda: `LoadImage`'da true, `FinishMosaic` ve proje açılışında false; böylece sonraki bir Mos sürerken — `MosaicDone` geçici olarak false — mozaik boyutundaki görünüm bir kez daha büyüyüp kaymaz), `_previewFiller` var ve `PaddingCount(height, width) > 0`. Öyleyse panel `UpToMould(width)/width` × `UpToMould(height)/height` kat büyür (görselin kendisi, `inputBitmap`, değişmez). |
| `UpdatePadPreview()` / `RefreshPadView()` (private) | `_previewFiller = ChooseFiller(ImageColours(inputBitmap), excludeUsed: false)` (Kalıp Dolgu kapalıysa null), ardından `RefreshPadView`: `ImageDisplayWidth/Height`, `SourceViewWidth/Height`, `BitmapPixelWidth/Height`, `ZoomInfo`, `ShowPadBackground`, `PadBrush` bildirilir. Çağıranlar: `ApplyPaddingChoiceAsync` (Mos'tan önce), `UpdateDimensions` (önizleme taşı yoksa seçer, varsa yalnızca yeniler), Görsel Ayarları uygulanınca, `MosaicDone` değişince ve dolgu ekleyip kaldırınca (`RefreshPadView`). Yeni görsel yüklenince `_previewFiller` sıfırlanır. |
| `ApplyPaddingChoiceAsync()` | `UsePadding` değişince. Mozaik yoksa (`!MosaicDone`): `UpdatePadPreview()` ve (görsel varsa, iş sürmüyorsa) durum: kapatınca `PadRemoved`, açınca `PadPreview` ("Kalıp dolgusu: N dolgu taşı (#ID …); Mos dolguyu bu taşla yapar"), boyut zaten tam kalıpsa `PadNotNeeded`, taş yoksa `PadNoStone`. Mozaik varken Mos/dışa aktarma sürüyorsa hiçbir şey yapmaz (seçim o işin sonucuna ve sonraki Mos'a uygulanır). Açılınca dolgu gerekmiyorsa, kapatılınca bu oturumda dolgu eklenmemişse (`IsPadded` değil; açılmış projenin kayıtlı dolgusu dahil) döner. Sonra `IsProcessing` altında işçi iş parçacığında açıkken `PadResult(reducedBitmap, _stockOnHand ?? _loadedStock)`, kapalıyken `MosaicEngine.RemovePadding()`. Ardından `FinishMosaic` (`ElapsedTime` korunur), `DisposeIfReplaced`; durum: kapatınca `PadRemoved` ("Kalıp dolgusu kaldırıldı"), açınca dolgu notu (yapılamadıysa `PadTitle` diyaloğu). |
| `ShowPadNote()` | UI'da, yolun kendi durum metninden sonra: `_padNote`'u " · " ile duruma ekler, `_padAlert` ise `PadTitle` başlıklı diyalog gösterir; ikisini sıfırlar. |
| `InPadding(y, x)` → `bool` | Bu oturumda eklenmiş dolgunun bir taşı mı (`IsPadded` ve `y ≥ UnpaddedRows` ya da `x ≥ UnpaddedCols`). |
| `StockConfigured()` → `bool` | Stok ayarında Sheet ID var mı. |
| `FixClassicMosaicToStock(stock)` → `bool` | İşçi iş parçacığında klasik Mos sonucunu yerinde `MosaicEngine.FixCurrentMosaicToStock` ile stoğa uydurur. |
| `ShowStockAwareResult(stock, windowIfLong = false)` | `MosaicEngine.LastStockResult`'tan katalogda `StockKg`, `RemainingKg` (= `AvailableKg − kullanılan × StoneWeightKg`) ve kırmızı noktaları yazar; tam rapor (`StockAwareChanged`, `StockAwareMove`, `StockAwareAdded`, `StockAwareSmall`, `StockAwareLevel1..4`, `StockAwareShort`, `StockAwareUnknown`) `SetStockAwareReport`'a, kısa özet durum çubuğuna. Durum satırı `StatusBarFitChars` (110) karakteri aşarsa durum çubuğunda yalnızca sayılar kalır (`StockAwareShortCount`, `StockAwareUnknownCount`) ve `windowIfLong` true ise (Mos ve Stok Kontrol) tam rapor `Alert` ile pencerede açılır (`StockAwareSeeWindow`); kaydırıcıdan çağrıldığında pencere açılmaz (`StockAwareSeeTooltip`). |
| `ShowStockMarks(stock, counts)` | Okunan stoktan kırmızı nokta (kullanım > `Capacity`) ve kalan kg hesaplar. |
| `SetStockAwareReport(text)` | `_stockAwareReport` yazar ve `StockAwareTip`'i bildirir. |
| `AppendStartupStatus(message)` | Durum çubuğu `StatusReady` ise mesajı yazar, değilse mevcut uyarının (ör. atlanan katalog satırları) sonuna `" · "` ile ekler. |
| `FilterCatalogByUsedColors()` | Kataloğu yalnız `arMB[0]`'da kullanılan kodlara indirger, `PopulatePaletteAndAssigned`. Dolgu taşı (`IsPadded` iken `FillerId`) işaretlenmez: sonraki Mos onu görselin içinde kullanmasın. Atanan listesinde ise dolgu da görünür (gerçek taştır). |
| `PopulatePaletteAndAssigned()` | Seçili katalog sırasıyla `PaletteColors` ve `AssignedColors`'ı doldurur (piksel sayısı 0 olanlar atlanır, ama numara sayacı ilerler). |
| `UpdatePropTexture(codeName, y, x)` | Seçili varyant dokusunu (80×80) ve 1–16 arası küçük resimleri (44×44) yükler. |
| `AutoSelectGridColor(bmp)` | Görselin ortalama parlaklığına göre gri tonlardan derz rengi seçer (< 100 → ton 2, > 155 → ton 4, aksi 3). |
| `RecalcUsedColorInfo()` | `UsedColorInfo`'yu yeniden hesaplar. |
| `ReorderCatalogList()` | Boş gövde (yer tutucu). |

## Önemli davranışlar ve iş kuralları
- **Stok kontrol yalnız işaretler**: `CheckStockAsync` yetersiz taşlarda `rgb.stokYetersiz` ve `ColorItem.StockShort`'u true yapar (kırmızı nokta, kırmızı kalan kg); katalog seçimine dokunmaz.
- **Stok çek seçimi değiştirir**: `FetchStockAsync`, Sheet'te bulunan taşlardan stok ≤ 0 olanları hariç, > 0 olanları dahil eder; Sheet'te olmayan taşlar olduğu gibi kalır.
- **Stok işlemleri tek seferde bir tane**: `RunStockAction` `IsStockBusy` ile `CanUseStock`'u kapatır. `ClearStock*`/`AddStockAsync` onay ister; `ShowConfirm` yoksa işlem yapılmaz. Check/Clear/Add için `ScriptUrl` de gerekir, Fetch/Open için yalnız `SheetId`.
- **Google Drive** ([DriveService](../Services/DriveService.md)): Drive'a Kaydet açık dosyanın adını ve konumunu değiştirmez; Drive'daki aynı adlı proje değiştirilir (eskisi Drive çöp kutusuna gider). Drive'dan açılan proje `%LOCALAPPDATA%\mosair\drive\<proje klasörü>\<ad>` dosyasından, orijinal görsel yanında olarak açılır; bu yüzden başlık çubuğunda o adı gösterir, sonraki Ctrl+S ise her zamanki gibi `Masaüstü/mosairPROJECT`'e yazar. Drive işlemleri iptal edilemez (İptal düğmesi görünmez), ama durum çubuğu dalgasını (`IsBusy`) sürer.
- **Stok kontrol için mozaik şart**: Proje adı yoksa `StockNoProject`, `arMA[0]`'da piksel sayısı > 0 taş yoksa `StockNoMosaic` uyarısı.
- **Yeni Mos stok işaretlerini siler**: `RunMosaicAsync` tüm `stokYetersiz`, `StockShort`, `RemainingKg` değerlerini temizler (`StockKg` korunur) ve `ProjectService.ForgetWpfState()` çağırır (WPF projesinden okunan ek veri artık geçersiz). `LoadImage` da `ForgetWpfState` çağırır.
- **Optimum taban seçimi**: Mos'tan sonra katalog "yalnız kullanılan taşlar"a daraltılır. Optimum açıkken yeni Mos'ta, seçim hâlâ bu otomatik seçimle (`_optimumAutoSelection`) aynıysa önce kullanıcının asıl seçimi (`_optimumUserSelection`) geri yüklenir; böylece Optimum her seferinde daraltılmış değil tam havuzdan başlar. Kullanıcı checkbox'lara dokunduysa mevcut seçim yeni taban olur.
- `ApplyStockSelection` stok kararlarını `_optimumUserSelection`'a da yazar; stok filtresi tam havuzu daraltır.
- **Optimum kaydırıcısı**: `OptimalK` değişimi 350 ms debounce ile `ApplyOptimalKAsync` tetikler; `IsProcessing` veya `!OptimalAvailable` ise atlanır. `RunMosaicAsync` önerilen K'yı atarken `_suppressOptimalApply` ile gereksiz yeniden uygulamayı engeller.
- **`TargetColors` her Mos'ta yeniden hesaplanır**: toplam taşın %10'u (10'a yuvarlanmış), en az 2, en fazla `ceil(256/RgbIncrement)^3 - 1`. Kullanıcı değeri korunmaz.
- Aktif renk yoksa (`MosaicData.arRGB.Count == 0`) `AlertMosaicNoColors` uyarısı, işlem yapılmaz.
- **Katalog uyarısı**: Açılışta `ColorCatalogService.SkippedLines` doluysa okunamayan katalog satırları `StatusCatalogSkipped` ile durum çubuğunda gösterilir.
- **Görüntü boyutu sınırı yok**: Taş dokulu görüntü hiçbir zaman bütün olarak oluşturulmaz (`MosaicView` karolarla çizer), bu yüzden ekranda bellek sınırı ve "Değer Çok Büyük" uyarısı yoktur. Dışa aktarmada da boyut sınırı yoktur: büyük görüntüyü `MosaicExporter` parça parça yazar, seçilen görüntü kalitesi (taş başına piksel) düşürülmez; yalnızca JPEG kenar başına 65.535 px ile sınırlıdır. Mos sırasında `OutOfMemoryException` → `AlertMemoryTitle` / `AlertMemoryBody` ("cm değerini küçültün").
- N, ızgara ve ızgara rengi değişiklikleri anında uygulanır: `MosaicView` önbelleğini temizler, önce genel görünümü ve eldeki karoları gösterir, yeni karolar geldikçe keskinleşir.
- **Stoğa göre (`UseStockAware`, varsayılan kapalı)**: Mos sonucu, Sheet'ten okunan stoğa (Bizdeki eksi diğer mozaik sütunları) göre `StockAwareAssigner` ile düzeltilir; stoğu yetmeyen taşın fazlası benzer taşlara taşınır. Aynı düzeltme taş sayısı kaydırıcısında (`_stockOnHand`) ve Stok Kontrol'den önce (`FixToStockAsync`) de uygulanır; Sheet'e son sayılar tek yazımda gider. Stok okunamazsa Mos düz çalışır.
- **En az kullanım kuralı kapalı**: `StockOptions()` `MinUsage = 0` döndürür; çok az kullanılan taşlar düşürülmez. Kural yalnız `StockCompareRunner` test aracında `MOSAIR_MINUSAGE=1` ile açılır.
- **Stok yükleme**: Açılışta (`LoadStockOnStartupAsync`) ve her görsel/proje yüklemesinde stok arka planda okunur; tooltip'lerde kg görünür, seçim değişmez.
- **Geç biten arka plan işleri**: `LoadImage`, `OpenProjectAsync`, `RunMosaicAsync` ve `FixToStockAsync` `StartNewContent()` çağırır: bekleyen kaydırıcı uygulaması iptal edilir, `_contentVersion` artar. `ApplyOptimalKAsync`, `RunMosaicAsync`, `FixToStockAsync`, `OpenProjectAsync`'in arka plan okuması ve doku yüklemesi ile `SaveProjectAsync` (yalnızca `CurrentFileName` için) başlarken sürümü alır; bittiğinde sürüm değişmişse sonucu atar, ekrana yazmaz. Büyük bir Mos'un sonucu artık ayrı bir RS üretimi yüzünden kaybolmaz. Böylece başka bir görsel açıldıktan sonra eski mozaik geri gelmez.
- **Undo/redo sırası**: Önce taş varyantı yığını (`_stoneUndoStack`/`_stoneRedoStack`) boşaltılır, o boşsa `PixelEditService` piksel düzenlemeleri geri alınır/yinelenir. Yeni `SelectStone` redo yığınını temizler.
- **Genişlik 1.2 cm katı**: `UpdateDimensions` görselin genişliğini (`_imageWidthCm`) `round(·10/12)·1.2`'ye (en az 2 taş = 2.4 cm) çeker; bir taş = 1 kaynak piksel olduğundan görsel genişliğini aşamaz.
- **Kalıp koordinatı** (Özellikler paneli): 13 sütun × 26 satırlık bloklara göre hesaplanır; `xi = x mod 13`, `yi = y mod 26`, tek numaralı blok sütunlarında `yi = 25 - yi` (yılan sıralama). Not: kalıp **sayısı** (`MosaicEngine.CalculateDimensions`) ise genişlik ve yüksekliği 26'şar taşa bölerek hesaplanır; iki kural WPF'ten olduğu gibi alınmıştır.
- **Dışa aktarma** ekrandakiyle aynı çiziciyi (`MosaicRenderSource.RenderRegion`) kullanır; dosyayı `MosaicExporter` yazar. UI iş parçacığında `RenderSource.WithStoneSnapshot()` ile taş renkleri ve varyantlarının kopyası alınır; dışa aktarma sürerken yapılan piksel düzenlemeleri dosyaya girmez (20 m'de kopya ~20 MB). Görüntü kalitesini kullanıcı dışa aktarma listesinde seçer (`ExportQualities`); `Ctrl/⌘+E` `DefaultExportQuality`'yi (40) kullanır; ekrandaki görünüm dosya boyutunu etkilemez. Listedeki tahmini dosya boyutları, liste açılınca `RefreshExportEstimatesAsync` ile arka planda hesaplanır ve mozaik, ızgara ya da derz rengi değişene kadar saklanır.
- `ColorItem.ThumbnailBitmap` / `TooltipBitmap` tembel yüklenir ve önbelleğe alınır; ilk tooltip açılışında disk erişimi olur.
- `OpenProjectAsync` önce taş renklerini gösterir (`StatusGeneratingRs`), dokular yüklenince taş dokulu görünüme geçer. Açılışta taş varyantı geri-al yığınları temizlenir, `IsPixelEditActive`/`IsSourcePixelMode`/`IsTargetPixelMode` `PixelEditService` ile eşitlenir (`ProjectService.ApplyProject` düzenleme modunu kapatır) ve `_lastRunOptimal = false` yapılarak Optimum kaydırıcısı gizlenir (projede Optimum analizi yoktur).
- **Mozaik yeniden kurulunca** (yeni Mos veya Optimum taş sayısı değişimi) `FinishMosaic` taş varyantı geri-al/yinele geçmişini siler; eski kayıtlar yeni mozaiğe uygulanmaz.
- **Tam kalıba tamamlama** ([MosaicEngine](../Services/MosaicEngine.md#tam-kalıba-tamamlama-dolgu)): **Kalıp Dolgu** (`UsePadding`) açıkken Mos, Optimum taş sayısı değişimi ve stok düzeltmesinden sonra mozaik sağa ve alta 26 taşlık tam kalıba kadar tek bir dolgu taşıyla doldurulur; düğme/menü açılıp kapatılınca ekrandaki mozaiğe hemen uygulanır (`ApplyPaddingChoiceAsync`). WPF, kare olmayan dolgulu bir projeyi açarken kalıp satırlarını görselin oranından hesapladığı için dosyadakinden az sayabilir. Dolgu taşları gerçek taştır: Stok Kontrol'ün yazdığı adetlere (`arMA[0]`), atanan listesine ve kalıp bilgisine girer. Dolgu taşı görselin renklerine en uzak katalog taşıdır; stok aranmaz. Mos'tan önce düğme görseli dolgulu gösterir (`PadPreview`, `PadBrush`).
- **İptal** (durum çubuğundaki İptal düğmesi ya da Esc → `CancelWork`): İptal edilebilen işler `BeginCancellable`/`EndCancellable` arasında çalışır ve `Task.Run` içinde [`WorkCancellation.Token`](../Services/WorkCancellation.md)'ı atar. Belirteç `Parallel.For`'a da geçer; motorun uzun döngüleri kontrol noktalarında `OperationCanceledException` fırlatır. Sonuç işe göre değişir:
  - **Mos, Optimum:** Analiz (`OptimalPaletteService.Analyze`) paylaşılan veriyi değiştirmeden önce çalışır (`MosaicEngine.RunOptimal`). İptal edilince önceki mozaik olduğu gibi kalır: `_lastRunOptimal` eski değerine döner, `MosaicDone = true`, `FilterCatalogByUsedColors` yeniden uygulanır, durum `StatusMosCancelled`. Önceden mozaik yoksa yüklenen görsel kalır (yine `StatusMosCancelled`).
  - **Mos, klasik:** `RunM3` paylaşılan veriyi giderek değiştirir (`classicStarted`). Yarım mozaik güvenilmez; `ClearMosaic` ile kaldırılır, yüklenen görsel yeniden gösterilir, durum `StatusMosCancelledCleared`.
  - **Stoğa göre düzeltme, Mos içinde (Optimum veya klasik) ya da taş sayısı kaydırıcısında:** Mozaik düzeltmesiz kalır (Optimum aynı taş sayısında düz `ApplyOptimalK` ile kurulur; klasik mozaik zaten tamdır ve değişmemiştir). Durum satırına `StatusStockFitCancelled` eklenir; kırmızı noktalar ve rapor yazılmaz.
  - **Stok Kontrol içindeki düzeltme:** Mozaik eski hâline döner (Optimum aynı taş sayısında yeniden kurulur, taş varyantları yeniden rastgele seçilir) ve tabloya hiçbir şey yazılmaz: `StatusStockCheckCancelled`.
  - **Dışa aktarma:** Durur, yarım dosya silinir: `StatusExportCancelled`.
  - **İptal edilemeyenler:** Stok tablosu işlemleri (Stok Çek, Stok Kontrol'ün tabloya yazma adımı, Sil, Ekle) ve proje açma. Bunlar sürerken düğme görünmez.
  - Ölçülen durma süreleri (tıklamadan işin durmasına kadar): Optimum 4000×4000 taş 8 ms (önceki mozaik değişmeden), klasik 2000×2000 125 ms, stoğa göre düzeltme 1200×1200 510 ms, akışla yazılan 20 m PNG 34 ms (yarım dosya silindi). Kontrol noktaları sonucu değiştirmez (Optimum ve klasik için karşılaştırma aracıyla aynı çıktı doğrulandı).
  - Son kontrol noktasından sonra basılan iptal etkisizdir: iş normal biter ve "İptal ediliyor..." yazısının yerine normal sonuç yazılır.

## Dikkat / bilinen sınırlamalar
- `OpenProjectAsync`, `MosaicEngine.LastOptimalResult`'ı sıfırlamaz; ancak `_lastRunOptimal = false` yaptığı için `OptimalAvailable` false olur ve kaydırıcı gizlenir. Optimum seçim dizileri ve stok işaretleri `OpenProjectAsync`'te sıfırlanmaz.
- `MinZoomLevel` en az 0,001'dir: çok büyük bir mozaikte sanal genişlik `C·100` çok büyür (ör. 1667 taş × 100 = 166.700 px), 0,001 ile 167 px olur, yani Ekrana Sığdır yine çalışır.
- `StatusGeneratingRs` ("Açıldı, taş dokuları yükleniyor...") proje açılırken doku yüklemesi sırasında gösterilir.
- `OpenProjectAsync`'te arka plandaki okuma/çözme/görsel yükleme hataları yakalanır ve "açılamadı" olarak gösterilir; `ApplyProject` sonrası UI iş parçacığındaki adımlarda oluşan istisnalar yakalanmaz. Doku yükleme hatası sessizce yutulur; görünüm taş renkleriyle kalır.
- Proje okunurken (`IsProcessing`) önceki içerik ekranda kalır; Mos ve dışa aktarma kilitlidir, kaydetme önceki mozaiği kaydeder (açılan proje henüz uygulanmamıştır).
- `FixToStockAsync` başındaki kod yorumu hâlâ "çok az kullanılan taşlar düşürülür" der; en az kullanım kuralı kapalı olduğundan bu olmaz (`StockAwareSmall` satırı raporda çıkmaz).
- Optimum Mos iptal edildiğinde önceki mozaik kalır, ama `RunMosaicAsync`'in başında temizlenen şeyler geri gelmez: önceki mozaiğin kırmızı noktaları ve "kalan" kg'ı, stoğa göre raporu (`_stockAwareReport`), `_stockOnHand` ve `ProjectService.ForgetWpfState` ile unutulan WPF verisi. Katalog seçimi de Optimum'un tabanına (`_optimumUserSelection`) dönmüş olabilir; `FilterCatalogByUsedColors` yalnızca listeyi kullanılan taşlara indirger.
- Taş sayısı kaydırıcısında stoğa uydurma iptal edilince önceki taş sayısının kırmızı noktaları ve kalan kg'ı katalogda kalır; yeni (düzeltmesiz) mozaiğe ait değildir.
- `ApplyOptimalKAsync` yalnızca `UseStockAware` açık ve `_stockOnHand` doluyken `BeginCancellable` çağırır; stoksuz taş sayısı değişiminde kayıtsız bir `CancellationTokenSource` kullanılır, İptal düğmesi görünmez (`MosaicEngine.ApplyOptimalK`'da kontrol noktası yoktur). Optimum Mos'ta analizden sonraki kısım (`ApplyOptimalK`, doku hazırlığı) ve Mos başındaki stok okuma durdurulamaz; okuma sırasında basılan iptal, analizin ilk kontrol noktasında etkili olur.
- `FixToStockAsync` açılmış bir projede çalışmaz (`StockAwareNeedsMos`): taş çözünürlüğünde kaynak görsel verisi yoktur; önce Mos gerekir.
- Görsel her kayıtta gönderilir; script (2026-10-08 sürümü) Drive'daki görsel farklıysa onu yenisiyle değiştirir, aynıysa dokunmaz. Daha eski bir dağıtım var olan görseli değiştirmez. Script'in eski bir dağıtımı proje klasörünü ve görseli tanımaz (proje kökte durur, görsel gitmez); güncel script için [DriveService](../Services/DriveService.md#apps-script-assetsmosair-drivegs).
- `SetSourceFromCatalog`'daki `"source: "` metni yerelleştirilmemiş.
- `ReorderCatalogList` ve `OnImagePointerMoved` boş; `PixelCoordInfo`, `PixelDetailInfo`, `PixelColorInfo`, `PixelScaleInfo`, `DimensionInfo` XAML'e bağlı değil.
- `OnImagePressed` x ve y için aynı ölçeği (`imageControlWidth / dataM3 sütun sayısı`) kullanır; kare taş varsayar.
- Dolgu alanında düzenleme engeli yalnızca bu oturumda dolgu eklenmiş mozaikte çalışır; açılan dolgulu bir projede dolgu taşları sıradan taşlar gibi düzenlenebilir.
- `ColorItem.ColorBrush`, `AssignedItem.RowBrush/TextBrush` her erişimde yeni fırça üretir (veya döndürür); büyük listelerde gereksiz tahsis.
- `UpdateDimensions` `Convert.ToInt32` kullandığı için .5 durumlarında banker's rounding uygular.
- `RefreshExportEstimatesAsync` çalışırken önbellek anahtarı değişirse (ör. ızgara açılıp kapanırsa) çalışan döngü durur, yeni çağrı da `_estimatingExport` yüzünden hemen döner; eksik tahminler liste yeniden açılana kadar "hesaplanıyor…" kalır.
- Bellek kontrolü o an boş belleği kullanır (`MosaicExporter.FreeMemoryBytes()` = toplam − sistemin kullandığı).
- Kalite tahminleri sürerken mozaik, ızgara ya da derz rengi değişirse devam eden hesap bitince baştan başlar (`_estimateExportAgain`); menü "hesaplanıyor…"da takılı kalmaz. Menü açılırken önce eski tahminler atılır, sonra metinler yazılır.

## İlgili dosyalar
- [MainWindow](../MainWindow.md) — view ve olay işleyicileri
- [ARAYUZ](../../ARAYUZ.md) — kullanıcı arayüzü açıklaması
- [MosaicEngine](../Services/MosaicEngine.md), [OptimalPaletteService](../Services/OptimalPaletteService.md)
- [StoneTextureService](../Services/StoneTextureService.md), [MosaicRenderSource](../Services/MosaicRenderSource.md), [MosaicExporter](../Services/MosaicExporter.md), [ImageService](../Services/ImageService.md)
- [MosaicView](../Controls/MosaicView.md), [GridOverlay](../Controls/GridOverlay.md)
- [ColorCatalogService](../Services/ColorCatalogService.md), [StockSheetService](../Services/StockSheetService.md), [DriveService](../Services/DriveService.md)
- [DriveSettingsDialog](../Controls/DriveSettingsDialog.md), [DriveOpenDialog](../Controls/DriveOpenDialog.md)
- [ProjectService](../Services/ProjectService.md), [PixelEditService](../Services/PixelEditService.md), [Loc](../Services/Loc.md), [NewImageWatcher](../Services/NewImageWatcher.md)
- [MosaicData](../Models/MosaicData.md), [Rgb](../Models/Rgb.md), [Region](../Models/Region.md)
- [StockAwareAssigner](../Services/StockAwareAssigner.md), [Stoğa göre raporu](../../STOGA_GORE_RAPOR.md)
- [GridOverlay](../Controls/GridOverlay.md), [StockSettingsDialog](../Controls/StockSettingsDialog.md), [AlertDialog](../Controls/AlertDialog.md), [ConfirmDialog](../Controls/ConfirmDialog.md)
