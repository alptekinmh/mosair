# NewImageWatcher

> Kaynak: `mosair/Services/NewImageWatcher.cs` · Güncelleme: 2026-10-06

## Amaç

Kullanıcının **İndirilenler** (Downloads) ve **Masaüstü** klasörlerine yeni gelen JPEG ve PNG dosyalarını fark edip pencereye bildirmek; pencere de sağ altta "mosair'de açılsın mı?" bildirimini gösterir. Tarayıcı indirmeleri, kopyalama ve başka programların kaydettiği dosyalar yakalanır.

## Nerede kullanılır

| Çağıran | Kullanım |
|---|---|
| `MainWindow` | `_imageWatcher` alanı; `ImageArrived` olayını `Dispatcher.UIThread.Post` ile `ShowImageToast`'a bağlar. `ApplyWatchNewImages` `Start` / `Stop`, pencere kapanınca `Dispose`. Bildirimin başlığını `Place`'e göre seçer. |
| `MainViewModel.ExportImageAsync` | Yazmadan önce `Ignore(path)`: dışa aktarılan dosya bildirilmez. |
| `MainViewModel.SaveScreenshotAsync` | Yazmadan önce `Ignore(path)`: ekran görüntüsü bildirilmez. |

## Yapı

### `Place` (enum)

| Değer | Anlamı |
|---|---|
| `Downloads` | Dosya İndirilenler klasörüne geldi (`ToastNewDownload`) |
| `Desktop` | Dosya Masaüstüne geldi (`ToastNewDesktop`) |

### Alanlar

| Ad | Tip | Açıklama |
|---|---|---|
| `ImageArrived` | `event Action<string, Place>?` | Dosyanın tam yolu ve klasörü. **İş parçacığı havuzundan** çağrılır; UI'a dokunan dinleyici kendisi UI iş parçacığına geçmelidir. |
| `_watchers` | `List<FileSystemWatcher>` | Etkin izleyiciler (en çok iki). |
| `_seen` | `ConcurrentDictionary<string, DateTime>` | Bildirilen (ya da hazır olması beklenen) yollar ve ilk görülme zamanları (büyük/küçük harf duyarsız); her dosya uygulama açık kaldıkça bir kez bildirilir. |
| `Ignored` | `static ConcurrentDictionary<string, DateTime>` | mosair'in kendi yazdığı dosyalar (`Ignore`). Kayıtlar silinmez; yalnızca 2 dakikadan eskiyse dikkate alınmaz. |

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `Start()` | Zaten izliyorsa hiçbir şey yapmaz. `DownloadsFolder()` ve `Environment.SpecialFolder.DesktopDirectory` için birer `FileSystemWatcher` kurar; iki yol aynıysa (büyük/küçük harf duyarsız) Masaüstü ayrıca eklenmez. | `MainWindow.ApplyWatchNewImages` |
| `Stop()` | Bütün izleyicileri dispose eder, listeyi boşaltır. Zaten bekleyen (dosyanın bitmesini bekleyen) kontroller sürer ve olay yine gelebilir; pencere bunu `WatchNewImages` ile süzer. | `ApplyWatchNewImages`, `Dispose` |
| `Dispose()` | `Stop()`. | `MainWindow` (`Closed`) |
| `Ignore(path)` (static) | `Path.GetFullPath(path)`'i şimdiki zamanla kaydeder; bu yol 2 dakika içinde gelirse olay çıkmaz. Hata yutulur. | `MainViewModel.ExportImageAsync`, `SaveScreenshotAsync` |
| `IsImage(path)` (static) | Uzantı (küçük harfe çevrilmiş) `.jpg`, `.jpeg` ya da `.png` mi. | `Seen` |
| `DownloadsFolder()` (static) | Windows'ta `SHGetKnownFolderPath(FOLDERID_Downloads)` (taşınmış/yeniden yönlendirilmiş İndirilenler de bulunur); başarısızsa ya da başka işletim sisteminde `UserProfile/Downloads`. | `Start` |

## Önemli davranışlar ve iş kuralları

### İzleme

- Her klasör için `FileSystemWatcher`: `IncludeSubdirectories = false` (yalnızca klasörün kendisi), `NotifyFilter = FileName`. Klasör yoksa ya da izleyici kurulamazsa (izin, ağ sürücüsü) o klasör sessizce atlanır.
- Hem `Created` hem `Renamed` dinlenir. Tarayıcılar dosyayı önce `x.jpg.crdownload` (Chrome, Edge) ya da `x.jpg.part` (Firefox) adıyla yazar, bitince `x.jpg`'ye çevirir; bu yeniden adlandırma `Renamed` olarak gelir. Geçici adların uzantısı uygun olmadığı için yazılırken olay çıkmaz.
- mosair'in dışa aktarması da önce `.part` dosyasına yazıp sonra asıl ada taşır; bu yüzden `Ignore` asıl yolu kaydeder.

### Süzme ve bekleme (`Seen`)

1. `IsImage` değilse bırakılır.
2. Yol `_seen`'e eklenemiyorsa (daha önce görüldüyse) bırakılır: her dosya bir kez bildirilir (bir dosya için `Created` + `Renamed` ya da birkaç yazma olayı gelebilir, tarayıcı aynı adla yeniden yazabilir). Dosya 30 sn içinde okunur hale gelmezse yol `_seen`'den çıkarılır, sonraki olayda yeniden denenir.
3. Arka planda (`Task.Run`) `WaitUntilReadyAsync`: 500 ms aralıkla en çok 60 kez (≈30 sn) dosya `FileShare.Read` ile açılmaya çalışılır. Dosya boş değilse ve boyutu art arda iki denemede aynıysa hazır sayılır. `IOException` (hâlâ yazılıyor) beklemeye devam ettirir; dosya kaybolursa ya da `UnauthorizedAccessException` gelirse vazgeçilir. 30 sn'de hazır olmazsa olay çıkmaz.
4. Yol `Ignored`'da ve kaydı 2 dakikadan yeniyse bırakılır.
5. `ImageArrived(path, place)`.

Bu yüzden bildirim, dosya oluştuktan en az yaklaşık 1 saniye sonra çıkar.

## Dikkat / bilinen sınırlamalar

- Alt klasörler izlenmez (ör. `Masaüstü/mosairEXPORT`, `Masaüstü/mosairPROJECT`); bu mosair'in kendi klasörlerindeki dosyaları zaten dışarıda bırakır.
- Yalnızca JPEG ve PNG; BMP ve TIFF (Görsel Yükle'nin kabul ettiği diğer biçimler) bildirilmez. Uzantıya bakılır, içerik denetlenmez.
- `_seen` ve `Ignored` sözlükleri temizlenmez; uygulama açık kaldıkça (çok sayıda dosya gelirse) küçük de olsa büyür.
- Var olan bir dosyanın üzerine yazılması (`Changed`) bildirilmez; yalnızca yeni oluşturulan ya da yeni adı verilen dosyalar.
- `FileSystemWatcher` arabelleği taşarsa (çok kısa sürede çok sayıda dosya) olaylar kaybolabilir; `Error` olayı dinlenmez.
- `ImageArrived` dinleyicisi iş parçacığı havuzunda çağrılır; dinleyicide oluşan bir istisna o görevde kalır.
- `DownloadsFolder()` Windows dışında yerelleştirilmiş klasör adlarını (ör. Linux'ta `xdg-user-dir`) dikkate almaz; macOS'ta `~/Downloads` doğrudur.

## İlgili dosyalar

- [MainWindow](../MainWindow.md) — bildirim kutusu (`toastPanel`) ve `ShowImageToast`
- [MainViewModel](../ViewModels/MainViewModel.md) — `WatchNewImages`, `ExportImageAsync`, `SaveScreenshotAsync`
- [Loc](Loc.md) — `MenuWatchImages`, `Toast*`, `StatusToastBusy`
- [Kullanıcı arayüzü (ARAYUZ)](../../ARAYUZ.md#yeni-görsel-bildirimi)
