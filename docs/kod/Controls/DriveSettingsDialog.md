# DriveSettingsDialog

> Kaynak: `mosair/Controls/DriveSettingsDialog.axaml`, `mosair/Controls/DriveSettingsDialog.axaml.cs` · Güncelleme: 2026-10-07

## Amaç

Google Drive proje klasörünün iki ayarını kullanıcıdan alan modal pencere:

1. **Drive klasörü bağlantısı**: projelerin kaydedileceği ve açılacağı klasör (bağlantının tamamı ya da yalnızca kimlik).
2. **Apps Script URL**: Drive'a yazan ve okuyan Apps Script web uygulamasının `/exec` adresi.

Ayrıca kurulum adımlarını gösterir, script kodunu panoya kopyalar ve girilen değerlerle bağlantıyı dener. Sonuç olarak yeni bir `DriveService.Config` döndürür ya da iptalde `null`.

## Nerede kullanılır

`MainWindow` yapıcısında `MainViewModel.ShowDriveSettings` geri çağrısına bağlanır:

```csharp
_vm.ShowDriveSettings = current =>
    new Controls.DriveSettingsDialog(current).ShowDialog<DriveService.Config?>(this);
```

Akış: **Dosya → Google Drive → Drive Klasörü Ayarları...** ya da toolbar'daki Drive ikonunun yanındaki **▾** okunun Flyout'u → `OnDriveSettings` → `MainViewModel.ConfigureDriveAsync` → bu diyalog → sonuç `null` değilse `DriveService.SaveConfig` ve durum `DriveSettingsSaved`. Ayar yokken Drive'a kaydetme/açma denenirse `MainViewModel.DriveConfigOrAsk` da aynı yoldan bu pencereyi açar.

## Yapı

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| (pencere) | `Window` | `Width=580`, `SizeToContent="Height"`, `CanResize="False"` | `CenterOwner`, arka plan `BgCard`. Pencere başlığı yapıcıda `DriveSettingsTitle`. |
| `TitleText` | `TextBlock` | — | `DriveSettingsTitle`. |
| `FolderLabel`, `FolderHint` | `TextBlock` | — | `DriveFolderLabel`, `DriveFolderHint`. |
| `FolderExample` | `TextBlock` (`Classes="example"`) | — | `drive.google.com/drive/folders/` + vurgulu kısaltılmış kimlik örneği. |
| `FolderBox` | `TextBox` | mevcut `FolderUrl` | Bağlantının tamamı ya da kimlik. |
| `ScriptUrlLabel`, `ScriptUrlHint` | `TextBlock` | — | `DriveScriptUrlLabel`, `DriveScriptUrlHint`. |
| `ScriptUrlExample` | `TextBlock` (`Classes="example"`) | — | Kısaltılmış, vurgulu `…/exec` adresi örneği. |
| `ScriptUrlBox` | `TextBox` | mevcut `ScriptUrl` | — |
| `StepsTitle`, `StepsText` | `TextBlock` | — | `DriveStepsTitle`, `DriveSteps` (4 adımlı kurulum). `BgPanel` zeminli kutu içinde. |
| `CopyScriptButton` | `Button` | — | `DriveCopyScript`; `OnCopyScript`. |
| `TestButton` | `Button` | — | `DriveTest`; `OnTest`. |
| `ResultText` | `TextBlock` (`Classes="hint"`) | boş | Kopyalama ve deneme sonucu. |
| `NoteText` | `TextBlock` | — | `DriveSettingsNote` (yalnızca bu bilgisayarda saklanır; güvenlik uyarısı); renk `FgWarn`. |
| `CancelButton`, `SaveButton` | `Button` | — | `DlgCancel`, `DlgSave`; `OnCancelClick`, `OnSaveClick`. `SaveButton` paylaşılan `Button.primary` stilini kullanır (vurgu mavisi; bkz. [App](../App.md)). |
| `HighlightBrush` | `static IBrush` (private) | `#3a7bfd` | Örneklerde girilecek kısmın rengi (sabit; temayı izlemez). |

Stiller: `TextBlock.hint` (11 pt, `FgSecondary`, sarmalı) ve `TextBlock.example` (11 pt, `FgMuted`, eş aralıklı yazı tipi, sarmalı); [StockSettingsDialog](StockSettingsDialog.md) ile aynı.

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `DriveSettingsDialog()` | Yalnızca `InitializeComponent`. | Avalonia |
| `DriveSettingsDialog(DriveService.Config current)` | Bütün metinleri `Loc.Get` ile doldurur, örnekleri `SetExample` ile kurar, kutulara mevcut değerleri yazar. | `MainWindow` (`ShowDriveSettings`) |
| `SetExample(target, before, part, after)` (private static) | `TextBlock.Inlines`'ı üç `Run`'dan kurar; ortadaki `part` vurgulu ve yarı kalın. | Yapıcı |
| `Current()` (private) | Kutulardaki değerlerden (`Trim` edilmiş) yeni `Config`. | `OnTest`, `OnSaveClick` |
| `OnCopyScript` (private) | `DriveService.ScriptCode()` metnini `TopLevel` panosuna yazar, `ResultText` = `DriveScriptCopied`. Pano yoksa bir şey yapmaz. | `CopyScriptButton` |
| `OnTest` (private) | Kaydetmeden, kutulardaki değerlerle dener: `IsConfigured` değilse `DriveNotConfigured`; değilse düğmeyi kapatır, `DriveTesting`, `DriveService.PingAsync` → `DriveTestOk` (klasör adıyla) ya da `DriveTestFailed` (hata mesajıyla); sonunda düğme yeniden açılır. | `TestButton` |
| `OnSaveClick` (private) | `Close(Current())`. | `SaveButton` |
| `OnCancelClick` (private) | `Close(null)`. | `CancelButton` |

## Önemli davranışlar ve iş kuralları

- Klasör alanına bağlantının tamamı yapıştırılabilir. Değer **olduğu gibi** saklanır; klasör kimliği her istekte `DriveService.FolderId` ile ayrılır (`/folders/<id>`, `?id=<id>` ya da yalnızca kimlik). Bu, Sheet ID'yi kaydederken ayıklayan [StockSettingsDialog](StockSettingsDialog.md)'dan farklıdır.
- Script URL doğrulanmaz; yalnızca baştaki/sondaki boşluklar kırpılır.
- **Bağlantıyı dene** kaydetmez; ayarlar yalnızca **Kaydet** ile `drive.json`'a yazılır (`%APPDATA%\mosair\drive.json`, yalnızca o bilgisayarda).
- **Güvenlik:** Script URL'si klasöre yazma ve okuma yetkisi verir (ayrıntı: [DriveService](../Services/DriveService.md#güvenlik)). Kaynak koddaki örnekler kısaltılmış yer tutuculardır; gerçek klasör bağlantısı veya script adresi asla repoya, belgelere ya da derlemelere konmaz.

## Dikkat / bilinen sınırlamalar

- Pencere başlık çubuğundan kapatılırsa `ShowDialog<Config?>` `null` döner (iptal ile aynı).
- Boş değerler de kaydedilebilir; bu durumda bir sonraki Drive işlemi yine "ayarlanmamış" uyarısını verip pencereyi açar.
- Deneme sürerken pencere kapatılabilir; sonuç yazılacak pencere artık görünmez (zararsız).

## İlgili dosyalar

- [DriveService](../Services/DriveService.md)
- [DriveOpenDialog](DriveOpenDialog.md)
- [StockSettingsDialog](StockSettingsDialog.md)
- [MainWindow](../MainWindow.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
- [Kullanıcı arayüzü (ARAYUZ)](../../ARAYUZ.md#google-drive-proje-klasörü)
