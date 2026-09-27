# MosaicEngine.cs

## Genel Bakis

`MosaicEngine` projenin cekirdek orkestrasyon sinifidir. Gorsel yukleme, boyut hesaplama ve M3 mozaiklestirme algoritmasinin tum asamalarini yonetir. Orijinal WPF projesindeki `im.cs` dosyasinin M3/M1 metodlarinin Avalonia/SkiaSharp ortamina tasindigi tek dosyadir.

Statik sinif olarak tasarlanmistir — tum state ve metodlar `static`'tir. Islem sirasi: gorsel yukle → boyut hesapla → palet uret → piksel esle → iteratif renk azalt → katalog rengi ata → gercek tas doku render.

## Alanlar (Fields)

| Alan | Tip | Aciklama |
|------|-----|----------|
| `width`, `height` | `double` | Tas gridi boyutlari (sutun ve satir sayisi) |
| `wi`, `he` | `int` | Kalip sayilari (ceiling/floor) |
| `actualWidth`, `actualHeight` | `double` | Gercek fiziksel boyut (mm cinsinden) |
| `rate` | `double` | Gorselin en-boy orani (height/width) |
| `excessiveW`, `excessiveH` | `double` | Kalip tasma miktari (0-1 arasi) |
| `rgbM` | `int` | Hedef ara renk sayisi (varsayilan 3, Mos'ta hesaplanir) |
| `RGBInc` | `int` | Baslangic RGB adimi (varsayilan 19, UI'dan 10 gelir) |
| `minRGBInc` | `int` | Minimum renk silme artisi (varsayilan 2) |
| `numOfMinRGB` | `int` | Mevcut minimum piksel esigi (iterasyonda artar) |
| `boolLab` | `bool` | LAB renk uzayi kullanilsin mi |
| `boolAv` | `bool` | Ortalama mesafe mi (true) yoksa Oklid mi (false) |
| `penW` | `int` | Kalip cizgi kalinligi (varsayilan 2) |

## Metodlar

### Public API

#### `LoadImage(string path) → SKBitmap?`
Gorseli diskten yukler ve `MosaicData.inputBitmap`'e atar. `ImageService.LoadImage()` uzerinden `SKBitmap.Decode()` kullanir.

#### `CalculateDimensions(double widthCm) → DimensionResult?`
Kullanicinin girdigi genislik (cm) degerinden tas gridi boyutlarini hesaplar.

**Hesaplama mantigi:**
1. `numOfStonesInRow = Convert.ToInt32((widthCm * 10.0) / 12.0)` — Her tas 12mm, banker's rounding kullanilir
2. `actualWidth = numOfStonesInRow * 12` — Gercek genislik (mm)
3. `width = numOfStonesInRow` — Sutun sayisi
4. `mouldNW = numOfStonesInRow / 26.0` — Her kalip 26 tas icerir
5. `wi = Ceiling(mouldNW)` — Yatay kalip sayisi
6. `rate = inputBitmap.Height / inputBitmap.Width` — En-boy orani
7. `height = Convert.ToInt32(width * rate)` — Satir sayisi (banker's rounding)
8. `he = Floor(mouldNH)` — Dikey kalip sayisi

**Onemli:** `Convert.ToInt32` kullanilir, `(int)` cast degil. `Convert.ToInt32` banker's rounding yapar (0.5'i en yakin cift sayiya yuvarlar), `(int)` ise truncation yapar. Bu fark buyuk boyutlarda 1 satir/sutun farki yaratabilir.

#### `RunM3(int targetColors, int rgbIncrement, bool useLab, bool useAverage, Action<int>? onProgress) → SKBitmap`
M3 mozaiklestirme algoritmasinin tam pipeline'idir. Asagidaki asamalardan olusur:

**Asama 1 — Hazirlik:**
1. `Resize(inputBitmap, width, height)` — Orijinal gorseli tas gridi boyutuna kucult
2. `CreateSingleRegion(targetColors, R, C)` — Tum gorseli tek bolge olarak tanimla
3. `InitM3()` — arMA/arMB listelerini temizle
4. `GenerateInitialPalette()` — RGB uzayinda RGBInc adimlarla tum kombinasyonlari uret

**Asama 2 — Ilk eslestirme:**
5. `RunM1(R, C)` — Her pikseli en yakin palet rengine esle (brute-force)
6. `CopyM1ToM3(R, C)` — M1 sonuclarini M3 dizisine kopyala

**Asama 3 — Iteratif renk azaltma:**
```
while (ar3.Count > rgbM):
    minRGBInc ayarla (>2000 ise 5, >1000 ise 2, degilse 1)
    RemoveMinimalColors() — numOfPixel < numOfMinRGB olan renkleri sil
    Resize(inputBitmap, width, height) — orijinalden yeniden kucult
    ProcessM3() — kalan renklerle yeniden esle
    numOfMinRGB += minRGBInc
```

**Asama 4 — Katalog rengi atama (Section 2):**
7. `arMA → arMB` derin kopyala
8. Her ara renk icin `ColorMatcher.FindCatalogDistances()` + `SelectNearest()` ile en yakin katalog tasi rengini bul
9. `ApplyStoneColor()` ile dataM3'teki piksel renklerini katalog rengine degistir

**Asama 5 — uc birlestirme (Section 3):**
10. Ayni katalog rengine eslenmis farkli ara renkleri birlestir (uc = unite color)
11. `dat[,,3]` degerlerini uc ile guncelle

**Asama 6 — codeName bazli birlestirme (Section 4):**
12. Ayni `codeName`'e sahip renkleri tek satirda topla, `numOfPixel` degerlerini topla

**Asama 7 — Final (Section 5):**
13. `dat[,,3]`'e her rengin `ID` degerini yaz
14. `arMA = arMB` kopyala, `dataM3Backup` olustur
15. `dataM3`'ten final bitmap olustur
16. RS pipeline: `PopulateRandomIndices → LoadTextures → ResizeTextures → GenerateRSBitmap`

#### `Reset()`
Tum statik verileri temizler: arMA/arMB/arMBR, dat, dataM1/M3/M3F/M3Backup, tum bitmap'ler, StoneTextureService.

### Private Metodlar

#### `CreateSingleRegion(int targetColors, int R, int C)`
Tek bolge olusturur. `drl.arar[0]` olarak tum gorseli kapsar (x1=0, y1=0, x2=C, y2=R). `drl.dat[R,C,4]` dizisini olusturur ve tum hucreleri `(1,1,1,0)` ile doldurur.

#### `InitM3()`
`arMA` ve `arMB` listelerini temizler.

#### `GenerateInitialPalette()`
`RGBInc` adimlarla 0-254 araliginda tum RGB kombinasyonlarini uretir. `RGBInc=10` icin `26^3 = 17.576` renk olusur. Her bolge icin ayri bir liste olusturulur (tek bolge senaryosunda 1 liste).

#### `RunM1(int R, int C)`
Ilk eslestirme pasi. Her piksel icin tum `arRGB` katalog renklerine karsi mesafe hesaplar. LAB veya RGB moduna gore mesafe hesabi yapar, ortalama veya Oklid secenegini uygular. Sonuc `dataM1[R,C,3]` dizisine yazilir.

#### `ProcessM1(int R, int C)`
`RunM1` ile ayni mantik, ancak resize adimini icermez (bitmap zaten hazir).

#### `CopyM1ToM3(int R, int C)`
`dataM1` dizisini `dataM3`'e kopyalar.

#### `ProcessM3(int reg, int R, int C)`
Iteratif dongunun ic eslestirme metodu. `arMA[reg]` paletindeki renklere karsi her pikseli yeniden esler. Sadece belirli bolgeye ait pikselleri isler (`dat[i,j,2] == reg+1` kontrolu). Yeni eslestirme sonucu `dataM3`'e yazilir, `numOfPixel` sayaclari guncellenir.

#### `RemoveMinimalColors(int reg)`
`arMA[reg]` listesinden `numOfPixel < numOfMinRGB` olan renkleri siler. `RemoveAt(i)` ile siler ve `i--` yapar (geri adim).

#### `AssignColorNumbers(int R, int C, int reg)`
Kalan renklere sirayla `u` (numara) atar. Her renk icin `ri/gi/bi` (intermediate renkler) ve `dis` (ortalama) hesaplar. `SetColorNumber` ile `dat[,,3]`'e yazar.

#### `SetColorNumber(int R, int C, rgb r)`
Tum piksel gridini tarayarak `dataM3`'te `r` rengine uyan piksellerin `dat[,,3]` degerini `r.u` ile gunceller.

#### `ApplyStoneColor(rgb ro, byte r_, byte g_, byte b_, int R, int C) → int`
`dataM3`'te eski rengi yeni renkle degistirir. Eslesen piksel sayisini dondurur.

#### `BackupM3(int R, int C)`
`arMBR = arMB` kopyasi, `dataM3Backup = dataM3` kopyasi.

#### `CloneRgb(rgb src) → rgb`
Derin kopya. `gc<T>.DeepCopy()` (BinaryFormatter) yerine manuel alan kopyalamasi. .NET 10 uyumlu.

#### `CloneList(List<rgb> src) → List<rgb>`
Liste derin kopyasi.

#### `CloneNestedList(List<List<rgb>> src) → List<List<rgb>>`
Ic ice liste derin kopyasi.

## Diger Dosyalarla Iliskisi

| Dosya | Iliski |
|-------|--------|
| `MosaicData.cs` | Tum statik verilerin deposu — dataM1/M3, arMA/arMB, bitmap'ler |
| `ImageService.cs` | Resize, ToByteArray, FromByteArray, ToAvaloniaBitmap islemleri |
| `ColorMatcher.cs` | Section 2'de katalog rengi eslestirme (FindCatalogDistances, SelectNearest) |
| `ColorCatalogService.cs` | Aktif renk listesi (arRGB) hazirlama |
| `StoneTextureService.cs` | Pipeline'in son adimi — gercek tas doku render |
| `Region.cs` | drl/dr siniflari, bolge ve dat dizisi |
| `Rgb.cs` | rgb/cooo veri siniflari |
| `MainViewModel.cs` | RunMosaicAsync icinden cagirir, progress callback alir |

## Orijinal WPF Karsiligi

- **Kaynak:** `mosairWPF/ourRobotWpf/picture/im.cs`
- M3 iteratif dongu, RunM1, ProcessM3, palet uretimi, renk silme, Section 2-5 islemleri orijinalden birebir tasinmistir
- `gc<T>.DeepCopy()` (BinaryFormatter) yerine manuel `CloneRgb/CloneList/CloneNestedList` kullanilir
- EmguCV `CvInvoke.Resize(Inter.Area)` yerine `ImageService.Resize()` (manuel area-weighted)
- EmguCV `Mat` yerine `SKBitmap`
- `pix3` modu varsayilan olarak calisir, `boolUseOnce` sadece `pix2` icin gecerlidir
