# MainWindow

> Kaynak: `mosair/MainWindow.axaml`, `mosair/MainWindow.axaml.cs` · Güncelleme: 2026-10-06

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
| `Title` | `" "` | Başlık metni boş; marka adı başlık çubuğunda yeşil "mosair" olarak çizilir. |
| `Icon` | `avares://mosair/Assets/mosair-icon.png` | — |
| `Window.Resources` | `InvDouble` | Tema renkleri (`BgMain`, `FgPrimary` …) `App.axaml`'da, uygulama düzeyinde tanımlıdır; bkz. [App](App.md). |

### Yerleşim ağacı

```
Window
└─ DockPanel
   ├─ [Top] Başlık çubuğu (Border, 32 px, PointerPressed=OnTitleBarPointerPressed)
   │   └─ "mosair" logosu + Menu
   │       ├─ Dosya (MenuFile): menuLoadImage, menuOpenProject | menuSave, menuSaveAs | menuExport, Farklı Dışa Aktar
   │       ├─ Düzen (MenuEdit): Tümünü Seç, Tümünü Kaldır
   │       ├─ Görünüm (MenuView): menuFitToScreen
   │       ├─ Araçlar (MenuTools): menuMosaicize, Piksel Düzenle, Izgara Göster,
   │       │     menuGridColor*, menuInterp*, menuDetail*  (*kodda doldurulur: BuildToolsMenu)
   │       │     | Optimum, Stoğa göre, Taş Sayısı ▸ (Önerilen / Artır / Azalt),
   │       │     Stok ▸ (Aç, Ayarlar | Çek ▸ (Devre dışı bırak / Kırmızıyla işaretle), Kontrol, Sil, Tümünü Sil, Ekle)
   │       └─ Yardım (MenuHelp): Kullanım Kılavuzu (F1)
   ├─ [Top] Araç çubuğu (toolbar)
   │   ├─ Sol: Görüntü Yükle, Proje Aç, Kaydet (saveProjectBtn: saveIcon/saveCheckIcon), Farklı Kaydet,
   │   │       Mos düğmesi (mosAnimGrid: mosQ0..mosQ3), Piksel Düzenle + source/target göstergesi,
   │   │       Izgara (Flyout: aç/kapa, GridColorPresets, GridColorShades), İnterpolasyon (Flyout),
   │   │       Detay (Flyout: StonePixelSize kaydırıcısı 10–100, adım 10),
   │   │       5 stok düğmesi (CanUseStock; tablo ve Sil düğmelerinde sağ tık ContextMenu; tablo ve Stok Çek düğmelerinin yanında açılır ok Flyout'u: tablo → Aç / Ayarlar, Stok Çek → Devre dışı bırak / Kırmızıyla işaretle),
   │   │       Optimum onay kutusu, "Stoğa göre" onay kutusu (UseStockAware; ipucu StockAwareTip) + Taş kaydırıcısı (OptimalAvailable)
   │   └─ Sağ: exportBtn (exportArrow animasyonu; sağ tık = klasörü aç) + açılır ok (Flyout: Dışa Aktar / Farklı Dışa Aktar),
   │           Tema düğmesi (iconDark / iconLight), Dil düğmesi (Flyout: TR / EN)
   ├─ [Bottom] Durum çubuğu: Panel → ActivityWave (IsActive = IsBusy, arka plan dalgası) + Grid "*,Auto,*" (Margin 8,3)
   │   ├─ Sol: UsedColorInfo
   │   ├─ Orta: StatusText + Progress (IsProcessing) + ElapsedTime
   │   └─ Sağ: ZoomInfo + Ekrana Sığdır düğmesi
   └─ Ana içerik: Grid (380 | 4 | * | 4 | 220)
       ├─ Sütun 0 — Sol panel
       │   ├─ Boyut satırı: WidthCm kutusu (InvDouble) + cm, StoneInfo / MouldInfo / OriginalInfo
       │   ├─ Tümünü Seç / Tümünü Kaldır düğmeleri
       │   └─ 3 sütun (başlıklar ColCatalog / ColMatch / ColAssigned)
       │       ├─ catalogListBox (CatalogColors): onay kutusu + renk + kod/ad + stok kg
       │       ├─ paletteScroll (PaletteColors): eşleşen renk kareleri
       │       └─ assignedScroll (AssignedColors): ID, CodeName, PixelCount
       ├─ Sütun 1 — GridSplitter
       ├─ Sütun 2 — Tuval (Border, DragDrop.AllowDrop="True")
       │   ├─ imageScroller (ScrollViewer)
       │   │   └─ Panel (ImageDisplayWidth × ImageDisplayHeight)
       │   │       ├─ Image (DisplayBitmap; yalnızca !MosaicDone; wheel/move/press/release olayları)
       │   │       ├─ ctrl:MosaicView x:Name="mosaicView" (yalnızca MosaicDone; RenderSource, OverviewBitmap,
       │   │       │     StonePixelSize, ShowGrid, GridColor; aynı wheel/move/press/release olayları)
       │   │       └─ ctrl:GridOverlay
       │   ├─ navPanel (150×150 gezgin, sağ üst; ImageLoaded): Image (NavBitmap) + NavView* dikdörtgeni
       │   └─ N uyarısı (NWarningVisible): WarnPerfTitle / WarnPerfBody + Tamam (OnDismissNWarning)
       ├─ Sütun 3 — GridSplitter
       └─ Sütun 4 — Özellikler paneli (PropTitle; HasSelection)
           ├─ Renk örneği, PropStoneId / PropStoneName
           ├─ Doku önizleme (PropTextureBitmap)
           ├─ Taş varyantları (PropStoneThumbs → OnSelectStone)
           ├─ RGB (PropRgbInfo), Koordinat (PropPixelCoord / PropMouldCoord)
           └─ Düzenleme bilgisi (IsPixelEditActive: SelectedStoneText, EditedPixelCountText, geri al/yinele ipucu)
```

### Özel alanlar (code-behind)

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `_vm` | `MainViewModel` | yapıcıda | DataContext. |
| `_isPanning`, `_panStart`, `_scrollStart` | `bool`, `Point`, `Vector` | `false` | Sağ tuşla kaydırma durumu. |
| `_syncingScroll` | `bool` | `false` | Palet/Atanan sütunlarının kaydırma eşitlemesinde yeniden giriş kilidi. |
| `_mosAnimTimer`, `_mosAnimFrame` | `DispatcherTimer?`, `int` | `null`, 0 | Mos düğmesi animasyonu (350 ms). |
| `_exportAnimTimer`, `_exportAnimFrame` | `DispatcherTimer?`, `int` | `null`, 0 | Dışa aktarma ok animasyonu (90 ms, `ExportAnimOffsets`). |
| `CmdKey` | `static readonly KeyModifiers` | `Meta` (macOS) / `Control` | `Loc.IsMac`'e göre. |
| `_interpMenuItems`, `_detailMenuItems`, `_gridColorMenuItems`, `_gridShadeMenuItems`, `_gridShadeSeparator` | listeler | boş | Araçlar menüsünde kodla oluşturulan öğeler ve onay işaretleri. |
| `CheckGeometry` | `const string` | — | Menü onay işaretinin yol geometrisi. |
| `_isLightTheme` | `bool` | `false` | Tema durumu. |

## Public API

`MainWindow`'un yapıcısı dışında public üyesi yoktur. Yapıcının yaptıkları:

| Adım | Ne yapar |
|---|---|
| `InitializeComponent()` | XAML'ı yükler. |
| `_vm.ShowAlert` | `AlertDialog` gösterir. |
| `_vm.ShowConfirm` | `ConfirmDialog` gösterir, `bool` döndürür. |
| `_vm.ShowStockSettings` | `StockSettingsDialog` gösterir, `StockSheetService.Config?` döndürür. |
| `_vm.OpenUrl` | `TopLevel.GetTopLevel(this)?.Launcher.LaunchUriAsync` ile tarayıcıda açar (stok tablosu). |
| `_vm.StoneInvalidated += …` | Tek taş değişince (piksel düzenleme, geri al/yinele, varyant seçimi) `mosaicView.InvalidateStone(row, col)`: yalnızca o taşı içeren karolar yeniden çizilir. |
| `AddHandler(DragDrop.DropEvent / DragOverEvent)` | Sürükle-bırak. |
| `KeyDown += OnKeyDown` | Pencere düzeyi kısayollar. |
| `paletteScroll` ↔ `assignedScroll` | İki `ScrollChanged` lambdası dikey ofseti karşılıklı eşitler (`_syncingScroll` ile döngü engellenir). |
| `_vm.PropertyChanged` lambdası | `IsProcessing` → Mos animasyonu (`StartMosAnim`/`StopMosAnim`), `IsExporting` → dışa aktarma animasyonu (`StartExportAnim`/`StopExportAnim`), `GridColorShades` → ton menüsünü yeniden kur, `SelectedInterpolation` / `StonePixelSize` / `GridColor` → menü onaylarını yenile. |
| `BuildToolsMenu()`, `SetMenuShortcutTexts()` | Kodla oluşan menüler ve kısayol metinleri. |

### Olay işleyicileri

| Handler | Bağlı olduğu kontrol | Ne yapar |
|---|---|---|
| `OnTitleBarPointerPressed` | Başlık çubuğu `Border` | Sol tıkta pencereyi sürükler (`BeginMoveDrag`), çift tıkta büyüt/eski boyut. Tıklanan öğe `Button`, `MenuItem` veya `Menu` içindeyse hiçbir şey yapmaz. |
| `OnLoadImage` | `menuLoadImage`, toolbar Görüntü Yükle, Ctrl/⌘+I | Dosya seçici (`*.png, *.jpg, *.jpeg, *.bmp, *.tiff`) → `_vm.LoadImage`. |
| `OnOpenProject` | `menuOpenProject`, toolbar Proje Aç, Ctrl/⌘+O | `*.mos` seçici → `_vm.OpenProject` → `FitToWindow`. |
| `OnSaveProject` | `menuSave`, `saveProjectBtn`, Ctrl/⌘+S | Masaüstü/mosairPROJECT kuralına göre kaydeder, kaynak görüntüyü kopyalar, 1,2 sn `saveCheckIcon` gösterir. |
| `OnSaveAsProject` | `menuSaveAs`, toolbar Farklı Kaydet, Ctrl/⌘+Shift+S | `SaveAsDialog()` çağırır. |
| `OnExportImage` | `menuExport`, `exportBtn`, dışa aktarma Flyout'undaki ilk düğme, Ctrl/⌘+E | Masaüstü/mosairEXPORT'a zaman damgalı JPEG → `_vm.ExportImageAsync`. |
| `OnExportAsImage` | Dosya menüsü "Farklı Dışa Aktar", dışa aktarma Flyout'undaki ikinci düğme | Kayıt seçici (JPEG/PNG) → `_vm.ExportImageAsync`. |
| `OnExportPointerPressed` | `exportBtn`'i saran `Panel` | Sağ tıkta mosairEXPORT klasörünü işletim sisteminin dosya yöneticisinde açar (`Process.Start`, `UseShellExecute`). |
| `OnSelectAll` | Düzen menüsü, sol paneldeki BtnSelectAll | `_vm.SetAllColors(false)` (hiçbir renk hariç değil). |
| `OnDeselectAll` | Düzen menüsü, sol paneldeki BtnDeselectAll | `_vm.SetAllColors(true)`. |
| `OnResetSize` | `menuFitToScreen`, durum çubuğundaki sığdır düğmesi, Ctrl/⌘+0 | `_vm.FitToWindow(imageScroller.Bounds…)`. |
| `OnRunMosaic` | `menuMosaicize`, Mos düğmesi (`CanRunMosaic`), Ctrl/⌘+M | `await _vm.RunMosaicAsync()` ardından `FitToWindow`. |
| `OnTogglePixelEdit` | Araçlar menüsü (`MosaicDone`), toolbar kalem düğmesi | `_vm.TogglePixelEditMode()`; mod kapanınca `catalogListBox` seçimini temizler. |
| `OnToggleGrid` | Araçlar menüsü, toolbar Izgara Flyout'undaki düğme | `_vm.ShowGrid` tersine çevrilir. |
| `OnGridMainColorPick` | Izgara Flyout'u, `GridColorPresets` düğmeleri (`Tag` = renk) | `_vm.GridColor` + `_vm.SelectMainColor` (tonları üretir). |
| `OnGridColorPick` | Izgara Flyout'u, `GridColorShades` düğmeleri | Yalnızca `_vm.GridColor`. |
| `OnSelectInterpolation` | İnterpolasyon Flyout'u, `InterpolationMethods` düğmeleri (`Tag`) | `_vm.SelectedInterpolation`. |
| `OnToggleOptimum` | Araçlar → Optimum | `_vm.UseOptimal` tersine çevrilir (toolbar'daki onay kutusu doğrudan bağlamadır). |
| `OnToggleStockAware` | Araçlar → Stoğa göre (onay işareti `UseStockAware`'e bağlı) | `_vm.UseStockAware` tersine çevrilir (toolbar'daki "Stoğa göre" onay kutusu doğrudan bağlamadır). |
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
| `OnToggleTheme` | Tema düğmesi | `Application.Current!.RequestedThemeVariant` `Light`/`Dark`; `iconDark`/`iconLight` görünürlüğü. |
| `OnSetLanguageTr` / `OnSetLanguageEn` | Dil Flyout'undaki TR / EN düğmeleri | `Loc.Instance.Lang = "tr"/"en"` + `_vm.RefreshLocalized()`. |
| `OnWidthGotFocus` | Genişlik `TextBox` (`GotFocus`) | Metnin tamamını seçer (`Dispatcher.UIThread.Post` ile). |
| `OnWidthTextInput` | Genişlik `TextBox` (`TextInput`) | Yazılan `,` karakterini `.` yapar. |
| `OnWidthChanged` | Genişlik `TextBox` (`LostFocus`) | Virgülleri noktaya çevirir, `_vm.UpdateDimensions()`. |
| `OnWidthKeyDown` | Genişlik `TextBox` (`KeyDown`) | Enter'da `OnWidthChanged` ile aynı işlem. |
| `OnColorCheckChanged` | `catalogListBox` satırındaki `CheckBox` (`Click`) | `ColorItem.IsExcluded` ayarlanır, `_vm.SyncColorExclusion(item)`. |
| `OnCatalogSelectionChanged` | `catalogListBox` (`SelectionChanged`) | Yalnızca piksel düzenleme açıkken seçili katalog taşını kaynak yapar (`_vm.SetSourceFromCatalog`). |
| `OnImageWheel` | Tuvaldeki `Image` veya `MosaicView` (`PointerWheelChanged`) | İmleç merkezli yakınlaştırma: ×1,25 / ×0,8, sınır `MinZoomLevel`–20; ofseti imleç sabit kalacak şekilde düzeltir, gezgini günceller. |
| `OnImagePointerPressed` | `Image` veya `MosaicView` (`PointerPressed`); gönderen herhangi bir `Control` olabilir | Sağ tuş: kaydırmayı başlatır, işaretçiyi yakalar. Sol/orta: `_vm.OnImagePressed(...)`; orta tuşla (düzenleme dışı) katalog seçimini temizler. |
| `OnImagePointerMoved` | `Image` veya `MosaicView` (`PointerMoved`); gönderen herhangi bir `Control` olabilir | Kaydırma sürüyorsa ofseti günceller; değilse `_vm.OnImagePointerMoved` (özellikler paneli). |
| `OnImagePointerReleased` | `Image` veya `MosaicView` (`PointerReleased`) | Kaydırmayı bitirir, yakalamayı bırakır. |
| `OnScrollChanged` | `imageScroller` (`ScrollChanged`) | `UpdateNav()` → `_vm.UpdateNavigator(...)`. |
| `OnNavPointerPressed` / `OnNavPointerMoved` | `navPanel` | Tıklanan/sürüklenen noktayı görünümün merkezine getirir (`NavigateFromNav` → `_vm.NavigatorTarget`). Konum, kenarlığın içindeki içerik paneline (`NavContent`) göre alınır; `UpdateNav` da bu panelin boyutunu (`NavInnerSize`) gönderir. |
| `OnSelectStone` | Özellikler paneli varyant düğmeleri (`Tag` = `Index`) | `_vm.SelectStone(index)`. |
| `OnDismissNWarning` | N uyarısındaki Tamam düğmesi | `_vm.DismissNWarning()`. |
| `OnKeyDown` | Pencere (`KeyDown +=`) | Kısayollar (aşağıda). |
| `OnDragOver` / `OnDrop` | Pencere (`AddHandler`) | Sürükle-bırak (aşağıda). |
| `OnMosAnimTick` | `_mosAnimTimer.Tick` | `mosQ0..mosQ3` karelerini sırayla yakar (opaklık 1 / 0,15). |
| `OnExportAnimTick` | `_exportAnimTimer.Tick` | `exportArrow`'un `TranslateTransform.Y` değerini `ExportAnimOffsets` dizisinden alır. |
| Menü lambdaları | `BuildToolsMenu`, `RebuildGridShadeItems` içinde oluşturulan `MenuItem.Click` | İnterpolasyon → `SelectedInterpolation`; Detay 10…100 → `StonePixelSize`; ana renk → `GridColor` + `SelectMainColor`; ton → `GridColor`. |

## Önemli davranışlar ve iş kuralları

### Kısayollar

`SetMenuShortcutTexts` menülerde görünen kısayol metinlerini kodda atar (`MenuItem.InputGesture`), böylece macOS'ta ⌘, diğerlerinde Ctrl gösterilir. `InputGesture` **yalnızca gösterimdir**; asıl davranış `OnKeyDown` içindedir.

| Menü öğesi | `InputGesture` |
|---|---|
| `menuLoadImage` | `CmdKey` + I |
| `menuOpenProject` | `CmdKey` + O |
| `menuSave` | `CmdKey` + S |
| `menuSaveAs` | `CmdKey` + Shift + S |
| `menuExport` | `CmdKey` + E |
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
| S | `CmdKey` | `OnSaveProject` | `_vm.MosaicDone` |
| S | `CmdKey` + Shift | `OnSaveAsProject` | `_vm.MosaicDone` |
| E | `CmdKey` | `OnExportImage` | `_vm.CanExport` |
| 0 / NumPad0 | `CmdKey` | `OnResetSize` | — |
| M | `CmdKey` | `OnRunMosaic` | (`RunMosaicAsync` içinde `CanRunMosaic`) |
| F1 | yok | `ShowHelp()` | — |

Koşul sağlanmasa da Ctrl/⌘+S/E olayı `Handled` işaretlenir.

### Kayıt ve dışa aktarma klasörleri

| İşlem | Hedef | Kural |
|---|---|---|
| Kaydet (`OnSaveProject`) | `Masaüstü/mosairPROJECT/<ad>/<ad>.mos` | `<ad>` = `ProjectService.CurrentPictureFileName` dosya adı (uzantısız), yoksa `mosair_project`. Klasörler yoksa oluşturulur. Kaynak görüntü `<ad><uzantı>` olarak aynı klasöre, **yalnızca orada yoksa** kopyalanır. Mevcut `.mos` sorulmadan üzerine yazılır. |
| Farklı Kaydet (`SaveAsDialog`) | Seçilen `X/foo.mos` → **`X/foo/foo.mos`** | Önerilen ad kaynak görüntü adı. Seçilen klasörün içinde proje adıyla alt klasör açılır. Görüntü kopyalanmaz. |
| Dışa aktar (`OnExportImage`) | `Masaüstü/mosairEXPORT/<M.dd.yyyy>_<HH.mm.ss>__<ad>__<G>x<Y>.jpeg` | `<ad>` kaynak görüntü adı ya da `mosair`; `<G>x<Y>` = `WidthCm` × `HeightCm` (tam sayıya yuvarlanmış). Klasör `GetExportDir()` ile oluşturulur. |
| Farklı dışa aktar (`OnExportAsImage`) | Seçilen yol | JPEG veya PNG; biçim uzantıdan belirlenir (`ExportImageAsync`). |

Masaüstü yolu: kayıtta `Environment.SpecialFolder.Desktop`, dışa aktarmada `Environment.SpecialFolder.DesktopDirectory` kullanılır.

### Sürükle-bırak

- `OnDragOver`: veri dosya içeriyorsa `DragDropEffects.Copy`, yoksa `None`.
- `OnDrop`: bırakılan dosyalardan uzantısı `.png/.jpg/.jpeg/.bmp/.tiff` olan **ilki** `_vm.LoadImage` ile yüklenir; `.mos` sürükle-bırakla açılmaz.
- İşleyiciler pencereye eklenmiştir ama `DragDrop.AllowDrop="True"` yalnızca tuval `Border`'ında vardır; bırakma pratikte tuval alanında çalışır.

### Tema

`OnToggleTheme` uygulama genelinde `ThemeVariant.Light` ↔ `ThemeVariant.Dark` geçişi yapar. Pencere renkleri `ThemeDictionaries` + `DynamicResource` ile otomatik değişir. Seçim kalıcı değildir; açılış teması `App.axaml`'daki `Dark`'tır.

### Dil

`Loc.Instance.Lang` atanınca `Loc` tüm indeksleyici bağlamalarını (`{Binding [Anahtar], Source={x:Static svc:Loc.Instance}}`) yeniler; ViewModel'in ürettiği metinler için ayrıca `_vm.RefreshLocalized()` çağrılır. Varsayılan dil `tr`; seçim kalıcı değildir.

### Araçlar menüsü

`BuildToolsMenu`, toolbar'daki Flyout kontrollerinin menü eşlerini kodla üretir: `MainViewModel.InterpolationMethods`, Detay için 10–100 (adım 10, kaydırıcıyla aynı), `MainViewModel.GridColorPresets` ve dinamik `GridColorShades`. Seçili değer `PathIcon` onay işaretiyle gösterilir (`NewMenuCheck` oluşturur, `SetMenuCheck` gösterir/gizler, `RefreshToolsMenuChecks` hepsini yeniler; renk öğelerinin başlığı `ColorSwatch` kutusudur); aynı renk hem ana renkte hem tonda varsa yalnızca ilki işaretlenir.

## Dikkat / bilinen sınırlamalar

- Dosya menüsündeki `menuSave` ve `menuSaveAs` `MosaicDone`'a, `menuExport` ve mosairEXPORT As öğesi `CanExport`'a bağlıdır; toolbar ve klavye yoluyla aynı koşullar geçerlidir.
- `OnSaveProject` ve `SaveAsDialog` klasör oluşturma, kaydetme ve görsel kopyalamayı `try/catch` içinde yapar; disk/izin hatasında uygulama kapanmaz, `MainViewModel.ReportSaveFailed` "Proje kaydedilemedi" uyarısını gösterir ve ✓ simgesi gösterilmez.
- Kayıt (`SpecialFolder.Desktop`) ve dışa aktarma (`SpecialFolder.DesktopDirectory`) farklı özel klasör sabitleri kullanır; çoğu sistemde aynı yeri gösterir ama tutarsızdır.
- `OnRunMosaic`, `RunMosaicAsync` hiçbir şey yapmadan dönse bile `FitToWindow` çağırır.
- `ShowHelp` her çağrıda yeni bir modal `HelpWindow` açar ve beklemez.
- Menülerdeki stok ve diğer bazı başlıklar `Loc` anahtarlarıyla gelir; yeni menü eklerken anahtar hem `Tr` hem `En` sözlüğüne eklenmelidir.

## İlgili dosyalar

- [MainViewModel](ViewModels/MainViewModel.md)
- [App](App.md)
- [HelpWindow](HelpWindow.md)
- [AlertDialog](Controls/AlertDialog.md) · [ConfirmDialog](Controls/ConfirmDialog.md) · [StockSettingsDialog](Controls/StockSettingsDialog.md)
- [GridOverlay](Controls/GridOverlay.md), [MosaicView](Controls/MosaicView.md)
- [InvariantDoubleConverter](Converters/InvariantDoubleConverter.md)
- [Loc](Services/Loc.md)
- [ProjectService](Services/ProjectService.md)
- [PixelEditService](Services/PixelEditService.md)
- [Kullanıcı arayüzü (ARAYUZ)](../ARAYUZ.md)
