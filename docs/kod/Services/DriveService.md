# DriveService

> Kaynak: `mosair/Services/DriveService.cs` (+ `mosair/Assets/mosair-drive.gs`) · Güncelleme: 2026-10-08

## Amaç

Projeleri (`.mos`) orijinal görselleriyle birlikte bir Google Drive klasörüne kaydetmek ve oradan açmak. Uygulamada Google oturumu açılmaz: stok tablosundaki gibi, kullanıcının kendi hesabında yayınladığı küçük bir Apps Script web uygulamasına ([`Assets/mosair-drive.gs`](#apps-script-assetsmosair-drivegs)) JSON `POST` gönderilir; Drive'a script yazar ve okur. Dosyalar gidiş ve dönüşte gzip ile sıkıştırılır ve base64 olarak taşınır.

Görsel Ayarları kullanılan projede gönderilen görsel, `ProjectService.WriteSnapshot`'ın proje yanına yazdığı **ayarlı** görseldir (`orijinal/` alt klasörü gönderilmez); Drive'daki görsel farklıysa script onu yenisiyle değiştirir (2026-10-08 sürümü). Klasör düzeni masaüstündeki **mosairPROJECT** ile aynıdır: her proje Drive klasörünün içinde görselin adını taşıyan bir alt klasöre konur; içinde `<ad>.mos` ve orijinal görsel bulunur.

## Nerede kullanılır

| Çağıran | Kullanım |
|---|---|
| `MainViewModel.ConfigureDriveAsync` | `LoadConfig` → `ShowDriveSettings` (ayar penceresi) → `SaveConfig` |
| `MainViewModel.DriveConfigOrAsk` | `LoadConfig`, `IsConfigured` |
| `MainViewModel.SaveToDriveAsync` | `CacheDir` (tek seferlik `upload_<guid>` klasörü), `SaveAsync` |
| `MainViewModel.OpenFromDriveAsync` | `DownloadAsync` |
| `DriveOpenDialog` | `PingAsync` (klasör adı), `ListAsync`, `ThumbnailsAsync`, `FolderWebUrl`, `DriveFile` |
| `DriveSettingsDialog` | `Config`, `IsConfigured`, `ScriptCode` (panoya kopyalama), `PingAsync` ("Bağlantıyı dene") |
| `MainWindow.axaml.cs` | `DriveService.Config` ve `DriveService.DriveFile` tipleri (`ShowDriveSettings`, `ShowDriveOpen` delegeleri) |

## Yapı

### `Config`

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `FolderUrl` | `string` | `""` | Drive klasörünün tarayıcıdaki bağlantısı ya da yalnızca klasör kimliği. Olduğu gibi saklanır; kimlik her istekte `FolderId` ile ayrılır. |
| `ScriptUrl` | `string` | `""` | Apps Script web uygulamasının adresi, biçim: `https://script.google.com/macros/s/<DEPLOYMENT_ID>/exec`. |

**Ayar dosyası:** `Environment.SpecialFolder.ApplicationData` altında `mosair/drive.json` (Windows'ta `%APPDATA%\mosair\drive.json`). Kullanıcıya özeldir, girintili JSON olarak yazılır; `stock.json` ile aynı yerdedir.

> Bu değerler repoya, belgelere veya derlemelere asla konmamalıdır: script adresini bilen herkes klasöre yazıp oradan okuyabilir. Repo herkese açıktır.

### `DriveFile` (record)

| Ad | Tip | Açıklama |
|---|---|---|
| `Id` | `string` | Drive dosya kimliği (`get` isteğinde gönderilir) |
| `Name` | `string` | Dosya adı (ör. `st1.mos`) |
| `Folder` | `string` | Drive klasörünün içindeki proje klasörünün adı; `.mos` doğrudan klasörün kökündeyse `""` |
| `Size` | `long` | Bayt; yanıtta yoksa 0 |
| `Modified` | `DateTime` | Son değişiklik (yerel saate çevrilir); okunamazsa `DateTime.MinValue` |
| `ImageId` | `string` | Aynı proje klasöründeki orijinal görselin dosya kimliği (önizleme için); yoksa ya da script eskiyse `""` |

### Özel üyeler

| Ad | Açıklama |
|---|---|
| `ConfigPath` | Ayar dosyası yolu |
| `CacheDir` (public) | `LocalApplicationData/mosair/drive` (Windows'ta `%LOCALAPPDATA%\mosair\drive`). Kaydederken proje burada tek seferlik bir `upload_<guid>\<ad>` klasörüne yazılır; bu klasör (görsel kopyasıyla birlikte) gönderme başarılı da olsa başarısız da olsa silinir. Drive'dan indirilen projeler `<proje klasörü>\` altına görselleriyle konur (açmak için diskte dosya gerekir). |
| `Http` | Paylaşılan `HttpClient`: yönlendirme açık (en fazla 5), zaman aşımı **10 dk** (büyük projeler) |
| `GetAsync(c, fileId, imageName)` | `{ action: "get", fileId[, imageName] }`; `data` boşsa boş dizi, değilse base64 çözülüp gunzip edilmiş baytlar |
| `SafeName(name)` | Geçersiz dosya adı karakterlerini atar |
| `PictureFileName(project)` | `.mos` JSON'unun kök düzeyindeki `PictureFileName` değerini `Utf8JsonReader` ile (tüm ağacı yüklemeden) okur; yoksa ya da JSON bozuksa `null` |
| `Gzip` / `Gunzip` | `GZipStream` ile sıkıştırma (`CompressionLevel.Optimal`) ve açma |
| `PostAsync` | İsteği gönderir, yanıtı yorumlar (aşağıda) |

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `LoadConfig()` | Ayar dosyasını okur; yoksa veya bozuksa boş `Config` (hata yutulur) | `MainViewModel` |
| `SaveConfig(Config)` | Klasörü oluşturup ayar dosyasını yazar | `MainViewModel.ConfigureDriveAsync` |
| `FolderId(folderUrl)` → `string` | Klasör kimliğini ayırır: önce `/folders/<id>`, sonra `?id=<id>` / `&id=<id>`, yoksa metnin kendisi en az 10 karakterlik `[A-Za-z0-9_-]` ise o; hiçbiri değilse `""`. Metin önce `Trim` edilir. | `IsConfigured`, `PostAsync`, `FolderWebUrl` |
| `IsConfigured(Config)` → `bool` | `FolderId` boş değil ve `ScriptUrl` boş değil | `MainViewModel.DriveConfigOrAsk`, `DriveSettingsDialog.OnTest` |
| `ScriptCode()` → `string` | Derlemeye gömülü (`EmbeddedResource`) `mosair-drive.gs` kaynağını UTF-8 olarak okur; kaynak adı `mosair-drive.gs` ile biten ilk kaynak. Bulunamazsa `""`. | `DriveSettingsDialog.OnCopyScript` |
| `PingAsync(Config)` → `string` | `{ action: "ping" }`; klasörün adını döndürür (bağlantıyı ve script'i dener) | `DriveSettingsDialog.OnTest`, `DriveOpenDialog.LoadAsync` |
| `SaveAsync(Config, folderName, name, project, imageName = null, image = null)` | `{ action: "save", folderName, name, data[, imageName, image] }`; `data` ve `image` gzip'lenmiş baytların base64'ü. Görsel yalnızca hem `image` hem `imageName` verilmişse gönderilir. | `MainViewModel.SaveToDriveAsync` |
| `ListAsync(Config)` → `List<DriveFile>` | `{ action: "list" }`; yanıttaki `files` dizisini `DriveFile` listesine çevirir (`folder` ve `imageId` yoksa `""`) | `DriveOpenDialog.LoadAsync` |
| `ThumbnailsAsync(Config, ids)` → `Dictionary<string, byte[]>` | `{ action: "thumbs", ids }`; Drive'ın küçük resimlerini (PNG baytları) kimliğe göre döndürür; boş gelenler sözlüğe girmez. `ids` boşsa istek gönderilmez. | `DriveOpenDialog.LoadThumbnailsAsync` |
| `FolderWebUrl(Config)` → `string` | `https://drive.google.com/drive/folders/` + `FolderId` (klasörün tarayıcı adresi) | `DriveOpenDialog.OnOpenInBrowser` |
| `DownloadAsync(Config, DriveFile)` → `string` | Projeyi `GetAsync` ile alır, `CacheDir\<Folder ya da "_">\<ad>` olarak yazar (geçersiz karakterler atılır; ad boş kalırsa `drive.mos`). Sonra `.mos` içindeki `PictureFileName`'i okur; doluysa aynı proje klasöründeki o adlı dosyayı (`get` + `imageName`) indirip projenin yanına yazar. Projenin yolunu döndürür; eski dosyaların üzerine yazar. | `MainViewModel.OpenFromDriveAsync` |

## Önemli davranışlar ve iş kuralları

### İstek ve yanıt (`PostAsync`)

1. `FolderId` boşsa ya da `ScriptUrl` boşsa `DriveNotConfigured` metniyle `InvalidOperationException`.
2. Gövdeye her zaman `folderId` eklenir; JSON olarak `ScriptUrl`'ye (`Trim` edilmiş) `POST` edilir.
3. Yanıt gövdesi `<!DOCTYPE` veya `<html` içeriyorsa (Google bazen kısa süre HTML sayfa döner) 2 sn beklenip bir kez yeniden gönderilir. İkinci yanıt da HTML ise `DriveErrDeploy` metniyle hata atılır; sayfanın `<title>` değeri varsa mesaja yeni satırda parantez içinde eklenir (dağıtım/erişim hatası, Google giriş sayfası vb.).
4. Gövde JSON değilse yine `DriveErrDeploy`.
5. `status != "ok"` ise `message` (yoksa ham gövde) 300 karakterle kırpılıp `Exception` olarak atılır. Hatalar çağırana yükselir; `MainViewModel` bunları `DriveFailed` ile durum çubuğuna ve uyarıya, `DriveOpenDialog` pencerenin içine yazar, ayar penceresi `DriveTestFailed` ile gösterir.

HTTP durum kodu ayrıca kontrol edilmez; yalnızca gövdeye bakılır.

### Sıkıştırma ve boyut

Proje JSON'u ve görsel gzip ile sıkıştırılıp base64 ile gönderilir; script bunları açıp olağan dosyalar olarak saklar. İndirirken script dosyayı gzip'leyip base64 ile döndürür, uygulama açar. Drive'daki dosyalar her durumda sıkıştırılmamış, olağan `.mos` ve görsel dosyalarıdır (başka bir bilgisayarda doğrudan indirilip açılabilir). Apps Script büyük isteklere ve yanıtlara sınır koyar (yaklaşık 50 MB); sıkıştırma bu sınıra daha geç ulaşılmasını sağlar ama sınırı kaldırmaz. Görsel ilk kayıtta projeyle aynı istekte gider.

### Apps Script (`Assets/mosair-drive.gs`)

`mosair.csproj` içinde `EmbeddedResource` olarak derlemeye gömülür; ayar penceresindeki **Script kodunu kopyala** düğmesi kodu panoya kopyalar. Tek giriş noktası `doPost(e)`'dir; her istek önce `allowedFolder(req.folderId)` ile klasörü bulur, yanıtlar `reply(obj)` ile JSON olarak döner, her hata `{ status: "error", message }` olur.

| `action` | Gövde | Ne yapar | Yanıt |
|---|---|---|---|
| `ping` | `folderId` | Klasörü açar | `{ status: "ok", folder: <klasör adı> }` |
| `save` | `folderId`, `name`, `data`, isteğe bağlı `folderName`, `image`, `imageName` | Ad `.mos` ile bitmezse hata. `folderName` varsa hedef, klasörün içindeki o adlı alt klasördür (`subfolder`: `/` ve `\` `_` olur, boşsa `mosair_project`; yoksa oluşturulur). `data` açılıp `replaceFile` ile yazılır: hedefteki **aynı adlı** dosyalar not edilir, **önce** yenisi oluşturulur, ancak ondan sonra eskiler Drive çöp kutusuna taşınır (yazma başarısız olursa eski kopya yerinde kalır). `image` ve `imageName` verilmişse görsel açılır; hedefte o adda dosya yoksa ya da varsa ama boyutu farklıysa `replaceFile` ile yazılır (eskisi çöp kutusuna); boyutu aynıysa dokunulmaz. Böylece ayarları değişen projenin görseli Drive'da da yenilenir. | `{ status: "ok", id, name, folder }` |
| `list` | `folderId` | Klasörün kendisindeki ve bir alt düzeydeki proje klasörlerindeki adı `.mos` ile biten dosyalar (`addMos`), son değişikliğe göre en yeni üstte. Alt klasördeki her `.mos` için `imageId` = o klasördeki ilk `image/*` dosyası; kökteki `.mos` için `""`. | `{ status: "ok", folder, files: [{ id, name, folder, size, modified (ISO), imageId }] }` |
| `get` | `folderId`, `fileId`, isteğe bağlı `imageName` | Adı `.mos` ile bitmeyen dosyayı reddeder; dosyanın klasörün kendisinde ya da bir proje klasöründe olduğunu (`parentInside`) denetler, değilse hata. `imageName` yoksa projeyi, varsa aynı klasördeki o adlı dosyayı gzip'leyip base64 döndürür; o dosya yoksa `data: ""`. | `{ status: "ok", name, data }` |
| `thumbs` | `folderId`, `ids` | Her kimlik için dosya klasörün içindeyse (`parentInside`) Drive küçük resmini (`getThumbnail`) base64 PNG olarak döndürür; küçük resim yoksa ya da dosya okunamazsa `""`; klasör dışındaki dosyalar atlanır. | `{ status: "ok", thumbs: { <id>: <base64> } }` |

- **`ALLOWED_FOLDERS`** (isteğe bağlı script özelliği, Proje ayarları → Komut dosyası özellikleri): virgülle ayrılmış klasör kimlikleri. Tanımlıysa yalnızca bu klasörlerle çalışılır, diğerleri `Bu klasöre izin verilmiyor (ALLOWED_FOLDERS)` hatası alır.
- Script hata mesajları Türkçedir (`Klasör belirtilmedi`, `Dosya adı .mos ile bitmeli`, `Yalnızca .mos projeleri açılabilir`, `Dosya bu klasörde değil`, `Bilinmeyen işlem: …`); uygulama bunları `DriveFailed` / `DriveTestFailed` içinde olduğu gibi gösterir.

**Kurulum (bir kez, Drive hesabının sahibi):**

1. Ayar penceresinde **Script kodunu kopyala**.
2. `script.google.com` → Yeni proje → kodu yapıştırıp kaydet.
3. Dağıt → Yeni dağıtım → Tür: **Web uygulaması**; Yürüten: **Ben**, Erişimi olan: **Herkes**.
4. İzinleri onayla; verilen `/exec` adresini ayar penceresindeki **Apps Script URL** alanına yapıştır, klasör bağlantısını gir, **Bağlantıyı dene**.

Script kodu değiştiğinde (uygulamanın yeni bir sürümünden kopyalandığında) kod yapıştırılıp kaydedildikten sonra **mevcut dağıtım** yeni sürümle güncellenmelidir: Dağıt → **Dağıtımları yönet** → düzenle (kalem) → Sürüm: **Yeni sürüm** → Dağıt. Böylece `/exec` adresi değişmez ve uygulamadaki ayar aynı kalır (yeni bir dağıtım oluşturulursa adres değişir). Eski bir dağıtımla uygulama kısmen çalışır: proje klasörleri, görselin gönderilip indirilmesi ve önizlemeler (`folderName`/`image` alanları, `get`'in `imageName`'i, `list`'in alt klasörleri ve `imageId`'si, `thumbs`) güncel script'i gerektirir; eski script'le proje klasörün köküne yazılır ve önizleme görünmez. Bu adımlar `.gs` dosyasının baş yorumunda da yazar.

### Güvenlik

- Script "Yürüten: Ben" ile dağıtıldığı için dağıtan hesabın yetkisiyle çalışır ve "Erişimi olan: Herkes" olduğu için adresi bilen herkes oturum açmadan çağırabilir.
- `folderId` isteği gönderen taraftan gelir. **`ALLOWED_FOLDERS` tanımlı değilse** script adresini bilen biri, dağıtan hesabın erişebildiği **herhangi bir** klasöre `.mos` ve görsel yazabilir, bu klasörlerdeki (ve bir alt düzeydeki) `.mos` dosyalarını, yanlarındaki dosyaları ve küçük resimlerini okuyabilir, aynı adlı `.mos` dosyalarını çöp kutusuna taşıyıp değiştirebilir. Bu yüzden `ALLOWED_FOLDERS` ile yalnızca proje klasörüne izin verilmesi önerilir; adres yalnızca güvenilen kişilerle paylaşılmalıdır.
- Klasör bağlantısı, klasör kimliği ve script adresi hiçbir dosyaya, belgeye ya da commit mesajına yazılmaz.

## Dikkat / bilinen sınırlamalar

- `LoadConfig` bozuk ayar dosyasını sessizce yok sayar.
- Zaman aşımı 10 dk'dır; Drive işlemleri iptal edilemez.
- `DownloadAsync` indirdiği dosyaları `CacheDir`'den silmez; aynı proje yeniden indirilince üzerine yazılır.
- Görsel Drive'a yalnızca proje klasöründe o adda dosya yokken gönderilir; görsel değişip adı aynı kaldıysa Drive'daki eski görsel kalır.
- `list` yalnızca bir alt düzeye bakar; daha derindeki projeler listelenmez. Bir proje klasöründe birden fazla görsel varsa önizleme için ilk bulunan kullanılır.
- `FolderId` bağlantı dışı metinde en az 10 karakter ister; daha kısa bir kimlik "ayarlanmamış" sayılır.

## İlgili dosyalar

- [DriveSettingsDialog](../Controls/DriveSettingsDialog.md)
- [DriveOpenDialog](../Controls/DriveOpenDialog.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
- [Kullanıcı arayüzü (ARAYUZ)](../../ARAYUZ.md#google-drive-proje-klasörü)
