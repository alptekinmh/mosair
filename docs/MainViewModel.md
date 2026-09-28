# MainViewModel.cs

## Genel Bakis

MVVM mimarisindeki ViewModel katmani. UI ile servis katmani arasindaki kopruyu kurar. Gorsel yukleme, mozaiklestirme, zoom kontrolu, renk katalogu yonetimi, piksel duzenleme (undo/redo dahil), navigator ve sonuc gosterimi bu siniftan yonetilir. `INotifyPropertyChanged` ile veri binding destegi saglar.

## Siniflar

### ColorItem
Renk katalogu listesinde her bir satiri temsil eden yardimci sinif.

| Ozellik | Tip | Aciklama |
|---------|-----|----------|
| `Index` | int | Katalogdaki sira numarasi |
| `R`, `G`, `B` | byte | RGB renk degerleri |
| `CodeName` | string | Tas kodu (orn: "BR-01") |
| `Name` | string | Tas adi |
| `ID` | int | Benzersiz kimlik |
| `IsExcluded` | bool | Kullanici tarafindan haric birakildi mi |
| `DisplayText` | string | Hesaplanan gosterim metni |
| `ColorBrush` | IBrush | RGB'den SolidColorBrush |
| `TooltipBitmap` | Bitmap? | Lazy-loaded buyuk doku onizlemesi (tooltip) |
| `ThumbnailBitmap` | Bitmap? | Lazy-loaded 16x14 kucuk doku onizlemesi (liste satiri) |
| `TooltipHeader` | string | Tooltip baslik metni |

### PaletteItem
Palet sutunundaki renk satirini temsil eder.

| Ozellik | Tip | Aciklama |
|---------|-----|----------|
| `Index` | int | Palet sirasi |
| `R`, `G`, `B` | byte | RGB degerleri |
| `ColorBrush` | IBrush | Renk firca |

### AssignedItem
Atanan sutunundaki renk satirini temsil eder.

| Ozellik | Tip | Aciklama |
|---------|-----|----------|
| `Num` | string | Sira numarasi |
| `CodeName` | string | Tas kodu |
| `PixelCount` | string | Kullanilan piksel sayisi |
| `R`, `G`, `B` | byte | RGB degerleri |
| `RowBrush` | IBrush | Satir arka plan rengi |
| `TextBrush` | IBrush | Metin rengi (luminance bazli siyah/beyaz) |

### MainViewModel

## Alanlar ve Varsayilan Degerler

| Alan | Varsayilan | Aciklama |
|------|-----------|----------|
| `_widthCm` | 93.6 | Mozaik genisligi (cm) |
| `_initialZoomLevel` | 2 | Ilk yukleme zoom seviyesi |
| `_targetColors` | 15 | rgbM — hedef ara renk sayisi |
| `_rgbIncrement` | 10 | rgbInc — baslangic renk adimi |
| `_useLab` | false | LAB renk uzayi kullanimi |
| `_useAverage` | false | Ortalama uzaklik modu |
| `_interpolationMethod` | Area | Interpolasyon metodu |
| `_showGrid` | true | Grid cizgileri gorunurlugu |
| `_stonePixelSize` | 20 | RS bitmap tas piksel boyutu (N) |
| `_gridColor` | Black | Grid cizgi rengi |
| `_statusText` | "Hazir" | Durum mesaji |
| `_zoomLevel` | 1 | Mevcut zoom seviyesi (0.01–100) |

## Hesaplanan Ozellikler

| Ozellik | Formul | Aciklama |
|---------|--------|----------|
| `ImageDisplayWidth` | `_bitmapPixelWidth * _zoomLevel` | Gorselin gosterim genisligi |
| `ImageDisplayHeight` | `_bitmapPixelHeight * _zoomLevel` | Gorselin gosterim yuksekligi |
| `ZoomInfo` | `"N={zoom} {w}x{h}"` | Zoom bilgi metni |
| `CanRunMosaic` | `ImageLoaded && !IsProcessing && !IsExporting` | Mos butonu aktiflik |
| `CanExport` | `MosaicDone && !IsProcessing && !IsExporting` | Disa Aktar aktiflik |

## Navigator Ozellikleri

| Ozellik | Aciklama |
|---------|----------|
| `NavViewLeft` | Viewport dikdortgeni sol pozisyonu |
| `NavViewTop` | Viewport dikdortgeni ust pozisyonu |
| `NavViewWidth` | Viewport dikdortgeni genisligi |
| `NavViewHeight` | Viewport dikdortgeni yuksekligi |

## Properties Panel Ozellikleri

| Ozellik | Aciklama |
|---------|----------|
| `HasSelection` | Piksel secildi mi |
| `PropStoneName` | Secili tasin codeName degeri |
| `PropStoneId` | Secili tasin ID'si (orn: "#42") |
| `PropPixelCoord` | Piksel koordinati (orn: "Y: 5  X: 12") |
| `PropMouldCoord` | Kalip koordinati (orn: "yi: 3  xi: 8") |
| `PropRgbInfo` | RGB degerleri (orn: "128, 64, 200") |
| `PropColorBrush` | Secili rengin SolidColorBrush firsati |
| `PropTextureBitmap` | 64x64 tas dokusu onizlemesi |

## Piksel Duzenleme Ozellikleri

| Ozellik | Aciklama |
|---------|----------|
| `IsPixelEditActive` | Duzenleme modu aktif mi |
| `IsSourcePixelMode` | Kaynak piksel secim modu |
| `IsTargetPixelMode` | Hedef piksel secim modu |
| `EditedPixelCount` | Duzenlenen piksel sayisi |

## Metodlar

### LoadImage(string path)
1. `MosaicEngine.Reset()` + `PixelEditService.Reset()` ile onceki verileri temizler
2. `RefreshCatalogList()` ile renk katalogu tam listeye doner
3. `MosaicEngine.LoadImage(path)` ile gorseli yukler
4. Bitmap boyutlarini kaydeder, `ZoomLevel = 2`
5. `ImageService.ToAvaloniaBitmap()` ile Avalonia Bitmap'e donusturur
6. `UpdateDimensions()` ile boyut bilgilerini hesaplar

### RunMosaicAsync()
1. `ColorCatalogService.SetActiveColors()` — aktif renkleri belirler
2. rgbM hesaplamasi (otomatik)
3. `Task.Run` icinde `MosaicEngine.RunM3()` (UI donmaz)
4. Progress callback UI thread'e aktarilir
5. Tamamlaninca: RS bitmap uretilir, katalog filtrelenir

### OnImagePressed(...)
Sol tik ile piksel secimi yapar. Piksel duzenleme modu aktifse source/target islemini yurutur. Orta tik ile edit modunu toggle eder. Secili pikselin bilgilerini Properties paneline yansitir.

### OnImagePointerMoved(...)
Fare hareketinde piksel koordinatlarini ve olcek bilgisini gunceller.

### UndoPixelEdit()
`PixelEditService.UndoLastEdit()` cagirarak son piksel duzenlemesini geri alir. EditedPixelCount ve StatusText guncellenir, overlay yeniden cizilir.

### RedoPixelEdit()
`PixelEditService.RedoLastEdit()` cagirarak geri alinan duzenlemeyi yeniden uygular.

### UpdatePropTexture(string codeName)
Secili tasin 64x64 doku onizlemesini yukler. `StoneTextureService.LoadSingleThumbnail` kullanir.

### UpdateNavigator(...)
Mini map viewport dikdortgen pozisyonunu ve boyutunu hesaplar.

### FitToWindow(double w, double h)
Gorseli pencere boyutuna sigdirir.

### RegenerateRS()
N degeri degistiginde RS bitmap'i yeniden olusturur. 300ms debounce uygular, 2GB sinir kontrolu yapar.

### RedrawOverlay()
Grid cizgileri aciksa `ImageService.DrawOverlay()` ile bitmap uzerine ekler, kapaliysa saf bitmap gosterir.

### SaveProject / OpenProject
`.mos` formatinda proje kaydet ve yukle. `ProjectService` kullanir.

### ExportImageAsync(string path)
RS bitmap veya export bitmap'i PNG/JPEG olarak kaydeder. Bitmap'in kopyasi alinir ve encode `Task.Run` ile arka planda yapilir; bu sure boyunca `IsExporting` true olur (UI donmaz, Disa Aktar ikonu animasyon oynatir) ve kopya sayesinde piksel duzenlemeleri yazilan dosyayi etkilemez. Hata olursa `StatusText`'e yazilir.

### RefreshCatalogList / FilterCatalogByUsedColors
Tam katalog listesini yukler / mos sonrasi yalnizca kullanilan renkleri gosterir.

## Diger Dosyalarla Iliskisi

| Dosya | Iliski |
|-------|--------|
| `MainWindow.axaml` | DataContext olarak atanir; tum binding'ler buraya gider |
| `Services/MosaicEngine.cs` | `LoadImage`, `CalculateDimensions`, `RunM3`, `Reset` |
| `Services/ImageService.cs` | `ToAvaloniaBitmap`, `DrawOverlay` |
| `Services/ColorCatalogService.cs` | `SetActiveColors`, `GetCatalogList`, `SetLeaveOut` |
| `Services/PixelEditService.cs` | Piksel duzenleme, undo/redo |
| `Services/StoneTextureService.cs` | Doku thumbnail yukleme |
| `Services/ProjectService.cs` | Proje kaydet/yukle |
| `Models/MosaicData.cs` | `rsBitmap`, `exportBitmap`, `dataM3`, `arMA` okunur |
