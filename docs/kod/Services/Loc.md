# Loc

> Kaynak: `mosair/Services/Loc.cs` · Güncelleme: 2026-10-06

## Amaç

Uygulamanın Türkçe/İngilizce arayüz metinlerini sağlayan basit yerelleştirme sınıfı. Metinler kod içindeki iki sözlükte (`Tr`, `En`) tutulur; dil çalışma anında değiştirilebilir ve XAML bağlamaları otomatik güncellenir. Ayrıca platforma göre kısayol tuşu metinlerini (`⌘` / `Ctrl`) üretir.

## Nerede kullanılır

| Çağıran | Kullanım |
|---|---|
| `MainWindow.axaml`, `HelpWindow.axaml` | `{Binding [Anahtar], Source={x:Static svc:Loc.Instance}}` indeksleyici bağlaması; `IsTr`/`IsEn` ile dile göre görünürlük; `KeyMod*` kısayol metinleri |
| `MainWindow.axaml.cs` | Dil menüsü: `Loc.Instance.Lang = "tr"` / `"en"`, ardından `MainViewModel.RefreshLocalized()` |
| `MainViewModel` | Durum, uyarı ve stok metinleri için `Loc.Get` / `Loc.Fmt`; `PropertyChanged` ile `Lang` değişimini dinler |
| `HelpWindow.axaml.cs` | `Loc.Instance.Lang`'a göre TR/EN bölümleri seçer |
| `AlertDialog`, `ConfirmDialog`, `StockSettingsDialog` | Düğme ve etiket metinleri |
| `StockSheetService`, `StoneTextureService`, `ImageService` | Hata mesajları (`StockErr*`, `StatusRsBitmapTooLarge`) |

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

Her iki sözlükte de **199** anahtar vardır ve anahtar kümeleri birebir aynıdır. Gruplar önek ile ayrılır:

| Önek | Sayı | İçerik |
|---|---|---|
| `Stock*` | 57 | Google Sheet stok entegrasyonu: başlıklar, onaylar, sonuçlar, hata metinleri (`StockErr*`), ayar diyaloğu, açılışta stok yükleme (`StockLoadedOnStart`, `StockLoadOnStartFailed`) ve "Stoğa göre" sonuç/uyarı metinleri (`StockAware*`, `StockCountsWritten`) |
| `Menu*` | 37 | Menü başlıkları ve öğeleri (Dosya, Düzen, Görünüm, Araçlar, Stok, Yardım…; `MenuStockAware` dahil) |
| `Status*` | 24 | Durum çubuğu metinleri (çoğu biçim dizesi); küçültülmüş dışa aktarma için `StatusExportReducedN` ve `StatusSavedReducedN` dahil |
| `Tip*` | 22 | Araç çubuğu, stok düğmesi ve "Stoğa göre" kutusu (`TipStockAware`) ipuçları |
| `Alert*` | 18 | Uyarı diyaloğu başlık/gövde çiftleri (`*Title` / `*Body`, `*Failed` vb.) |
| `Prop*` | 12 | Özellikler paneli etiketleri ve biçimleri |
| `Dlg*` | 10 | Diyalog düğmeleri (`DlgYes`, `DlgNo`…) ve dosya diyaloğu başlıkları |
| `Lbl*` | 7 | Sol panel etiketleri (`LblStockAware` dahil) |
| `Col*` | 3 | Palet sütun başlıkları |
| `Info*` | 3 | Boyut bilgisi biçimleri |
| `Warn*` | 3 | Performans uyarısı |
| `Btn*` | 2 | Tümünü seç / seçimi kaldır |
| `OptimumInfoFmt` | 1 | Optimum bilgi metni |

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
- `StatusNTooLarge`, `AlertNTooLargeTitle`, `AlertNTooLargeBody`, `StatusRegenRs` ve `StatusErrorTooLarge` artık hiçbir yerde kullanılmıyor (taş dokulu görüntü karolarla çizildiği için N kaydırıcısında boyut sınırı ve yeniden üretim kalmadı). Sözlüklerde duruyorlar; sayıya dahildirler.
- `StatusGeneratingRs` ("Acildi, tas dokulari yukleniyor...") proje açılırken taş görüntüleri yüklenirken gösterilir.
- `StockAwareSmall` (en az kullanım kuralıyla çıkarılan taşlar) sözlükte durur, ancak uygulamada bu kural kapalı olduğundan (`MinUsage = 0`) normalde gösterilmez.
- `HelpWindow.axaml` içindeki kısayol açıklamaları gibi bazı metinler XAML'e doğrudan yazılmıştır ve bu sözlüklerden gelmez.

## İlgili dosyalar

- [MainWindow](../MainWindow.md)
- [HelpWindow](../HelpWindow.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
- [StockSheetService](./StockSheetService.md)
- [AlertDialog](../Controls/AlertDialog.md), [ConfirmDialog](../Controls/ConfirmDialog.md), [StockSettingsDialog](../Controls/StockSettingsDialog.md)
- [Arayüz rehberi](../../ARAYUZ.md)
