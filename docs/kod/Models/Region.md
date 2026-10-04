# Region

> Kaynak: `mosair/Models/Region.cs` · Güncelleme: 2026-10-04

## Amaç

Mozaiğin bölge (region) modelini tutar. Dosyada iki sınıf vardır:

- `dr`: tek bir taş/piksel konumu (`x`, `y`).
- `drl`: dikdörtgen bir bölge ve bölgeler arası paylaşılan statik veriler (`arar`, `dat`).

Mevcut uygulama her zaman **tek bölge** ile çalışır (`MosaicEngine.CreateSingleRegion` tüm görüntüyü kapsayan bir `drl` oluşturur); çoklu bölge yapısı WPF sürümünden kalmıştır.

## Nerede kullanılır

| Yer | Kullanım |
|---|---|
| `MosaicEngine` | `drl.arar` listesini kurar, `drl.dat` dizisini doldurur, bölge başına hedef renk sayısını (`rgbM`) okur. |
| `PixelEditService` | Piksel düzenlerken `drl.dat[y, x, 3]` hücresine yeni taş `ID` değerini yazar, geri alırken eskisini geri yazar. |
| `ProjectService` | `drl.arar` ve `drl.dat` değerlerini `.mos` dosyasına kaydeder/yükler. |
| `MainViewModel` | `drl.dat` üzerinden hücre bilgisi okur. |

## Yapı

### `dr`

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `x`, `y` | `int` (alan) | 0 | Taş sütunu ve satırı. |
| `L` | `double` (özellik) | 0 | Parlaklık değeri için ayrılmış özellik. |

### `drl`

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `arar` | `static List<drl>` | boş | Tüm bölgeler. Pratikte tek eleman. |
| `dat` | `static int[,,]` | `new int[1, 1, 4]` | Hücre başına 4 kanal: `[satır, sütun, k]`. Aşağıdaki tabloya bakın. |
| `ar` | `List<dr>` | boş | Bölgeye ait hücre listesi. |
| `x1`, `y1`, `x2`, `y2` | `int` | 0 | Bölge dikdörtgeni (taş birimi). Tek bölgede `0,0` → `C,R`. |
| `rgbM` | `int` | 3 | Bu bölge için hedef renk sayısı. |

`drl.dat` kanalları:

| k | Anlam |
|---|---|
| 0, 1 | Hücrenin bölgeye dahil olduğunu gösteren bayraklar (tek bölgede hep 1). |
| 2 | Bölge kimliği (tek bölgede 1). |
| 3 | Hücreye atanmış taşın katalog `ID` değeri (ya da ara aşamada `u`/`uc` sırası). |

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `dr()` / `dr(x, y)` | Konum oluşturur. | `MosaicEngine.CreateSingleRegion` |
| `drl()` | Boş bölge. | `MosaicEngine.CreateSingleRegion` |
| `drl(x1, y1, x2, y2)` | Dikdörtgenle bölge. | `ProjectService` (proje yüklerken) |

## Önemli davranışlar ve iş kuralları

- `arar` ve `dat` statiktir; aynı anda tek bir mozaik durumu tutulabilir. `MosaicEngine.Reset` ikisini de sıfırlar (`dat` tekrar `new int[1, 1, 4]` olur).
- `dat` boyutu `[R, C, 4]`; `R` satır (yükseklik), `C` sütun (genişlik) taş sayısıdır.
- WPF'te üretilmiş bir `.mos` yüklenirken (kayıttaki `Source` değeri `"mosair"` değilse) `ProjectService` diziyi yatay çevirir; `drl.dat` ve bölge koordinatları (`x1`, `x2`) da aynı şekilde aynalanır.

## Dikkat / bilinen sınırlamalar

- `dr.L` hiçbir yerde doldurulmuyor; yalnızca WPF'ten kalan bir alan.
- Sınıf adları (`dr`, `drl`) ve alan adları kısa ve anlamsızdır; yeniden adlandırma `.mos` uyumluluğunu etkilemez ama çok sayıda servise dokunur.
- Statik durum nedeniyle iki mozaiği paralel hesaplamak (örn. testlerde) mümkün değildir; `CompareRunner` bu yüzden her varyant öncesi `MosaicEngine.Reset` çağırır.

## İlgili dosyalar

- [Rgb](Rgb.md)
- [MosaicData](MosaicData.md)
- [MosaicEngine](../Services/MosaicEngine.md)
- [PixelEditService](../Services/PixelEditService.md)
- [ProjectService](../Services/ProjectService.md)
