# ImageAdjustService

> Kaynak: `mosair/Services/ImageAdjustService.cs` · Güncelleme: 2026-10-08

## Amaç

Yüklenen görsele **Görsel Ayarları** sütunundaki ayarları uygular: Photoshop'un *Light* paneli gibi ışık ayarları ve *Hue/Saturation* paneli gibi ton/doygunluk ayarları. Mos ayarlanmış kopyadan yapılır; diskteki dosya değişmez.

Dosyada iki tip vardır:
- `ImageAdjustSettings`: ayar değerleri (JSON ile proje dosyasına yazılır);
- `ImageAdjustService`: onları bir bitmap'e uygulayan statik sınıf.

İlk sürümdeki 4 sayılık `ImageAdjustments` kaydının yerini `ImageAdjustSettings` aldı; eski projeler `FromLegacy` ile okunur.

## Nerede kullanılır

| Çağıran | Kullanım |
|---|---|
| [MainViewModel](../ViewModels/MainViewModel.md) | `_adjust` alanı (`ImageAdjustSettings`); `ApplyAdjustmentsAsync` değerlerin kopyasıyla (`Clone`) arka planda `ImageAdjustService.Apply(sourceBitmap, a)` çağırır; `AdjustSettingsForSave` projeye yazılacak kopyayı verir; `OpenProjectAsync` → `data.Adjust ?? ImageAdjustSettings.FromLegacy(data.ImageAdjust)` |
| [AdjustParam](../ViewModels/AdjustParam.md) | Kaydırıcı satırları; değer değişince `MainViewModel.OnAdjustParamChanged` ilgili alanı yazar |
| [ProjectService](ProjectService.md) | `ProjectData.Adjust` (`ImageAdjustSettings?`, yeni) ve eski `ProjectData.ImageAdjust` (`int[]?`, yalnızca okunur) |

## Yapı

### `ImageAdjustSettings` (sınıf)

Her değer 0 = değişiklik yok.

| Ad | Tip | Aralık | Açıklama |
|---|---|---|---|
| `RangeCount` | `const int` | 7 | Renk aralığı sayısı: 0 = **Ana** (tüm renkler), 1 kırmızılar (0°), 2 sarılar (60°), 3 yeşiller (120°), 4 camgöbekleri (180°), 5 maviler (240°), 6 eflatunlar (300°) |
| `Exposure` | `int` | −200…200 | Pozlama, yüzde bir EV olarak (−2,00…+2,00 EV) |
| `Brightness`, `Contrast`, `Highlights`, `Shadows`, `Whites`, `Blacks`, `Gamma` | `int` | −100…100 | Işık ayarları |
| `Hue` | `int[7]` | −180…180 | Aralık başına ton kaydırma (derece) |
| `Saturation` | `int[7]` | −100…100 | Aralık başına doygunluk |
| `Lightness` | `int[7]` | −100…100 | Aralık başına açıklık |
| `Colorize` | `bool` | | Renklendir: görselin tamamı tek tonda |
| `ColorizeHue` | `int` | 0…360 | Renklendir tonu |
| `ColorizeSaturation` | `int` | 0…100 | Renklendir doygunluğu (varsayılan 25) |
| `ColorizeLightness` | `int` | −100…100 | Renklendir açıklığı |
| `IsToneNeutral` | `bool` (JSON'a yazılmaz) | | Sekiz ışık değeri de 0 |
| `IsColorNeutral` | `bool` (JSON'a yazılmaz) | | Renklendir kapalı ve 7 aralığın üç değeri de 0 |
| `IsNeutral` | `bool` (JSON'a yazılmaz) | | İkisi birden |

| Metot | Ne yapar |
|---|---|
| `Clone()` | Bağımsız kopya; diziler her zaman 7 uzunlukta (dosyadaki kısa ya da eksik diziler sıfırla tamamlanır) |
| `FromLegacy(int[]? a)` | İlk sürümün `[parlaklık, kontrast, doygunluk, gama]` dizisinden: parlaklık, kontrast, gama aynı adlı alanlara, doygunluk **Ana** aralığın doygunluğuna yazılır (−100…100'e sıkıştırılır). `null` ya da 4'ten kısa dizide nötr ayar. |

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `Apply(SKBitmap source, ImageAdjustSettings a)` → `SKBitmap` | Kaynakla aynı `SKImageInfo`'da yeni bitmap açar; ışık ayarı varsa kaynağı ton eğrisi süzgeciyle çizer, yoksa olduğu gibi kopyalar; ton/doygunluk ayarı varsa piksel başına HSL geçişini yapar. Kaynak değişmez; dönen bitmap'in sahibi çağırandır. | `MainViewModel.ApplyAdjustmentsAsync` (işçi iş parçacığında) |
| `ToneCurve(ImageAdjustSettings a)` → `byte[256]` | R, G ve B için ortak ton eğrisi | `Apply` |
| `Adjust(ref r, ref g, ref b, a)` | Bir pikselin (0…1) ton/doygunluk/açıklık ayarı | HSL geçişi |

## Önemli davranışlar ve iş kuralları

### 1. Işık: ton eğrisi

Tek bir Skia renk tablosu (`SKColorFilter.CreateTable`; alfa değişmez). Her `v` için `x = v / 255`, sırasıyla:
- Pozlama ve parlaklık: `x = x · 2^(Pozlama/100) + Parlaklık/100 · 0,4`
- Gama: `x = kırp(x) ^ (2^(−Gama/100))` (+100 → üs 0,5, orta tonlar açılır)
- Kontrast: `x = (x − 0,5) · k + 0,5`; `k = 1 + Kontrast/50` (+100'de 3 kat) ya da negatifte `1 + Kontrast/100` (−100'de düz gri)
- Bölgesel ayarlar, yumuşak çan ağırlıklarıyla `Bell(x, merkez, genişlik) = e^(−((x−merkez)/genişlik)²)`:
  - Parlak Alanlar: `+ Highlights/100 · 0,30 · Bell(x, 0,75, 0,20)`
  - Gölgeler: `+ Shadows/100 · 0,30 · Bell(x, 0,25, 0,20)`
  - Beyazlar: `+ Whites/100 · 0,25 · Bell(x, 0,95, 0,12)`
  - Siyahlar: `+ Blacks/100 · 0,25 · Bell(x, 0,05, 0,12)`
- 0…1'e kırpılır, 0…255'e yuvarlanır.

Işık ayarlarının hepsi 0 ise süzgeç kullanılmaz, kaynak doğrudan kopyalanır.

### 2. Ton/Doygunluk: piksel başına HSL

Yalnızca `IsColorNeutral` false iken çalışır. Satırlar paralel işlenir; her satır bir tampona kopyalanıp geri yazılır. `Bgra8888` ve `Rgba8888` biçimleri desteklenir; başka biçimde (yükleyici üretmez) bu adım atlanır. Piksel HSL'ye çevrilir:
- **Renklendir açıksa:** ton = `ColorizeHue`, doygunluk = `ColorizeSaturation/100`, açıklık = pikselin parlaklığı (`0,299 R + 0,587 G + 0,114 B`) üstüne `ColorizeLightness` kaydırması.
- **Değilse:** kaydırmalar = Ana aralığın değerleri + her renk aralığının değerleri × ağırlık.
  - Ağırlık: pikselin tonu aralık merkezinin ±15° içindeyse 1, ±45°'den uzaksa 0, arada doğrusal azalır; ayrıca `min(1, doygunluk · 4)` ile çarpılır, böylece griler ve çok soluk renkler aralıklardan etkilenmez.
  - Ton: `(h + Δh) mod 360`.
  - Doygunluk (`d = Δ/100`): `d < 0` → `s · (1 + d)` (−100 gri); `d > 0` → `s + (1 − s) · d · min(1, 2s)` (gri pikseller renklenmez).
  - Açıklık: `d < 0` → `l · (1 + d)` (−100 siyah); `d > 0` → `l + (1 − l) · d` (+100 beyaz).
- Toplam kaydırma 0 olan piksel değişmez.

### Doğrulama

Test aracıyla 17 kontrol yapıldı, hepsi geçti:
- sekiz ışık ayarının her biri ortalama parlaklığı doğru yönde değiştiriyor;
- Kırmızılar tonu +120° kırmızıyı yeşile çeviriyor, mavi ve gri pikseller değişmiyor;
- Ana doygunluk −100 gri veriyor; Maviler açıklık +100 maviyi beyazlatıyor;
- Renklendir 240° görseli maviye boyuyor;
- varsayılan ayar nötr; JSON'a yazıp okuma ve eski 4 sayılık biçim doğru.

6000×6000 px görselde: yalnızca ışık ayarları ≈0,26 sn, ton/doygunluk ile birlikte ≈0,33 sn.

## Dikkat / bilinen sınırlamalar

- Her ayar değişiminde tam çözünürlüklü yeni bir kopya oluşur (görsel kadar bellek). `MainViewModel` önceki kopyayı, bir iş sürmüyorsa hemen bırakır.
- Pozlama görselin kodlanmış (gama uygulanmış) değerleri üzerinde çarpılır; fotoğraf programlarındaki doğrusal ışık hesabının yaklaşık karşılığıdır.
- Bölgesel ışık ayarları ton eğrisi olduğu için üç kanala ayrı ayrı uygulanır; çok büyük değerlerde renk tonu hafifçe kayabilir.
- Görsel bilgileri kartları (önizleme, ayrıntılar) ayarlanmamış dosyayı gösterir.

## İlgili dosyalar

- [MainViewModel](../ViewModels/MainViewModel.md)
- [AdjustParam](../ViewModels/AdjustParam.md)
- [AdjustSlider](../Controls/AdjustSlider.md)
- [ProjectService](ProjectService.md)
- [MosaicData](../Models/MosaicData.md) (`sourceBitmap`, `inputBitmap`)
- [Kullanıcı arayüzü (ARAYUZ)](../../ARAYUZ.md#görsel-ayarları)
