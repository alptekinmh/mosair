# ColorMatcher.cs

## Genel Bakis

`ColorMatcher` renk mesafesi hesaplama ve en yakin renk bulma islemlerini yapan statik siniftir. RGB ve CIELAB renk uzaylarinda mesafe olcumu destekler. M3 pipeline'inin Section 2 asamasinda (katalog rengi atama) kullanilir.

## Metodlar

### `RgbToLab(double r, double g, double b) → (double L, double A, double B)`

RGB degerlerini CIELAB renk uzayina donusturur. Donusum iki asamalidir:

**1. RGB → XYZ (lineer donusum):**
- sRGB gamma duzeltmesi: deger > 0.04045 ise `((v + 0.055) / 1.055)^2.4`, degilse `v / 12.92`
- 100 ile carpim
- sRGB → XYZ matrisi (D65 illuminant):
  ```
  X = 0.412453*R + 0.357580*G + 0.180423*B
  Y = 0.212671*R + 0.715160*G + 0.072169*B
  Z = 0.019334*R + 0.119193*G + 0.950227*B
  ```

**2. XYZ → CIELAB:**
- D65 referans beyazi ile normalizasyon: X/95.047, Y/100.0, Z/108.883
- Kup kok donusumu: deger > 0.008856 ise `v^(1/3)`, degilse `7.787*v + 16/116`
- L = 116*Y - 16, A = 500*(X - Y), B = 200*(Y - Z)

**Kullanim:** LAB modu aktifken (`boolLab=true`) piksel eslestirmede ve renk mesafesi hesaplamada kullanilir. LAB uzayi insan algisal farkliliklarini daha iyi yansitir.

### `CalculateDistance(rgb ra, rgb raa, int k) → cooo`

Iki renk arasindaki Oklid mesafesini hesaplar:
```
av = sqrt((b1-b2)^2 + (g1-g2)^2 + (r1-r2)^2)
```

**Parametreler:**
- `ra` — kaynak renk (ara renk)
- `raa` — hedef renk (katalog rengi)
- `k` — hedef rengin indeksi

**Donus:** `cooo` nesnesi (av=mesafe, n=indeks)

### `FindCatalogDistances(rgb ra) → List<cooo>`

Bir ara rengi tum aktif katalog renklerine (`MosaicData.arRGB`) karsi karsilastirir. `boolUseOnce=true` olan renkleri atlar (sadece pix2 modunda etkili).

**Donus:** Her katalog rengi icin mesafe listesi.

### `SelectNearest(List<cooo> aro) → int`

Mesafe listesinden en kucuk `av` degerine sahip olanin `n` (indeks) degerini dondurur. Brute-force minimum arama.

### `ResetUseOnce()`

Tum katalog renklerinin `boolUseOnce` flagini `false` yapar. `pix2` modunda her renk sadece bir kez kullanilabilir; `pix3` modunda (musteri uygulamasi) bu kisitlama yoktur ve bu metod her Section 2 basinda cagirilir.

## Diger Dosyalarla Iliskisi

| Dosya | Iliski |
|-------|--------|
| `MosaicEngine.cs` | Section 2'de `FindCatalogDistances` + `SelectNearest` cagirilir |
| `MosaicData.cs` | `arRGB` (aktif katalog renkleri) uzerinden calisir |
| `Rgb.cs` | `rgb` ve `cooo` veri siniflarini kullanir |

## Orijinal WPF Karsiligi

- **Kaynak:** `mosairWPF/ourRobotWpf/picture/im.cs`
- Orijinalde mesafe hesaplama ve en yakin renk bulma inline kod olarak dagilmisti
- `RgbToLab` donusumu orijinaldeki `rgb2lab` metodundan birebir tasinmistir
- Ayri bir sinifa cikarilarak sorumluluk ayirimi saglanmistir
