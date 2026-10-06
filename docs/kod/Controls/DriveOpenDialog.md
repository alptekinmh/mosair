# DriveOpenDialog

> Kaynak: `mosair/Controls/DriveOpenDialog.axaml`, `mosair/Controls/DriveOpenDialog.axaml.cs` · Güncelleme: 2026-10-06

## Amaç

Google Drive proje klasörü için Drive'ın ızgara görünümüne benzeyen modal proje tarayıcısı. Klasördeki `.mos` projelerini kendisi yükler, her projeyi orijinal görselin küçük resmini taşıyan bir kart olarak gösterir; arama, sıralama, yenileme ve klasörü tarayıcıda açma sunar. Seçilen `DriveService.DriveFile` ya da iptalde `null` döner; indirme ve açma işini `MainViewModel.OpenFromDriveAsync` yapar.

## Nerede kullanılır

`MainWindow` yapıcısında `MainViewModel.ShowDriveOpen` geri çağrısına bağlanır:

```csharp
_vm.ShowDriveOpen = config =>
    new Controls.DriveOpenDialog(config).ShowDialog<DriveService.DriveFile?>(this);
```

Akış: **Dosya → Google Drive → Drive'dan Aç...** ya da Drive ikonunun **▾** Flyout'u → `OnDriveOpen` → `MainViewModel.OpenFromDriveAsync` → (ayarlar `DriveConfigOrAsk` ile alınır) → bu diyalog (`PingAsync` + `ListAsync` + `ThumbnailsAsync` içeride) → seçilen dosya `DownloadAsync` ile indirilip `OpenProject` ile açılır.

## Yapı

### `DriveFileRow` (sınıf, `INotifyPropertyChanged`)

Bir proje kartı. Yapıcı: `DriveFileRow(DriveService.DriveFile file, string name, string details)`.

| Üye | Tip | Açıklama |
|---|---|---|
| `File` | `DriveService.DriveFile` | Seçilince döndürülen dosya |
| `Name` | `string` | Proje dosyasının adı (ör. `st1.mos`); kartta ve ipucunda |
| `Details` | `string` | `Modified.ToString("g")` + ` · ` + boyut (1 MB ve üstü `SizeMB` (x.x), altı `SizeKB` (en az 1)); tarih yoksa yalnızca boyut |
| `Thumbnail` | `Bitmap?` | Önizleme geldiğinde atanır; `Thumbnail` ve `HasThumbnail` değişikliğini bildirir |
| `HasThumbnail` | `bool` | `Thumbnail != null`; yer tutucu ikon ile görselin görünürlüğünü seçer |

### Kontroller

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| (pencere) | `Window` | `820 × 600`, `MinWidth=520`, `MinHeight=380` | Boyutlandırılabilir, `CenterOwner`, arka plan `BgCard`. Başlık `DriveOpenTitle`. |
| (logo) | `Viewbox` + `Path` ×3 | 20 × 20 | Toolbar'daki renkli Drive logosu |
| `TitleText` | `TextBlock` | — | `DriveOpenTitle`. |
| `FolderText` | `TextBlock` | — | Yüklenince `DriveOpenFolder` (klasör adı, proje sayısı); yüklenirken ve hatada boş. |
| `BrowserButton` | `Button` | — | `DriveShowInBrowser` ("Drive'da göster"); `OnOpenInBrowser`. |
| `SearchBox` | `TextBox` | — | Filigran `DriveSearch`; her değişiklikte `ApplyView`. |
| `SortBox` | `ComboBox` | 0 | `DriveSortNewest` (0, en yeni üstte) / `DriveSortName` (1, ada göre A-Z); `OnSortChanged`. |
| `RefreshButton` | `Button` | — | `DriveRefresh` ("Yenile"); yükleme sürerken pasif. `OnRefresh`. |
| `FileList` | `ListBox` | — | `ItemsPanel` = `WrapPanel` (yatay kaydırma kapalı). Kart şablonu: 176 px genişlik, 120 px önizleme alanı (`BgCanvas`, görsel `UniformToFill` ile kırpılır; görsel yoksa dört kareli `PathIcon` yer tutucu), altında ad (kalın, `…` ile kısaltılır) ve `Details`. Öğe kenar boşluğu 4, köşe yarıçapı 8. `SelectionChanged`, `DoubleTapped`. |
| `MessageText` | `TextBlock` | — | Listenin ortasında durum: yüklenirken `StatusDriveListing`, klasör boşsa `DriveOpenEmpty`, aramayla eşleşme yoksa `DriveNoMatch`, hatada `DriveFailed`. |
| `CancelButton` | `Button` | — | `DlgCancel`; `OnCancelClick`. |
| `OpenButton` | `Button` | `IsEnabled="False"` | `DriveOpenButton` ("Aç"); bir kart seçilince etkin. `OnOpenClick`. |

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `DriveOpenDialog()` | `InitializeComponent`; boş `Config`. | Avalonia |
| `DriveOpenDialog(DriveService.Config config)` | Metinleri `Loc`'tan doldurur, sıralamayı "En yeni üstte" yapar; pencere açılınca (`Opened`) `LoadAsync`. | `MainWindow` (`ShowDriveOpen`) |
| `LoadAsync` (private) | Listeyi ve önizlemeleri temizler, `RefreshButton`'ı kapatır, `StatusDriveListing` gösterir; `PingAsync` (klasör adı) + `ListAsync`, kartları kurar, `ApplyView`, sonra `LoadThumbnailsAsync`. Hata `MessageText`'e `DriveFailed` olarak yazılır. `_loadVersion` sayacı: eski bir yüklemenin sonucu yenisinin üzerine yazılmaz. | `Opened`, `OnRefresh` |
| `LoadThumbnailsAsync` (private) | Kartların boş olmayan `ImageId`'lerini (tekrarsız) tek `ThumbnailsAsync` isteğiyle alır, her karta `Bitmap` atar. Hatalar yutulur (önizleme isteğe bağlıdır). | `LoadAsync` |
| `ApplyView` (private) | Arama (proje adı ya da proje klasörü adı içinde, büyük/küçük harf duyarsız) ve sıralama (`Modified` azalan ya da ad A-Z) uygular, `FileList.ItemsSource`'u yeniler. | Arama, sıralama, yükleme |
| `OnOpenInBrowser` (private) | `TopLevel.Launcher.LaunchUriAsync(DriveService.FolderWebUrl(config))`: klasörü varsayılan tarayıcıda açar. Hata `MessageText`'e. | `BrowserButton` |
| `OnSelectionChanged` / `OnDoubleTapped` / `OnOpenClick` / `OnCancelClick` (private) | Aç düğmesini etkinleştirir; çift tık ve Aç `Close(row.File)`; İptal `Close(null)`. | Kontroller |

## Önemli davranışlar ve iş kuralları

- Liste pencere açıldıktan sonra yüklenir; ana pencerede `IsDriveBusy` bu sırada `false`'tur (pencere modaldır). İndirme seçimden sonra `MainViewModel`'de başlar.
- Önizlemeler listeden sonra gelir; o zamana kadar (ya da proje klasöründe görsel yoksa, klasörün kökündeki `.mos` için, script eskiyse) kartta yer tutucu ikon durur.
- Kartta proje klasörünün adı yazmaz, yalnızca `.mos` adı; arama ise klasör adında da arar.
- Script'in `thumbs` eylemi ya da `imageId` alanı olmayan eski sürümüyle de çalışır: önizlemeler eksik kalır, gerisi aynıdır.

## Dikkat / bilinen sınırlamalar

- Pencere başlık çubuğundan kapatılırsa `null` döner (iptal ile aynı).
- Tarih biçimi (`"g"`) ve boyuttaki ondalık ayırıcı arayüz dilini değil, işletim sisteminin bölge ayarını izler.
- Önizlemeler Drive'ın kendi küçük resimleridir; Drive yeni yüklenen bir görsel için henüz üretmediyse boş gelir (Yenile ile tekrar istenir).
- Önizleme bitmap'leri pencere kapanınca açıkça `Dispose` edilmez (GC'ye bırakılır).

## İlgili dosyalar

- [DriveService](../Services/DriveService.md)
- [DriveSettingsDialog](DriveSettingsDialog.md)
- [MainWindow](../MainWindow.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
- [Kullanıcı arayüzü (ARAYUZ)](../../ARAYUZ.md#google-drive-proje-klasörü)
