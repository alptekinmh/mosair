# InvariantDoubleConverter

> Kaynak: `mosair/Converters/InvariantDoubleConverter.cs` · Güncelleme: 2026-10-04

## Amaç

`double` ile metin arasında **kültürden bağımsız** dönüşüm yapan Avalonia `IValueConverter`. Türkçe sistemlerde ondalık ayırıcı virgül olduğu için standart bağlama `120,5` / `120.5` karışıklığı yaratır; bu dönüştürücü ekranda her zaman nokta gösterir ve girişte hem virgülü hem noktayı kabul eder.

## Nerede kullanılır

`MainWindow.axaml` içinde kaynak olarak tanımlanır ve sol paneldeki mozaik genişliği (cm) kutusunda kullanılır:

```xml
<conv:InvariantDoubleConverter x:Key="InvDouble"/>
...
<TextBox Text="{Binding WidthCm, Converter={StaticResource InvDouble}}" .../>
```

## Yapı

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `Instance` | `static readonly InvariantDoubleConverter` | yeni örnek | Kodda/`x:Static` ile kullanım için tekil örnek. XAML şu an bunu değil `InvDouble` kaynağını kullanıyor. |

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `Convert(value, targetType, parameter, culture)` | `double` ise `ToString("F1", CultureInfo.InvariantCulture)` (tek ondalık, nokta ayırıcı); değilse `value?.ToString()`. | Avalonia bağlama (VM → TextBox) |
| `ConvertBack(value, targetType, parameter, culture)` | Metindeki `,` karakterlerini `.` yapar, `NumberStyles.Float` + `InvariantCulture` ile ayrıştırır; başarısızsa `0.0` döner. | Avalonia bağlama (TextBox → VM) |

## Önemli davranışlar ve iş kuralları

- Gösterim her zaman tek ondalıklıdır (`120.0`); daha hassas değerler ekranda yuvarlanır ama ViewModel'deki değer değişmez (kullanıcı kutuyu düzenlemedikçe).
- `culture` parametresi bilinçli olarak yok sayılır.
- `MainWindow` ayrıca `OnWidthTextInput` ile yazılan `,` karakterini anında `.` yapar ve `OnWidthChanged`/`OnWidthKeyDown` içinde metni temizleyip `MainViewModel.UpdateDimensions` çağırır; dönüştürücü bu zincirin bağlama ayağıdır.

## Dikkat / bilinen sınırlamalar

- Geçersiz giriş (`abc`, boş metin) sessizce `0.0` olur; hata gösterilmez. Genişliğin 0 olmasını ViewModel tarafı ele almalıdır.
- Binlik ayırıcı desteklenmez: `1,200.5` → `1.200.5` → ayrıştırılamaz → `0.0`.

## İlgili dosyalar

- [MainWindow](../MainWindow.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
