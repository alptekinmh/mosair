# StockSheetService

> Kaynak: `mosair/Services/StockSheetService.cs` · Güncelleme: 2026-10-06

## Amaç

Taş stoğunu tutan Google Sheet ile entegrasyon. WPF uygulamasındaki stok işlevlerinden (`WindowPicture` stok olayları + `appsScript.js`) taşınmıştır.

- **Okuma:** Sheet'in herkese açık gviz CSV dışa aktarımı üzerinden.
- **Yazma:** Sheet'e bağlı Apps Script web uygulamasına JSON `POST` ile.
- **Stoğa göre mozaik:** taş başına eldeki stok ve diğer mozaiklere ayrılan pay (`StoneStock`, `FetchOnHandAsync`); [StockAwareAssigner](./StockAwareAssigner.md) için kapasiteyi verir.

## Nerede kullanılır

| Çağıran | Kullanım |
|---|---|
| `MainViewModel.ConfigureStockAsync` | `LoadConfig` → ayar diyaloğu → `SaveConfig` |
| `MainViewModel.FetchStockAsync` ("stok çek", `markOnly` ile ya da olmadan) | `FetchStockAsync` |
| `MainViewModel.RefreshStockAsync` (açılışta, görsel/proje yüklenince, Mos öncesi `_loadedStock` boşsa) | `FetchOnHandAsync(sheetId, projectName)` |
| `MainViewModel.FixToStockAsync` (Stok Kontrol yazmadan önce stok düzeltmesi) | `FetchOnHandAsync(sheetId, projectName)` |
| `MainViewModel` (stok raporu, katalog ipuçları), `StockCompareRunner` | `StoneStock`, `StoneWeightKg`; `StockCompareRunner` ayrıca `ParseOnHandCsv` |
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

### `StoneStock`

| Ad | Tip | Açıklama |
|---|---|---|
| `Id` | `int` | `mos` sütunu = katalog taş ID'si |
| `Code` | `string` | "Kod" (ör. C125); 68+ taşlarda boş olabilir |
| `Name` | `string` | "Öğe adı" (adında `adı` geçen ilk sütun). Aynı taşın farklı yüzeyleri aynı adı paylaşır; `StockAwareAssigner` bunu "aynı aile" olarak kullanır |
| `OnHandKg` | `double` | "Bizdeki (kg)" |
| `OtherMosaicsKg` | `double` | Sheet'teki diğer mozaik sütunlarına ayrılan stok (adet toplamı × `StoneWeightKg`, en az 0) |
| `AvailableKg` | `double` (hesaplanan) | `OnHandKg − OtherMosaicsKg` |
| `Capacity` | `int` (hesaplanan) | Bu mozaikte kullanılabilecek adet: `AvailableKg ≤ 0` ise 0, değilse ⌊`AvailableKg` / `StoneWeightKg`⌋ |

`StoneWeightKg` = 0,0033 (bir taş 3,3 g; Sheet'teki "Kullanılacaklar (kg)" tam olarak adet × 3,3 g'dır).

### Özel üyeler

| Ad | Açıklama |
|---|---|
| `ConfigPath` | Ayar dosyası yolu |
| `Http` | Paylaşılan `HttpClient`: yönlendirme açık (en fazla 5), zaman aşımı 60 sn |
| `FetchCsvAsync` | `https://docs.google.com/spreadsheets/d/{sheetId}/gviz/tq?tqx=out:csv` adresini 15 sn iptal süresiyle indirir, satırlara böler |
| `ReadNumberColumn` | `mos` sütunu tamsayı, değer sütunu sayı olan satırları döndürür (virgül → nokta, `InvariantCulture`) |
| `ParseOnHand` | `FetchOnHandAsync` ve `ParseOnHandCsv`'nin ortak ayrıştırıcısı (aşağıdaki "Taş ID eşleştirmesi") |
| `PostAsync` | JSON gövdeyi gönderir, yanıtı yorumlar, hata varsa `Exception` atar; HTML yanıtta bir kez yeniden dener |
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
| `FetchOnHandAsync(sheetId, projectName = null)` → `Dictionary<int, StoneStock>` | CSV'yi okuyup taş başına `StoneStock` döndürür. `projectName` verilirse o mozaiğin kendi sütunu diğer mozaiklerin payına katılmaz | `MainViewModel.RefreshStockAsync`, `MainViewModel.FixToStockAsync` |
| `ParseOnHandCsv(csv, projectName = null)` | Aynı ayrıştırma, hazır CSV metninden (çevrimdışı test) | `StockCompareRunner` |
| `ParseTrNumber(s)` → `double?` | Türkçe sayı biçimi: `"1.027,00"` → 1027,0; `"15,00"` → 15,0; boş → `null`; ayrıştırılamazsa `null` | `ParseOnHand` |

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

1. Yanıt gövdesi `<!DOCTYPE` veya `<html` içeriyorsa (Google bazen kısa süre HTML hata sayfası döner: meşgul, art arda iki yazma) 2 sn beklenip bir kez yeniden gönderilir. İkinci yanıt da HTML ise (genelde dağıtım/erişim izni hatası, Google giriş sayfası) `StockErrDeploy` metniyle hata atılır; sayfanın `<title>` değeri varsa mesaja parantez içinde eklenir.
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

### Taş ID eşleştirmesi (`FetchOnHandAsync` / `ParseOnHand`)
- Tablodaki **mos** sütunu, katalogdaki taş ID'sidir (`Assets/colorsBas.txt` satır sırası, 1–124; kod numarası 100 + ID, ör. B133 = #33). Eşleştirme yalnızca bu numarayla yapılır, taş adı veya Kod ile yapılmaz.
- 68–124 numaralı satırların Kod'u boştur ("Taş 68" …) ama stokları okunur; yalnızca Kod'u, adı ve Bizdeki'si boş satırlar atlanır.
- **mos ≤ 0** satırlar atlanır: katalogda karşılığı olmayan taşlar (ör. "Ege Bej") ve alt kısımdaki yüzlerce boş dolgu satırı.
- Mozaik sütunları Apps Script'in kuralıyla bulunur ("bizdeki" ile "13." arası); `projectName` verilirse o sütun "diğer mozaikler" toplamına girmez (`OtherMosaicsKg`, `AvailableKg`, `Capacity`).
- `ParseOnHand`'daki sayılar `ParseTrNumber` ile (Türkçe biçim) okunur; `FetchStockAsync`/`CheckStockAsync` ise `ReadNumberColumn` ile yalnızca virgülü noktaya çevirir.
- `MainViewModel.RefreshStockAsync` (açılışta `LoadStockOnStartupAsync` üzerinden, görsel veya proje yüklenince) ve `FixToStockAsync` bu okumayı kullanır.

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
