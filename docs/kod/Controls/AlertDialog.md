# AlertDialog

> Kaynak: `mosair/Controls/AlertDialog.axaml`, `mosair/Controls/AlertDialog.axaml.cs` · Güncelleme: 2026-10-07

## Amaç

Başlık + mesaj + tek "Tamam" düğmesinden oluşan modal bilgi/uyarı penceresi. Sonuç döndürmez.

## Nerede kullanılır

`MainWindow` yapıcısında `MainViewModel.ShowAlert` geri çağrısına bağlanır:

```csharp
_vm.ShowAlert = async (title, message) =>
{
    var dlg = new Controls.AlertDialog(title, message);
    await dlg.ShowDialog(this);
};
```

ViewModel tüm uyarılarını (görüntü yok, dışa aktarma hatası, stok sonuçları vb.) bu geri çağrı üzerinden gösterir; ViewModel pencere tipini bilmez.

## Yapı

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| (pencere) | `Window` | `Width=420`, `SizeToContent="Height"`, `CanResize="False"` | `WindowStartupLocation="CenterOwner"`, arka plan `BgCard` (tema rengi). |
| `TitleText` | `TextBlock` | — | 16 pt, SemiBold başlık. |
| `MessageText` | `SelectableTextBlock` | — | 13 pt, `TextWrapping="Wrap"` gövde metni; seçilip kopyalanabilir. En çok 460 px yüksekliğinde bir `ScrollViewer` içindedir: uzun raporlar (ör. büyük mozaikte Stoğa göre sonucu) pencereyi ekrandan taşırmaz, kaydırılır. |
| `OkButton` | `Button` | `Content="Tamam"` | Sağa yaslı ana düğme (`Classes="primary"`, vurgu mavisi); `Click="OnOkClick"`. |

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `AlertDialog()` | Yalnızca `InitializeComponent`. Avalonia tasarımcısı/XAML yükleyici için. | Avalonia |
| `AlertDialog(string title, string message)` | Pencere başlığını ve `TitleText`'i `title`, `MessageText`'i `message` yapar; düğme metnini `Loc.Get("WarnOk")` ile yerelleştirir. | `MainWindow` (`ShowAlert`) |
| `OnOkClick` (private) | `Close()`. | `OkButton` |

## Önemli davranışlar ve iş kuralları

- Düğme metni XAML'de `Tamam` olarak sabit, ancak parametreli yapıcı her zaman `Loc` ile üzerine yazar; dil İngilizce ise `WarnOk` karşılığı görünür.
- `ShowDialog(owner)` ile açıldığı için ana pencereyi kilitler. Ancak ViewModel'deki özel `Alert` yardımcı metodu `ShowAlert`'i `Dispatcher.UIThread.Post` ile "ateşle ve unut" biçiminde çağırır; yani ViewModel kodu diyaloğun kapanmasını beklemez.

## Dikkat / bilinen sınırlamalar

- Renkler `App.axaml`'daki tema anahtarlarından (`DynamicResource`) gelir; açık temada diyalog da açık görünür. Onay düğmesi de temaya bağlı paylaşılan `Button.primary` stilini kullanır (vurgu mavisi, üzerine gelince/basınca koyulaşır; bkz. [App](../App.md)).
- Escape/Enter tuşlarına özel bağlama yoktur.

## İlgili dosyalar

- [ConfirmDialog](ConfirmDialog.md)
- [StockSettingsDialog](StockSettingsDialog.md)
- [MainWindow](../MainWindow.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
- [Loc](../Services/Loc.md)
