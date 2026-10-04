# StockSettingsDialog

> Kaynak: `mosair/Controls/StockSettingsDialog.axaml`, `mosair/Controls/StockSettingsDialog.axaml.cs` · Güncelleme: 2026-10-04

## Amaç

Google Sheet stok entegrasyonunun iki ayarını kullanıcıdan alan modal pencere:

1. **Sheet ID**: stok tablosunun kimliği (okuma için).
2. **Script URL**: tabloya yazma yapan Apps Script web uygulamasının `/exec` adresi.

Sonuç olarak yeni bir `StockSheetService.Config` döndürür ya da iptalde `null`.

## Nerede kullanılır

`MainWindow` yapıcısında `MainViewModel.ShowStockSettings` geri çağrısına bağlanır:

```csharp
_vm.ShowStockSettings = current =>
    new Controls.StockSettingsDialog(current).ShowDialog<StockSheetService.Config?>(this);
```

Akış: Araçlar → Stok → Stok Ayarları menüsü veya toolbar'daki stok tablosu düğmesinin sağ tık `ContextMenu`'sü → `OnStockSettings` → `MainViewModel.ConfigureStockAsync` → bu diyalog → sonuç `null` değilse `StockSheetService.SaveConfig`.

## Yapı

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| (pencere) | `Window` | `Width=560`, `SizeToContent="Height"`, `CanResize="False"` | `CenterOwner`, arka plan `BgCard` (tema rengi). |
| `TitleText` | `TextBlock` | — | `StockSettingsTitle`. |
| `SheetIdLabel`, `SheetIdHint` | `TextBlock` | — | `StockSheetIdLabel`, `StockSheetIdHint`. |
| `SheetIdExample` | `TextBlock` (`Classes="example"`) | — | Tablo bağlantısı örneği; ID kısmı vurgulu. |
| `SheetIdBox` | `TextBox` | mevcut `SheetId` | ID veya tam bağlantı girilebilir. |
| `ScriptUrlLabel`, `ScriptUrlHint` | `TextBlock` | — | `StockScriptUrlLabel`, `StockScriptUrlHint`. |
| `ScriptUrlExample` | `TextBlock` (`Classes="example"`) | — | Apps Script `/exec` adresi örneği. |
| `ScriptUrlBox` | `TextBox` | mevcut `ScriptUrl` | — |
| `RequirementsTitle`, `RequirementsText` | `TextBlock` | — | Tablonun sağlaması gereken koşullar (`StockRequirementsTitle`, `StockRequirements`). |
| `NoteText` | `TextBlock` | — | Turuncu (`#e0a050`) uyarı notu (`StockSettingsNote`). |
| `CancelButton`, `SaveButton` | `Button` | — | `DlgCancel`, `DlgSave`; `OnCancelClick`, `OnSaveClick`. |
| `HighlightBrush` | `static IBrush` | `#6ea8ff` | Örneklerde girilecek kısmın rengi. |

Stiller: `TextBlock.hint` (11 pt, gri, sarmalı) ve `TextBlock.example` (11 pt, eş aralıklı yazı tipi).

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `StockSettingsDialog()` | Yalnızca `InitializeComponent`. | Avalonia |
| `StockSettingsDialog(StockSheetService.Config current)` | Tüm metinleri `Loc.Get` ile doldurur, örnekleri `SetExample` ile kurar, kutulara mevcut değerleri yazar. | `MainWindow` (`ShowStockSettings`) |
| `SetExample(target, before, part, after)` (private static) | `TextBlock.Inlines`'ı üç `Run`'dan kurar; ortadaki `part` vurgulu ve kalın. | Yapıcı |
| `ExtractSheetId(text)` (private static) | Metin bir tablo bağlantısıysa (regex ile) yalnızca ID bölümünü döndürür, değilse metni aynen döndürür. | `OnSaveClick` |
| `OnSaveClick` (private) | `Close(new StockSheetService.Config { SheetId = ExtractSheetId(...), ScriptUrl = ... })`; iki değer de `Trim` edilir. | `SaveButton` |
| `OnCancelClick` (private) | `Close(null)`. | `CancelButton` |

## Önemli davranışlar ve iş kuralları

- Kullanıcı tablo bağlantısının tamamını yapıştırabilir; ID otomatik ayıklanır (regex: `[A-Za-z0-9_-]+`).
- Script URL doğrulanmaz; yalnızca baştaki/sondaki boşluklar kırpılır.
- Değerler uygulama klasörüne değil, kullanıcı başına `ApplicationData/mosair/stock.json` dosyasına yazılır (`StockSheetService.SaveConfig`). macOS `.app` paketi salt okunur olduğu için bu tercih edilmiştir.
- **Güvenlik:** Script URL tabloya yazma yetkisi verir. Gerçek Sheet ID veya Script URL değerleri asla repoya, belgelere ya da derlemelere konmamalıdır; kaynak koddaki örnekler kısaltılmış yer tutuculardır.

## Dikkat / bilinen sınırlamalar

- Pencere başlık çubuğundan kapatılırsa `ShowDialog<Config?>` `null` döner (iptal ile aynı).
- Renkler `App.axaml`'daki tema anahtarlarından (`DynamicResource`) gelir; açık temada diyalog da açık görünür. Yalnızca mavi onay düğmesi sabit renklidir.
- Boş değerler de kaydedilebilir; bu durumda stok komutları `MainViewModel.TryGetStockConfig` içinde `StockNotConfigured` uyarısı verir (Sheet ID her komut için, Script URL yalnızca tabloya yazan komutlar için zorunludur).

## İlgili dosyalar

- [StockSheetService](../Services/StockSheetService.md)
- [AlertDialog](AlertDialog.md)
- [ConfirmDialog](ConfirmDialog.md)
- [MainWindow](../MainWindow.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
- [Kullanıcı arayüzü (ARAYUZ)](../../ARAYUZ.md)
