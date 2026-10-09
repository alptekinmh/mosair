# Loc

> Kaynak: `mosair/Services/Loc.cs` · Güncelleme: 2026-10-08

## Amaç

Uygulamanın Türkçe/İngilizce arayüz metinlerini sağlayan basit yerelleştirme sınıfı. Metinler kod içindeki iki sözlükte (`Tr`, `En`) tutulur; dil çalışma anında değiştirilebilir ve XAML bağlamaları otomatik güncellenir. Ayrıca platforma göre kısayol tuşu metinlerini (`⌘` / `Ctrl`) üretir.

## Nerede kullanılır

| Çağıran | Kullanım |
|---|---|
| `MainWindow.axaml`, `HelpWindow.axaml` | `{Binding [Anahtar], Source={x:Static svc:Loc.Instance}}` indeksleyici bağlaması; `IsTr`/`IsEn` ile dile göre görünürlük; `KeyMod*` kısayol metinleri |
| `MainWindow.axaml.cs` | Dışa aktarma listesinin kodla kurulan öğeleri (`MenuExport`, `MenuExportAs`, `ExportChooseQuality` bağlamaları); sağ alt bildirimin başlıkları, düğme metinleri ve geri sayımı (`ToastNewDownload` / `ToastNewDesktop`, `ToastQuestion`, `ToastExported`, `ToastScreenshotSaved`, `ToastProjectSaved`, `ToastOpen`, `ToastOpenFile`, `ToastShowFolder`, `ToastDismiss`, `ToastSeconds`) ve `StatusToastBusy`; dil menüsü: `Loc.Instance.Lang = "tr"` / `"en"`, ardından `MainViewModel.RefreshLocalized()` |
| `MainViewModel` | Durum, uyarı, dışa aktarma seçeneği, stok ve Google Drive metinleri için `Loc.Get` / `Loc.Fmt`; `PropertyChanged` ile `Lang` değişimini dinler |
| `HelpWindow.axaml.cs` | `Loc.Instance.Lang`'a göre TR/EN bölümleri seçer |
| `AlertDialog`, `ConfirmDialog`, `StockSettingsDialog`, `DriveSettingsDialog`, `DriveOpenDialog` | Düğme ve etiket metinleri |
| `StockSheetService`, `DriveService`, `StoneTextureService`, `ImageService`, `MosaicExporter` | Hata mesajları (`StockErr*`, `DriveNotConfigured`, `DriveErrDeploy`, `StatusRsBitmapTooLarge`, `ExportJpegTooLarge`, `ExportJpegFailed`) |

## Yapı

`public class Loc : INotifyPropertyChanged` — tek örnek (`Instance`) üzerinden kullanılır.

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `Instance` | `Loc` (static) | `new()` | Uygulama genelindeki tek örnek |
| `IsMac` | `bool` (static) | çalışma anında | `RuntimeInformation.IsOSPlatform(OSPlatform.OSX)` |
| `Mod` | `string` (özel, static) | `"⌘"` veya `"Ctrl"` | Kısayol değiştirici tuş metni |
| `_lang` | `string` (özel) | `"tr"` | Etkin dil kodu |
| `Lang` | `string` | `"tr"` | Etkin dil; `"tr"` veya `"en"` |
| `IsTr` / `IsEn` | `bool` | | `Lang` kısayolları (XAML görünürlüğü için) |
| `this[string key]` | `string` | | Etkin dildeki metin; anahtar yoksa anahtarın kendisi |
| `Tr` / `En` | `Dictionary<string, string>` (özel, static) | | Metin sözlükleri |

### Kısayol metinleri

| Özellik | macOS | Windows/Linux |
|---|---|---|
| `KeyMod` | `⌘` | `Ctrl` |
| `KeyModI`, `KeyModO`, `KeyModS`, `KeyModE`, `KeyModM`, `KeyMod0`, `KeyModZ`, `KeyModY` | `⌘+I` … `⌘+Y` | `Ctrl+I` … `Ctrl+Y` |
| `KeyModShiftS` | `⌘+⇧+S` | `Ctrl+Shift+S` |

Bu metinler yalnızca gösterim içindir; gerçek tuş bağlamaları başka yerde tanımlıdır.

### Anahtar grupları

Her iki sözlükte de **326** anahtar vardır ve anahtar kümeleri birebir aynıdır. Gruplar önek ile ayrılır:

| Önek | Sayı | İçerik |
|---|---|---|
| `Stock*` | 59 | Google Sheet stok entegrasyonu: başlıklar, onaylar, sonuçlar, hata metinleri (`StockErr*`), ayar diyaloğu, stok düzeltmesinin ilerlemesi (`StockFitProgress`, `StockFitProgressFull`), açılışta stok yükleme (`StockLoadedOnStart`, `StockLoadOnStartFailed`) ve "Stoğa göre" sonuç/uyarı metinleri (`StockAware*`, `StockCountsWritten`) |
| `Drive*` | 29 | Google Drive: ayar penceresi (`DriveSettingsTitle`, `DriveFolder*`, `DriveScriptUrl*`, `DriveSteps*`, `DriveCopyScript`, `DriveTest*`, `DriveScriptCopied`, `DriveSettingsNote`, `DriveSettingsSaved`), Drive'dan Aç proje tarayıcısı (`DriveOpen*`, `DriveRefresh`, `DriveShowInBrowser`, `DriveSearch`, `DriveSortNewest`, `DriveSortName`, `DriveNoMatch`), uyarı ve hata metinleri (`DriveNotConfigured`, `DriveErrDeploy`, `DriveNoMosaic`, `DriveFailed`) |
| `Menu*` | 50 | Menü başlıkları ve öğeleri (Dosya, Düzen, Görünüm, Araçlar, Stok, Yardım…; `MenuStockAware`, Araçlar'daki Anlık Mos renk seçenekleri `MenuLiveMosAll` / `MenuLiveMosSelected`, Görünüm'deki `MenuPropertiesPanel`, `MenuAdjustPanel` ("Görsel Ayarları"; şeritteki sekme de bunu yazar), `MenuSmoothMouse`, `MenuTheme` ("Tema") ve `MenuLightTheme` ("Açık Tema"), Dosya'daki `MenuScreenshot`, `MenuWatchImages` ve `MenuDrive` alt menüsü (`MenuDriveSave`, `MenuDriveOpen`, `MenuDriveSettings`), Düzen'deki `MenuCancelWork` dahil) |
| `Status*` | 34 | Durum çubuğu metinleri (çoğu biçim dizesi); arka planda proje kaydetme/açma için `StatusSavingProject` ("Proje kaydediliyor: {0}") ve `StatusOpeningProject` ("Proje açılıyor: {0}"); yeni görsel bildiriminde **Aç** iş sürerken basılınca `StatusToastBusy`; Drive işlemleri için `StatusDriveSaving`, `StatusDriveSaved`, `StatusDriveListing`, `StatusDriveDownloading`; büyük dışa aktarmanın yüzde ilerlemesi için `StatusExportingPct`, ekran görüntüsü için `StatusScreenshotSaved` ve iptal metinleri (`StatusCancelling`, `StatusMosCancelled`, `StatusMosCancelledCleared`, `StatusStockFitCancelled`, `StatusStockCheckCancelled`, `StatusExportCancelled`) dahil |
| `Theme*` | 5 | **Görünüm → Tema** altındaki palet adları: `ThemeLapis`, `ThemePastel` (Adaçayı ve Lavanta), `ThemeGraphite` (Grafit ve Petrol), `ThemeTravertine` (Traverten), `ThemeInk` (Mürekkep ve Leylak) |
| `Tip*` | 36 | Araç çubuğu, Görsel Ayarları (`TipAdjust` başlık, `TipAdjSlider` "Sağ tıklayınca sıfırlanır", `TipAdjustShow` gizli sütunun şeridi, `TipAdjColorize` Renklendir kutusu, `TipAdjLiveMos` Anlık Mos kutusu, renk seçenekleri `TipLiveMosAll` / `TipLiveMosSelected`), stok düğmesi, Google Drive düğmesi (`TipDrive`), "Stoğa göre" kutusu (`TipStockAware`), durum çubuğundaki İptal düğmesi (`TipCancel`) ve Özellikler panelinin gizle/göster düğmeleriyle seçimi bırakma düğmesi (`TipPanelHide`, `TipPanelShow`, `TipClearSelection`) ipuçları |
| `Alert*` | 17 | Uyarı diyaloğu başlık/gövde çiftleri (`*Title` / `*Body`, `*Failed` vb.; Drive uyarılarının başlığı `AlertDriveTitle`); `AlertMemoryBody` yalnızca cm değerini küçültmeyi önerir |
| `Prop*` | 22 | Özellikler paneli etiketleri ve biçimleri; taş seçili değilken görünen görsel bilgisi kartları (`PropImage`, `PropDetails` ve Ayrıntılar satırları `PropResolution`, `PropMegapixels`, `PropAspect`, `PropFileSize`, `PropModified`; `PropStoneHint`, `PropNoImage`, `PropImageMissing`) |
| `Adj*` | 28 | Görsel Ayarları sütunu: başlık `AdjTitle` ("GÖRSEL AYARLARI"), sekmeler `AdjTabLight` ("Işık") / `AdjTabColor` ("Ton/Doygunluk"), Anlık Mos kutusu ve Araçlar menüsü `AdjLiveMos` ("Anlık Mos"), renk seçenekleri `AdjLiveMosAll` ("Tüm renkleri kullan") / `AdjLiveMosSelected` ("Seçili renkleri kullan"), Işık satırları `AdjExposure`, `AdjBrightness`, `AdjContrast`, `AdjHighlights`, `AdjShadows`, `AdjWhites`, `AdjBlacks`, `AdjGamma`, renk satırları `AdjHue`, `AdjSaturation`, `AdjLightness`, renk aralıkları `AdjRange0`…`AdjRange6` ("Ana (tüm renkler)", "Kırmızılar", "Sarılar", "Yeşiller", "Camgöbekleri", "Maviler", "Eflatunlar"), `AdjColorize` ("Renklendir"), alttaki kısayol ipucu `AdjHint`, `AdjReset` ("Sıfırla"), Mos'tan sonra ayar değişince durum notu `AdjNeedsMos` |
| `Toast*` | 11 | Sağ alt bildirim. Yeni görsel: başlıklar (`ToastNewDownload`, `ToastNewDesktop`), soru (`ToastQuestion`), düğmeler (`ToastOpen`, `ToastDismiss`). Kaydedilen dosya: başlıklar `ToastExported` ("DIŞA AKTARILDI"), `ToastScreenshotSaved` ("EKRAN GÖRÜNTÜSÜ KAYDEDİLDİ"), `ToastProjectSaved` ("PROJE KAYDEDİLDİ"), düğmeler `ToastOpenFile` ("Aç") ve `ToastShowFolder` ("Klasörü aç" / "Show in folder"). Geri sayım biçimi (`ToastSeconds`, `{0} sn` / `{0} s`) |
| `Dlg*` | 10 | Diyalog düğmeleri (`DlgYes`, `DlgNo`…) ve dosya diyaloğu başlıkları |
| `Export*` | 11 | Dışa aktarma listesi ve uyarıları: alt menü başlığı (`ExportChooseQuality`), seçenek metinleri (`ExportDimsPx`, `ExportChoiceQuick`, `ExportChoiceAs`, `ExportChoiceAsPngOnly`, Mos'tan önceki tek seçenek `ExportChoiceImage`, varsayılan kalitenin işareti `ExportDefaultQuality` = "(varsayılan)", `ExportEstimating`), JPEG uyarı/onayları (`ExportJpegTooLarge`, `ExportJpegMemoryConfirm`, `ExportJpegFailed`) |
| `Lbl*` | 5 | Sol panel etiketleri (`LblStockAware` dahil) |
| `Col*` | 3 | Palet sütun başlıkları |
| `Info*` | 2 | Sol paneldeki taş/kalıp kartının satır adları: `InfoStonesLabel` ("Taş" / "Stones") ve `InfoMouldsLabel` ("Kalıp" / "Moulds"). `InfoStones`, `InfoMoulds` ve `InfoOriginal` kaldırıldı. |
| `Warn*` | 1 | `WarnOk` ("Anladım" / "OK") |
| `Btn*` | 4 | Katalog satırındaki **Kalıba Tamamla** (`BtnCompleteMoulds`, Araçlar menüsünde de) ve Tümünü Seç \| Tümünü Kaldır düğmesi (`BtnSelectAll` "Tümünü Seç", `BtnDeselectAll` "Tümünü Kaldır"), durum çubuğundaki İptal düğmesi (`BtnCancel`) |
| `Mould*` | 5 | Kalıba Tamamla durum metinleri: `MouldWorking`, `MouldAlreadyWhole`, `MouldNoColour`, `MouldNoStockFiller`, `MouldDone` ("Kalıba tamamlandı: {0} × {1} cm, dolgu {2}"); ipucu `TipCompleteMoulds` |
| `Size*` | 3 | Dosya boyutu biçimleri (`SizeKB`, `SizeMB`, `SizeGB`; Drive'dan Aç kartları `SizeKB`/`SizeMB` kullanır) |

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `Loc.Get(key)` → `string` | `Instance[key]`; etkin dildeki metni döndürür, yoksa `key` | Kod tarafı (ViewModel, servisler, diyaloglar) |
| `Loc.Fmt(key, params object[] args)` → `string` | `string.Format(Get(key), args)`; `{0}`, `{1}` yer tutuculu metinler için | Kod tarafı |
| `Instance[key]` | İndeksleyici; XAML'de `{Binding [Key]}` ile bağlanır | XAML |
| `Lang` (set) | Dili değiştirir ve bildirim yayınlar | `MainWindow.axaml.cs` |

## Önemli davranışlar ve iş kuralları

- `Lang` değişince sırasıyla `""` (tüm özellikler), `"Item[]"` (indeksleyici bağlamaları), `"Lang"`, `"IsTr"`, `"IsEn"` için `PropertyChanged` yayınlanır; tüm `{Binding [Key]}` bağlamaları kendiliğinden yenilenir.
- Kodda `Loc.Get`/`Loc.Fmt` ile üretilip bir özelliğe atanmış metinler (ör. durum çubuğu) otomatik güncellenmez; bunun için `MainViewModel.RefreshLocalized()` çağrılır.
- `En` sözlüğünde bulunmayan anahtar İngilizce'de de anahtar adını gösterir (Türkçe'ye geri düşmez).
- Varsayılan dil Türkçe'dir; seçilen dil kalıcı olarak saklanmaz.

## Yeni metin nasıl eklenir

1. Uygun önekle bir anahtar adı seçin (ör. menü için `MenuXxx`, uyarı için `AlertXxxTitle` + `AlertXxxBody`, biçim dizesi ise sonuna `Fmt`).
2. Anahtarı **hem** `Tr` **hem** `En` sözlüğüne, ilgili yorum bloğunun altına ekleyin. Biçim dizesiyse yer tutucular (`{0}`, `{1}`) iki dilde de aynı sayıda olmalıdır.
3. Kullanın:
   - XAML: `Text="{Binding [AnahtarAdı], Source={x:Static svc:Loc.Instance}}"` (`xmlns:svc="using:mosair.Services"`).
   - C#: `Loc.Get("AnahtarAdı")` veya `Loc.Fmt("AnahtarAdı", değer)`.
4. Metin koddan bir özelliğe atanıyorsa, dil değişiminde yenilenmesi için `MainViewModel.RefreshLocalized()` içine ekleyin.

## Dikkat / bilinen sınırlamalar

- Türkçe metinlerin bir kısmı Türkçe karakter içermeden yazılmıştır (ör. `AlertExportTitle` = "Disa Aktarma").
- Eksik anahtar hata vermez; ekranda anahtar adı görünür. Yeni anahtarı iki sözlüğe de eklemeyi unutmayın.
- `Fmt`, `string.Format`'ı geçerli kültürle çağırır; sayı biçimleri sistem kültürüne göre değişebilir.
- Detay (N) ayarı kaldırılırken ona ait anahtarlar (`MenuDetail`, `TipDetail`, `LblDetail`, `WarnPerfTitle`, `WarnPerfBody`, `StatusRegenRs`, `StatusNTooLarge`, `StatusErrorTooLarge`, `AlertNTooLargeTitle`, `AlertNTooLargeBody`) iki sözlükten de silindi; `ExportCurrentQuality` yerini `ExportDefaultQuality`'ye bıraktı. Kullanıcıya görünen dışa aktarma metinlerinde "N" geçmez.
- `StatusGeneratingRs` ("Acildi, tas dokulari yukleniyor...") proje açılırken taş görüntüleri yüklenirken gösterilir.
- Kalıp Dolgu ve Kullanılmayan Taşlar kaldırılırken (2026-10-09) `PadTitle`, `PadRemoved`, `PadDone`, `PadPreview`, `PadNotNeeded`, `PadNoStone`, `PadNoEdit`, `BtnPadding`, `TipPadding`, `BtnUnusedStones` ve `TipUnusedStones` iki sözlükten de silindi.
- `StockAwareSmall` (en az kullanım kuralıyla çıkarılan taşlar) sözlükte durur, ancak uygulamada bu kural kapalı olduğundan (`MinUsage = 0`) normalde gösterilmez.
- `HelpWindow.axaml` içindeki kısayol açıklamaları gibi bazı metinler XAML'e doğrudan yazılmıştır ve bu sözlüklerden gelmez.

## İlgili dosyalar

- [MainWindow](../MainWindow.md)
- [HelpWindow](../HelpWindow.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
- [StockSheetService](./StockSheetService.md)
- [DriveService](./DriveService.md)
- [AlertDialog](../Controls/AlertDialog.md), [ConfirmDialog](../Controls/ConfirmDialog.md), [StockSettingsDialog](../Controls/StockSettingsDialog.md), [DriveSettingsDialog](../Controls/DriveSettingsDialog.md), [DriveOpenDialog](../Controls/DriveOpenDialog.md)
- [Arayüz rehberi](../../ARAYUZ.md)
