# StoneTextureService.cs

## Genel Bakis

Gercek tas dokularinin yuklenmesi, boyutlandirilmasi ve final mozaik bitmap'inin olusturulmasindan sorumlu statik servis sinifi. Mozaiklestirme tamamlandiktan sonra her piksel pozisyonuna gercek tas fotograflari yerlestirir. 02_RS klasorundeki 124 tas tipinin her birinden 16 farkli JPG foto kullanilir.

## Dahili State

| Alan | Tip | Aciklama |
|------|-----|----------|
| `_textures` | `Dictionary<string, List<byte[,,]>>` | codeName → 16 orijinal doku (BGR byte dizisi) |
| `_resizedTextures` | `Dictionary<string, List<byte[,,]>>` | codeName → 16 NxN boyutlandirilmis doku |
| `_rsDirNames` | `string[]?` | 02_RS altindaki klasor yollari (cache) |
| `_rsBasePath` | `string?` | 02_RS kok dizini |

## Metodlar

### FindRSPath() → string?
02_RS klasorunu 4 konumda sirasyla arar:

1. `{exeDir}/Assets/02_RS` — publish ciktisi
2. `{exeDir}/mosaicFiles/02_RS` — alternatif klasor yapisi
3. `{exeDir}/02_RS` — dogrudan yan dizin
4. `D:\dev\mosairWPF\...\mosaicFiles\02_RS` — gelistirme ortami fallback

Ilk bulunan yol dondurulur. Hicbiri yoksa `null`.

### PopulateRandomIndices(int R, int C)
`MosaicData.arn[R*C]` dizisini `Random.Next(1, 16)` ile doldurur. Her piksel icin 1–15 arasi rastgele bir doku indeksi atanir. Orijinal WPF'deki `ra.Next(1, 16)` davranisiyla birebir uyumlu.

### LoadTextures()
1. Cache'leri temizler
2. `FindRSPath()` ile 02_RS yolunu bulur
3. `MosaicData.arMA` icindeki her renk icin:
   - `FindFolder(codeName)` ile klasoru arar
   - 1.jpg–16.jpg dosyalarini `SKBitmap.Decode` ile yukler
   - `ImageService.ToByteArray()` ile BGR byte[,,] dizisine cevirir
   - Klasor veya dosya bulunamazsa `CreateSolidTexture()` ile duz renk olusturur
4. Sonuc: `_textures[codeName]` = 16 elemanlI liste

### FindFolder(string codeName) → string?
`_rsDirNames` uzerinde dongu yapar. Klasor adinda `codeName` arayan `IndexOf` kontrolu:
- `pos > 0` kosulu onemli — klasor adinin **basinda degil icinde** aramasi yapar
- Ornek: "123-BR01-Bianco" icinde "BR01" aranirsa pos=4 > 0, eslenir

Bu davranis orijinal WPF `getAddressOfRealStones()` metoduyla birebir uyumludur.

### CreateSolidTexture(rgb color, int size) → byte[,,]
Doku dosyasi bulunamadiginda fallback olarak kullanilir. `size x size x 3` boyutunda duz renkli BGR byte dizisi olusturur.

### ResizeTextures(int N)
Tum dokular NxN piksele boyutlandirilir (varsayilan N=20):
1. `_textures` uzerinde dongu
2. Her doku icin: boyut zaten NxN ise aynen kopyala
3. Degilse: `FromByteArray` → `Resize` (area-weighted) → `ToByteArray` zinciri
4. Sonuc: `_resizedTextures[codeName]` = 16 elemanlI NxN doku listesi

### GenerateRSBitmap(int R, int C, int N) → SKBitmap?
Final gercek tas doku bitmap'ini olusturur:

1. **Renk → codeName haritasi**: `Dictionary<(byte b, byte g, byte r), string>` — `arMA` icindeki tum renkler O(1) erisim icin hash'lenir
2. **dataRS dizisi**: `byte[R*N, C*N, 3]` — cikis bitmap boyutu
3. **Piksel dongusu**: Her (i, j) piksel icin:
   - `MosaicData.arn[e]` ile rastgele doku indeksi `n` alinir
   - `MosaicData.dataM3[i,j]` den BGR renk okunur
   - Haritadan `codeName` bulunur → `_resizedTextures[codeName][n]` dokusundan NxN blok kopyalanir
4. `ImageService.FromByteArray(dataRS, R*N, C*N)` ile SKBitmap'e donusturulur

Ornek: 78x156 grid, N=20 → cikis bitmap: 1560x3120 piksel

### Reset()
`_textures` ve `_resizedTextures` cache'lerini temizler. `_rsDirNames` null yapilir. Yeni gorsel yuklendiginde veya `MosaicEngine.Reset()` icinden cagrilir.

## Doku Sistemi Akisi

```
PopulateRandomIndices(R, C)      → rastgele indeksler
    ↓
LoadTextures()                   → 02_RS'den 16 JPG/renk yuklenir
    ↓
ResizeTextures(N)                → tum dokular NxN'e boyutlandirilir
    ↓
GenerateRSBitmap(R, C, N)       → her piksele NxN doku blogu yerlestirilir
    ↓
MosaicData.rsBitmap              → final gercek tas gorunumlu bitmap
```

## Diger Dosyalarla Iliskisi

| Dosya | Iliski |
|-------|--------|
| `Services/MosaicEngine.cs` | `RunM3()` sonunda bu servis cagrilir; `Reset()` icinden `StoneTextureService.Reset()` |
| `Services/ImageService.cs` | `ToByteArray`, `FromByteArray`, `Resize` metodlari kullanilir |
| `Models/MosaicData.cs` | `arMA` (renk listeleri), `dataM3` (piksel verileri), `arn` (rastgele indeksler), `N`, `rsBitmap` |
| `Assets/02_RS/` | 124 klasor, ~2013 JPG dosya — gercek tas fotograflari |

## Orijinal WPF Karsiligi

| WPF Dosya/Metod | mosairMac Karsiligi |
|-----------------|-------------------|
| `picture/im.cs` → `popRandomRSNums()` | `PopulateRandomIndices()` |
| `picture/im.cs` → `populateRSArrayFrom500Using_arMB()` | `LoadTextures()` |
| `picture/im.cs` → `resizeRC500WithN(N)` | `ResizeTextures()` |
| `picture/im.cs` → `RS()` | `GenerateRSBitmap()` |
| `picture/realStone.cs` → `realStoneRegion`, `rcolor` | Ayri sinif yerine Dictionary + List yaklasimi |
| `picture/WindowPicture.xaml.cs` → `getAddressOfRealStones()` | `FindFolder()` |
