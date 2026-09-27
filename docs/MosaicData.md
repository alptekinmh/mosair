# MosaicData.cs

## Genel Bakis

Mozaiklestirme surecinin tum ara ve sonuc verilerini tutan statik veri deposudur. Renk kataloglari, piksel dizileri, bitmap nesneleri ve sabitler bu sinifta merkezi olarak saklanir. Tum alanlar statiktir; uygulama yasam dongusu boyunca tek bir veri seti uzerinde calisilir.

## Siniflar ve Alanlar

### `MosaicData` (static class)

**Renk Kataloglari:**
- `arRGBAll` — Tam renk katalogu. `colorsBas.txt`'den yuklenen 124 tas renginin tamami. Kullanici cikarma yapsa bile bu liste degismez.
- `arcs` — Her katalog renginin cikarilip cikarilmadigini tutan boolean listesi. `arRGBAll` ile ayni uzunlukta.
- `arRGB` — Aktif katalog renkleri. `arRGBAll`'dan `boolLeaveOut == false` olanlarin filtreli hali. M1 ilk eslestirmesinde ve katalog atama asamasinda kullanilir.
- `arLAB` — LAB renk uzayina donusturulmus katalog renkleri (su an aktif kullanilmiyor).

**Piksel Veri Dizileri (BGR siralamasinda):**
- `dataM1[R, C, 3]` — M1 (ilk eslestirme) sonucu. Her piksel `[i,j,0]=B, [i,j,1]=G, [i,j,2]=R` formatinda.
- `dataM3[R, C, 3]` — M3 (iteratif indirgeme) sonucu. Surecin ana calisma dizisi; her iterasyonda guncellenir. Katalog atamasi sonrasinda da bu dizi uzerinden piksel renkleri degistirilir.
- `dataM3F[R, C, 3]` — Kullanilmiyor (orijinalde vardi, temizlenmemis). Performans raporunda kaldirilmasi onerilmistir.
- `dataM3Backup[R, C, 3]` — `dataM3`'un yedegi. Islem sonunda `BackupM3()` ile olusturulur.

**Bolge Bazli Renk Listeleri:**
- `arMA` — `List<List<rgb>>`: Her bolge icin calisma paleti. Iteratif indirgeme sirasinda renkler bu listeden silinir. Islem sonunda katalog atanmis hali saklanir.
- `arMB` — `List<List<rgb>>`: Katalog atamasi sonrasi palet. `arMA`'nin derin kopyasi uzerine katalog renkleri atanir. Ayni katalog rengine dusen renkler birlestirilir.
- `arMBR` — `List<List<rgb>>`: `arMB`'nin yedegi.

**Bitmap Nesneleri:**
- `inputBitmap` — Kullanicinin yukladigi orijinal gorsel. Boyut hesaplamalarinda ve resize isleminde kaynak olarak kullanilir.
- `reducedBitmap` — Tas boyutlarina (R x C) resize edilmis gorsel. M1 ve M3 islemlerinde piksel verisi bundan okunur.
- `exportBitmap` — M3 son sonucunun bitmap hali (R x C boyutunda). Disa aktarma ve overlay islemlerinde kullanilir.
- `rsBitmap` — Gercek tas dokularinin render edildigi buyuk bitmap (R*N x C*N boyutunda). Olustu ise bu gosterilir ve disa aktarilir.

**Sabitler:**
- `N = 20` — Tas doku buyutme katsayisi. Her bir tas pikseli N x N piksellik bir doku bloguyla gosterilir.
- `arn` — `int[R*C]`: Her piksel pozisyonu icin rastgele doku indeksi (1-15 arasi). `StoneTextureService.PopulateRandomIndices()` ile doldurulur.

## Diger Dosyalarla Iliskisi

- **MosaicEngine.cs** — Tum veri dizilerini (`dataM1`, `dataM3`, `arMA`, `arMB`), bitmap nesnelerini ve paleti okuyan/yazan ana servis. `Reset()` metodu tum alanlari sifirlar.
- **ImageService.cs** — `ToByteArray()` bitmap → `byte[,,]`, `FromByteArray()` `byte[,,]` → bitmap donusumleri yapar. `Resize()` ile `inputBitmap` → `reducedBitmap` olusturulur.
- **StoneTextureService.cs** — `dataM3` piksel renkleri ve `arMA` renk kodlari uzerinden doku eslestirmesi yapar, `rsBitmap` uretir. `arn` rastgele indekslerini kullanir.
- **ColorCatalogService.cs** — `arRGBAll`, `arcs`, `arRGB` listelerini doldurur.
- **MainViewModel.cs** — `rsBitmap` ve `exportBitmap`'i overlay ve disa aktarma islemlerinde okur.
- **Region.cs** — `drl.dat` dizisi `MosaicData` ile birlikte bolge/renk numarasi takibini yapar.

## Orijinal WPF Karsiligi

- **Kaynak:** `mosairWPF/ourRobotWpf/picture/lum.cs` — Orijinalde tum bu veriler `lum` sinifinin statik alanlari olarak dagilmisti. `arRS`, `arRSVar`, `RSMat`, `dataRS` gibi ek alanlar da vardi; bunlardan sadece musteri uygulamasi icin gerekenler tasinmistir.
- `dataM3F` orijinalde kullaniliyordu ancak musteri uygulamasinda islevi yoktur.
- Bitmap'ler orijinalde EmguCV `Mat` nesneleriydi, SkiaSharp `SKBitmap` ile degistirilmistir.
