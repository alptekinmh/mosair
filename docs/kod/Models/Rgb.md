# Rgb

> Kaynak: `mosair/Models/Rgb.cs` · Güncelleme: 2026-10-04

## Amaç

Uygulamanın temel renk/taş veri tipi. Dosya adı `Rgb.cs` olsa da içindeki sınıflar küçük harfle adlandırılmıştır: `rgb` (bir taş rengi ya da palet rengi) ve `cooo` (renk mesafesi hesabının ara sonucu). İsimler eski WPF sürümünden birebir taşınmıştır; sınıflar düz alanlardan (field) oluşur, davranış içermez.

## Nerede kullanılır

| Yer | Kullanım |
|---|---|
| `ColorCatalogService` | `Assets/colorsBas.txt` satırlarından `rgb` nesneleri üretir, `MosaicData.arRGBAll` listesine ekler. |
| `MosaicEngine` | Palet üretimi, renk indirgeme, bölge (`reg`) / sıra (`u`, `uc`) numaralandırma, piksel sayımı (`numOfPixel`). |
| `ColorMatcher` | `CalculateDistance` ve `FindCatalogDistances` `cooo` listesi döndürür; `SelectNearest` en küçük `av` değerini seçer. |
| `ProjectService` | `rgb` alanlarını `.mos` dosyasına yazar/okur. |
| `MainViewModel` | Katalog listesi, stok işaretleri (`stokYetersiz`), hariç tutma (`boolLeaveOut`). |

## Yapı

### `rgb`

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `r`, `g`, `b` | `double` | 0 | Rengin RGB bileşenleri (0–255). |
| `ri`, `gi`, `bi` | `double` | 0 | İlk (indirgeme öncesi) RGB değerleri; `MosaicEngine` palet oluştururken `r/g/b` kopyasını buraya yazar. |
| `dis` | `double` | 0 | Parlaklık ortalaması `(r+g+b)/3`. Katalog yüklenirken ve palet oluşturulurken hesaplanır. |
| `L`, `A`, `B` | `double` | 0 | CIELAB karşılığı (`ColorMatcher.RgbToLab` ile doldurulur). |
| `unitPrice`, `price`, `area` | `double` | 0 | Fiyat/alan alanları. Yalnızca kopyalanır/serileştirilir, hesapta kullanılmaz. |
| `boolUseOnce` | `bool` | `false` | `ColorMatcher` eşleştirmesinde bir kez kullanılmış rengi atlamak için. |
| `boolUCDone` | `bool` | `false` | `MosaicEngine` içinde `uc` değerinin atandığını işaretler. |
| `boolLeaveOut` | `bool` | `false` | Renk katalogdan hariç tutulmuş mu (kullanıcı onay kutusunu kaldırdıysa `true`). |
| `stokYetersiz` | `bool` | `false` | Stok kontrolünde "Tahmini Kalan" eksiye düşen taş. `MainViewModel` ayarlar, yeni mozaikte sıfırlanır. |
| `n` | `int` | 0 | Katalog/palet içindeki indeks (piksel sayımı sırasında yazılır). |
| `numOfPixel` | `int` | 0 | Bu rengin mozaikte kaç taş/piksel kullandığı. |
| `reg` | `int` | 0 | Rengin ait olduğu bölge (`drl`) numarası. |
| `u` | `int` | 0 | Bölge içindeki sıra numarası. |
| `uc` | `int` | 1 | Birleştirme sonrası ortak sıra numarası (`u` ile eşlenir). |
| `ID` | `int` | 0 | Katalog kimliği (`colorsBas.txt` satır sırası, 1'den başlar). |
| `codeName` | `string` | `""` | Taş kodu, örn. `B101`. Doku klasörü eşleşmesinde kullanılır. |
| `name` | `string` | `""` | Görünen ad (dosyadaki 5., 6. ve varsa 7. sütunlar). |

### `cooo`

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `n` | `int` | 0 | Karşılaştırılan katalog renginin indeksi. |
| `av` | `double` | 0 | Hesaplanan renk mesafesi. |

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `rgb()` | Boş nesne. | `MosaicEngine` (klonlama), `ProjectService` |
| `rgb(r, g, b)` | Yalnızca renk. | `MosaicEngine.GenerateInitialPalette` |
| `rgb(r, g, b, codeName, name, ID)` | Katalog kaydı. | — |
| `rgb(r, g, b, dis, ID, codeName, name)` | Katalog kaydı + parlaklık. | `ColorCatalogService.LoadCatalog` |
| `rgb(r, g, b, codeName, name, ID, unitPrice)` | Katalog kaydı + birim fiyat. | — |
| `cooo(av, n)` | Mesafe sonucu. | `ColorMatcher.CalculateDistance` |

## Önemli davranışlar ve iş kuralları

- `rgb` bir referans tipidir. Listeler arası kopyalarda (`arMA` → `arMB` vb.) `MosaicEngine` içindeki `CloneRgb`/`CloneNestedList` kullanılır; doğrudan atama aynı nesneyi paylaşır.
- `MosaicData.arRGBAll` ve `MosaicData.arRGB` aynı `rgb` örneklerini paylaşır: `boolLeaveOut` değişince `ColorCatalogService.SetActiveColors` `arRGB` listesini yeniden kurar.
- `uc` varsayılanı 1'dir, diğer sayaçlar 0'dır.

## Dikkat / bilinen sınırlamalar

- Sınıf adları (`rgb`, `cooo`) C# isimlendirme kurallarına uymaz; yeniden adlandırma `.mos` serileştirmesini ve çok sayıda servisi etkiler.
- `unitPrice`, `price`, `area` şu an yalnızca taşınır; anlamlı bir değer üreten kod yoktur.
- `L/A/B` alanlarındaki `B` (Lab b*) ile `b` (mavi) yalnızca büyük/küçük harfle ayrılır.

## İlgili dosyalar

- [MosaicData](MosaicData.md)
- [Region](Region.md)
- [ColorCatalogService](../Services/ColorCatalogService.md)
- [ColorMatcher](../Services/ColorMatcher.md)
- [MosaicEngine](../Services/MosaicEngine.md)
- [ProjectService](../Services/ProjectService.md)
