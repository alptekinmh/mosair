# ProjectService.cs

## Genel Bakis

`.mos` formatinda proje kaydet ve yukle islemlerini yoneten statik servis. Tum mozaik verileri (dataM1, dataM3, renk listeleri, piksel duzenlemeleri, bolgeler) JSON formatinda tek dosyada saklanir. Orijinal gorsel dosyasi da proje klasorune kopyalanir.

## Veri Yapilari

### ProjectData
Proje dosyasinin JSON icerigini temsil eder.

| Ozellik | Aciklama |
|---------|----------|
| `DataM1Flat/Dims` | dataM1 dizisi (flatten + boyutlar) |
| `DataM3Flat/Dims` | dataM3 dizisi |
| `DataM3FFLat/Dims` | dataM3F dizisi |
| `DataM3BackupFlat/Dims` | dataM3 yedegi |
| `DrlDatFlat/Dims` | Bolge verileri |
| `ArRGBAll/ArRGB` | Tum ve aktif renk listeleri |
| `ArMA/ArMB/ArMBR` | Mozaik renk asamalari |
| `EditedPixels` | Piksel duzenleme kayitlari |
| `Regions` | Bolge tanimlari |
| `Width/Height` | Mozaik boyutlari |
| `RgbM/N` | Renk parametreleri ve tas piksel boyutu |
| `ShowGrid/ShowMouldLines` | Gorunum ayarlari |
| `GridColorR/G/B` | Grid rengi |
| `InterpolationMethod` | Interpolasyon yontemi |
| `ZoomLevel/WidthCm` | Zoom ve genislik |
| `PictureFileName` | Orijinal gorsel dosya adi |

### RgbData
`rgb` modelinin JSON-seriletirebilir karsiligi. `FromRgb()` ve `ToRgb()` donusum metodlari icerir.

### EditedPixelData
`PixelEditRecord`'un JSON-seriletirebilir karsiligi. Source, Target ve Original renkleri saklar.

### RegionData
Bolge taniminin JSON-seriletirebilir karsiligi (X1, Y1, X2, Y2, RgbM).

## Statik Ozellikler

| Ozellik | Aciklama |
|---------|----------|
| `CurrentFileName` | Son kaydedilen/acilan proje dosya yolu |
| `CurrentPictureFileName` | Orijinal gorsel dosya yolu |

## Metodlar

### Save(filePath, widthCm, zoomLevel, showGrid, showMouldLines, gcR, gcG, gcB, interpMethod)
1. `ProjectData` nesnesi olusturur
2. 3D dizileri `Flatten3D` ile duzlestirir
3. Renk listelerini `RgbData.FromRgb()` ile donusturur
4. Piksel duzenlemelerini kaydeder
5. Orijinal gorseli proje klasorune kopyalar
6. JSON olarak dosyaya yazar

### Open(filePath) → ProjectData?
1. JSON dosyasini okur ve deserialize eder
2. `Unflatten3D` ile 3D dizileri geri olusturur
3. Renk listelerini `ToRgb()` ile dondurur
4. Piksel duzenlemelerini `PixelEditService.EditedPixels`'a yukler
5. Motor parametrelerini (width, height, rgbM, N) geri yukler
6. `CurrentFileName` ve `CurrentPictureFileName` gunceller

## Yardimci Metodlar (private)

| Metod | Islem |
|-------|-------|
| `Flatten3D` | byte[,,] → byte[] + int[] dims |
| `Unflatten3D` | byte[] + dims → byte[,,] |
| `FlattenInt3D` | int[,,] → int[] + int[] dims |
| `UnflattenInt3D` | int[] + dims → int[,,] |
| `FindOriginalImagePath` | Orijinal gorsel dosyasini bulur |

## Diger Dosyalarla Iliskisi

| Dosya | Iliski |
|-------|--------|
| `ViewModels/MainViewModel.cs` | SaveProject/OpenProject metodlari cagirir |
| `MainWindow.axaml.cs` | OnSaveProject/OnOpenProject handler'lari tetikler |
| `Models/MosaicData.cs` | Tum veri dizileri buradan okunur/yazilir |
| `Services/PixelEditService.cs` | EditedPixels kaydedilir/yuklenir |
| `Services/MosaicEngine.cs` | width, height, rgbM parametreleri |
