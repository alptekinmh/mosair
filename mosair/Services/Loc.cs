using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace mosair.Services
{
    public class Loc : INotifyPropertyChanged
    {
        public static Loc Instance { get; } = new();
        public static bool IsMac { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        private static readonly string Mod = IsMac ? "⌘" : "Ctrl";

        public string KeyMod => Mod;
        public string KeyModI => $"{Mod}+I";
        public string KeyModO => $"{Mod}+O";
        public string KeyModS => $"{Mod}+S";
        public string KeyModShiftS => IsMac ? "⌘+⇧+S" : "Ctrl+Shift+S";
        public string KeyModE => $"{Mod}+E";
        public string KeyModM => $"{Mod}+M";
        public string KeyMod0 => $"{Mod}+0";
        public string KeyModZ => $"{Mod}+Z";
        public string KeyModY => $"{Mod}+Y";

        private string _lang = "tr";

        public string this[string key] =>
            (_lang == "en" ? En : Tr).TryGetValue(key, out var v) ? v : key;

        public static string Get(string key) =>
            Instance[key];

        public static string Fmt(string key, params object[] args) =>
            string.Format(Get(key), args);

        public bool IsTr => _lang == "tr";
        public bool IsEn => _lang == "en";

        public string Lang
        {
            get => _lang;
            set
            {
                if (_lang == value) return;
                _lang = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Lang"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsTr"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsEn"));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        // ─── TÜRKÇE ───
        private static readonly Dictionary<string, string> Tr = new()
        {
            // Menu
            ["MenuFile"] = "Dosya",
            ["MenuLoadImage"] = "Gorsel Yukle",
            ["MenuOpenProject"] = "Proje Ac",
            ["MenuSave"] = "Proje Kaydet",
            ["MenuSaveAs"] = "Proje Farkli Kaydet",
            ["MenuExport"] = "mosairEXPORT",
            ["MenuExportAs"] = "mosairEXPORT As",
            ["MenuEdit"] = "Duzenle",
            ["MenuSelectAll"] = "Tum Renkleri Sec",
            ["MenuDeselectAll"] = "Tum Renkleri Kaldir",
            ["MenuView"] = "Gorunum",
            ["MenuFitToScreen"] = "Ekrana Sigdir",
            ["MenuTools"] = "Araclar",
            ["MenuMosaicize"] = "Mozaiklestir",
            ["MenuPixelEdit"] = "Piksel Duzenle",
            ["MenuShowGrid"] = "Izgara Goster",
            ["MenuGridColor"] = "Izgara Rengi",
            ["MenuInterp"] = "Interpolasyon Yontemi",
            ["MenuDetail"] = "Detay Seviyesi",
            ["MenuOptimum"] = "Optimum Tas Sayisi",
            ["MenuStoneCount"] = "Tas Sayisi",
            ["MenuStonesSuggested"] = "Onerilen Degere Don",
            ["MenuStonesMore"] = "Bir Tas Artir",
            ["MenuStonesLess"] = "Bir Tas Azalt",
            ["MenuStock"] = "Stok",
            ["MenuStockOpen"] = "Stok Tablosunu Ac",
            ["MenuStockFetch"] = "Stok Cek",
            ["MenuStockCheck"] = "Stok Kontrol",
            ["MenuStockAdd"] = "Stok Ekle",
            ["MenuHelp"] = "Yardim",
            ["MenuUserGuide"] = "Kullanim Kilavuzu",

            // Toolbar tooltips
            ["TipLoadImage"] = "Gorsel Yukle",
            ["TipOpenProject"] = "Proje Ac (.mos)",
            ["TipSave"] = "Proje Kaydet",
            ["TipMosaicize"] = "Mozaiklestir",
            ["TipPixelEdit"] = "Piksel Duzenle (Orta Tus)\n1) Kaynak renk sec\n2) Hedef piksele uygula",
            ["TipSaveAs"] = "Proje Farkli Kaydet",
            ["TipExport"] = "mosairEXPORT",
            ["TipFit"] = "Ekrana Sigdir",
            ["TipGrid"] = "Izgara Goster/Gizle",
            ["TipInterp"] = "Interpolasyon Yontemi",
            ["TipDetail"] = "Detay Seviyesi (N)",
            ["TipLanguage"] = "Dil",
            ["TipTheme"] = "Tema Degistir",

            // Buttons
            ["BtnSelectAll"] = "Tum Renkleri Sec",
            ["BtnDeselectAll"] = "Tum Renkleri Kaldir",

            // Column headers
            ["ColCatalog"] = "Katalog Renk",
            ["ColMatch"] = "Eslesme",
            ["ColAssigned"] = "Atanan Renk",

            // Left panel
            ["LblGrid"] = "Grid",
            ["LblGridColor"] = "Grid Rengi",
            ["LblInterp"] = "Interp",
            ["LblDetail"] = "Detay",
            ["LblOptimum"] = "Optimum",
            ["TipOptimum"] = "Optimum taş sayısını otomatik bul",
            ["LblStones"] = "Taş",
            ["OptimumInfoFmt"] = "öneri {0}",

            // Google Sheet stock
            ["TipStockSheet"] = "Stok tablosunu tarayıcıda aç (yanındaki ok: tabloyu aç / stok tablosu ayarları)",
            ["TipStockSheetMenu"] = "Stok tablosu: aç veya ayarla",
            ["TipStockFetch"] = "Stok çek: Google Sheet'ten stoğu oku, stoğu biten taşları devre dışı bırak (yanındaki ok: yalnızca kırmızıyla işaretle)",
            ["TipStockFetchMenu"] = "Stok çek seçenekleri",
            ["MenuStockFetchDisable"] = "Stoğu olmayanları devre dışı bırak",
            ["MenuStockFetchMark"] = "Stoğu olmayanları kırmızıyla işaretle",
            ["TipStockCheck"] = "Stok kontrol: bu mozaiğin taş adetlerini tabloya yaz, stoğu yetmeyen taşları işaretle",
            ["TipStockClear"] = "Stok temizle: bu mozaiğin sütununu temizle (sağ tık: tüm mozaik sütunları)",
            ["TipStockAdd"] = "Stok ekle: Tahmini Kalan'ı Bizdeki'ye taşı, mozaik sütunlarını temizle",
            ["MenuStockSettings"] = "Stok Ayarları...",
            ["MenuStockClearOne"] = "Bu mozaiğin sütununu temizle",
            ["MenuStockClearAll"] = "Tüm mozaik sütunlarını temizle",
            ["StockTitle"] = "Stok",
            ["StockNotConfigured"] = "Stok tablosu ayarlı değil. Tablo butonuna sağ tıklayıp Sheet ID ve Script URL girin.",
            ["StockErrFmt"] = "Stok işlemi başarısız:\n{0}",
            ["StockErrColumns"] = "'{0}' sütunu bulunamadı. Bulunan başlıklar: {1}",
            ["StockErrEmpty"] = "Tablo boş döndü.",
            ["StockErrDeploy"] = "Script URL geçersiz veya yayın ayarları hatalı. Apps Script → Manage deployments → Access: Anyone olmalı.",
            ["StockNoProject"] = "Proje adı bulunamadı. Önce bir görsel yükleyin.",
            ["StockNoMosaic"] = "Taş verisi yok. Önce Mos çalıştırın.",
            ["StockFetched"] = "Stok çekildi: {0} taş okundu, {1} taş devre dışı",
            ["StockFetchedMarked"] = "Stok çekildi: {0} taş okundu, {1} taşın stoğu yok (kırmızı nokta, seçim değişmedi)",
            ["StockCheckOk"] = "Stok kontrol ({0}): tüm taşlar için stok yeterli",
            ["StockCheckShort"] = "Stok kontrol ({0}): {1} taşın stoğu yetersiz (kırmızı nokta)",
            ["StockKgLeft"] = "kaldı",
            ["StockClearTitle"] = "Stok Temizle",
            ["StockClearOneConfirm"] = "'{0}' mozaiğinin sütunu temizlenecek ve başlığı mozaikX olarak sıfırlanacak.\n\nEmin misiniz?",
            ["StockClearedOne"] = "'{0}' sütunu temizlendi",
            ["StockClearAllTitle"] = "Tümünü Temizle",
            ["StockClearAllConfirm"] = "Tüm mozaik sütunları temizlenecek ve başlıkları sıfırlanacak.\n\nEmin misiniz?",
            ["StockClearedAll"] = "Tüm mozaik sütunları temizlendi",
            ["StockAddTitle"] = "Stok Ekle",
            ["StockAddConfirm"] = "Tahmini Kalan değerleri Bizdeki sütununa taşınacak, sonra tüm mozaik sütunları temizlenecek.\n\nEmin misiniz?",
            ["StockAdded"] = "Stok eklendi",
            ["StockSettingsTitle"] = "Stok Tablosu Ayarları",
            ["StockSheetIdLabel"] = "Google Sheet ID",
            ["StockScriptUrlLabel"] = "Apps Script URL",
            ["StockSheetIdHint"] = "Stok tablosunu tarayıcıda açın; adres çubuğundaki bağlantıda /d/ ile /edit arasındaki kısım Sheet ID'dir. Bağlantının tamamını da yapıştırabilirsiniz, ID kaydederken otomatik ayrılır. Stok Çek ve Stok Kontrol bu tabloyu okur.",
            ["StockScriptUrlHint"] = "Tabloda Uzantılar → Apps Komut Dosyası'nı açın, ardından Dağıt → Dağıtımları yönet'e gidin ve Web uygulaması URL'sini kopyalayın. Adres /exec ile bitmelidir. Stok Kontrol, Stok Sil ve Stok Ekle tabloya bu adres üzerinden yazar.",
            ["StockRequirementsTitle"] = "Doğru çalışması için",
            ["StockRequirements"] = "• Tablo, \"Bağlantıya sahip olan herkes\" için en az Görüntüleyen erişimiyle paylaşılmış olmalıdır.\n• İlk satırda mos, Bizdeki (kg) ve Tahmini Kalan başlıklı sütunlar bulunmalıdır.\n• Web uygulaması \"Erişimi olanlar: Herkes\" ayarıyla dağıtılmış olmalıdır. Betik tabloya önceden kurulmuş olmalıdır; bu adımları genellikle tablo sahibi yapar.\n• Tabloyu değiştiren işlemleri denemek için önce tablonun bir kopyasıyla çalışmanız önerilir.",
            ["StockSettingsNote"] = "Bu bilgiler sadece bu bilgisayarda saklanır. Script URL'yi bilen herkes tabloya yazabilir; yalnızca güvendiğiniz kişilerle paylaşın.",
            ["StockSettingsSaved"] = "Stok ayarları kaydedildi",
            ["DlgYes"] = "Evet",
            ["DlgNo"] = "Hayır",
            ["DlgSave"] = "Kaydet",
            ["DlgCancel"] = "İptal",

            // Properties
            ["PropTitle"] = "Properties",
            ["PropColor"] = "RENK",
            ["PropTexture"] = "DOKU",
            ["PropVariants"] = "VARYANTLAR",
            ["PropRgb"] = "RGB",
            ["PropCoordinate"] = "KOORDİNAT",
            ["PropPixel"] = "Piksel",
            ["PropMold"] = "Kalıp",
            ["PropEditing"] = "DÜZENLEME",
            ["PropUndoRedo"] = $"{Mod}+Z Geri Al  {Mod}+Y Yinele",

            // Warning
            ["WarnPerfTitle"] = "⚠ Performans Bildirimi",
            ["WarnPerfBody"] = "Optimum performans N=40 değerinde sağlanmaktadır. Daha yüksek değerler çıktı kalitesini artırır ancak işlem süresini önemli ölçüde uzatabilir.",
            ["WarnOk"] = "Anladım",

            // Status (format strings)
            ["StatusReady"] = "Hazir",
            ["StatusImageLoaded"] = "Gorsel yuklendi",
            ["StatusStarting"] = "Mozaiklestirme baslatildi...",
            ["StatusActiveColors"] = "Aktif renk: {0}/{1}",
            ["StatusCalculated"] = "rgbM={0} hesaplandi, isleniyor...",
            ["StatusCompleted"] = "Tamamlandi — {0} renk, {1}s",
            ["StatusUsedColors"] = "{0} renk arasindan {1} renk kullanildi",
            ["StatusSaved"] = "Kaydedildi: {0}",
            ["StatusExporting"] = "Disa aktariliyor: {0}...",
            ["StatusOpenFailed"] = "Dosya acilamadi",
            ["StatusGeneratingRs"] = "Acildi, RS olusturuluyor...",
            ["StatusOpened"] = "Acildi: {0}",
            ["StatusRegenRs"] = "RS yeniden olusturuluyor (N={0})...",
            ["StatusNTooLarge"] = "N={0} cok buyuk ({1}MB), kucultun",
            ["StatusError"] = "Hata: {0}",
            ["StatusErrorTooLarge"] = "Hata: Gorsel cok buyuk (N={0}, {1}x{2}). N degerini veya cm degerini kucultun.",
            ["StatusNoColors"] = "Hata: Aktif renk yok!",
            ["StatusStoneUndo"] = "Tas degisikligi geri alindi",
            ["StatusStoneRedo"] = "Tas degisikligi yeniden uygulandi",
            ["StatusPixelEdit"] = "Pixel edit: {0}",
            ["StatusRsBitmapTooLarge"] = "RS bitmap cok buyuk: {0}x{1}",
            ["StatusCatalogSkipped"] = "Renk katalogunda okunamayan satirlar atlandi (colorsBas.txt satir: {0})",
            ["AlertSaveFailed"] = "Proje kaydedilemedi:\n{0}",

            // Dialog titles
            ["DlgSelectImage"] = "Gorsel Sec",
            ["DlgSaveImage"] = "Gorseli Kaydet",
            ["DlgSaveProject"] = "Projeyi Kaydet",
            ["DlgOpenProject"] = "Proje Ac",
            ["DlgExportImage"] = "Gorseli Disa Aktar",
            ["DlgImages"] = "Gorseller",

            // Dimension info
            ["InfoStones"] = "{0} x {1} = {2} tas",
            ["InfoMoulds"] = "{0} x {1} = {2} kalip",
            ["InfoOriginal"] = "(orj im = {0} x {1} pixels)",

            // Properties format
            ["PropStoneFmt"] = "Taş #{0}",
            ["PropEditedFmt"] = "Düzenlenen: {0}",

            // Alert dialogs
            ["AlertExportTitle"] = "Disa Aktarma",
            ["AlertExportNoMosaic"] = "Disa aktarilacak mozaik yok.\nOnce bir gorsel yukleyin ve mozaiklestirme yapin.",
            ["AlertMosaicTitle"] = "Mozaiklestirme",
            ["AlertMosaicNoImage"] = "Gorsel yuklenmeden mozaiklestirme yapilamaz.\nOnce bir gorsel yukleyin.",
            ["AlertMosaicNoColors"] = "Aktif renk yok!\nEn az bir renk secili olmalidir.",
            ["AlertProjectTitle"] = "Proje",
            ["AlertProjectOpenFailed"] = "Proje dosyasi acilamadi.\nDosya bozuk veya uyumsuz olabilir. Eski WPF surumuyle (binary) kaydedilmis projeleri once guncel WPF'te acip yeniden kaydedin.",
            ["AlertMemoryTitle"] = "Bellek Yetersiz",
            ["AlertMemoryBody"] = "Islem icin yeterli bellek yok.\nDetay (N) veya cm degerini kucultun.",
            ["AlertNTooLargeTitle"] = "Deger Cok Buyuk",
            ["AlertNTooLargeBody"] = "N={0} degeri cok buyuk ({1}MB bellek gerektirir).\nDetay veya cm degerini kucultun.",
            ["AlertErrorTitle"] = "Hata",
            ["AlertErrorBody"] = "Beklenmeyen bir hata olustu:\n{0}",
            ["AlertImageTitle"] = "Gorsel Yukleme",
            ["AlertImageFailed"] = "Gorsel yuklenemedi.\nDosya formati desteklenmiyor veya dosya bozuk olabilir.",
            ["AlertResolutionTitle"] = "Cozunurluk Yetersiz",
            ["AlertResolutionBody"] = "Yuklenen gorselin cozunurlugu, istenen mozaik boyutu icin yeterli degildir.\nDaha yuksek cozunurluklu bir gorsel yukleyiniz.",
        };

        // ─── ENGLISH ───
        private static readonly Dictionary<string, string> En = new()
        {
            // Menu
            ["MenuFile"] = "File",
            ["MenuLoadImage"] = "Load Image",
            ["MenuOpenProject"] = "Open Project",
            ["MenuSave"] = "Save Project",
            ["MenuSaveAs"] = "Save Project As",
            ["MenuExport"] = "mosairEXPORT",
            ["MenuExportAs"] = "mosairEXPORT As",
            ["MenuEdit"] = "Edit",
            ["MenuSelectAll"] = "Select All Colors",
            ["MenuDeselectAll"] = "Deselect All Colors",
            ["MenuView"] = "View",
            ["MenuFitToScreen"] = "Fit to Screen",
            ["MenuTools"] = "Tools",
            ["MenuMosaicize"] = "Mosaicize",
            ["MenuPixelEdit"] = "Pixel Edit",
            ["MenuShowGrid"] = "Show Grid",
            ["MenuGridColor"] = "Grid Color",
            ["MenuInterp"] = "Interpolation Method",
            ["MenuDetail"] = "Detail Level",
            ["MenuOptimum"] = "Optimum Stone Count",
            ["MenuStoneCount"] = "Stone Count",
            ["MenuStonesSuggested"] = "Back to Suggested",
            ["MenuStonesMore"] = "One More Stone",
            ["MenuStonesLess"] = "One Fewer Stone",
            ["MenuStock"] = "Stock",
            ["MenuStockOpen"] = "Open Stock Sheet",
            ["MenuStockFetch"] = "Fetch Stock",
            ["MenuStockCheck"] = "Check Stock",
            ["MenuStockAdd"] = "Add Stock",
            ["MenuHelp"] = "Help",
            ["MenuUserGuide"] = "User Guide",

            // Toolbar tooltips
            ["TipLoadImage"] = "Load Image",
            ["TipOpenProject"] = "Open Project (.mos)",
            ["TipSave"] = "Save Project",
            ["TipMosaicize"] = "Mosaicize",
            ["TipPixelEdit"] = "Edit Pixel (Middle Click)\n1) Select source color\n2) Apply to target pixel",
            ["TipSaveAs"] = "Save Project As",
            ["TipExport"] = "mosairEXPORT",
            ["TipFit"] = "Fit to Screen",
            ["TipGrid"] = "Show/Hide Grid",
            ["TipInterp"] = "Interpolation Method",
            ["TipDetail"] = "Detail Level (N)",
            ["TipLanguage"] = "Language",
            ["TipTheme"] = "Toggle Theme",

            // Buttons
            ["BtnSelectAll"] = "Select All Colors",
            ["BtnDeselectAll"] = "Deselect All Colors",

            // Column headers
            ["ColCatalog"] = "Catalog Color",
            ["ColMatch"] = "Match",
            ["ColAssigned"] = "Assigned Color",

            // Left panel
            ["LblGrid"] = "Grid",
            ["LblGridColor"] = "Grid Color",
            ["LblInterp"] = "Interp",
            ["LblDetail"] = "Detail",
            ["LblOptimum"] = "Optimum",
            ["TipOptimum"] = "Find the optimum stone count automatically",
            ["LblStones"] = "Stones",
            ["OptimumInfoFmt"] = "suggested {0}",

            // Google Sheet stock
            ["TipStockSheet"] = "Open the stock sheet in the browser (arrow next to it: open sheet / stock sheet settings)",
            ["TipStockSheetMenu"] = "Stock sheet: open or set up",
            ["TipStockFetch"] = "Fetch stock: read stock from the Google Sheet and disable stones that ran out (arrow next to it: only mark them red)",
            ["TipStockFetchMenu"] = "Fetch stock options",
            ["MenuStockFetchDisable"] = "Disable stones with no stock",
            ["MenuStockFetchMark"] = "Mark stones with no stock in red",
            ["TipStockCheck"] = "Check stock: write this mosaic's stone counts to the sheet and mark stones that fall short",
            ["TipStockClear"] = "Clear stock: clear this mosaic's column (right-click: all mosaic columns)",
            ["TipStockAdd"] = "Add stock: move Estimated Remaining into On Hand and clear the mosaic columns",
            ["MenuStockSettings"] = "Stock Settings...",
            ["MenuStockClearOne"] = "Clear this mosaic's column",
            ["MenuStockClearAll"] = "Clear all mosaic columns",
            ["StockTitle"] = "Stock",
            ["StockNotConfigured"] = "The stock sheet is not set up. Right-click the sheet button and enter the Sheet ID and Script URL.",
            ["StockErrFmt"] = "Stock action failed:\n{0}",
            ["StockErrColumns"] = "Column '{0}' not found. Headers found: {1}",
            ["StockErrEmpty"] = "The sheet came back empty.",
            ["StockErrDeploy"] = "Script URL is invalid or the deployment is misconfigured. Apps Script → Manage deployments → Access must be Anyone.",
            ["StockNoProject"] = "No project name found. Load an image first.",
            ["StockNoMosaic"] = "No stone data. Run Mos first.",
            ["StockFetched"] = "Stock fetched: {0} stones read, {1} stones disabled",
            ["StockFetchedMarked"] = "Stock fetched: {0} stones read, {1} have no stock (red dot, selection unchanged)",
            ["StockCheckOk"] = "Stock check ({0}): enough stock for every stone",
            ["StockCheckShort"] = "Stock check ({0}): {1} stones are short (red dot)",
            ["StockKgLeft"] = "left",
            ["StockClearTitle"] = "Clear Stock",
            ["StockClearOneConfirm"] = "The column for mosaic '{0}' will be cleared and its header reset to mosaicX.\n\nAre you sure?",
            ["StockClearedOne"] = "Column '{0}' cleared",
            ["StockClearAllTitle"] = "Clear All",
            ["StockClearAllConfirm"] = "All mosaic columns will be cleared and their headers reset.\n\nAre you sure?",
            ["StockClearedAll"] = "All mosaic columns cleared",
            ["StockAddTitle"] = "Add Stock",
            ["StockAddConfirm"] = "Estimated Remaining values will be moved into On Hand, then all mosaic columns will be cleared.\n\nAre you sure?",
            ["StockAdded"] = "Stock added",
            ["StockSettingsTitle"] = "Stock Sheet Settings",
            ["StockSheetIdLabel"] = "Google Sheet ID",
            ["StockScriptUrlLabel"] = "Apps Script URL",
            ["StockSheetIdHint"] = "Open the stock sheet in your browser; the Sheet ID is the part of the link between /d/ and /edit. You can also paste the whole link; the ID is extracted when you save. Fetch Stock and Check Stock read this sheet.",
            ["StockScriptUrlHint"] = "In the sheet, open Extensions → Apps Script, then go to Deploy → Manage deployments and copy the Web app URL. It must end with /exec. Check Stock, Clear Stock and Add Stock write to the sheet through this address.",
            ["StockRequirementsTitle"] = "Requirements",
            ["StockRequirements"] = "• The sheet must be shared with \"Anyone with the link\" with at least Viewer access.\n• The first row must have columns titled mos, Bizdeki (kg) and Tahmini Kalan.\n• The web app must be deployed with \"Who has access: Anyone\". The script must already be installed in the sheet; this is usually done by the sheet owner.\n• To try actions that change the sheet, work on a copy of the sheet first.",
            ["StockSettingsNote"] = "These values are stored only on this computer. Anyone with the Script URL can write to the sheet; share it only with people you trust.",
            ["StockSettingsSaved"] = "Stock settings saved",
            ["DlgYes"] = "Yes",
            ["DlgNo"] = "No",
            ["DlgSave"] = "Save",
            ["DlgCancel"] = "Cancel",

            // Properties
            ["PropTitle"] = "Properties",
            ["PropColor"] = "COLOR",
            ["PropTexture"] = "TEXTURE",
            ["PropVariants"] = "VARIANTS",
            ["PropRgb"] = "RGB",
            ["PropCoordinate"] = "COORDINATE",
            ["PropPixel"] = "Pixel",
            ["PropMold"] = "Mold",
            ["PropEditing"] = "EDITING",
            ["PropUndoRedo"] = $"{Mod}+Z Undo  {Mod}+Y Redo",

            // Warning
            ["WarnPerfTitle"] = "⚠ Performance Notice",
            ["WarnPerfBody"] = "Optimal performance is achieved at N=40. Higher values improve output quality but may significantly increase processing time.",
            ["WarnOk"] = "OK",

            // Status (format strings)
            ["StatusReady"] = "Ready",
            ["StatusImageLoaded"] = "Image loaded",
            ["StatusStarting"] = "Starting mosaic...",
            ["StatusActiveColors"] = "Active colors: {0}/{1}",
            ["StatusCalculated"] = "rgbM={0} calculated, processing...",
            ["StatusCompleted"] = "Completed — {0} colors, {1}s",
            ["StatusUsedColors"] = "{1} of {0} colors used",
            ["StatusSaved"] = "Saved: {0}",
            ["StatusExporting"] = "Exporting: {0}...",
            ["StatusOpenFailed"] = "File could not be opened",
            ["StatusGeneratingRs"] = "Opened, generating RS...",
            ["StatusOpened"] = "Opened: {0}",
            ["StatusRegenRs"] = "Regenerating RS (N={0})...",
            ["StatusNTooLarge"] = "N={0} too large ({1}MB), reduce it",
            ["StatusError"] = "Error: {0}",
            ["StatusErrorTooLarge"] = "Error: Image too large (N={0}, {1}x{2}). Reduce N or cm value.",
            ["StatusNoColors"] = "Error: No active colors!",
            ["StatusStoneUndo"] = "Stone change undone",
            ["StatusStoneRedo"] = "Stone change reapplied",
            ["StatusPixelEdit"] = "Pixel edit: {0}",
            ["StatusRsBitmapTooLarge"] = "RS bitmap too large: {0}x{1}",
            ["StatusCatalogSkipped"] = "Unreadable color catalog lines were skipped (colorsBas.txt line: {0})",
            ["AlertSaveFailed"] = "The project could not be saved:\n{0}",

            // Dialog titles
            ["DlgSelectImage"] = "Select Image",
            ["DlgSaveImage"] = "Save Image",
            ["DlgSaveProject"] = "Save Project",
            ["DlgOpenProject"] = "Open Project",
            ["DlgExportImage"] = "Export Image",
            ["DlgImages"] = "Images",

            // Dimension info
            ["InfoStones"] = "{0} x {1} = {2} stones",
            ["InfoMoulds"] = "{0} x {1} = {2} molds",
            ["InfoOriginal"] = "(orig img = {0} x {1} pixels)",

            // Properties format
            ["PropStoneFmt"] = "Stone #{0}",
            ["PropEditedFmt"] = "Edited: {0}",

            // Alert dialogs
            ["AlertExportTitle"] = "Export",
            ["AlertExportNoMosaic"] = "No mosaic to export.\nLoad an image and run mosaicize first.",
            ["AlertMosaicTitle"] = "Mosaicize",
            ["AlertMosaicNoImage"] = "Cannot mosaicize without an image.\nLoad an image first.",
            ["AlertMosaicNoColors"] = "No active colors!\nAt least one color must be selected.",
            ["AlertProjectTitle"] = "Project",
            ["AlertProjectOpenFailed"] = "Could not open project file.\nThe file may be corrupted or incompatible. Projects saved by an older WPF version (binary) must first be opened and saved again in the current WPF.",
            ["AlertMemoryTitle"] = "Insufficient Memory",
            ["AlertMemoryBody"] = "Not enough memory for this operation.\nReduce Detail (N) or cm value.",
            ["AlertNTooLargeTitle"] = "Value Too Large",
            ["AlertNTooLargeBody"] = "N={0} is too large ({1}MB memory required).\nReduce Detail or cm value.",
            ["AlertErrorTitle"] = "Error",
            ["AlertErrorBody"] = "An unexpected error occurred:\n{0}",
            ["AlertImageTitle"] = "Image Loading",
            ["AlertImageFailed"] = "Could not load image.\nThe file format may be unsupported or the file may be corrupted.",
            ["AlertResolutionTitle"] = "Insufficient Resolution",
            ["AlertResolutionBody"] = "The resolution of the uploaded image is not sufficient for the requested mosaic size.\nPlease upload a higher resolution image.",
        };
    }
}
