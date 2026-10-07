# ImageAdjustService

> Kaynak: `mosair/Services/ImageAdjustService.cs` · Güncelleme: 2026-10-07

## Amaç

Yüklenen görsele Görsel Ayarları panelindeki temel ayarları (parlaklık, kontrast, doygunluk, gama) uygular. Mos ayarlanmış kopyadan yapılır; diskteki dosya değişmez. Dosyada iki tip vardır: ayar değerlerini taşıyan `ImageAdjustments` ve onları uygulayan `ImageAdjustService`.

## Nerede kullanılır

| Çağıran | Kullanım |
|---|---|
| [MainViewModel](../ViewModels/MainViewModel.md) | `_adjust` alanı (`ImageAdjustments`); `ApplyAdjustmentsAsync` arka planda `ImageAdjustService.Apply(sourceBitmap, a)` çağırır; `ImageAdjustArray` → `ToArray()`; `OpenProjectAsync` → `ImageAdjustments.FromArray(data.ImageAdjust)` |
| [ProjectService](ProjectService.md) | `ProjectData.ImageAdjust` (`int[]?`) alanı bu değerleri saklar |

## Yapı

### `ImageAdjustments` (readonly record struct)

| Ad | Tip | Açıklama |
|---|---|---|
| `Brightness`, `Contrast`, `Saturation`, `Gamma` | `int` | Her biri −100…100; 0 = değişiklik yok. `default` dört sıfırdır. |
| `IsNeutral` | `bool` | Dördü de 0 ise true. |
| `ToArray()` | `int[]?` | Proje dosyası için `[parlaklık, kontrast, doygunluk, gama]`; nötrse `null` (alan dosyaya yazılmaz). |
| `FromArray(int[]? a)` | `ImageAdjustments` | Dosyadan okur; `null` ya da 4'ten kısa dizide nötr. Değerler −100…100'e sıkıştırılır. |

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `ImageAdjustService.Apply(SKBitmap source, ImageAdjustments a)` → `SKBitmap` | Kaynakla aynı `SKImageInfo`'da yeni bir bitmap açar, şeffaf temizler ve kaynağı tek bir Skia renk süzgeciyle (`SKColorFilter`) çizer. Kaynak değişmez; dönen bitmap'in sahibi çağırandır. | `MainViewModel.ApplyAdjustmentsAsync` (işçi iş parçacığında) |

## Önemli davranışlar ve iş kuralları

Süzgeç iki parçanın birleşimidir (`SKColorFilter.CreateCompose(doygunluk, ton)`: önce ton eğrisi, sonra doygunluk):

1. **Ton eğrisi** (`SKColorFilter.CreateTable`, R, G ve B için aynı 256 girdilik tablo; alfa değişmez). Her `v` için `x = v / 255`:
   - Gama: `x = x ^ (2 ^ (−Gama/100))` (+100 → üs 0,5, orta tonlar açılır; −100 → üs 2, koyulaşır; 0 ve 1 yerinde kalır).
   - Kontrast: orta gri etrafında `x = (x − 0,5) · k + 0,5`; `k = 1 + Kontrast/50` (Kontrast ≥ 0, +100'de 3 kat) ya da `1 + Kontrast/100` (negatifte, −100'de 0 → düz gri).
   - Parlaklık: `x += Parlaklık/100 · 0,4` (en çok aralığın ±%40'ı).
   - Sonuç 0…1'e kırpılıp 0…255'e yuvarlanır.
2. **Doygunluk** (`SKColorFilter.CreateColorMatrix`): her pikseli Rec. 709 parlaklığıyla (`0,2126 R + 0,7152 G + 0,0722 B`) `s = 1 + Doygunluk/100` oranında karıştırır; −100 → gri tonlu, +100 → iki kat canlı.

- Test aracıyla doğrulandı (286 px ve 6000×6000 px görsel): parlaklık ±50 ortalamayı yükseltip düşürür, kontrast −100 düz gri (yayılım 0), doygunluk −100 renksiz (doygunluk 0), gama ±50 orta tonları açar/koyulaştırır. 6000×6000 px bir görselde bir uygulama ≈0,34 sn sürer.
- Görüntü ön çarpımlı alfa ile işlenir; saydam olmayan görsellerde etkisi yoktur.

## Dikkat / bilinen sınırlamalar

- Her ayar değişiminde tam çözünürlüklü yeni bir kopya oluşur (görsel kadar bellek). `MainViewModel` önceki kopyayı, bir iş sürmüyorsa hemen bırakır.
- Görsel bilgileri kartları (baskın renkler, önizleme) ayarlanmamış dosyayı gösterir.

## İlgili dosyalar

- [MainViewModel](../ViewModels/MainViewModel.md)
- [ProjectService](ProjectService.md)
- [MosaicData](../Models/MosaicData.md) (`sourceBitmap`, `inputBitmap`)
- [Kullanıcı arayüzü (ARAYUZ)](../../ARAYUZ.md#görsel-ayarları)
