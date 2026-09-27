# Rgb.cs

## Genel Bakis

Projedeki temel renk veri yapilarini icerir. `rgb` sinifi hem palet renklerini hem de katalog tas renklerini temsil eder ve mozaiklestirme surecinin her asamasinda kullanilir. `cooo` sinifi ise renk mesafesi hesaplamalarinda sonucu (mesafe + indeks) tasimak icin kullanilir.

## Siniflar ve Alanlar

### `rgb`

Mozaiklestirme algoritmasinin ana renk nesnesi. Bir renk, ilk uretildigi andan katalog tasina atanmasina kadar bu sinifla temsil edilir.

**Renk Degerleri:**
- `r, g, b` — Mevcut RGB degerleri (0-255). Katalog atamasi sonrasinda tas renginin degerlerini tasir.
- `ri, gi, bi` — Baslangic RGB degerleri. Katalog atamasi oncesinde orijinal ara rengi saklamak icin kullanilir.
- `dis` — Parlaklik ortalamasi: `(r + g + b) / 3.0`. Siralamada kullanilir.
- `L, A, B` — CIELAB renk uzayi degerleri. `boolLab` aktifken renk mesafesi hesaplamalarinda RGB yerine bu degerler kullanilir.

**Katalog ve Kimliklendirme:**
- `ID` — Katalog tas kimlik numarasi (colorsBas.txt'deki sira).
- `codeName` — Tas kodu (ornegin "BCR-001"). 02_RS klasorunde doku aranirken kullanilir.
- `name` — Tas adi (ornegin "Bianco Carrara").

**Sayaclar ve Indeksler:**
- `n` — Rengin palet icindeki indeksi.
- `numOfPixel` — Bu renge eslenen piksel sayisi. Iteratif renk silme isleminde az pikselli renkler cikarilirken kontrol edilir.
- `reg` — Bolge numarasi (musteri uygulamasinda her zaman 1).
- `u` — Bolge icindeki renk numarasi (1'den baslar). `dat[i,j,3]` ile eslenir.
- `uc` — Birlestirme sonrasi renk numarasi. Ayni katalog rengine atanan farkli ara renkler ayni `uc` degerini alir.

**Bayraklar:**
- `boolUseOnce` — Sadece pix2 modunda kullanilir. Musteri uygulamasinda (pix3) etkisizdir.
- `boolUCDone` — Birlestirme islemi yapilip yapilmadigini takip eder.
- `boolLeaveOut` — Kullanici tarafindan katalogdan cikartilmis renk. Aktif renk listesine dahil edilmez.
- `stokYetersiz` — Stok yetersizligi bayragi (musteri uygulamasinda kullanilmaz).

**Fiyat/Alan (musteri uygulamasinda kullanilmaz):**
- `unitPrice` — Birim fiyat.
- `price` — Toplam fiyat.
- `area` — Alan hesaplamasi.

**Constructor'lar:**
- `rgb()` — Bos constructor.
- `rgb(r, g, b)` — Palet uretiminde kullanilir (GenerateInitialPalette).
- `rgb(r, g, b, codeName, name, ID)` — Katalog rengi olustururken.
- `rgb(r, g, b, dis, ID, codeName, name)` — Katalog yuklerken (dis hesaplanmis).
- `rgb(r, g, b, codeName, name, ID, unitPrice)` — Fiyatli katalog rengi.

### `cooo`

Renk mesafesi hesaplama sonuclarini tasir.

- `av` — Hesaplanan mesafe degeri (Oklid veya ortalama).
- `n` — Bu mesafenin ait oldugu rengin palet/katalog indeksi.

`ColorMatcher.CalculateDistance()` bu sinifin orneklerini uretir, `SelectNearest()` en kucuk `av` degerli olani secer.

## Diger Dosyalarla Iliskisi

- **MosaicData.cs** — `arRGBAll`, `arRGB`, `arMA`, `arMB`, `arMBR` listelerinin eleman tipi `rgb`'dir.
- **MosaicEngine.cs** — Palet uretimi, iteratif renk silme, katalog atamasi ve birlestirme islemlerinin tamami `rgb` nesneleri uzerinden calisir.
- **ColorMatcher.cs** — `CalculateDistance()` iki `rgb` arasindaki mesafeyi hesaplar ve `cooo` olarak dondurur.
- **ColorCatalogService.cs** — colorsBas.txt'den okunan her satir bir `rgb` nesnesi olarak `arRGBAll`'a eklenir.
- **StoneTextureService.cs** — `codeName` alani uzerinden 02_RS klasorunde doku dosyalari aranir.

## Orijinal WPF Karsiligi

- **Kaynak:** `mosairWPF/ourRobotWpf/picture/lum.cs` — Orijinalde `rgb` sinifi `lum.cs` icinde tanimlanmisti. Alanlar bire bir aynidir.
- `cooo` sinifi da ayni dosyada bulunuyordu.
- Orijinalde bazi alanlar (price, area, stokYetersiz) uretim tarafli islemler icin kullaniliyordu; musteri uygulamasinda pasif durumdadir ancak veri yapisi uyumu icin korunmustur.
