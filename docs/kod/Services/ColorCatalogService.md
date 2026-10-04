# ColorCatalogService

> Kaynak: `mosair/Services/ColorCatalogService.cs` · Güncelleme: 2026-10-04

## Amaç
Taş renk kataloğunu metin dosyasından yükler. Tüm katalog `MosaicData.arRGBAll`'da, mozaikte kullanılabilecek aktif taşlar `MosaicData.arRGB`'de tutulur. Taşları hariç tutma işlemini yönetir ve arayüz için katalog listesini `CatalogColorInfo` nesneleri olarak sunar.

## Nerede kullanılır
| Dosya | Kullanım |
|---|---|
| [MainViewModel](../ViewModels/MainViewModel.md) | `LoadDefaultCatalog` (başlangıçta), `SetActiveColors` (mozaik öncesi ve seçim değişince), `SetLeaveOut` (kullanıcı taş hariç tutunca), `GetCatalogList` (katalog listesi) |
| [CompareRunner](../CompareRunner.md) | `LoadDefaultCatalog` |

`LoadCatalog` dışarıdan doğrudan çağrılmaz, yalnızca `LoadDefaultCatalog` üzerinden kullanılır.

## Yapı

### `CatalogColorInfo` (sınıf)
| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `Index` | int | – | `arRGBAll` içindeki sıra |
| `R`, `G`, `B` | byte | – | Taş rengi |
| `CodeName` | string | "" | Taş kodu (ör. `B101`) |
| `Name` | string | "" | Taş adı |
| `ID` | int | – | Katalog ID'si (dosyadaki dolu satır numarası, 1'den başlar) |
| `IsExcluded` | bool | – | Taş hariç tutulmuş mu (`boolLeaveOut`) |

### Kullanılan `MosaicData` alanları
| Ad | Açıklama |
|---|---|
| `arRGBAll` | Tüm katalog |
| `arRGB` | Aktif (hariç tutulmamış) taşlar. M1/M3 ve optimum bu listeyi kullanır. |
| `arcs` | Taş başına hariç tutma bayrakları. Katalog yeniden yüklendiğinde seçimi korur. |

## Public API
| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `LoadCatalog(path)` | Dosyayı okur, `arRGBAll`'u doldurur, ardından `InitArcs` ve `SetActiveColors` çağırır | `LoadDefaultCatalog` |
| `LoadDefaultCatalog()` | `AppContext.BaseDirectory/Assets/colorsBas.txt` dosyası varsa yükler. Dosya yoksa sessizce hiçbir şey yapmaz. | MainViewModel, CompareRunner |
| `SetLeaveOut(index, leaveOut)` | Taşın `boolLeaveOut` ve `arcs` değerini günceller, sonra aktif listeyi yeniler | MainViewModel |
| `SetActiveColors()` | `arRGB` listesini `boolLeaveOut` false olan taşlardan yeniden kurar | MainViewModel, dahili |
| `GetCatalogList()` | Tüm katalog için `CatalogColorInfo` listesi döndürür | MainViewModel |

## Algoritma / akış

### Dosya biçimi ve yükleme (`LoadCatalog`)
Her satır boşlukla ayrılır: R G B KOD AD1 AD2 [AD3]. Örnek: `196 179 160 B101 stone1 Honlu`.

1. `arRGBAll` temizlenir. Boş satırlar atlanır, dolu her satırda `id` bir artırılır.
2. `u[0..2]` byte'a çevrilir. `codeName = u[3]` ve `name = u[4] + " " + u[5]` olur. 7. token varsa o da isme eklenir.
3. Taş `new rgb(r, g, b, (r+g+b)/3.0, id, codeName, name)` ile oluşturulur. `dis` alanına parlaklık ortalaması yazılır.
4. `InitArcs` çalışır. `arcs` boşsa veya uzunluğu katalogla uyuşmuyorsa tamamen false değerlerle yeniden kurulur. Uzunluk uyuşuyorsa `arcs` içindeki bayraklar taşların `boolLeaveOut` alanına geri yazılır, böylece önceki seçim korunur.
5. `SetActiveColors` çalışır.

## Önemli davranışlar ve iş kuralları
- Taş `ID` değeri dosyadaki sıradan türetilir. Dosyaya satır eklemek veya satırların sırasını değiştirmek ID'leri kaydırır. Bu, ID'leri kaydedilmiş projeleri ve stok eşleşmesini etkiler.
- `SetLeaveOut` geçersiz bir indeks alırsa bayrakları değiştirmez, ancak `SetActiveColors` yine de çağrılır.
- Katalog dosyası `Assets/colorsBas.txt` olarak uygulama klasöründe dağıtılır (124 satır).

## Dikkat / bilinen sınırlamalar
- Ayrıştırma `Split(' ')` ile yapılır. 6'dan az token içeren bir satır veya çift boşluk IndexOutOfRangeException ya da FormatException hatasına yol açar. `LoadCatalog` bu hatayı yakalamaz. O ana kadar okunan satırlar `arRGBAll`'da kalır, `InitArcs` ve `SetActiveColors` çalışmaz.
- 7. tokendan sonraki kelimeler isimden sessizce düşer.
- `LoadDefaultCatalog`, dosya yoksa kullanıcıya bir şey bildirmez. Bu durumda katalog boş kalır.

## İlgili dosyalar
- [ColorMatcher](./ColorMatcher.md), [MosaicEngine](./MosaicEngine.md), [StockSheetService](./StockSheetService.md), [ProjectService](./ProjectService.md)
- [Rgb](../Models/Rgb.md), [MosaicData](../Models/MosaicData.md)
- [MainViewModel](../ViewModels/MainViewModel.md), [Arayüz kılavuzu](../../ARAYUZ.md)
