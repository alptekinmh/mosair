# AdjustSlider

> Kaynak: `mosair/Controls/AdjustSlider.cs` · Güncelleme: 2026-10-08

## Amaç

Görsel Ayarları sütunundaki ince kaydırıcı. Photoshop'un ayar kaydırıcıları gibi: ince bir iz (düz ya da renk geçişli) ve altında küçük bir üçgen tutamaç. Fluent'in `Slider`'ı yerine kendi çizen bir `Control`'dür; böylece tutamaç küçük, iz renk geçişli olabilir. Değerler tam sayıya yuvarlanır.

## Nerede kullanılır

| Dosya | Kullanım |
|---|---|
| [MainWindow](../MainWindow.md) | Görsel Ayarları sütununda her satırın veri şablonunda (`vm:AdjustParam`): `Minimum/Maximum/DefaultValue` → `Min/Max/Default`, `Value` → `SliderValue` (iki yönlü), `TrackBrush` → `Track`; `EmptyTrackBrush`, `ThumbBrush`, `ThumbBorderBrush` tema renklerinden (`BrdrTer`, `FgPrimary`, `BgInput`) |
| [MainWindow](../MainWindow.md) | Araç çubuğundaki Optimum taş kaydırıcısı: `ShowValue=True`, `Value` → `OptimalK`, `Maximum` → `OptimalKMax`, `DefaultValue` → `OptimalKSuggested` (sağ tık önerilen sayıya döner), `ValueBrush` = `FgPrimary`. `DraggingChanged` burada da çağrılır; sürüklerken ayar değişmediği için Anlık Mos'a etkisi yoktur. |

## Yapı

| Özellik | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `Minimum`, `Maximum` | `double` | −100, 100 | Aralık |
| `Value` | `double` | 0 | Değer; varsayılan bağlama iki yönlü |
| `DefaultValue` | `double` | 0 | Sağ tık ve `Delete`'in döndüğü değer |
| `TrackBrush` | `IBrush?` | — | İzin rengi (ör. renk çemberi geçişi) |
| `EmptyTrackBrush` | `IBrush?` | — | `TrackBrush` yokken kullanılan düz iz rengi; ikisi de yoksa gri |
| `ThumbBrush` | `IBrush?` | — | Üçgenin dolgusu (yoksa beyaz) |
| `ThumbBorderBrush` | `IBrush?` | — | Üçgenin çerçevesi; odaktayken 1,5 px, değilse 1 px |
| `ShowValue` | `bool` | false | Değeri izin üstüne, üçgenin hizasına yazar (araç çubuğundaki Optimum taş sayısı). Açıkken her şey 14 px aşağı kayar. |
| `ValueBrush` | `IBrush?` | — | Değer yazısının rengi (yoksa `ThumbBrush`, o da yoksa beyaz) |

Ölçüler: iz kontrolün üstünde 4 px yüksekliğinde (y = 3), köşeleri 2 px yuvarlak; iki uçta 6 px boşluk bırakılır ki üçgen dışarı taşmasın. Üçgen izin altında (y = 8), 10 px geniş, 8 px yüksek. Kontrolün yüksekliği 17 px (`ShowValue` ile 31 px: üstte 11 px yarı kalın değer, üçgenin ortasına göre ortalanır ve kontrolün dışına taşmaz); genişliği verilen alan kadar (sınırsızsa 120 px). Odaklanabilir; imleç el işaretidir.

## Davranış

| Giriş | Etki |
|---|---|
| Sol tık / sürükleme | Değeri imlecin yerine getirir (fare yakalanır). Başlarken ve bırakılınca (ya da yakalama kaybolunca, `OnPointerCaptureLost`) statik `DraggingChanged(bool)` olayı çağrılır; `MainWindow` bunu `MainViewModel.SetAdjustDragging`'e iletir: Anlık Mos sürükleme bitene kadar bekler |
| `Shift` basılı başlayan sürükleme | İnce ayar: başlangıç değerinden, imlecin kaydığı yolun dörtte biri kadar |
| Sağ tık | `DefaultValue` (odağı da alır) |
| Tekerlek | ±1 (`Ctrl` ile ±10); kontrol pasifken etkisiz |
| `←` `↓` / `→` `↑` | −1 / +1 (`Shift` ile ±10) |
| `Home` / `End` | `Minimum` / `Maximum` |
| `Delete` / `Backspace` | `DefaultValue` |

Her değer `Minimum…Maximum`'a sıkıştırılıp tam sayıya yuvarlanır. `Value`, aralık, iz ve tutamaç renkleri değişince yeniden çizilir; odak değişince de (çerçeve kalınlığı).

## Dikkat / bilinen sınırlamalar

- Yalnızca yatay çalışır.
- Erişilebilirlik (ekran okuyucu) bilgisi sağlamaz; değer, satırdaki sayı kutusunda okunur.

## İlgili dosyalar

- [AdjustParam](../ViewModels/AdjustParam.md)
- [MainWindow](../MainWindow.md)
- [ImageAdjustService](../Services/ImageAdjustService.md)
- [Kullanıcı arayüzü (ARAYUZ)](../../ARAYUZ.md#görsel-ayarları)
