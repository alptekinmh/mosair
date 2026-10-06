# MainViewModel
> Kaynak: `mosair/ViewModels/MainViewModel.cs` · Güncelleme: 2026-10-06

## Amaç
Ana pencerenin tek view model'idir: görsel yükleme, mozaikleme (klasik M3 ve Optimum), RS (taş dokulu) bitmap üretimi, zoom/navigator, renk kataloğu seçimi, piksel düzenleme, taş varyantı seçimi, proje aç/kaydet, dışa aktarma ve Google Sheet stok işlemlerinin (stoğa göre mozaik, "Stoğa göre" dahil) UI tarafındaki durumunu tutar. Ağır işi servislere (`MosaicEngine`, `StoneTextureService`, `ProjectService`, `StockSheetService`, `PixelEditService`) devreder; kendisi durum, iş sırası ve kullanıcıya gösterilen metinlerden sorumludur. Dosyada ayrıca katalog/palet/atama listelerinin satır modelleri (`ColorItem`, `PaletteItem`, `AssignedItem`, `StoneThumbItem`) bulunur.

## Nerede kullanılır
| Dosya | Kullanım |
|---|---|
| `mosair/MainWindow.axaml` | `DataContext` olarak bağlanır; `CatalogColors`, `PaletteColors`, `AssignedColors`, `PropStoneThumbs`, durum çubuğu, Optimum kaydırıcısı, stok menüsü vb. binding'ler. `ColorItem` alanları katalog satırı ve tooltip'te (`StockShort`, `StockKgText`, `RemainingText`, `TooltipBitmap`, `ThumbnailBitmap`), `AssignedItem` alanları "Atanan" tablosunda kullanılır. |
| `mosair/MainWindow.axaml.cs` | `_vm` alanı üzerinden metotları çağırır (`LoadImage`, `RunMosaicAsync`, `OpenProject`, `SaveProject`, `ExportImageAsync`, stok metotları, `UndoPixelEdit`/`RedoPixelEdit`, `SelectStone`, `FitToWindow`, `UpdateNavigator` ...) ve `ShowAlert`, `ShowConfirm`, `ShowStockSettings`, `OpenUrl` delegelerini atar. |

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
| `OpenUrl` | `Func<string, Task>?` | Tarayıcıda URL açma. |

**Görsel & zoom**

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `DisplayBitmap` | `Bitmap?` | null | Ekrandaki görsel (orijinal, sonra RS/export bitmap). |
| `ZoomLevel` | `double` | 1 | `[MinZoomLevel, 20]` aralığına kırpılır; `ImageDisplayWidth/Height` ve `ZoomInfo`'yu bildirir. |
| `MinZoomLevel` | `double` | 1.0 | `FitToWindow`/`AdjustZoomForBitmapChange` sığdırma zoom'una (en az 0.01) ayarlar. |
| `ImageDisplayWidth`, `ImageDisplayHeight` | `double` | — | Bitmap piksel boyutu × zoom. |
| `BitmapPixelWidth`, `BitmapPixelHeight` | `int` | 1 | Gösterilen bitmap'in piksel boyutu. |
| `StoneColumns`, `StoneRows` | `int` | — | `MosaicEngine.width` / `MosaicEngine.height` (GridOverlay için). |
| `ZoomInfo` | `string` | — | `"N={zoom}  {w}x{h}"` (etiket "N" olsa da zoom değeridir). |
| `NavViewLeft`, `NavViewTop`, `NavViewWidth`, `NavViewHeight` | `double` | 0 | Navigator küçük resmindeki görünüm dikdörtgeni. |
| `ImageLoaded` | `bool` | false | Görsel yüklü mü; `CanRunMosaic`'i etkiler. |

**Mozaik ayarları**

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `WidthCm` | `double` | 93.6 | Mozaik genişliği; 0.01'den küçük değişiklik yok sayılır. `UpdateDimensions` 1.2 cm katına yuvarlar. |
| `HeightCm` | `double` | 0 | `MosaicEngine.CalculateDimensions` sonucundan (salt okunur). |
| `TargetColors` | `int` | 15 | M3 renk sayısı; `RunMosaicAsync` her çalışmada yeniden hesaplar. |
| `RgbIncrement` | `int` | 10 | M3 palet adımı. |
| `UseLab`, `UseAverage` | `bool` | false | M3 seçenekleri. |
| `SelectedInterpolation` | `InterpolationMethod` | `Area` | Ölçekleme yöntemi; `InterpolationMethods` (statik) tüm enum değerleri. |
| `ShowGrid` | `bool` | true | Derz/ızgara; set edilince `RegenerateRS`. |
| `StonePixelSize` | `int` | 40 | Taş başına piksel (N); `MosaicData.N`'yi de yazar ve `RegenerateRS` çağırır. |
| `NWarningVisible` | `bool` | false | N > 40 performans uyarısı görünürlüğü. |
| `GridColor` | `Color` | (128,128,128) | Derz rengi; ızgara açıksa `RegenerateRS`, değilse `RedrawOverlay`. |
| `GridColorPresets` | `Color[]` (statik) | 12 renk | Ana renk paleti. |
| `GridColorShades` | `Color[]` | boş | `SelectMainColor` ile üretilen 7 ton (siyah → renk → beyaz). |

**İşlem durumu & komut uygunluğu**

| Ad | Tip | Açıklama |
|---|---|---|
| `Progress` | `int` | İlerleme yüzdesi. |
| `IsProcessing`, `IsExporting` | `bool` | Değişince `CanRunMosaic` ve `CanExport` bildirilir. |
| `MosaicDone` | `bool` | Değişince `CanExport` ve `OptimalAvailable` bildirilir. |
| `CanRunMosaic` | `bool` | `ImageLoaded && !IsProcessing && !IsExporting`. |
| `CanExport` | `bool` | `MosaicDone && !IsProcessing && !IsExporting`. |

**Optimum**

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `UseOptimal` | `bool` | true | Mos'un Optimum (`MosaicEngine.RunOptimal`) mi M3 (`RunM3`) mü çalıştıracağı. |
| `OptimalAvailable` | `bool` | — | `MosaicDone && _lastRunOptimal`; taş sayısı kaydırıcısını açar. |
| `OptimalK` | `int` | 0 | Seçili taş sayısı; değişince `UpdateOptimalInfo` ve (bastırılmadıysa) `ScheduleOptimalApply`. |
| `OptimalKMax` | `int` | 1 | `LastOptimalResult.CandidateCount`. |
| `OptimalKSuggested` | `int` | 0 | `LastOptimalResult.KOptimal`. |
| `OptimalInfo` | `string` | `""` | `OptimumInfoFmt` ile önerilen taş sayısı. |
| `_optimumUserSelection` | `bool[]?` | null | Son Optimum çalıştırmasından önceki kullanıcı katalog seçimi (`boolLeaveOut` dizisi). |
| `_optimumAutoSelection` | `bool[]?` | null | Optimum sonrası uygulamanın uyguladığı "yalnız kullanılan taşlar" seçimi. |
| `_suppressOptimalApply` | `bool` | false | `RunMosaicAsync` içinde `OptimalK` atanırken yeniden uygulamayı engeller. |

**Katalog & stok**

| Ad | Tip | Açıklama |
|---|---|---|
| `CatalogColors` | `ObservableCollection<ColorItem>` | Tüm katalog. |
| `PaletteColors` | `ObservableCollection<PaletteItem>` | Mozaikte kullanılan renkler. |
| `AssignedColors` | `ObservableCollection<AssignedItem>` | Kullanılan taşlar ve piksel sayıları. |
| `IsStockBusy` | `bool` | Stok işlemi sürerken true (private set). |
| `CanUseStock` | `bool` | `!IsStockBusy`; stok menü/butonlarını kilitler. |
| `UseStockAware` | `bool` | Varsayılan `true` ("Stoğa göre" onay kutusu). Kalıcı saklanmaz; her açılışta açık başlar. Değişince `StockAwareTip` de bildirilir. |
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
| `HasSelection` | `bool` | Bir piksel seçili mi. |
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
| `DimensionInfo`, `DimensionSize`, `DimensionArea` | `string` | Boyut/alan metinleri. |
| `StoneInfo`, `MouldInfo`, `OriginalInfo` | `string` | `InfoStones`, `InfoMoulds`, `InfoOriginal`. |
| `PixelCoordInfo`, `PixelDetailInfo`, `PixelColorInfo`, `PixelScaleInfo` | `string` | Piksel bilgisi; `PixelDetailInfo`/`PixelColorInfo` `OnImagePressed`'de yazılır, hiçbiri XAML'e bağlı değil. |

**Dil**: Constructor `Loc.Instance.PropertyChanged`'e abone olur; `Loc.Lang` değişince `UpdateOptimalInfo` çağrılır. Diğer metinler `RefreshLocalized` ile yenilenir.

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `MainViewModel()` | `ColorCatalogService.LoadDefaultCatalog` + `RefreshCatalogList`; hata `StatusText`'e yazılır. Dil aboneliği. | MainWindow |
| `LoadImage(path)` | Yeni görsel: `StartNewContent()`, `ProjectService` dosya adlarını ayarlar, `ForgetWpfState`, `_stockOnHand`/`_mosaicMadeThisSession`/stok raporunu sıfırlar, `MosaicEngine.Reset`, mozaik/düzenleme/undo durumunu ve `StoneTextureService`'i sıfırlar, görseli yükler, zoom = 2, `UpdateDimensions`, `AutoSelectGridColor`; başarılıysa `_loadedStock = null` ve arka planda `RefreshStockAsync()`. | MainWindow (menü, sürükle-bırak) |
| `UpdateDimensions()` | Genişliği 1.2 cm katına yuvarlar (en az 2 taş), görsel genişliğinden (1 px = 1 taş) büyükse kırpar ve `AlertResolutionTitle` uyarısı verir; boyut/taş/kalıp metinlerini günceller. | MainWindow, `LoadImage`, `OpenProject`, `RefreshLocalized` |
| `RunMosaicAsync()` | Mos: `StartNewContent()`, stok işaretlerini temizler, Optimum taban seçimini hazırlar, `SetActiveColors`, `TargetColors` hesaplar, arka planda `RunOptimal` veya `RunM3` + `BuildRsBitmap`, sonra Optimum kaydırıcısını kurar ve `FinishMosaic`. `UseStockAware` açıksa ve stok ayarında Sheet ID varsa sonuç Mos içinde stoğa uydurulur (Sheet ID yoksa düz Mos, not gösterilmez): `_loadedStock` yoksa önce `RefreshStockAsync()` beklenir; Optimum'da `RunOptimal(prepareTextures: stock == null)` ardından `ApplyOptimalKFor(KOptimal, stock)`, klasikte `RunM3` ardından `FixClassicMosaicToStock(stock)`. Başarılıysa `_stockOnHand = stock` ve `ShowStockAwareResult`; stok okunamadıysa düz Mos + `StockAwareNoStock` notu, klasik düzeltme yapılamadıysa `StockAwareCannotFix` uyarısı. Bitince sürüm değişmişse sonuç atılır; değişmemişse `_mosaicMadeThisSession = result != null`. | MainWindow |
| `SaveProject(filePath)` → `bool` | `ProjectService.Save` (genişlik, zoom, ızgara, derz rengi, interpolasyon). Hata olursa `ReportSaveFailed` çağırır ve `false` döner. | MainWindow |
| `ReportSaveFailed(ex)` | Durum çubuğuna hata yazar, `AlertSaveFailed` uyarısını gösterir. | `SaveProject`, MainWindow |
| `OpenProject(filePath)` (`async void`) | `StartNewContent()`, `ProjectService.Open`, kaynak görseli varsa yükler, ayarları uygular, `dataM3`'ten export bitmap üretir, kataloğu kullanılan taşlara filtreler, ardından RS bitmap'i arka planda üretir ve `FitToWindow`. `_stockOnHand`, `_mosaicMadeThisSession` ve stok raporu sıfırlanır; sonunda `_loadedStock = null` ve `RefreshStockAsync()`. | MainWindow |
| `ExportImageAsync(path)` | RS (yoksa export) bitmap'in kopyasını `.jpg/.jpeg` → JPEG, diğerleri PNG olarak arka planda yazar. | MainWindow |
| `RefreshLocalized()` | Dil değişiminden sonra yerelleştirilmiş metinleri yeniler, `StatusText` = `StatusReady`. | MainWindow |
| `FitToWindow(w, h)` | Viewport'u hatırlar, sığdırma zoom'unu hesaplar (kenar payı 16 px), `MinZoomLevel` ve `ZoomLevel`'i ayarlar (en fazla 10). | MainWindow, `OpenProject` |
| `UpdateNavigator(viewportW, viewportH, offsetX, offsetY, navSize)` | `NavView*` dikdörtgenini hesaplar. | MainWindow |
| `SyncColorExclusion(item)` | `ColorCatalogService.SetLeaveOut` ile tek taşın hariç durumunu modele yazar. | MainWindow (checkbox) |
| `SetAllColors(excluded)` | Tüm kataloğu seçer / bırakır, `SetActiveColors`. | MainWindow |
| `SelectMainColor(c)` | `GridColorShades`'i 7 tonla doldurur. | MainWindow, `AutoSelectGridColor` |
| `DismissNWarning()` | Performans uyarısını kapatır. | MainWindow |
| `OnImagePressed(...)` | Tıklanan taşı bulur (ID önce `drl.dat`, sonra `arMA[0]`, sonra `arRGB` renk eşleşmesi), kalıp koordinatını hesaplar, özellikler panelini doldurur; orta tuş piksel düzenleme modunu değiştirir; düzenleme açıksa kaynak/hedef piksel adımını yürütür. | MainWindow |
| `OnImagePointerMoved(...)` | Boş gövde. | MainWindow |
| `SelectStone(stoneIndex)` | Seçili pikselin `MosaicData.arn` varyantını değiştirir, undo yığınına ekler, redo'yu temizler, `RegenerateRS`. | MainWindow (varyant tıklama) |
| `TogglePixelEditMode()` | `PixelEditService.TogglePixelEditMode` ve durum senkronu (mozaik yoksa yok sayılır). | MainWindow, `OnImagePressed` |
| `SetSourceFromCatalog(item)` | Katalog taşını piksel düzenleme kaynağı yapar, hedef moda geçer. | MainWindow |
| `UndoPixelEdit()` / `RedoPixelEdit()` | Önce taş varyantı yığını, boşsa `PixelEditService.UndoLastEdit` / `RedoLastEdit`. | MainWindow (kısayollar) |
| `OpenStockSheetAsync()` | Ayarlı Sheet'i `StockSheetService.SheetUrl` ile tarayıcıda açar. | MainWindow |
| `ConfigureStockAsync()` | Stok ayar diyaloğunu açar, `StockSheetService.SaveConfig`. | MainWindow |
| `LoadStockOnStartupAsync()` | `RefreshStockAsync()` çağırır. | MainWindow (`Opened`) |
| `RefreshStockAsync()` | `SheetId` ayarlı değilse hiçbir şey yapmaz. Ayarlı tablodan proje adıyla stoğu okur (`FetchOnHandAsync`; bu mozaiğin kendi sütunu diğer mozaiklerin payına katılmaz), `_loadedStock`'a ve eşleşen `ColorItem.StockKg`'ya yazar; seçim ve kırmızı noktalar değişmez. Okuma sürerken başka görsel yüklendiyse (`StockProjectName()` değiştiyse) sonucu atar. Sonuç (`StockLoadedOnStart`) veya hata (`StockLoadOnStartFailed`) `AppendStartupStatus` ile durum çubuğuna not olarak eklenir. | `LoadStockOnStartupAsync`, `LoadImage`, `OpenProject`, `RunMosaicAsync` |
| `FetchStockAsync(markOnly = false)` | Eldeki stoğu çeker (`StockSheetService.FetchStockAsync`), `StockKg` yazar. `markOnly` false ("Stok Çek"): Sheet'te stok ≤ 0 olan taşları hariç tutar, > 0 olanları dahil eder (`ApplyStockSelection`), durum `StockFetched`. `markOnly` true (yalnız işaretle): seçime dokunmaz, stok ≤ 0 olan taşlara yalnızca kırmızı nokta (`stokYetersiz`/`StockShort`), durum `StockFetchedMarked`. | MainWindow (`OnStockFetch`, `OnStockFetchMark`) |
| `CheckStockAsync()` | `UseStockAware` açıksa önce `FixToStockAsync` ile mozaiği stoğa uydurur; sonra mozaikteki taş sayılarını (`arMA[0]`) proje adıyla Sheet'e tek seferde gönderir, yetersiz taşları işaretler, `RemainingKg`/`StockKg` yazar. Düzeltme çalıştıysa kırmızı noktalar ve kalan kg, Sheet'ten geri okunan "Tahmini Kalan" (Google henüz yeniden hesaplamamış olabilir) yerine düzeltmede okunan stoktan (`ShowStockMarks`) gelir ve durum `StockAwareWritten` / `StockCountsWritten` ile biter. Düzeltme çalışamadıysa (neden diyalogda gösterilir) normal kontrol yine yapılır (`StockCheckOk` / `StockCheckShort`). | MainWindow |
| `ClearStockOneAsync()` / `ClearStockAllAsync()` | Onaydan sonra bu projenin / tüm projelerin sütununu temizler. | MainWindow |
| `AddStockAsync()` | Onaydan sonra `StockSheetService.AddStockAsync`. | MainWindow |

**Önemli private metotlar**

| Metot | Ne yapar |
|---|---|
| `RunStockAction(action, doneMessage)` | `IsStockBusy` kilidi; hata → `StatusError` + `StockErrFmt` uyarısı. |
| `TryGetStockConfig(needScript, out config)` | `SheetId` (gerekirse `ScriptUrl`) yoksa `StockNotConfigured` uyarısı, false. |
| `StockProjectName()` | Sheet sütun başlığı: önce resim dosya adı, yoksa proje dosya adı (uzantısız). |
| `ApplyStockSelection(leaveOutForId)` | Stok kararını (true/false/null) kataloğa ve `_optimumUserSelection`'a uygular; seçim önceden "otomatik" seçimle aynıysa `_optimumAutoSelection`'ı da günceller. |
| `CaptureCatalogSelection()` / `ApplyCatalogSelection(leaveOut)` | `boolLeaveOut` + `MosaicData.arcs` + `ColorItem.IsExcluded` senkronu. |
| `RestoreOptimumUserSelectionIfUntouched()` | Seçim hâlâ otomatik seçimse kullanıcının Optimum öncesi seçimini geri yükler. |
| `ScheduleOptimalApply()` / `ApplyOptimalKAsync(k)` | 350 ms debounce sonra `ApplyOptimalKFor(k, stock)` + `BuildRsBitmap` + `FinishMosaic`. `UseStockAware` açık ve `_stockOnHand` doluysa yeni taş sayısı da aynı stoğa uydurulur; `ShowStockAwareResult` ve `StockAwareRecheck` notu (Sheet sütunu hâlâ son Stok Kontrol sayılarını tutar). |
| `DisposeIfReplaced(old, current)` | Mos / Optimum taş sayısı değişiminden sonra motorun değiştirdiği eski `exportBitmap`'i UI iş parçacığında serbest bırakır. |
| `BuildRsBitmap()` | Derz genişliği `max(1, N/11)` (ızgara açıksa), dokuları `N - gw`'ye boyutlar, `StoneTextureService.GenerateRSBitmap`. |
| `FinishMosaic(result, rsBmp, elapsed)` | RS bitmap'i `MosaicData.rsBitmap`'e koyar; RS üretilemediyse ve `result` varsa eski `rsBitmap`'i dispose edip null yapar (önceki mozaik görünmesin) ve piksel boyutunu `result`'tan alır; mozaik yeniden kurulduğu için `_stoneUndoStack`/`_stoneRedoStack`'i temizler ve `EditedPixelCount`'u günceller; `MosaicDone`, `FilterCatalogByUsedColors`, Optimum ise `_optimumAutoSelection` yakalar, `UsedColorInfo` ve `StatusCompleted`. |
| `StartNewContent()` → `int` | Bekleyen/süren `RegenerateRS`'i (`_rsRegenerateCts`) ve bekleyen kaydırıcı uygulamasını (`_optimalApplyCts`) iptal eder, `_contentVersion`'ı artırıp döndürür. |
| `FixToStockAsync(config, projectName)` → stok veya null | Stok Kontrol öncesi düzeltme. Mozaik bu oturumda Mos ile üretilmemişse (`_mosaicMadeThisSession` false veya `MosaicEngine.LastRunPool` null) `StockAwareNeedsMos`; stok okunamazsa `StockAwareReadFailed` (diyalog, null döner). Kapasiteyi aşan taş yoksa `StockAwareOk` ile stoğu döndürür. Varsa (piksel düzenlemesi varsa `StockAwareEditsConfirm` onayından sonra) `StartNewContent()`, arka planda Optimum için `ApplyOptimalKFor(OptimalK, stock)`, klasik için `FixClassicMosaicToStock`, ardından `BuildRsBitmap`; sürüm değiştiyse sonucu atar; klasik düzeltme olmazsa `StockAwareCannotFix`. Başarıda `FinishMosaic` + `ShowStockAwareResult`. `_stockOnHand` okunan stoğa ayarlanır. |
| `StockOptions()` | `new StockAwareOptions()`; `MinUsage = 0`, yani az kullanılan taşları düşürme kuralı uygulamada **kapalıdır**. |
| `ApplyOptimalKFor(k, stock)` | İşçi iş parçacığında: `stock` null ise `MosaicEngine.ApplyOptimalK(k)`, değilse `ApplyOptimalKWithStock(k, kapasite, aile adı, StockOptions())`. |
| `FixClassicMosaicToStock(stock)` → `bool` | İşçi iş parçacığında klasik Mos sonucunu yerinde `MosaicEngine.FixCurrentMosaicToStock` ile stoğa uydurur. |
| `ShowStockAwareResult(stock)` | `MosaicEngine.LastStockResult`'tan katalogda `StockKg`, `RemainingKg` (= `AvailableKg − kullanılan × StoneWeightKg`) ve kırmızı noktaları yazar; tam rapor (`StockAwareChanged`, `StockAwareMove`, `StockAwareAdded`, `StockAwareSmall`, `StockAwareLevel1..4`, `StockAwareShort`, `StockAwareUnknown`) `SetStockAwareReport`'a, kısa özet durum çubuğuna. |
| `ShowStockMarks(stock, counts)` | Okunan stoktan kırmızı nokta (kullanım > `Capacity`) ve kalan kg hesaplar. |
| `SetStockAwareReport(text)` | `_stockAwareReport` yazar ve `StockAwareTip`'i bildirir. |
| `AppendStartupStatus(message)` | Durum çubuğu `StatusReady` ise mesajı yazar, değilse mevcut uyarının (ör. atlanan katalog satırları) sonuna `" · "` ile ekler. |
| `RegenerateRS()` (`async void`) | N/ızgara/renk/varyant değişince RS bitmap'i 300 ms debounce, `SemaphoreSlim` kilidi ve iptal ile yeniden üretir; `AdjustZoomForBitmapChange`. |
| `FilterCatalogByUsedColors()` | Kataloğu yalnız `arMB[0]`'da kullanılan kodlara indirger, `PopulatePaletteAndAssigned`. |
| `PopulatePaletteAndAssigned()` | Seçili katalog sırasıyla `PaletteColors` ve `AssignedColors`'ı doldurur (piksel sayısı 0 olanlar atlanır, ama numara sayacı ilerler). |
| `UpdatePropTexture(codeName, y, x)` | Seçili varyant dokusunu (80×80) ve 1–16 arası küçük resimleri (44×44) yükler. |
| `AutoSelectGridColor(bmp)` | Görselin ortalama parlaklığına göre gri tonlardan derz rengi seçer (< 100 → ton 2, > 155 → ton 4, aksi 3). |
| `RedrawOverlay()` | `DisplayBitmap`'i RS (yoksa export) bitmap'ten yeniler. |
| `AdjustZoomForBitmapChange(oldW, oldH)` | Bitmap genişliği değişince görünen boyutu koruyacak şekilde zoom'u ölçekler. |
| `RecalcUsedColorInfo()` | `UsedColorInfo`'yu yeniden hesaplar. |
| `ReorderCatalogList()` | Boş gövde (yer tutucu). |

## Önemli davranışlar ve iş kuralları
- **Stok kontrol yalnız işaretler**: `CheckStockAsync` yetersiz taşlarda `rgb.stokYetersiz` ve `ColorItem.StockShort`'u true yapar (kırmızı nokta, kırmızı kalan kg); katalog seçimine dokunmaz.
- **Stok çek seçimi değiştirir**: `FetchStockAsync`, Sheet'te bulunan taşlardan stok ≤ 0 olanları hariç, > 0 olanları dahil eder; Sheet'te olmayan taşlar olduğu gibi kalır.
- **Stok işlemleri tek seferde bir tane**: `RunStockAction` `IsStockBusy` ile `CanUseStock`'u kapatır. `ClearStock*`/`AddStockAsync` onay ister; `ShowConfirm` yoksa işlem yapılmaz. Check/Clear/Add için `ScriptUrl` de gerekir, Fetch/Open için yalnız `SheetId`.
- **Stok kontrol için mozaik şart**: Proje adı yoksa `StockNoProject`, `arMA[0]`'da piksel sayısı > 0 taş yoksa `StockNoMosaic` uyarısı.
- **Yeni Mos stok işaretlerini siler**: `RunMosaicAsync` tüm `stokYetersiz`, `StockShort`, `RemainingKg` değerlerini temizler (`StockKg` korunur) ve `ProjectService.ForgetWpfState()` çağırır (WPF projesinden okunan ek veri artık geçersiz). `LoadImage` da `ForgetWpfState` çağırır.
- **Optimum taban seçimi**: Mos'tan sonra katalog "yalnız kullanılan taşlar"a daraltılır. Optimum açıkken yeni Mos'ta, seçim hâlâ bu otomatik seçimle (`_optimumAutoSelection`) aynıysa önce kullanıcının asıl seçimi (`_optimumUserSelection`) geri yüklenir; böylece Optimum her seferinde daraltılmış değil tam havuzdan başlar. Kullanıcı checkbox'lara dokunduysa mevcut seçim yeni taban olur.
- `ApplyStockSelection` stok kararlarını `_optimumUserSelection`'a da yazar; stok filtresi tam havuzu daraltır.
- **Optimum kaydırıcısı**: `OptimalK` değişimi 350 ms debounce ile `ApplyOptimalKAsync` tetikler; `IsProcessing` veya `!OptimalAvailable` ise atlanır. `RunMosaicAsync` önerilen K'yı atarken `_suppressOptimalApply` ile gereksiz yeniden uygulamayı engeller.
- **`TargetColors` her Mos'ta yeniden hesaplanır**: toplam taşın %10'u (10'a yuvarlanmış), en az 2, en fazla `ceil(256/RgbIncrement)^3 - 1`. Kullanıcı değeri korunmaz.
- Aktif renk yoksa (`MosaicData.arRGB.Count == 0`) `AlertMosaicNoColors` uyarısı, işlem yapılmaz.
- **Katalog uyarısı**: Açılışta `ColorCatalogService.SkippedLines` doluysa okunamayan katalog satırları `StatusCatalogSkipped` ile durum çubuğunda gösterilir.
- **N > 40 uyarısı** (`NWarningVisible`) oturumda yalnız bir kez gösterilir (`_nWarningShown`).
- **800M piksel sınırı**: `RegenerateRS`, `R·N·C·N > 800_000_000` ise RS üretmez; `StatusNTooLarge` ve `AlertNTooLargeTitle` gösterir. `OutOfMemoryException` → `AlertMemoryTitle`.
- `RegenerateRS` 300 ms debounce, önceki çağrıyı iptal eder ve `_rsLock` ile aynı anda tek üretim yapar; mozaik yoksa hiçbir şey yapmaz.
- **Stoğa göre (`UseStockAware`, varsayılan açık)**: Mos sonucu, Sheet'ten okunan stoğa (Bizdeki eksi diğer mozaik sütunları) göre `StockAwareAssigner` ile düzeltilir; stoğu yetmeyen taşın fazlası benzer taşlara taşınır. Aynı düzeltme taş sayısı kaydırıcısında (`_stockOnHand`) ve Stok Kontrol'den önce (`FixToStockAsync`) de uygulanır; Sheet'e son sayılar tek yazımda gider. Stok okunamazsa Mos düz çalışır.
- **En az kullanım kuralı kapalı**: `StockOptions()` `MinUsage = 0` döndürür; çok az kullanılan taşlar düşürülmez. Kural yalnız `StockCompareRunner` test aracında `MOSAIR_MINUSAGE=1` ile açılır.
- **Stok yükleme**: Açılışta (`LoadStockOnStartupAsync`) ve her görsel/proje yüklemesinde stok arka planda okunur; tooltip'lerde kg görünür, seçim değişmez.
- **Geç biten arka plan işleri**: `LoadImage`, `OpenProject`, `RunMosaicAsync` ve `FixToStockAsync` `StartNewContent()` çağırır: bekleyen/süren `RegenerateRS` ve bekleyen kaydırıcı uygulaması iptal edilir, `_contentVersion` artar. `RegenerateRS`, `ApplyOptimalKAsync`, `RunMosaicAsync`, `FixToStockAsync` ve `OpenProject`'in RS üretimi başlarken sürümü alır; bittiğinde sürüm değişmişse sonucu atar (bitmap dispose edilir), ekrana yazmaz. Böylece başka bir görsel açıldıktan sonra eski mozaik geri gelmez.
- **Undo/redo sırası**: Önce taş varyantı yığını (`_stoneUndoStack`/`_stoneRedoStack`) boşaltılır, o boşsa `PixelEditService` piksel düzenlemeleri geri alınır/yinelenir. Yeni `SelectStone` redo yığınını temizler.
- **Genişlik 1.2 cm katı**: `UpdateDimensions` `WidthCm`'yi `round(WidthCm·10/12)·1.2`'ye (en az 2 taş = 2.4 cm) çeker; bir taş = 1 kaynak piksel olduğundan görsel genişliğini aşamaz.
- **Kalıp koordinatı** (Özellikler paneli): 13 sütun × 26 satırlık bloklara göre hesaplanır; `xi = x mod 13`, `yi = y mod 26`, tek numaralı blok sütunlarında `yi = 25 - yi` (yılan sıralama). Not: kalıp **sayısı** (`MosaicEngine.CalculateDimensions`) ise genişlik ve yüksekliği 26'şar taşa bölerek hesaplanır; iki kural WPF'ten olduğu gibi alınmıştır.
- **Dışa aktarma** bitmap'in kopyasını yazar; dışa aktarım sırasında yapılan düzenlemeler dosyayı etkilemez.
- `ColorItem.ThumbnailBitmap` / `TooltipBitmap` tembel yüklenir ve önbelleğe alınır; ilk tooltip açılışında disk erişimi olur.
- `OpenProject` önce export bitmap'i gösterir (`StatusGeneratingRs`), RS hazır olunca değiştirir. Açılışta taş varyantı geri-al yığınları temizlenir, `IsPixelEditActive`/`IsSourcePixelMode`/`IsTargetPixelMode` `PixelEditService` ile eşitlenir (`ProjectService.Open` düzenleme modunu kapatır) ve `_lastRunOptimal = false` yapılarak Optimum kaydırıcısı gizlenir (projede Optimum analizi yoktur).
- **Mozaik yeniden kurulunca** (yeni Mos veya Optimum taş sayısı değişimi) `FinishMosaic` taş varyantı geri-al/yinele geçmişini siler; eski kayıtlar yeni mozaiğe uygulanmaz.

## Dikkat / bilinen sınırlamalar
- `OpenProject`, `MosaicEngine.LastOptimalResult`'ı sıfırlamaz; ancak `_lastRunOptimal = false` yaptığı için `OptimalAvailable` false olur ve kaydırıcı gizlenir. Optimum seçim dizileri ve stok işaretleri `OpenProject`'te sıfırlanmaz.
- `OpenProject` içinde `GridColor` ve `ShowGrid` atamaları (önceki `MosaicDone` true ise) debounced `RegenerateRS` tetikleyebilir; aynı anda `OpenProject` kendi RS'ini üretir → çift üretim ve `IsProcessing`'in erken false olması mümkün.
- `RegenerateRS`'in `finally` bloğu, 800M kontrolünde erken dönse bile `IsProcessing = false` yapar; başka bir işlem sürerken bayrağı düşürebilir.
- `OpenProject` `async void`; `ProjectService.Open` sonrası oluşan istisnalar (RS üretimi hariç) yakalanmaz. RS üretim hatası sessizce yutulur.
- `FixToStockAsync` başındaki kod yorumu hâlâ "çok az kullanılan taşlar düşürülür" der; en az kullanım kuralı kapalı olduğundan bu olmaz (`StockAwareSmall` satırı raporda çıkmaz).
- `FixToStockAsync` açılmış bir projede çalışmaz (`StockAwareNeedsMos`): taş çözünürlüğünde kaynak görsel verisi yoktur; önce Mos gerekir.
- `RedrawOverlay`'deki `"Bitmap too large for display"` ve `SetSourceFromCatalog`'daki `"source: "` metinleri yerelleştirilmemiş.
- `ReorderCatalogList` ve `OnImagePointerMoved` boş; `PixelCoordInfo`, `PixelDetailInfo`, `PixelColorInfo`, `PixelScaleInfo`, `DimensionInfo` XAML'e bağlı değil.
- `OnImagePressed` x ve y için aynı ölçeği (`imageControlWidth / MosaicEngine.width`) kullanır; kare taş varsayar.
- `ColorItem.ColorBrush`, `AssignedItem.RowBrush/TextBrush` her erişimde yeni fırça üretir (veya döndürür); büyük listelerde gereksiz tahsis.
- `UpdateDimensions` `Convert.ToInt32` kullandığı için .5 durumlarında banker's rounding uygular.

## İlgili dosyalar
- [MainWindow](../MainWindow.md) — view ve olay işleyicileri
- [ARAYUZ](../../ARAYUZ.md) — kullanıcı arayüzü açıklaması
- [MosaicEngine](../Services/MosaicEngine.md), [OptimalPaletteService](../Services/OptimalPaletteService.md)
- [StoneTextureService](../Services/StoneTextureService.md), [ImageService](../Services/ImageService.md)
- [ColorCatalogService](../Services/ColorCatalogService.md), [StockSheetService](../Services/StockSheetService.md)
- [ProjectService](../Services/ProjectService.md), [PixelEditService](../Services/PixelEditService.md), [Loc](../Services/Loc.md)
- [MosaicData](../Models/MosaicData.md), [Rgb](../Models/Rgb.md), [Region](../Models/Region.md)
- [StockAwareAssigner](../Services/StockAwareAssigner.md), [Stoğa göre raporu](../../STOGA_GORE_RAPOR.md)
- [GridOverlay](../Controls/GridOverlay.md), [StockSettingsDialog](../Controls/StockSettingsDialog.md), [AlertDialog](../Controls/AlertDialog.md), [ConfirmDialog](../Controls/ConfirmDialog.md)
