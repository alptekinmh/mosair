# StockSheetService

> Kaynak: `mosair/Services/StockSheetService.cs` · Güncelleme: 2026-10-04

## Amaç

Taş stoğunu tutan Google Sheet ile entegrasyon. WPF uygulamasındaki stok işlevlerinden (`WindowPicture` stok olayları + `appsScript.js`) taşınmıştır.

- **Okuma:** Sheet'in herkese açık gviz CSV dışa aktarımı üzerinden.
- **Yazma:** Sheet'e bağlı Apps Script web uygulamasına JSON `POST` ile.

## Nerede kullanılır

| Çağıran | Kullanım |
|---|---|
| `MainViewModel.ConfigureStockAsync` | `LoadConfig` → ayar diyaloğu → `SaveConfig` |
| `MainViewModel.FetchStockAsync` ("stok çek") | `FetchStockAsync` |
| `MainViewModel.CheckStockAsync` ("stok kontrol") | `CheckStockAsync` |
| `MainViewModel.ClearStockOneAsync` / `ClearStockAllAsync` / `AddStockAsync` | `ClearOneAsync` / `ClearAllAsync` / `AddStockAsync` |
| `MainViewModel.OpenStockSheetAsync` | `SheetUrl` |
| `StockSettingsDialog`, `MainWindow.axaml.cs` | `StockSheetService.Config` tipi |

## Yapı

### `Config`

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `SheetId` | `string` | `""` | Google Sheet kimliği (okuma için yeterli) |
| `ScriptUrl` | `string` | `""` | Apps Script web uygulaması adresi, biçim: `https://script.google.com/macros/s/<DEPLOYMENT_ID>/exec` (yazma işlemleri için gerekli) |

**Ayar dosyası:** `Environment.SpecialFolder.ApplicationData` altında `mosair/stock.json` (Windows'ta `%APPDATA%\mosair\stock.json`, macOS'ta kullanıcının `~/.config/mosair/stock.json` karşılığı). Kullanıcıya özeldir, uygulama klasörü dışında durur (macOS `.app` paketi salt okunurdur). Girintili JSON olarak yazılır.

> Bu değerler repoya veya derlemelere asla konmamalıdır: script adresi Sheet'e yazma yetkisi verir. Repo herkese açıktır.

### `CheckResult`

| Ad | Tip | Açıklama |
|---|---|---|
| `ShortIds` | `HashSet<int>` | "Tahmini Kalan" değeri negatif olan taş ID'leri (stok yetersiz) |
| `Remaining` | `Dictionary<int, double>` | Taş ID → "Tahmini Kalan" (Sheet'teki mozaikler düşüldükten sonra kalan, kg) |
| `OnHand` | `Dictionary<int, double>` | Taş ID → "Bizdeki (kg)" (eldeki stok); yalnızca `Remaining`'de bulunan ID'ler için |

### Özel üyeler

| Ad | Açıklama |
|---|---|
| `ConfigPath` | Ayar dosyası yolu |
| `Http` | Paylaşılan `HttpClient`: yönlendirme açık (en fazla 5), zaman aşımı 60 sn |
| `FetchCsvAsync` | `https://docs.google.com/spreadsheets/d/{sheetId}/gviz/tq?tqx=out:csv` adresini 15 sn iptal süresiyle indirir, satırlara böler |
| `ReadNumberColumn` | `mos` sütunu tamsayı, değer sütunu sayı olan satırları döndürür (virgül → nokta, `InvariantCulture`) |
| `PostAsync` | JSON gövdeyi gönderir, yanıtı yorumlar, hata varsa `Exception` atar |
| `ParseCsvLine` | Tırnak ve `""` kaçışını destekleyen basit CSV satır ayrıştırıcı |

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `LoadConfig()` | Ayar dosyasını okur; yoksa veya bozuksa boş `Config` döner (hata yutulur) | `MainViewModel` (`ConfigureStockAsync`, `TryGetStockConfig`) |
| `SaveConfig(Config)` | Klasörü oluşturup ayar dosyasını yazar | `MainViewModel.ConfigureStockAsync` |
| `SheetUrl(sheetId)` | Sheet'in tarayıcı adresini üretir | `MainViewModel.OpenStockSheetAsync` |
| `FetchStockAsync(sheetId)` → `Dictionary<int, double>` | `mos` ve "Bizdeki (kg)" sütunlarından taş ID → kg sözlüğü; aynı ID birden çok satırda varsa toplanır | `MainViewModel.FetchStockAsync` |
| `CheckStockAsync(scriptUrl, sheetId, projectName, stones)` → `CheckResult` | Mozaiğin taş sayılarını Sheet'teki proje sütununa yazar, sonra CSV'yi okuyup kalanları döndürür | `MainViewModel.CheckStockAsync` |
| `ClearOneAsync(scriptUrl, sheetId, projectName)` | Bu projenin sütununu temizler | `MainViewModel.ClearStockOneAsync` |
| `ClearAllAsync(scriptUrl, sheetId)` | Tüm mozaik sütunlarını temizler | `MainViewModel.ClearStockAllAsync` |
| `AddStockAsync(scriptUrl, sheetId)` | "stok ekle" işlemini tetikler | `MainViewModel.AddStockAsync` |

## Önemli davranışlar ve iş kuralları

### Kullanılan sütunlar

Başlıklar `Trim().ToLowerInvariant()` sonrası eşlenir:

| Sütun | Eşleşme kuralı | Kullanan |
|---|---|---|
| `mos` | tam olarak `mos` | hepsi (taş ID'si) |
| Bizdeki (kg) | `bizdeki` ve `(kg)` içeren **ilk** sütun | `FetchStockAsync`, `CheckStockAsync` (`OnHand`, isteğe bağlı) |
| Tahmini Kalan | `tahmini` ve `kalan` içeren sütun (son eşleşen) | `CheckStockAsync` (zorunlu) |

Zorunlu sütun yoksa `StockErrColumns` metniyle (beklenen sütunlar + bulunan başlıklar) `Exception` atılır. CSV'de başlık dışında satır yoksa `StockErrEmpty`.

### Apps Script POST eylemleri

| Metot | Gövde | "bulunamadı" hata sayılır mı |
|---|---|---|
| `CheckStockAsync` | `{ projectName, sheetId, stones: [{ mos, count }, ...] }` (`action` alanı yok; script'in varsayılan eylemi) | Hayır |
| `ClearOneAsync` | `{ action: "clearOne", sheetId, projectName }` | Evet |
| `ClearAllAsync` | `{ action: "clearAll", sheetId }` | Evet |
| `AddStockAsync` | `{ action: "stokEkle", sheetId }` | Evet |

`projectName`, `MainViewModel` tarafından görsel dosya adından (yoksa proje dosya adından) uzantısız olarak türetilir ve Sheet'teki sütun başlığıdır.

### Hata işleme (`PostAsync`)

1. Yanıt gövdesi `<!DOCTYPE` veya `<html` içeriyorsa (genelde dağıtım/erişim izni hatası, Google giriş sayfası) `StockErrDeploy` metniyle hata.
2. Gövde JSON ise `status` ve `message` okunur; değilse `message` = ham gövde.
3. Şu durumlarda `Exception(message)` atılır (mesaj 300 karakterle kırpılır):
   - `status == "error"`;
   - `treatNotFoundAsError` ve mesaj `bulunamadi`/`bulunamadı` içeriyor (script eksik sütunu `status: "ok"` ile bildirir; WPF bunu hata sayar);
   - `status` boş ve gövde `error` içeriyor.

Hatalar çağırana yükselir; `MainViewModel.RunStockAction` bunları durum çubuğu/uyarı olarak gösterir.

### Stok kontrol akışı

1. POST ile taş sayıları yazılır.
2. WPF'teki gibi 100 ms beklenir (gviz kısa süre eski veri dönebilir).
3. CSV okunur; `Tahmini Kalan < 0` olanlar `ShortIds`'e girer.
4. `OnHand` yalnızca `Remaining`'de yer alan ID'ler için doldurulur (WPF ile aynı).

## Dikkat / bilinen sınırlamalar

- Okuma için Sheet'in "bağlantıya sahip herkes görüntüleyebilir" olması gerekir; gviz dışa aktarımı oturum açmadan çalışır.
- CSV satırlara `\n`/`\r` ile bölünür; hücre içinde satır sonu olan tırnaklı alanlar bozulur.
- 100 ms bekleme, gviz önbelleği nedeniyle her zaman yeterli olmayabilir; kontrol sonucu bir önceki duruma ait olabilir.
- `PostAsync` HTTP durum kodunu ayrıca kontrol etmez; yalnızca gövdeye bakar. `status` boşken gövdede geçen herhangi bir `error` kelimesi hata sayılır.
- `LoadConfig` bozuk ayar dosyasını sessizce yok sayar.

## İlgili dosyalar

- [StockSettingsDialog](../Controls/StockSettingsDialog.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
- [Loc](./Loc.md) (`Stock*` metinleri)
- [ProjectService](./ProjectService.md) (`CurrentPictureFileName` → proje sütun adı)
- [Arayüz rehberi](../../ARAYUZ.md)
