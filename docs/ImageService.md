# ImageService.cs

## Genel Bakis

`ImageService` tum SkiaSharp gorsel islemlerini iceren statik siniftir. Orijinal WPF projesindeki EmguCV (OpenCV .NET wrapper) cagrilarinin tamami bu dosyada yeniden yazilmistir. Gorsel yukleme, boyutlandirma, piksel donusumu, overlay cizimi, Avalonia bitmap donusumu ve disa aktarma islemlerini yapar.

## Metodlar

### `LoadImage(string path) → SKBitmap?`

Dosya yolundan gorsel yukler. `SKBitmap.Decode()` kullanir. Dosya yoksa `null` dondurur.

**Not:** `SKBitmap.Decode()` Windows'ta genellikle `Bgra8888` formatinda bitmap dondurur. Diger platformlarda veya bazi gorsel formatlarinda `Rgba8888` donebilir.

**Orijinal:** `CvInvoke.Imread()`

### `Resize(SKBitmap src, int dstW, int dstH) → SKBitmap` (unsafe)

Gorseli hedef boyuta yeniden boyutlandirir. Iki farkli yol kullanir:

**Buyutme (upscale):** `dstW >= srcW && dstH >= srcH` ise SkiaSharp'in yerlesik `SKBitmap.Resize()` metodu Mitchell cubic resampler ile kullanilir.

**Kucultme (downscale):** OpenCV `Inter.Area` algoritmasinin manuel implementasyonu. Area-weighted averaging yontemi kullanir:

```
Her hedef piksel (dx, dy) icin:
  1. Kaynak gorunum alanini hesapla: srcX0..srcX1, srcY0..srcY1
  2. Alan icindeki tum kaynak pikselleri agirlikli ortala
  3. Agirlik = yatay kesisim * dikey kesisim (alan orani)
  4. Sonuc = toplam(piksel * agirlik) / toplam(agirlik)
```

**Unsafe pointer erisimi:** `src.GetPixels()` ile ham piksel bellegine erisir. `isBgra` flagi ile Bgra8888 ve Rgba8888 formatlarini ayirt eder. Stride olarak `src.RowBytes` kullanir.

**Cikti:** Her zaman `Rgba8888` formatinda yeni bitmap.

**Orijinal:** `CvInvoke.Resize(src, dst, size, 0, 0, Inter.Area)`

### `FlipHorizontal(SKBitmap src) → SKBitmap`

Gorseli yatay eksen etrafinda cevirir. SKCanvas `Scale(-1, 1)` + `Translate` kullanir.

**Orijinal:** `CvInvoke.Flip(mat, mat, FlipType.Horizontal)`

### `FromByteArray(byte[,,] data, int rows, int cols) → SKBitmap` (unsafe)

BGR byte dizisini SKBitmap'e donusturur. Dahili veri yapilari BGR sirasinda calistigi icin kanal eslestirmesi yapar:

```
data[i,j,2] → R (kirmizi)
data[i,j,1] → G (yesil)
data[i,j,0] → B (mavi)
Alpha = 255 (opak)
```

**Cikti:** `Rgba8888`, `AlphaType.Opaque` formatinda bitmap.

**Unsafe pointer erisimi:** `bitmap.GetPixels()` ile dogrudan piksel bellegine yazar. Managed dizi sinir kontrolleri atlanarak performans kazanilir.

### `ToByteArray(SKBitmap bitmap) → byte[,,]` (unsafe)

SKBitmap'i BGR byte dizisine donusturur. `bitmap.ColorType` kontrolu ile iki formati destekler:

**Bgra8888** (Windows varsayilani): Byte sirasi B,G,R,A — dogrudan BGR eslestirmesi:
```
data[i,j,0] = src[offset+0]  // B
data[i,j,1] = src[offset+1]  // G
data[i,j,2] = src[offset+2]  // R
```

**Rgba8888**: Byte sirasi R,G,B,A — ters eslestirme:
```
data[i,j,0] = src[offset+2]  // B
data[i,j,1] = src[offset+1]  // G
data[i,j,2] = src[offset+0]  // R
```

### `DrawOverlay(SKBitmap src, int stoneSize, bool showGrid, bool showMouldLines, bool showRowColNum, bool showMouldId, int penWidth) → SKBitmap`

Mozaik bitmap uzerine grid ve kalip cizgileri cizer. Kaynak bitmap'in kopyasi uzerinde calisir.

**Katmanlar:**
1. **Grid cizgileri** (`showGrid`): Gri renk, 1px kalinlik, `stoneSize` piksel aralikla yatay ve dikey cizgiler
2. **Kalip cizgileri** (`showMouldLines`): Siyah renk, `penWidth` kalinlik, `stoneSize * 26` piksel aralikla (her kalip 26 tas)
3. **Kalip numaralari** (`showMouldId`): Beyaz metin + siyah golge, her kalip hucresinin ortasinda
4. **Satir/sutun numaralari** (`showRowColNum`): Siyah metin, her tasin icinde (0-12 arasi dongu)

**Not:** `src.Copy()` ile tam bitmap kopyasi yapilir (performans raporunda belirtilen darbogaz).

### `ToAvaloniaBitmap(SKBitmap bmp) → Avalonia.Media.Imaging.Bitmap`

SKBitmap'i Avalonia UI'nin `Bitmap` turune donusturur. PNG encode/decode yontemi kullanir:

```csharp
SKImage.FromBitmap(bmp)
  → image.Encode(PNG, 100)
  → data.ToArray()
  → new MemoryStream(bytes)
  → new Avalonia.Media.Imaging.Bitmap(stream)
```

**Performans notu:** Bu metod bir darbogaz olusturur. RS bitmap 1560x3120 piksel oldugunda encode/decode dongusu yuz milisaniyeleri bulur ve gecici ~15MB byte[] olusturur. Ideal cozum `WriteableBitmap` ile dogrudan piksel kopyasidir (performans optimizasyonu Faz 1'de planlanmistir).

### `ExportImage(SKBitmap bmp, string path, SKEncodedImageFormat format, int quality)`

Bitmap'i dosyaya kaydeder. PNG veya JPEG formatini destekler. `quality` parametresi JPEG icin sikistrima kalitesini belirler (0-100). Dosya `File.Create` ile acilir; var olan dosyanin uzerine yazarken dosya once sifirlanir. Arka plan thread'inden cagrilabilir (`MainViewModel.ExportImageAsync`).

## Diger Dosyalarla Iliskisi

| Dosya | Iliski |
|-------|--------|
| `MosaicEngine.cs` | Resize, ToByteArray, FromByteArray pipeline boyunca surekli cagirilir |
| `StoneTextureService.cs` | Doku yukleme/resize/render icin ToByteArray ve FromByteArray kullanir |
| `MainViewModel.cs` | ToAvaloniaBitmap ile UI gosterimi, ExportImage ile disa aktarma |
| `MosaicData.cs` | inputBitmap, reducedBitmap, exportBitmap, rsBitmap uzerinde calisir |

## Orijinal WPF Karsiligi

- **Kaynak:** `mosairWPF/ourRobotWpf/picture/im.cs` (EmguCV islemleri)
- EmguCV `Mat` → SkiaSharp `SKBitmap`
- `CvInvoke.Resize(Inter.Area)` → Manuel area-weighted averaging (unsafe)
- `CvInvoke.Flip` → SKCanvas Scale
- `Mat.GetData<byte>()` → `ToByteArray` (unsafe pointer)
- `Mat(rows, cols, DepthType.Cv8U, 3)` → `FromByteArray` (unsafe pointer)
- `CvInvoke.Line/PutText` → SKCanvas.DrawLine/DrawText
- Avalonia donusumu ve export islemleri yenidir (WPF'te `BitmapSource` kullaniliyordu)
