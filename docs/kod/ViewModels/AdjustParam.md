# AdjustParam

> Kaynak: `mosair/ViewModels/AdjustParam.cs` · Güncelleme: 2026-10-08

## Amaç

Görsel Ayarları sütununun görünüm modelleri. Dosyada iki sınıf vardır:
- `AdjustParam`: bir kaydırıcı satırı (ad, değer kutusu, kaydırıcı);
- `AdjustRange`: Ton/Doygunluk sekmesindeki bir renk aralığı yuvarlağı.

## Nerede kullanılır

| Dosya | Kullanım |
|---|---|
| [MainViewModel](MainViewModel.md) | `LightParams` (8 satır), `ColorParams` (3 satır) ve `AdjustRanges` (7 yuvarlak) koleksiyonlarını `InitAdjustPanel`'de kurar; satırların `Changed` geri çağrısı `OnAdjustParamChanged`'dir |
| [MainWindow](../MainWindow.md) | Satır şablonu (`x:DataType="vm:AdjustParam"`): `Label`, `Text` (değer kutusu), `Min/Max/Default/SliderValue/Track` ([AdjustSlider](../Controls/AdjustSlider.md)); aralık şablonu (`vm:AdjustRange`): `Swatch`, `IsSelected`, `IsUsed`, `Label` (ipucu), `Index` (`OnAdjustRangeClick` için `Tag`) |

## `AdjustParam`

| Üye | Tip | Açıklama |
|---|---|---|
| `AdjustParam(labelKey, min, max, divisor = 1)` | | Yapıcı |
| `LabelKey` | `string` | `Loc` anahtarı |
| `Label` | `string` | `Loc.Get(LabelKey) + ":"`; dil değişince `RefreshLabel()` ile yenilenir |
| `Min`, `Max`, `Default` | `int` | Aralık ve sıfırlama değeri (`SetSilently` ile değişebilir: Renklendir'de Ton 0…360, Doygunluk 0…100, varsayılan 25) |
| `Divisor` | `int` | 1, ya da Pozlama için 100 (değer yüzde bir EV, kutu iki ondalık gösterir) |
| `Value` | `int` | `Min…Max`'a sıkıştırılır; değişince `Value`, `SliderValue`, `Text` bildirilir ve `Changed` çağrılır |
| `SliderValue` | `double` | Kaydırıcının bağlandığı değer; yazınca yuvarlanıp `Value`'ya geçer |
| `Text` | `string` | Değer kutusu. Okuma: negatif aralıklı satırlarda pozitif değer `+17` gibi; `Divisor` 100'de `+0,25` gibi iki ondalık (ondalık ayırıcı bölge ayarından). Yazma: `+` atılır, virgül noktaya çevrilir, değişmez kültürle sayı okunur, `Divisor` ile çarpılıp yuvarlanır; sayı değilse yok sayılır ve kutu güncel değeri yeniden gösterir |
| `Track` | `IBrush?` | İz rengi (null → kaydırıcının düz izi) |
| `Changed` | `Action<AdjustParam>?` | Değer değişince |
| `SetSilently(value, min?, max?, def?)` | | `Changed` çağırmadan değeri (ve istenirse aralığı) değiştirir: aralık değiştirme, proje açma, sıfırlama |
| `RefreshLabel()` | | `Label` değişti bildirimi |

## `AdjustRange`

| Üye | Tip | Açıklama |
|---|---|---|
| `Index` | `int` | 0 = Ana, 1–6 = kırmızılar … eflatunlar |
| `LabelKey`, `Label` | `string` | `AdjRange0…6`; ipucu olarak gösterilir |
| `Swatch` | `IBrush` | Yuvarlağın rengi (Ana için renk çemberi geçişi) |
| `IsSelected` | `bool` | Seçili aralık (mavi halka) |
| `IsUsed` | `bool` | Bu aralığın kendi ayarı var (köşede nokta) |
| `RefreshLabel()` | | Dil değişince |

## Dikkat / bilinen sınırlamalar

- Değer kutusu `UpdateSourceTrigger=LostFocus` ile bağlıdır. Yazılan değer `Enter`'a basınca (`MainWindow.OnAdjustTextKeyDown`), başka herhangi bir yere tıklanınca (`MainWindow.OnWindowPointerPressedCommit`) ya da kutudan `Tab` ile çıkınca uygulanır; ikisi de bağlamayı `UpdateSource()` ile hemen günceller ve odağı pencereye alır.

## İlgili dosyalar

- [MainViewModel](MainViewModel.md)
- [AdjustSlider](../Controls/AdjustSlider.md)
- [ImageAdjustService](../Services/ImageAdjustService.md)
- [MainWindow](../MainWindow.md)
