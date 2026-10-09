# MainWindow

> Kaynak: `mosair/MainWindow.axaml`, `mosair/MainWindow.axaml.cs` · Güncelleme: 2026-10-08

## Amaç

Uygulamanın tek ana penceresi. XAML tarafı tüm yerleşimi ve `MainViewModel` bağlamalarını tanımlar; code-behind tarafı ise ViewModel'in yapamadığı pencereye bağlı işleri yapar: dosya seçicileri, kayıt/dışa aktarma klasörleri, klavye kısayolları, sürükle-bırak, fare ile kaydırma/yakınlaştırma, gezgin (navigator), tema ve dil değişimi, animasyonlar ve diyalogların ViewModel'e bağlanması.

Bu belge **kablolamayı** anlatır. Kullanıcı gözünden kullanım için [ARAYUZ](../ARAYUZ.md).

## Nerede kullanılır

`App.OnFrameworkInitializationCompleted` içinde `desktop.MainWindow = new MainWindow()` ile oluşturulur. `DataContext` yapıcıda oluşturulan `MainViewModel` örneğidir (`x:DataType="vm:MainViewModel"`, derlenmiş bağlamalar).

## Yapı

### Pencere özellikleri

| Ad | Değer | Açıklama |
|---|---|---|
| `Width` × `Height` | 1200 × 800 (`MinWidth` 900, `MinHeight` 600) | Açılışta `WindowState="Maximized"`. |
| `ExtendClientAreaToDecorationsHint` | `True` | Sistem başlık çubuğu yerine özel başlık çubuğu; `ExtendClientAreaTitleBarHeightHint="32"`. |
| `Title` | `" "` | Başlık metni boş; marka adı başlık çubuğunda "mosair" olarak, logodaki adaçayı yeşiliyle (`Brand`) çizilir. |
| `Icon` | `avares://mosair/Assets/mosair-icon.png` | — |
| `Window.Styles` | araç çubuğu düğmeleri, menü, `section-title`, `Border.info-card` … | `Button.mos-btn`: `AccentFill` zemin, `OnAccent` yazı; üzerine gelince `AccentFillHover`, basınca `AccentFillPressed`. `TextBlock.section-title`: bütün panel başlıkları için 11 px SemiBold `FgMuted`. Renkler tema anahtarlarından gelir; koda gömülü vurgu rengi yoktur (marka ikonları ve ızgara rengi hariç; bkz. [App](App.md)). `Border.info-card`: Özellikler panelindeki görsel bilgisi kartları (arka plan `BgInput`, kenarlık `BrdrMain` 1 px, `CornerRadius` 6, `Padding` 9,8). |
| `Window.Resources` | `InvDouble` | Tema renkleri (`BgMain`, `FgPrimary` …) `App.axaml`'da, uygulama düzeyinde tanımlıdır; bkz. [App](App.md). |

### Yerleşim ağacı

```
Window
└─ DockPanel
   ├─ [Top] Başlık çubuğu (Border, 32 px, PointerPressed=OnTitleBarPointerPressed)
   │   ├─ Panel: DockPanel (Margin sağ 142 px, sistem pencere düğmeleri için) + üstünde DocumentTitle: açık dosyanın adı,
   │   │   pencere genişliğinin tam ortasında (menüden bağımsız; MaxWidth 340, uzunsa "…"; IsHitTestVisible=False, sürükleme bozulmaz)
   │   └─ "mosair" logosu + Menu
   │       ├─ Dosya (MenuFile): menuLoadImage, menuOpenProject | menuSave, menuSaveAs | menuExport ▸, menuExportAs ▸,
   │       │     Ekran Görüntüsü Al (MenuScreenshot; Click=OnScreenshot, IsEnabled=ImageLoaded)
   │       │     | Yeni Görselleri Bildir (MenuWatchImages; Click=OnToggleWatchImages; onay işareti WatchNewImages)
   │       │     | Google Drive ▸ (MenuDrive; IsEnabled=CanUseDrive): Drive'a Kaydet (OnDriveSave), Drive'dan Aç... (OnDriveOpen)
   │       │       | Drive Klasörü Ayarları... (OnDriveSettings)
   │       │     (dışa aktarma öğelerinin alt menüsü kodda doldurulur: "Görüntü kalitesi seçiniz" + 10 kalite seçeneği; BuildExportMenus)
   │       ├─ Düzen (MenuEdit): Tümünü Seç, Tümünü Kaldır | İşlemi İptal Et (MenuCancelWork; Click=OnCancelWork,
   │       │     IsEnabled=CanCancel, InputGesture="Escape")
   │       ├─ Görünüm (MenuView): menuFitToScreen, Yumuşak Fare Hareketi (MenuSmoothMouse; OnToggleSmoothMouse;
   │       │     onay işareti SmoothMouse), Özellikler Paneli (MenuPropertiesPanel; OnTogglePropertiesPanel;
   │       │     InputGesture F4; onay işareti IsPropertiesPanelOpen), Görsel Ayarları (MenuAdjustPanel;
   │       │     OnToggleAdjustPanel; onay işareti IsAdjustPanelOpen)
   │       ├─ Araçlar (MenuTools): menuMosaicize, Piksel Düzenle, Izgara Göster,
   │       │     menuGridColor*, menuInterp*  (*kodda doldurulur: BuildToolsMenu)
   │       │     | Optimum, Stoğa göre, Anlık Mos (OnToggleLiveMos; ✓ LiveMos), Anlık Mos: Tüm Renkler (OnLiveMosAllColors; ✓ LiveMosAllColors), Anlık Mos: Seçili Renkler (OnLiveMosSelectedColors; ✓ LiveMosSelectedColors), Taş Sayısı ▸ (Önerilen / Artır / Azalt),
   │       │     Stok ▸ (Aç, Ayarlar | Çek ▸ (Devre dışı bırak / Kırmızıyla işaretle), Kontrol, Sil, Tümünü Sil, Ekle)
   │       └─ Yardım (MenuHelp): Kullanım Kılavuzu (F1)
   ├─ [Top] Araç çubuğu (toolbar)
   │   ├─ Sol: Görüntü Yükle, Proje Aç, Kaydet (saveProjectBtn: saveIcon/saveCheckIcon), Farklı Kaydet,
   │   │       Mos düğmesi (mosAnimGrid: mosQ0..mosQ3), Piksel Düzenle + source/target göstergesi,
   │   │       Izgara (Flyout: aç/kapa, GridColorPresets, GridColorShades), İnterpolasyon (Flyout) │
   │   │       Google Drive düğmesi (renkli Drive logosu; Click=OnDriveSave, CanUseDrive, ipucu TipDrive) + yanında 14 px açılır ok
   │   │       (Flyout: Drive'a Kaydet / Drive'dan Aç... / Drive Klasörü Ayarları... → OnDriveSave / OnDriveOpen / OnDriveSettings),
   │   │       5 stok düğmesi (CanUseStock; tablo ve Sil düğmelerinde sağ tık ContextMenu; tablo ve Stok Çek düğmelerinin yanında açılır ok Flyout'u: tablo → Aç / Ayarlar, Stok Çek → Devre dışı bırak / Kırmızıyla işaretle),
   │   │       "Stoğa göre" onay kutusu (UseStockAware; ipucu StockAwareTip), Optimum onay kutusu (LblOptimum "Optimum Renk Sayısı", Margin 14 sol) + Taş kaydırıcısı
   │   │       (ctrl:AdjustSlider ShowValue; Value ↔ OptimalK, Maximum ↔ OptimalKMax, DefaultValue ↔ OptimalKSuggested,
   │   │       ipucu TipOptimalSlider; görünür: UseOptimal && OptimalAvailable, BoolConverters.And)
   │   └─ Sağ: Ekran görüntüsü düğmesi (kamera, OnScreenshot, ImageLoaded) │ exportBtn (exportArrow animasyonu; sol tık = kodla kurulan MenuFlyout: mosairEXPORT ▸ / mosairEXPORT As ▸,
   │           her biri "Görüntü kalitesi seçiniz" + 10 kalite seçeneği; sağ tık = klasörü aç),
   │           Tema düğmesi (iconDark / iconLight), Dil düğmesi (Flyout: TR / EN)
   ├─ [Bottom] Durum çubuğu: Panel → ActivityWave (IsActive = IsBusy, WaveColor = AccentFill, arka plan dalgası) + Grid "*,Auto,*" (Margin 8,3)
   │   ├─ Sol: UsedColorInfo
   │   ├─ Orta (soldan sağa): Progress (IsProcessing) + İptal düğmesi (`Danger` kırmızısı çerçeve ve ✕ simgesi, `DangerText` BtnCancel yazısı; IsVisible = CanCancel,
   │   │     ipucu TipCancel, Click=OnCancelWork) + StatusText + ElapsedTime
   │   └─ Sağ: ZoomInfo + Ekrana Sığdır düğmesi
   └─ Ana içerik: Grid x:Name="mainGrid" (380 | 4 | * | 4 | 300 | 4 | 220; Görsel Ayarları sütunu 260–600 px,
       Özellikler 160–360 px; gizli sütun 24 px ve önündeki ayırıcı 0 px)
       ├─ Sütun 0 — Sol panel
       │   ├─ Boyut bölümü: tek satırda WidthCm kutusu (InvDouble, 16 px SemiBold mono) + (ImageLoaded iken)
       │   │   "× DimensionHeight cm = DimensionArea" (aynı boyut; görsel yokken yalnızca "cm"); altında tek kart
       │   │   (ImageLoaded iken; BgInput, CornerRadius 6): Grid "Auto,10,Auto,22,Auto,22,Auto" (sayılar etiketin hemen ardından başlar) × 2 satır —
       │   │   InfoStonesLabel InfoStoneCols × InfoStoneRows = InfoStoneTotal / InfoMouldsLabel InfoMouldCols × InfoMouldRows = InfoMouldTotal
       │   │   (12 px mono, sayılar sağa hizalı, toplam SemiBold)
       │   ├─ Katalog satırı (Border BgBar, Padding 6,4): yalnızca solda Border (BgInput, CornerRadius 4, 22 px) içinde
       │   │     BtnSelectAll | 1 px çizgi | BtnDeselectAll (saydam, 10 px; ipuçları MenuSelectAll / MenuDeselectAll)
       │   └─ 3 sütun (başlıklar ColCatalog / ColMatch / ColAssigned)
       │       ├─ catalogListBox (CatalogColors): onay kutusu + renk + kod/ad + stok kg
       │       ├─ paletteScroll (PaletteColors): eşleşen renk kareleri
       │       └─ assignedScroll (AssignedColors): ID, CodeName, PixelCount
       ├─ Sütun 1 — GridSplitter
       ├─ Sütun 2 — Tuval (canvasBorder: Border, DragDrop.AllowDrop="True"; Background başta BgCanvas, sonra ApplyImageTint;
       │                   0,4 sn BrushTransition)
       │   ├─ imageScroller (ScrollViewer)
       │   │   └─ Panel x:Name="imagePanel" (ImageDisplayWidth × ImageDisplayHeight)
       │   │       ├─ Image x:Name="sourceImage" (DisplayBitmap; yalnızca ShowSourceView; Width/Height = SourceViewWidth/Height,
       │   │       │     sol üste hizalı; wheel/move/press/release olayları)
       │   │       ├─ ctrl:MosaicView x:Name="mosaicView" (yalnızca ShowMosaicView; RenderSource, OverviewBitmap,
       │   │       │     StonePixelSize, ShowGrid, GridColor; aynı wheel/move/press/release olayları)
       │   │       └─ ctrl:GridOverlay
       │   ├─ navPanel (150×150 gezgin, sağ üst; ImageLoaded): Image (NavBitmap) + NavView* dikdörtgeni
       ├─ Sütun 3 — GridSplitter x:Name="adjustSplitter" (Görsel Ayarları gizliyken görünmez)
       ├─ Sütun 4 — Görsel Ayarları (IsVisible = IsAdjustPanelOpen; BgPanel, solda 1 px çizgi)
       │   ├─ Başlık: AdjTitle (ipucu TipAdjust) + IsAdjusted ise AccentText noktası + gizle düğmesi
       │   │     (▬, OnToggleAdjustPanel, ipucu TipPanelHide)
       │   ├─ Sekmeler: iki ToggleButton.chip — AdjTabLight (IsChecked ↔ IsLightTab), AdjTabColor (↔ IsColorTab);
       │   ├─ Anlık Mos: CheckBox AdjLiveMos (IsChecked ↔ LiveMos, ipucu TipAdjLiveMos); altında (IsVisible = LiveMos,
       │   │     24 px içeride) iki RadioButton (GroupName liveMosColors): AdjLiveMosAll ↔ LiveMosAllColors,
       │   │     AdjLiveMosSelected ↔ LiveMosSelectedColors (ipuçları TipLiveMosAll / TipLiveMosSelected)
       │   ├─ [Bottom] AdjHint (kısayol ipucu, 10 px) + AdjReset düğmesi (OnAdjustReset, IsEnabled = IsAdjusted)
       │   └─ ScrollViewer → StackPanel (IsEnabled = CanAdjust)
       │       ├─ Işık (IsLightTab): ItemsControl LightParams → satır şablonu (aşağıda)
       │       └─ Ton/Doygunluk (IsColorTab):
       │           ├─ ItemsControl AdjustRanges (WrapPanel, IsEnabled = CanPickRange) → vm:AdjustRange: 28 px yuvarlak
       │           │     düğme (OnAdjustRangeClick, Tag = Index, ipucu Label): Swatch daire 20 px + IsSelected ise
       │           │     AccentBorder halkası 26 px + IsUsed ise köşede FgPrimary noktası
       │           ├─ ItemsControl ColorParams → satır şablonu
       │           └─ CheckBox AdjColorize (ipucu TipAdjColorize)
       │     Satır şablonu (vm:AdjustParam): üstte Label (solda) + değer kutusu (TextBox 60 px, sağa yaslı, MonoFont;
       │     Text ↔ Text, UpdateSourceTrigger=LostFocus, KeyDown = OnAdjustTextKeyDown); altında ctrl:AdjustSlider
       │     (Minimum/Maximum/DefaultValue ↔ Min/Max/Default, Value ↔ SliderValue, TrackBrush ↔ Track,
       │     EmptyTrackBrush = BrdrTer, ThumbBrush = FgPrimary, ThumbBorderBrush = BgInput, ipucu TipAdjSlider)
       ├─ Sütun 4 — Gizli Görsel Ayarları şeridi (IsVisible = !IsAdjustPanelOpen): tek düğme (OnToggleAdjustPanel,
       │     ipucu TipAdjustShow): ‹ oku, IsAdjusted ise AccentText noktası, 90° döndürülmüş MenuAdjustPanel
       ├─ Sütun 5 — GridSplitter x:Name="propsSplitter" (Özellikler gizliyken görünmez)
       ├─ Sütun 6 — Özellikler paneli (IsVisible = IsPropertiesPanelOpen)
       │   ├─ Başlık: PropTitle + sağda gizle düğmesi (▬, OnTogglePropertiesPanel, ipucu TipPanelHide)
       │   ├─ Görsel yokken (ShowNoImageHint): resim simgesi (PathIcon 28 px) + PropNoImage, ortalı
       │   ├─ Görsel bilgileri (ShowImageInfo = ImageLoaded && !HasSelection), kartlar (Margin 8, Spacing 8)
       │   │   ├─ Önizleme kartı (Border, CornerRadius 6, ClipToBounds): ImageInfoFound ise ImageInfoThumb
       │   │   │     (Stretch Uniform, MaxHeight 150, HighQuality) + sağ üstte ImageInfoType rozeti (#B0000000);
       │   │   │     altında BgInput şeridi: PropImage başlığı, ImageInfoName (sarmalı), !ImageInfoFound ise
       │   │   │     kırmızı (DangerText) PropImageMissing
       │   │   ├─ AYRINTILAR kartı (info-card; ImageInfoFound): Grid "Auto,*" × 5 satır — PropResolution/ImageInfoResolution,
       │   │   │     PropMegapixels/ImageInfoMegapixels, PropAspect/ImageInfoAspect, PropFileSize/ImageInfoSize,
       │   │   │     PropModified/ImageInfoDate (değerler sağa yaslı, eş aralıklı yazı tipi)
       │   │   └─ PropStoneHint (ShowStoneHint)
       │   └─ Taş bilgileri (HasSelection)
       │       ├─ Renk örneği, PropStoneId / PropStoneName; RENK başlığının sağında ✕ (OnClearSelection, ipucu TipClearSelection)
       │       ├─ Doku önizleme (PropTextureBitmap)
       │       ├─ Taş varyantları (PropStoneThumbs → OnSelectStone)
       │       ├─ RGB (PropRgbInfo), Koordinat (PropPixelCoord / PropMouldCoord)
       │       └─ Düzenleme bilgisi (IsPixelEditActive: SelectedStoneText, EditedPixelCountText, geri al/yinele ipucu)
       ├─ Sütun 6 — Gizli Özellikler şeridi (IsVisible = !IsPropertiesPanelOpen): tek düğme (OnTogglePropertiesPanel,
       │     ipucu TipPanelShow): ‹ oku + 90° döndürülmüş PropTitle (LayoutTransformControl)
       └─ Sütun 0–6 (ColumnSpan 7) — toastPanel: sağ alt bildirim (yeni görsel ya da kaydedilen dosya; IsVisible=False;
             sağ alt, Margin 0,0,12,12, Width 300, BgCard, CornerRadius 8, BoxShadow;
             PointerEntered/Exited = OnToastPointerEntered/Exited)
             ├─ toastThumb (56×56, UniformToFill) · toastTitle (AccentText, SemiBold) · toastName (kısaltılır; ipucu tam yol)
             │   · toastInfo (soru ya da klasör yolu; kısaltılır, ipucu tam metin) · sağ üstte ✕ (OnToastDismiss)
             ├─ toastSeconds (sol) · toastSecondary (metin koddan → OnToastSecondary)
             │   · toastPrimary (metin koddan, Classes="primary" → OnToastPrimary)
             └─ toastBar (ProgressBar, 3 px, 0–100, AccentFill; kalan süre)
```

### Özel alanlar (code-behind)

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `_vm` | `MainViewModel` | yapıcıda | DataContext. |
| `_isPanning`, `_panStart`, `_scrollStart` | `bool`, `Point`, `Vector` | `false` | Sağ tuşla kaydırma durumu. |
| `ZoomGlideSeconds` | `const double` | 0,07 | Yumuşak zoom'un zaman sabiti (her karede kalan yolun `1 − e^(−dt/0,07)` kadarı alınır; ~0,2 sn'de %95). |
| `_zoomTarget`, `_zoomGliding` | `double`, `bool` | 0, `false` | Yumuşak zoom'un hedefi ve sürüp sürmediği. |
| `_zoomImg`, `_zoomMouse`, `_zoomAnchor` | `Control?`, `Point`, `Point` | — | Tekerleğin çevrildiği kontrol (`Image` ya da `MosaicView`), imlecin `imageScroller`'daki yeri ve imlecin altındaki noktanın zoom 1'deki görüntü koordinatı. |
| `_zoomLastFrame`, `_panLastFrame` | `TimeSpan?` | `null` | Son animasyon karesinin zamanı (`RequestAnimationFrame`); kare aralığı 1 ms–100 ms'ye kırpılır, ilk karede 1/60 sn sayılır. |
| `PanGlideSeconds` | `const double` | 0,12 | Kaydırma süzülmesinin yavaşlama zaman sabiti. |
| `_panSamples`, `_panClock` | `Queue<(TimeSpan, Point)>`, `Stopwatch` | boş | Sürüklemenin son ~80 ms'lik konum örnekleri (en az 2 örnek tutulur); bırakma hızını bulmak için. |
| `_panVelocity`, `_panGliding` | `Vector`, `bool` | 0, `false` | Süzülme hızı (px/sn, kaydırma ofseti yönünde) ve sürüp sürmediği. |
| `_syncingScroll` | `bool` | `false` | Palet/Atanan sütunlarının kaydırma eşitlemesinde yeniden giriş kilidi. |
| `_mosAnimTimer`, `_mosAnimFrame` | `DispatcherTimer?`, `int` | `null`, 0 | Mos düğmesi animasyonu (350 ms). |
| `_exportAnimTimer`, `_exportAnimFrame` | `DispatcherTimer?`, `int` | `null`, 0 | Dışa aktarma ok animasyonu (90 ms, `ExportAnimOffsets`). |
| `_exportChoices` | `List<(MenuItem item, int quality, bool saveAs)>` | boş | Dışa aktarma listelerindeki (toolbar MenuFlyout'u ve Dosya menüsü) bütün kalite seçenekleri; metinleri `UpdateExportChoiceTexts` yeniler. |
| `CmdKey` | `static readonly KeyModifiers` | `Meta` (macOS) / `Control` | `Loc.IsMac`'e göre. |
| `_interpMenuItems`, `_gridColorMenuItems`, `_gridShadeMenuItems`, `_gridShadeSeparator` | listeler | boş | Araçlar menüsünde kodla oluşturulan öğeler ve onay işaretleri. |
| `CheckGeometry` | `const string` | — | Menü onay işaretinin yol geometrisi. |
| `_propertiesWidth` | `GridLength` | 220 | Özellikler paneli gizlenmeden önceki genişlik. |
| `_adjustWidth` | `GridLength` | 300 | Görsel Ayarları sütunu gizlenmeden önceki genişlik. |
| `_imageWatcher` | `NewImageWatcher` | yeni örnek | İndirilenler / Masaüstü izleyicisi ([NewImageWatcher](Services/NewImageWatcher.md)). |
| `_toastTimer` | `DispatcherTimer?` | `null` | Bildirim geri sayımı (50 ms; ilk bildirimde oluşturulur). |
| `_toastPath` | `string` | `""` | Bildirimdeki dosyanın tam yolu (yeni görsel, dışa aktarılan / kaydedilen dosya). |
| `_toastKind` | `ToastKind` (private enum: `NewImage`, `Export`, `Project`) | — | Bildirimin türü; düğmelerin ne yapacağını belirler. Ekran görüntüsü `Export` türüyle gösterilir. |
| `_toastThumbOwned` | `bool` | `false` | Küçük resim bildirim için çözüldüyse true (bir sonraki bildirimde dispose edilir); proje kaydındaki mozaik görüntüsü uygulamanın kendi bitmap'idir, dispose edilmez. |
| `_toastLeft`, `_toastLastTick`, `_toastHover` | `TimeSpan`, `DateTime`, `bool` | — | Kalan süre, son tik zamanı, fare bildirimin üzerinde mi. |
| `ToastTime` | `static readonly TimeSpan` | 7 sn | Bildirimin görünme süresi. |

## Public API

`MainWindow`'un yapıcısı dışında public üyesi yoktur. Yapıcının yaptıkları:

| Adım | Ne yapar |
|---|---|
| `InitializeComponent()` | XAML'ı yükler. |
| `_vm.ShowAlert` | `AlertDialog` gösterir. |
| `_vm.ShowConfirm` | `ConfirmDialog` gösterir, `bool` döndürür. |
| `_vm.ShowStockSettings` | `StockSettingsDialog` gösterir, `StockSheetService.Config?` döndürür. |
| `_vm.ShowDriveSettings` | `DriveSettingsDialog` gösterir, `DriveService.Config?` döndürür. |
| `_vm.ShowDriveOpen` | `DriveService.Config` ile `DriveOpenDialog` proje tarayıcısını gösterir (liste ve önizlemeleri kendisi yükler), seçilen `DriveService.DriveFile?`'ı döndürür. |
| `_vm.OpenUrl` | `TopLevel.GetTopLevel(this)?.Launcher.LaunchUriAsync` ile tarayıcıda açar (stok tablosu). |
| `_vm.StoneInvalidated += …` | Tek taş değişince (piksel düzenleme, geri al/yinele, varyant seçimi) `mosaicView.InvalidateStone(row, col)`: yalnızca o taşı içeren karolar yeniden çizilir. |
| `AddHandler(DragDrop.DropEvent / DragOverEvent)` | Sürükle-bırak. |
| `_vm.FileSaved += …` | Dışa aktarma, ekran görüntüsü ya da proje kaydı bitince `Dispatcher.UIThread.Post` ile `ShowSavedToast(path, kind)`. |
| `_imageWatcher.ImageArrived += …` | İzleyici olayı iş parçacığından gelir; `Dispatcher.UIThread.Post` ile `ShowImageToast(path, place)`. Ardından `ApplyWatchNewImages()` (açılışta `WatchNewImages` true olduğundan izleme başlar); `Closed` olayında `_imageWatcher.Dispose()`. |
| `BuildExportMenus()` | `DataContext` atandıktan hemen sonra dışa aktarma listelerini kurar (aşağıda). |
| `KeyDown += OnKeyDown` | Pencere düzeyi kısayollar. |
| `paletteScroll` ↔ `assignedScroll` | İki `ScrollChanged` lambdası dikey ofseti karşılıklı eşitler (`_syncingScroll` ile döngü engellenir). |
| `_vm.PropertyChanged` lambdası | `IsPropertiesPanelOpen` → `ApplyPropertiesPanel()`; `WatchNewImages` → `ApplyWatchNewImages()`; `IsProcessing` → Mos animasyonu (`StartMosAnim`/`StopMosAnim`), `IsExporting` → dışa aktarma animasyonu (`StartExportAnim`/`StopExportAnim`), `GridColorShades` → ton menüsünü yeniden kur, `SelectedInterpolation` / `GridColor` → menü onaylarını yenile. |
| `BuildToolsMenu()`, `SetMenuShortcutTexts()` | Kodla oluşan menüler ve kısayol metinleri. |

### Olay işleyicileri

| Handler | Bağlı olduğu kontrol | Ne yapar |
|---|---|---|
| `OnTitleBarPointerPressed` | Başlık çubuğu `Border` | Sol tıkta pencereyi sürükler (`BeginMoveDrag`), çift tıkta büyüt/eski boyut. Tıklanan öğe `Button`, `MenuItem` veya `Menu` içindeyse hiçbir şey yapmaz. |
| `OnLoadImage` | `menuLoadImage`, toolbar Görüntü Yükle, Ctrl/⌘+I | Dosya seçici (`*.png, *.jpg, *.jpeg, *.bmp, *.tiff`) → `LoadImageAndFit`. |
| `LoadImageAndFit(path)` | `OnLoadImage`, `OnDrop`, `OnToastPrimary` | `_vm.LoadImage(path)`; görsel yüklendiyse (`_vm.ImageLoaded`) `_vm.FitToWindow(imageScroller.Bounds.Width, imageScroller.Bounds.Height)`: yeni görsel görsel alanına sığmış açılır (`LoadImage`'ın kurduğu `ZoomLevel = 2` ekranda kalmaz). |
| `ApplySourceImageQuality()` | Kurucu, `ZoomLevel` değişimi (`PropertyChanged`) | Mos öncesi görseli gösteren `sourceImage` için `RenderOptions.BitmapInterpolationMode`: `ZoomLevel < 1` iken `HighQuality` (görsel kendi boyutundan küçük gösterilirken en yakın piksel ölçekleme pikselleri atlar, fotoğraf bozuk görünür), 1x ve üstünde `LowQuality` (pikseller keskin kalır). |
| `OnOpenProject` | `menuOpenProject`, toolbar Proje Aç, Ctrl/⌘+O | `*.mos` seçici (Masaüstü/mosairPROJECT varsa `SuggestedStartLocation` olarak orada açılır) → `await _vm.OpenProjectAsync(path)` (okuma arka planda, pencere donmaz) → `true` dönerse `FitToWindow`. |
| `OnSaveProject` | `menuSave`, `saveProjectBtn`, Ctrl/⌘+S (`CanSaveProject`) | Masaüstü/mosairPROJECT kuralına göre `await _vm.SaveProjectAsync(mosPath)` (yazma arka planda); başarısızsa döner. Ardından kaynak görüntüyü (yoksa) `Task.Run` içinde kopyalar ve 1,2 sn `saveCheckIcon` gösterir. |
| `OnSaveAsProject` | `menuSaveAs`, toolbar Farklı Kaydet, Ctrl/⌘+Shift+S (`CanSaveProject`) | `SaveAsDialog()` çağırır; o da seçilen yola `await _vm.SaveProjectAsync(fullPath)`. |
| `OnScreenshot` | Araç çubuğundaki kamera düğmesi (dışa aktarmanın solunda) ve **Dosya → Ekran Görüntüsü Al** | Görsel alanını (`imageScroller`) ekranın gerçek ölçeğiyle (`RenderScaling`) `RenderTargetBitmap`'e çizer, kaydırma çubukları hariç görünen kısmın (`Viewport`) yalnızca görsel panelinin (`imagePanel`, `TranslatePoint` ile konumu) kapladığı dikdörtgenini alır, çevredeki boş tuval girmez; saydam pikseller için tuvalin o anki rengini (`canvasBorder.Background`; görsele uyan ton, yoksa temanın `BgCanvas`'ı) kullanır ve `_vm.SaveScreenshotAsync` ile `mosairEXPORT/tarih_saat__ad__ekran.png` olarak kaydeder. Mini harita ayrı bir katmanda olduğu için görüntüye girmez. Klasör oluşturulamazsa `ShowExportFolderError` uyarı gösterir (aynısı hızlı dışa aktarmada da). |
| `OnDriveSave` | Toolbar'daki Drive ikonu, ok Flyout'u ve **Dosya → Google Drive → Drive'a Kaydet** (`CanUseDrive`) | `_vm.SaveToDriveAsync()`. |
| `OnDriveOpen` | Ok Flyout'u ve **Dosya → Google Drive → Drive'dan Aç...** | `_vm.OpenFromDriveAsync()` `true` dönerse (proje açıldıysa) `_vm.FitToWindow(imageScroller.Bounds.Width, imageScroller.Bounds.Height)`; iptal ya da hatada zoom olduğu gibi kalır. |
| `OnDriveSettings` | Ok Flyout'u ve **Dosya → Google Drive → Drive Klasörü Ayarları...** | `_vm.ConfigureDriveAsync()`. |
| `OnCancelWork` | Durum çubuğundaki İptal düğmesi ve **Düzenle → İşlemi İptal Et** (`CanCancel`) | `_vm.CancelWork()`: süren iptal edilebilir işi (Mos, stoğa göre düzeltme, dışa aktarma) durdurur; Esc ile aynı. |
| `OnExportImage` | Ctrl/⌘+E | `ExportQuickAsync(MainViewModel.DefaultExportQuality)`: varsayılan kaliteyle (40) hızlı dışa aktarma (sonucu beklenmez). |
| `ExportQuickAsync(quality)` | `OnExportImage`, listelerdeki mosairEXPORT seçenekleri | Masaüstü/mosairEXPORT'a zaman damgalı dosya; uzantı `_vm.QuickExportExtension(quality)` (`jpeg` ya da `png`) → `_vm.ExportImageAsync(path, quality)`. |
| `ExportAsAsync(quality)` | Listelerdeki mosairEXPORT As seçenekleri | Kayıt seçici (`DlgExportImage`; JPEG `*.jpg/*.jpeg`, PNG `*.png`; varsayılan uzantı `jpg`) → `_vm.ExportImageAsync(path, quality)`. |
| `BuildExportMenus()` | Yapıcı | Toolbar için bir `MenuFlyout` (`BottomEdgeAlignedRight`) kurar: `MenuExport` ve `MenuExportAs` başlıklı (Loc bağlamalı) iki öğe, ikisi de `FillExportChoices` ile doldurulur; `Opening` → `RefreshExportChoices`; `exportBtn.Flyout`'a atanır. Dosya menüsündeki `menuExport` ve `menuExportAs` de aynı şekilde doldurulur, `SubmenuOpened` → `RefreshExportChoices`. Son olarak `_vm.ExportEstimatesChanged += UpdateExportChoiceTexts`. |
| `FillExportChoices(parent, saveAs)` | `BuildExportMenus` | Alt menüyü temizler; pasif başlık öğesi (`ExportChooseQuality`: "Görüntü kalitesi seçiniz"), `Separator`, sonra `MainViewModel.ExportQualities` için birer `MenuItem` ekler ve `_exportChoices`'a kaydeder. Tıklanınca `exportBtn.Flyout` gizlenir, `saveAs` ise `ExportAsAsync(quality)`, değilse `ExportQuickAsync(quality)`. |
| `RefreshExportChoices()` | Flyout `Opening`, `menuExport`/`menuExportAs` `SubmenuOpened` | `UpdateExportChoiceTexts()`, ardından tahminleri başlatır: `_vm.RefreshExportEstimatesAsync()` (beklenmez). |
| `UpdateExportChoiceTexts()` | `RefreshExportChoices`, `_vm.ExportEstimatesChanged` | Her seçeneğin başlığını `_vm.ExportChoiceLabel(quality, saveAs)` yapar (piksel boyutu + tahmini dosya boyutu; "N" gösterilmez). Mos'tan önce yalnızca varsayılan kalitenin seçeneği görünür (metni görselin boyutu), diğerleri gizlenir. |
| `OnExportPointerPressed` | `exportBtn`'i saran `Panel` | Sağ tıkta mosairEXPORT klasörünü işletim sisteminin dosya yöneticisinde açar (`Process.Start`, `UseShellExecute`). Sol tıkta listeyi düğmenin kendi `Flyout`'u açar. |
| `OnSelectAll` | Düzen menüsü, sol paneldeki BtnSelectAll | `_vm.SetAllColors(false)` (hiçbir renk hariç değil). |
| `OnDeselectAll` | Düzen menüsü, sol paneldeki BtnDeselectAll | `_vm.SetAllColors(true)`. |
| `OnResetSize` | `menuFitToScreen`, durum çubuğundaki sığdır düğmesi, Ctrl/⌘+0 | `_vm.FitToWindow(imageScroller.Bounds…)`. |
| `OnTogglePropertiesPanel` | Özellikler başlığındaki ▬ düğmesi, gizli panelin şeridi, **Görünüm → Özellikler Paneli**, F4 | `_vm.IsPropertiesPanelOpen` tersine çevrilir; sütun değişikliğini `ApplyPropertiesPanel` yapar. |
| `OnClearSelection` | Özellikler → RENK başlığının yanındaki ✕ | `_vm.ClearSelection()` (taş seçimini bırakır, görsel bilgileri görünür). |
| `OnToggleAdjustPanel` | Görsel Ayarları başlığındaki ▬, gizli sütunun şeridi, **Görünüm → Görsel Ayarları** | `_vm.IsAdjustPanelOpen` tersine çevrilir; sütun değişikliğini `ApplyAdjustPanel` yapar. Özellikler paneline dokunmaz. |
| `ApplyAdjustPanel()` | `IsAdjustPanelOpen` değişimi (`PropertyChanged`) | Görsel Ayarları sütununu ([aşağıda](#görsel-ayarları-sütunu)) gizler ya da önceki genişliğiyle açar. |
| `OnAdjustReset` | Görsel Ayarları → Sıfırla | `_vm.ResetAdjustments()`. |
| `OnAdjustRangeClick` | Ton/Doygunluk → renk aralığı yuvarlağı | Düğmenin `Tag`'indeki indeksle `_vm.SelectAdjustRange(index)`. |
| `OnAdjustTextKeyDown` | Görsel Ayarları değer kutusu (`Classes="adj-num"`), `KeyDown` | `Enter`'da `CommitAdjustBox`: `Text` bağlamasını `UpdateSource()` ile hemen uygular, odağı pencereye alır (kutu değeri uygulanmış haliyle gösterir). |
| `OnWindowPointerPressedCommit` | Pencere, `PointerPressed` (Tunnel, işlenmiş olaylar dahil; kurucuda eklenir) | Odak bir `adj-num` kutusundaysa ve tıklama o kutunun dışındaysa `CommitAdjustBox`: odak alamayan bir yere (ör. görsel alanı) tıklanınca da yazılan değer uygulanır. |
| `OnToggleWatchImages` | **Dosya → Yeni Görselleri Bildir** | `_vm.WatchNewImages` tersine çevrilir; izleyiciyi `ApplyWatchNewImages` başlatır/durdurur. |
| `ApplyWatchNewImages()` | Yapıcı, `WatchNewImages` değişimi | true → `_imageWatcher.Start()`; false → `Stop()` ve `HideToast()`. |
| `ShowImageToast(path, place)` | `ImageArrived` (UI iş parçacığında) | `WatchNewImages` kapalıysa ya da `path` açık görselin kendisiyse (`ProjectService.CurrentPictureFileName`, büyük/küçük harf duyarsız) hiçbir şey yapmaz. Değilse `ShowToast(NewImage, …)`: başlık `ToastNewDownload`/`ToastNewDesktop`, bilgi satırı `ToastQuestion`, düğmeler `ToastDismiss` / `ToastOpen`, küçük resim `DecodeThumb(path)`. |
| `ShowSavedToast(path, kind)` | `_vm.FileSaved` | Bilgi satırı dosyanın klasörü. `Project` → `ShowToast(Project, …)`: başlık `ToastProjectSaved`, düğmeler `ToastDismiss` / `ToastShowFolder`, küçük resim `_vm.NavBitmap` (paylaşılan, dispose edilmez). `Export` / `Screenshot` → `ShowToast(Export, …)`: başlık `ToastExported` / `ToastScreenshotSaved`, düğmeler `ToastShowFolder` / `ToastOpenFile`, küçük resim `DecodeThumb(path)`. |
| `DecodeThumb(path)` (static) | Bildirimler | Dosyadan `Bitmap.DecodeToWidth(stream, 112)`; dosya yoksa, 64 MB'tan büyükse (çok büyük dışa aktarmalar) ya da okunamazsa `null` (kutu boş kalır). |
| `ShowToast(kind, path, title, info, secondary, primary, ownedThumb, sharedThumb)` | `ShowImageToast`, `ShowSavedToast` | Ortak gösterim: tür, yol, başlık, dosya adı (+ tam yol ipucu), bilgi satırı (+ ipucu), iki düğmenin metni, küçük resim (önceki bildirim için çözülmüşse dispose edilir). Süreyi `ToastTime`'a (7 sn) kurar, bildirimi gösterir, zamanlayıcıyı başlatır (yeni bildirim öncekinin yerine geçer). |
| `_toastTimer.Tick` (lambda) | 50 ms | Fare üzerinde değilse geçen süreyi düşer; süre bittiyse `HideToast()`, değilse `UpdateToastCountdown()`. |
| `UpdateToastCountdown()` | Gösterim ve her tik | `toastBar.Value` = kalan / `ToastTime` (7 sn) × 100; `toastSeconds` = `ToastSeconds` (yukarı yuvarlanmış saniye, en az 1). |
| `HideToast()` | Kapat, ✕, süre bitimi, bildirim düğmeleri, izlemenin kapatılması | Zamanlayıcıyı durdurur, bildirimi gizler. |
| `OnToastPointerEntered` / `OnToastPointerExited` | `toastPanel` | `_toastHover`: geri sayımı durdurur / sürdürür. |
| `OnToastDismiss` | Sağ üstteki ✕ | `HideToast()`. |
| `OnToastPrimary` | Sağdaki (mavi) düğme | `NewImage` (**Aç**): `_vm.IsProcessing` ya da `_vm.IsExporting` ise `StatusText = StatusToastBusy` (bildirim açık kalır); değilse `HideToast()`, dosya hâlâ varsa `LoadImageAndFit(path)`. `Export` (**Aç**): `HideToast()`, `OpenWithSystem(path)`. `Project` (**Klasörü aç**): `HideToast()`, `ShowInFolder(path)`. |
| `OnToastSecondary` | Soldaki düğme | `HideToast()`; `Export` türünde (**Klasörü aç**) ardından `ShowInFolder(path)`. Diğer türlerde **Kapat**. |
| `OpenWithSystem(path)` | `OnToastPrimary` | `Process.Start` (`UseShellExecute = true`): dosyayı sistemin varsayılan programıyla açar; hata durum çubuğuna `StatusError`. |
| `ShowInFolder(path)` | Bildirim düğmeleri | Windows: `explorer.exe /select,"<yol>"`; macOS: `open -R <yol>` (Finder'da seçili); diğerleri: `OpenWithSystem(klasör)`. Hata durum çubuğuna `StatusError`. |
| `OnRunMosaic` | `menuMosaicize`, Mos düğmesi (`CanRunMosaic`), Ctrl/⌘+M | `await _vm.RunMosaicAsync()` ardından `FitToWindow`. |
| `OnTogglePixelEdit` | Araçlar menüsü (`MosaicDone`), toolbar kalem düğmesi | `_vm.TogglePixelEditMode()`; mod kapanınca `catalogListBox` seçimini temizler. |
| `OnToggleGrid` | Araçlar menüsü, toolbar Izgara Flyout'undaki düğme | `_vm.ShowGrid` tersine çevrilir. |
| `OnGridMainColorPick` | Izgara Flyout'u, `GridColorPresets` düğmeleri (`Tag` = renk) | `_vm.GridColor` + `_vm.SelectMainColor` (tonları üretir). |
| `OnGridColorPick` | Izgara Flyout'u, `GridColorShades` düğmeleri | Yalnızca `_vm.GridColor`. |
| `OnSelectInterpolation` | İnterpolasyon Flyout'u, `InterpolationMethods` düğmeleri (`Tag`) | `_vm.SelectedInterpolation`. |
| `OnToggleOptimum` | Araçlar → Optimum | `_vm.UseOptimal` tersine çevrilir (toolbar'daki onay kutusu doğrudan bağlamadır). |
| `OnToggleStockAware` | Araçlar → Stoğa göre (onay işareti `UseStockAware`'e bağlı) | `_vm.UseStockAware` tersine çevrilir (toolbar'daki "Stoğa göre" onay kutusu doğrudan bağlamadır). |
| `OnAdjustSliderDragging` | `AdjustSlider.DraggingChanged` (kurucuda abone, pencere kapanınca bırakılır) | `_vm.SetAdjustDragging(dragging)`. |
| `FreezeMosaicView` / `ReleaseMosaicFreeze` | `_vm.MosaicFrozen` true / false | Bir Mos (Mos düğmesi ya da Anlık Mos) ekrandaki mozaiğin yerine yenisini hazırlarken görsel alanının (`imageScroller`) o anki hâli `RenderTargetBitmap` ile `mosaicFreeze` görüntüsüne çizilir ve üstte gösterilir (Mos sürerken mozaik gizlenip görsel görünmesin). Bitişten 250 ms sonra (`_freezeTimer`; yeni mozaiğin karoları çizilsin) kaldırılır; bu arada yeni bir Mos başlarsa aynı görüntü kalır. |
| `OnToggleLiveMos` | Araçlar → Anlık Mos (onay işareti `LiveMos`'a bağlı) | `_vm.LiveMos` tersine çevrilir (Görsel Ayarları sütunundaki Anlık Mos kutusu doğrudan bağlamadır). |
| `OnLiveMosAllColors` / `OnLiveMosSelectedColors` | Araçlar → Anlık Mos: Tüm Renkler / Seçili Renkler | `_vm.LiveMosAllColors = true` / `false` (sütundaki iki seçenek doğrudan bağlamadır). |
| `OnStonesSuggested` | Araçlar → Taş Sayısı → Önerilen (`OptimalAvailable`) | İşlem sürmüyorsa `OptimalK = OptimalKSuggested`. |
| `OnStonesMore` | Araçlar → Taş Sayısı → Artır | `OptimalK + 1` (en çok `OptimalKMax`). |
| `OnStonesLess` | Araçlar → Taş Sayısı → Azalt | `OptimalK - 1` (en az 1). |
| `Opened` (lambda) | Pencere açıldığında | `_vm.LoadStockOnStartupAsync()`: stok kg'ı tablodan yükler. |
| `OnStockSheet` | Araçlar → Stok → Aç, toolbar stok tablosu düğmesi ve ok Flyout'u (`CanUseStock`) | `_vm.OpenStockSheetAsync()`. |
| `OnStockSettings` | Araçlar → Stok → Ayarlar, stok tablosu düğmesinin `ContextMenu`'sü ve ok Flyout'u | `_vm.ConfigureStockAsync()`. |
| `OnStockFetch` | Araçlar → Stok → Stok Çek → Devre dışı bırak, toolbar Stok Çek ve ok Flyout'u | `_vm.FetchStockAsync()`. |
| `OnStockFetchMark` | Araçlar → Stok → Stok Çek → Kırmızıyla işaretle, Stok Çek ok Flyout'u | `_vm.FetchStockAsync(markOnly: true)`. |
| `OnStockCheck` | Araçlar → Stok, toolbar Stok Kontrol | `_vm.CheckStockAsync()`. |
| `OnStockClearOne` | Araçlar → Stok, toolbar Stok Sil (sol tık ve `ContextMenu`) | `_vm.ClearStockOneAsync()`. |
| `OnStockClearAll` | Araçlar → Stok, Stok Sil düğmesinin `ContextMenu`'sü | `_vm.ClearStockAllAsync()`. |
| `OnStockAdd` | Araçlar → Stok, toolbar Stok Ekle | `_vm.AddStockAsync()`. |
| `OnShowHelp` | Yardım → Kullanım Kılavuzu | `ShowHelp()` → `new HelpWindow().ShowDialog(this)`. |
| `OnToggleTheme` | Tema düğmesi, **Görünüm → Tema → Açık Tema** | `ThemeService.SetLight(!ThemeService.IsLight)`. |
| `OnPickTheme` | **Görünüm → Tema →** palet | `ThemeService.SetPalette` (id öğenin `Tag`'inde: `lapis`, `pastel`, `grafit`, `traverten`, `murekkep`). |
| `OnThemeChanged` | `ThemeService.Changed` | `UpdateThemeUi()` (ay/güneş ikonu, `themeCheck_<id>` ve `themeCheckLight` onay işaretleri) ve `ApplyImageTint()`. Pencere kapanınca abonelik bırakılır. |
| `OnSetLanguageTr` / `OnSetLanguageEn` | Dil Flyout'undaki TR / EN düğmeleri | `Loc.Instance.Lang = "tr"/"en"` + `_vm.RefreshLocalized()`. |
| `OnWidthGotFocus` | Genişlik `TextBox` (`GotFocus`) | Metnin tamamını seçer (`Dispatcher.UIThread.Post` ile). |
| `OnWidthTextInput` | Genişlik `TextBox` (`TextInput`) | Yazılan `,` karakterini `.` yapar. |
| `OnWidthChanged` | Genişlik `TextBox` (`LostFocus`) | Virgülleri noktaya çevirir, `_vm.UpdateDimensions()`. |
| `OnWidthKeyDown` | Genişlik `TextBox` (`KeyDown`) | Enter'da `OnWidthChanged` ile aynı işlem. |
| `OnColorCheckChanged` | `catalogListBox` satırındaki `CheckBox` (`Click`) | `ColorItem.IsExcluded` ayarlanır, `_vm.SyncColorExclusion(item)`. |
| `OnCatalogSelectionChanged` | `catalogListBox` (`SelectionChanged`) | Yalnızca piksel düzenleme açıkken seçili katalog taşını kaynak yapar (`_vm.SetSourceFromCatalog`). |
| `OnImageWheel` | Tuvaldeki `Image` veya `MosaicView` (`PointerWheelChanged`) | İmleç merkezli yakınlaştırma. Adım `e.Delta.Y` ±4'e kırpılır (0 ise bir şey yapılmaz); hedef `1,25^adım` ile çarpılır, böylece fare tekerleğinin bir çentiği ×1,25 / ×0,8, dokunmatik yüzeyin kesirli adımları orantılı olarak daha az olur. Önce kaydırma süzülmesi durur. Hedef, süzülme sürüyorsa önceki hedeften, yoksa geçerli zoom'dan hesaplanır ve `[MinZoomLevel, 20]` aralığına kırpılır. `_zoomImg`, `_zoomMouse` ve `_zoomAnchor` (imlecin altındaki nokta ÷ geçerli zoom) kaydedilir. `SmoothMouse` kapalıysa zoom `ApplyZoomAnchored(hedef)` ile hemen uygulanır; açıksa süzülme yoksa başlatılır (`TopLevel.RequestAnimationFrame(ZoomGlideFrame)`). |
| `ZoomGlideFrame(now)` | `RequestAnimationFrame` | Süzülme durdurulmuşsa çıkar. `next = cur · (hedef/cur)^(1 − e^(−dt/ZoomGlideSeconds))` (log ölçekte üstel yaklaşma); hedefe %0,2'den yakınsa hedefe oturur. `ApplyZoomAnchored(next)`; bittiyse ya da zoom değişmediyse (sınıra dayandı) süzülme biter, değilse sonraki kare istenir. |
| `ApplyZoomAnchored(zoom)` | `OnImageWheel`, `ZoomGlideFrame` | `_vm.ZoomLevel = zoom` (sınırı VM uygular); `imageScroller.UpdateLayout()` ile yeni boyut yerleşime işlenir (yoksa ofset eski boyuta göre kırpılır); `_zoomAnchor · ZoomLevel` noktası `TranslatePoint` ile `imageScroller`'a çevrilip ofset bu nokta `_zoomMouse`'un altına gelecek kadar kaydırılır (negatif olmaz). Kenar boşluğu ve görüntü pencereden küçükken ortalanması da böylece doğru hesaplanır. Ardından `UpdateNav()`. |
| `StopZoomGlide()` / `StopPanGlide()` | Görsele basma, `OnResetSize`, `LoadImageAndFit`, `OnToggleSmoothMouse` (kapatınca); `StopPanGlide` ayrıca `OnImageWheel` | Süzülmeyi durdurur (bekleyen kare hiçbir şey yapmadan çıkar). |
| `RecordPanSample(p)` | Sürükleme başlarken, `OnImagePointerMoved` (kaydırma sürerken), bırakırken | Konumu zamanla birlikte kuyruğa ekler; 80 ms'den eski örnekleri (en az 2 kalacak şekilde) atar. |
| `StartPanGlide(releasedAt)` | `OnImagePointerReleased` | Son örneği ekler; hız = (en eski örnek − bırakma noktası) / geçen süre (ofset yönünde). `SmoothMouse` kapalıysa, süre ≤ 5 ms ise ya da hız < 150 px/sn ise süzülmez; yoksa `PanGlideFrame` başlar. |
| `PanGlideFrame(now)` | `RequestAnimationFrame` | Ofset `hız · dt` kadar kayar (negatif olmaz), hız `e^(−dt/PanGlideSeconds)` ile azalır, `UpdateNav()`. Ofset değişmediyse (kenar) ya da hız < 20 px/sn ise durur. |
| `OnToggleSmoothMouse` | **Görünüm → Yumuşak Fare Hareketi** | `_vm.SmoothMouse` tersine çevrilir; kapatılınca süren süzülmeler durur. |
| `OnImagePointerPressed` | `Image` veya `MosaicView` (`PointerPressed`); gönderen herhangi bir `Control` olabilir | Her basışta zoom ve kaydırma süzülmesini durdurur. Sağ tuş: kaydırmayı başlatır (örnek kuyruğu temizlenip ilk örnek eklenir), işaretçiyi yakalar. Sol/orta: `_vm.OnImagePressed(...)`; orta tuşla (düzenleme dışı) katalog seçimini temizler. |
| `OnImagePointerMoved` | `Image` veya `MosaicView` (`PointerMoved`); gönderen herhangi bir `Control` olabilir | Kaydırma sürüyorsa konum örneğini kaydeder (`RecordPanSample`) ve ofseti günceller (imleci birebir izler); değilse `_vm.OnImagePointerMoved` (özellikler paneli). |
| `OnImagePointerReleased` | `Image` veya `MosaicView` (`PointerReleased`) | Kaydırmayı bitirir, yakalamayı bırakır, `StartPanGlide` ile bırakma hızına göre süzülmeyi başlatır. |
| `OnScrollChanged` | `imageScroller` (`ScrollChanged`) | `UpdateNav()` → `_vm.UpdateNavigator(...)`. |
| `OnNavPointerPressed` / `OnNavPointerMoved` | `navPanel` | Tıklanan/sürüklenen noktayı görünümün merkezine getirir (`NavigateFromNav` → `_vm.NavigatorTarget`). Konum, kenarlığın içindeki içerik paneline (`NavContent`) göre alınır; `UpdateNav` da bu panelin boyutunu (`NavInnerSize`) gönderir. |
| `OnSelectStone` | Özellikler paneli varyant düğmeleri (`Tag` = `Index`) | `_vm.SelectStone(index)`. |
| `OnKeyDown` | Pencere (`KeyDown +=`) | Kısayollar (aşağıda). |
| `OnDragOver` / `OnDrop` | Pencere (`AddHandler`) | Sürükle-bırak (aşağıda). |
| `OnMosAnimTick` | `_mosAnimTimer.Tick` | `mosQ0..mosQ3` karelerini sırayla yakar (opaklık 1 / 0,15). |
| `OnExportAnimTick` | `_exportAnimTimer.Tick` | `exportArrow`'un `TranslateTransform.Y` değerini `ExportAnimOffsets` dizisinden alır. |
| Menü lambdaları | `BuildToolsMenu`, `RebuildGridShadeItems` içinde oluşturulan `MenuItem.Click` | İnterpolasyon → `SelectedInterpolation`; ana renk → `GridColor` + `SelectMainColor`; ton → `GridColor`. |

## Önemli davranışlar ve iş kuralları

### Kısayollar

`SetMenuShortcutTexts` menülerde görünen kısayol metinlerini kodda atar (`MenuItem.InputGesture`), böylece macOS'ta ⌘, diğerlerinde Ctrl gösterilir. `InputGesture` **yalnızca gösterimdir**; asıl davranış `OnKeyDown` içindedir.

| Menü öğesi | `InputGesture` |
|---|---|
| `menuLoadImage` | `CmdKey` + I |
| `menuOpenProject` | `CmdKey` + O |
| `menuSave` | `CmdKey` + S |
| `menuSaveAs` | `CmdKey` + Shift + S |
| `menuExport` | `CmdKey` + E (alt menülü öğede yalnızca gösterim; kısayol varsayılan kaliteyle hızlı dışa aktarır) |
| `menuFitToScreen` | `CmdKey` + 0 (`Key.D0`) |
| `menuMosaicize` | `CmdKey` + M |

`OnKeyDown` tablosu (`CmdKey` = macOS'ta ⌘/`Meta`, diğerlerinde `Control`; değiştiriciler **tam eşleşmelidir**):

| Tuş | Değiştirici | Eylem | Koşul |
|---|---|---|---|
| Z | Ctrl **veya** ⌘ (iki platformda da) | `_vm.UndoPixelEdit()` | — |
| Y | Ctrl veya ⌘ | `_vm.RedoPixelEdit()` | — |
| Z | Ctrl+Shift veya ⌘+Shift | `_vm.RedoPixelEdit()` | — |
| I | `CmdKey` | `OnLoadImage` | — |
| O | `CmdKey` | `OnOpenProject` | — |
| S | `CmdKey` | `OnSaveProject` | `_vm.CanSaveProject` |
| S | `CmdKey` + Shift | `OnSaveAsProject` | `_vm.CanSaveProject` |
| E | `CmdKey` | `OnExportImage` (`DefaultExportQuality` ile) | `_vm.CanExport` |
| 0 / NumPad0 | `CmdKey` | `OnResetSize` | — |
| M | `CmdKey` | `OnRunMosaic` | (`RunMosaicAsync` içinde `CanRunMosaic`) |
| F1 | yok | `ShowHelp()` | — |
| F4 | yok | `OnTogglePropertiesPanel` | — |
| Esc | yok | `_vm.CancelWork()` (durum çubuğundaki İptal düğmesiyle aynı) | `_vm.CanCancel`; tablonun en başında denetlenir, olay `Handled` işaretlenir |

Koşul sağlanmasa da Ctrl/⌘+S/E olayı `Handled` işaretlenir. Esc ise yalnızca `CanCancel` true iken yakalanır; iptal edilecek iş yoksa olay diğer kontrollere (ör. açık bir menü ya da Flyout) geçer.

### Kayıt ve dışa aktarma klasörleri

| İşlem | Hedef | Kural |
|---|---|---|
| Kaydet (`OnSaveProject`) | `Masaüstü/mosairPROJECT/<ad>/<ad>.mos` | `<ad>` = `ProjectService.CurrentPictureFileName` dosya adı (uzantısız), yoksa `mosair_project`. Klasörler yoksa oluşturulur. Kaynak görüntü `<ad><uzantı>` olarak aynı klasöre, **yalnızca orada yoksa** kopyalanır. Mevcut `.mos` sorulmadan üzerine yazılır. |
| Farklı Kaydet (`SaveAsDialog`) | Seçilen `X/foo.mos` → **`X/foo/foo.mos`** | Önerilen ad kaynak görüntü adı. Seçilen klasörün içinde proje adıyla alt klasör açılır. Görüntü yeniden adlandırılmadan, kendi adıyla ve yalnızca orada yoksa projenin yanına kopyalanır (`ProjectService.WriteSnapshot`; hızlı kayıt da aynısını yapar). |
| Dışa aktar (`ExportQuickAsync`) | `Masaüstü/mosairEXPORT/<M.dd.yyyy>_<HH.mm.ss>__<ad>__<G>x<Y>.<jpeg\|png>` | `<ad>` kaynak görüntü adı ya da `mosair`; `<G>x<Y>` = `WidthCm` × `HeightCm` (tam sayıya yuvarlanmış). Uzantı `QuickExportExtension(quality)`: JPEG mümkünse ve tamponu kullanılabilir belleğin yarısını aşmıyorsa `jpeg`, değilse `png`. Klasör `GetExportDir()` ile oluşturulur. |
| Farklı dışa aktar (`ExportAsAsync`) | Seçilen yol | JPEG veya PNG; biçim uzantıdan belirlenir (`ExportImageAsync`). |

Masaüstü yolu: kayıtta `Environment.SpecialFolder.Desktop`, dışa aktarmada `Environment.SpecialFolder.DesktopDirectory` kullanılır.

### Sürükle-bırak

- `OnDragOver`: veri dosya içeriyorsa `DragDropEffects.Copy`, yoksa `None`.
- `OnDrop`: bırakılan dosyalardan uzantısı `.png/.jpg/.jpeg/.bmp/.tiff` olan **ilki** `LoadImageAndFit` ile yüklenip sığdırılır; `.mos` sürükle-bırakla açılmaz.
- İşleyiciler pencereye eklenmiştir ama `DragDrop.AllowDrop="True"` yalnızca tuval `Border`'ında vardır; bırakma pratikte tuval alanında çalışır.

### Özellikler panelini gizleme

`ApplyPropertiesPanel` `mainGrid.ColumnDefinitions[6]` (panel) ve `[5]` (ayırıcı) sütunlarını ayarlar:

- **Gizle:** o anki genişlik 24 px'ten büyükse `_propertiesWidth`'e saklanır; panel sütunu `MinWidth = MaxWidth = Width = 24`, ayırıcı sütunu 0 px ve `propsSplitter.IsVisible = false`. Yerine şerit görünür (XAML'de `!IsPropertiesPanelOpen`).
- **Göster:** `MinWidth = 160`, `MaxWidth = 360`, `Width = _propertiesWidth` (başlangıçta 220), ayırıcı 4 px ve görünür.

Durum kalıcı değildir; pencere her açılışta panel kapalı başlar (kurucu sonunda `ApplyPropertiesPanel()`, `IsPropertiesPanelOpen` varsayılanı false). Görsel Ayarları sütunu da aynı şekilde kapalı başlar (kurucuda `ApplyAdjustPanel()`, `IsAdjustPanelOpen` varsayılanı false; ilk açılışta 300 px). Bir taş seçilince (`HasSelection = true`) ViewModel paneli açar.

### Görsel Ayarları sütunu

`ApplyAdjustPanel` `mainGrid.ColumnDefinitions[4]` (sütun) ve `[3]` (ayırıcı) sütunlarını ayarlar; Özellikler panelininkiyle aynı mantık:

- **Gizle:** o anki genişlik 24 px'ten büyükse `_adjustWidth`'e saklanır; sütun `MinWidth = MaxWidth = Width = 24`, ayırıcı 0 px ve `adjustSplitter.IsVisible = false`. Yerine şerit görünür.
- **Göster:** `MinWidth = 260`, `MaxWidth = 600`, `Width = _adjustWidth` (başlangıçta 300), ayırıcı 4 px ve görünür.

Durum kalıcı değildir; pencere her açılışta sütun açık başlar. Özellikler paneli bu sütundan bağımsızdır.

### Yeni görsel bildirimi

Aynı `toastPanel` mosair'in yazdığı dosyalar için de kullanılır (aşağıda "Kaydedilen dosya bildirimi"). İndirilenler ve Masaüstü klasörlerine gelen JPEG/PNG dosyalarını [NewImageWatcher](Services/NewImageWatcher.md) bildirir; pencere sağ altta `toastPanel`'i 7 sn gösterir (fare üzerindeyken süre durur). `toastPanel` ana `Grid`'in beş sütununu da kaplar ve son eleman olduğu için panellerin üzerinde çizilir; Özellikler paneli gizliyken de canvas'ın sağ altında görünür. Yalnızca bir bildirim vardır; yenisi öncekinin içeriğini ve süresini değiştirir. **Aç** görseli `LoadImageAndFit` ile yükleyip görsel alanına sığdırır: açık mozaik için onay sorulmaz (sürükle-bırak ve Görsel Yükle ile aynı).

### Kaydedilen dosya bildirimi

`MainViewModel.FileSaved` dışa aktarma (mosairEXPORT / mosairEXPORT As), ekran görüntüsü ve proje kaydı (Kaydet, Farklı Kaydet; Drive değil) başarıyla bitince tetiklenir. Pencere aynı sağ alt bildirimi 7 sn gösterir (fare üzerindeyken durur, ✕ kapatır):

| Tür | Başlık | Bilgi satırı | Sol düğme | Sağ düğme | Küçük resim |
|---|---|---|---|---|---|
| Dışa aktarma | `ToastExported` | Dosyanın klasörü | **Klasörü aç** | **Aç** (varsayılan programla) | Dosyadan (64 MB'a kadar) |
| Ekran görüntüsü | `ToastScreenshotSaved` | Dosyanın klasörü | **Klasörü aç** | **Aç** | Dosyadan |
| Proje | `ToastProjectSaved` | `.mos`'un klasörü | **Kapat** | **Klasörü aç** | Mozaiğin genel görüntüsü (`NavBitmap`) |

**Klasörü aç** dosyayı Dosya Gezgini / Finder'da seçili gösterir. Bildirim tek kutudur: yeni görsel bildirimi ile kaydedilen dosya bildirimi birbirinin yerine geçer.

### Tema

`OnToggleTheme` uygulama genelinde `ThemeVariant.Light` ↔ `ThemeVariant.Dark` geçişi yapar; `OnPickTheme` renk paletini değiştirir (ikisi de [ThemeService](Services/ThemeService.md) üzerinden, seçim `ui.json`'da saklanır). Pencere renkleri `ThemeDictionaries` + `DynamicResource` ile otomatik değişir. Kurucu sonunda `UpdateThemeUi()` kaydedilmiş seçimi menüye ve ikona yansıtır.

**Görsele uyan arka plan (`ApplyImageTint`):** Tuval (`canvasBorder`) ve sol paneldeki ölçü bölümü (`dimsBorder`) yüklenen görselin rengine uyan sakin bir ton alır. `_vm.ImageAccent`'in tonu (H) korunur; doygunluk `min(S, 0.6)` × 0.35 (tuval) / 0.30 (ölçü bölümü) koyu temada, × 0.30 / 0.25 açık temada; açıklık koyu temada 0.14 / 0.16, açık temada 0.90 / 0.93 (`HslColor` → RGB). `ImageAccent` null ise (görsel yok) temanın `BgCanvas` / `BgBar` renkleri yerel değer olarak verilir. Çağrıldığı yerler: `_vm.PropertyChanged` (`ImageAccent`), pencerenin `ActualThemeVariantChanged` olayı (tema düğmesi ya da sistemin tema değişimi) ve `OnThemeChanged` (palet değişince görsel yokken yeni paletin `BgCanvas`/`BgBar`'ı). İki `Border`'da da 0,4 sn `BrushTransition` vardır. Amaç: görsel öne çıksın, taş renkleri güçlü bir renge karşı değerlendirilmesin. Her zaman açıktır, ayarı yoktur.

### Dil

`Loc.Instance.Lang` atanınca `Loc` tüm indeksleyici bağlamalarını (`{Binding [Anahtar], Source={x:Static svc:Loc.Instance}}`) yeniler; ViewModel'in ürettiği metinler için ayrıca `_vm.RefreshLocalized()` çağrılır. Varsayılan dil `tr`; seçim kalıcı değildir.

### Araçlar menüsü

`BuildToolsMenu`, toolbar'daki Flyout kontrollerinin menü eşlerini kodla üretir: `MainViewModel.InterpolationMethods`, `MainViewModel.GridColorPresets` ve dinamik `GridColorShades`. Seçili değer `PathIcon` onay işaretiyle gösterilir (`NewMenuCheck` oluşturur, `SetMenuCheck` gösterir/gizler, `RefreshToolsMenuChecks` hepsini yeniler; renk öğelerinin başlığı `ColorSwatch` kutusudur); aynı renk hem ana renkte hem tonda varsa yalnızca ilki işaretlenir.

## Dikkat / bilinen sınırlamalar

- Dosya menüsündeki `menuSave` ve `menuSaveAs` (ve toolbar'daki iki kaydet düğmesi) `CanSaveProject`'e (`MosaicDone && !IsSavingProject`; bir kayıt sürerken pasif), `menuExport` ve `menuExportAs` `CanExport`'a bağlıdır (ikisi de doğrudan dışa aktarmaz, yalnızca kalite alt menüsünü açar); toolbar ve klavye yoluyla aynı koşullar geçerlidir. Ekran Görüntüsü Al `ImageLoaded`'a, İşlemi İptal Et `CanCancel`'a bağlıdır. **Google Drive** alt menüsü, toolbar'daki Drive ikonu ve oku `CanUseDrive`'a bağlıdır (bir Drive işlemi sürerken pasif); mozaik yokken de etkindir, Drive'a Kaydet bu durumda `DriveNoMosaic` uyarısı verir.
- Menüdeki İşlemi İptal Et'in `InputGesture="Escape"` değeri XAML'de sabittir ve yalnızca gösterimdir; Esc'yi `OnKeyDown` yakalar. Repo kuralı gereği her özellik üst menüde de bulunur (CLAUDE.md, "Menüler").
- `OnSaveProject` ve `SaveAsDialog` klasör oluşturma, kaydetme ve görsel kopyalamayı `try/catch` içinde yapar; disk/izin hatasında uygulama kapanmaz, `MainViewModel.ReportSaveFailed` "Proje kaydedilemedi" uyarısını gösterir ve ✓ simgesi gösterilmez. Proje dosyası arka planda önce `.part` olarak yazılıp sonra yerine taşındığından, yarıda kalan bir kayıt var olan projeyi bozmaz. Kayıt tıklandığı andaki mozaiği yazar.
- Kayıt (`SpecialFolder.Desktop`) ve dışa aktarma (`SpecialFolder.DesktopDirectory`) farklı özel klasör sabitleri kullanır; çoğu sistemde aynı yeri gösterir ama tutarsızdır.
- `OnRunMosaic`, `RunMosaicAsync` hiçbir şey yapmadan dönse bile `FitToWindow` çağırır.
- `ShowHelp` her çağrıda yeni bir modal `HelpWindow` açar ve beklemez.
- Menülerdeki stok ve diğer bazı başlıklar `Loc` anahtarlarıyla gelir; yeni menü eklerken anahtar hem `Tr` hem `En` sözlüğüne eklenmelidir.

## İlgili dosyalar

- [MainViewModel](ViewModels/MainViewModel.md)
- [App](App.md)
- [HelpWindow](HelpWindow.md)
- [AlertDialog](Controls/AlertDialog.md) · [ConfirmDialog](Controls/ConfirmDialog.md) · [StockSettingsDialog](Controls/StockSettingsDialog.md) · [DriveSettingsDialog](Controls/DriveSettingsDialog.md) · [DriveOpenDialog](Controls/DriveOpenDialog.md) · [DriveService](Services/DriveService.md)
- [GridOverlay](Controls/GridOverlay.md), [MosaicView](Controls/MosaicView.md), [AdjustSlider](Controls/AdjustSlider.md)
- [AdjustParam](ViewModels/AdjustParam.md), [ImageAdjustService](Services/ImageAdjustService.md)
- [InvariantDoubleConverter](Converters/InvariantDoubleConverter.md)
- [Loc](Services/Loc.md)
- [NewImageWatcher](Services/NewImageWatcher.md)
- [ProjectService](Services/ProjectService.md)
- [PixelEditService](Services/PixelEditService.md)
- [Kullanıcı arayüzü (ARAYUZ)](../ARAYUZ.md)
