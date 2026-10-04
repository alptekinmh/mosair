# ConfirmDialog

> Kaynak: `mosair/Controls/ConfirmDialog.axaml`, `mosair/Controls/ConfirmDialog.axaml.cs` · Güncelleme: 2026-10-04

## Amaç

Evet/Hayır onayı isteyen modal pencere. Sonucu `bool` olarak döndürür.

## Nerede kullanılır

`MainWindow` yapıcısında `MainViewModel.ShowConfirm` geri çağrısına bağlanır:

```csharp
_vm.ShowConfirm = (title, message) =>
    new Controls.ConfirmDialog(title, message).ShowDialog<bool>(this);
```

ViewModel, Google Sheet tablosunu değiştiren geri dönüşsüz stok işlemlerinden (Stok Sil, Stok Ekle) önce bu onayı ister.

## Yapı

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| (pencere) | `Window` | `Width=440`, `SizeToContent="Height"`, `CanResize="False"` | `CenterOwner`, arka plan `BgCard` (tema rengi). |
| `TitleText` | `TextBlock` | — | 16 pt başlık. |
| `MessageText` | `TextBlock` | — | 13 pt, sarmalı gövde metni. |
| `NoButton` | `Button` | — | Gri (`#3a3a42`), `Click="OnNoClick"`. |
| `YesButton` | `Button` | — | Mavi (`#3a7bfd`), `Click="OnYesClick"`. |

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `ConfirmDialog()` | Yalnızca `InitializeComponent`. | Avalonia |
| `ConfirmDialog(string title, string message)` | Başlık/mesajı yazar; düğme metinlerini `Loc.Get("DlgYes")` ve `Loc.Get("DlgNo")` ile verir. | `MainWindow` (`ShowConfirm`) |
| `OnYesClick` (private) | `Close(true)`. | `YesButton` |
| `OnNoClick` (private) | `Close(false)`. | `NoButton` |

## Önemli davranışlar ve iş kuralları

- Pencere başlık çubuğundan (X) kapatılırsa `ShowDialog<bool>` varsayılan `false` döner; yani kapatmak "Hayır" ile aynıdır. Bu güvenli varsayılandır.
- Düğme sırası: solda Hayır, sağda Evet (vurgulu).

## Dikkat / bilinen sınırlamalar

- Renkler `App.axaml`'daki tema anahtarlarından (`DynamicResource`) gelir; açık temada diyalog da açık görünür. Yalnızca mavi onay düğmesi sabit renklidir.
- Klavye kısayolu (Enter = Evet, Esc = Hayır) tanımlı değildir.

## İlgili dosyalar

- [AlertDialog](AlertDialog.md)
- [StockSettingsDialog](StockSettingsDialog.md)
- [MainWindow](../MainWindow.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
- [StockSheetService](../Services/StockSheetService.md)
