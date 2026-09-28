# MainWindow.axaml + MainWindow.axaml.cs

## Genel Bakis

Uygulamanin ana penceresidir. Karanlik tema (#1a1a1e) uzerine kurulu profesyonel arayuz; gorsel yukleme, mozaiklestirme, piksel duzenleme, proje kaydet/yukle ve disa aktarma islemleri bu pencereden yonetilir. Boyut 1200x800, minimum 900x600. Baslangicta maximized acilir.

**Icon:** `avares://mosairMac/Assets/mosairMac.ico`

## AXAML Yapisi

Pencere bir `DockPanel` icinde 5 ana bolumden olusur:

### 1. Menu Bar (DockPanel.Dock="Top")
Arka plan `#2a2a30`, alt kenarda `#333338` border.

| Menu | Icerik |
|------|--------|
| **Dosya** | Gorsel Yukle (Ctrl+I), Proje Ac (Ctrl+O), Kaydet (Ctrl+S), Farkli Kaydet (Ctrl+Shift+S), Disa Aktar (Ctrl+E) |
| **Duzenle** | Tumunu Sec, Secimi Kaldir |
| **Gorunum** | Ekrana Sigdir (Ctrl+0) |
| **Araclar** | Mozaiklestir (Ctrl+M) |

### 2. Ust Toolbar (DockPanel.Dock="Top")
Arka plan `#232328`. PathIcon tabanli ikon butonlari (`icon-btn` style) ve yesil Mos butonu (`mos-btn` style).

| Eleman | Islem |
|--------|-------|
| Gorsel Yukle ikonu | Dosya secici acar |
| Proje Ac ikonu | .mos proje acar |
| Kaydet / Farkli Kaydet / Disa Aktar ikonlari | Proje ve gorsel kayit |
| Disa Aktar ikonu (`exportBtn`) | `IsExporting` true iken ok tepsiye dusme animasyonu oynatir |
| **Mos** butonu (yesil) | Mozaiklestirmeyi baslatir |
| ProgressBar | `IsProcessing` true iken gorunur |
| Gecen Sure + Durum | `ElapsedTime`, `StatusText` binding |
| sourcePix / targetPix gostergesi | Piksel duzenleme modu aktifken gorunur |

### 3. Status Bar (DockPanel.Dock="Bottom")
Arka plan `#1e1e22`, yukseklik 28px.

- **Sol:** Piksel bilgileri — PixelScaleInfo, PixelCoordInfo, PixelDetailInfo (yesil), PixelColorInfo, EditedPixelCount
- **Sag:** Ekrana Sigdir butonu + ZoomInfo

### 4. Ana Icerik (Grid — 5 Sutun)

```
Col 0: Sol Panel (380px, min 220, max 600)
Col 1: GridSplitter (4px)
Col 2: Canvas (*)
Col 3: GridSplitter (4px)
Col 4: Properties Panel (200px, min 140, max 360)
```

#### Sol Panel (Col 0)
Arka plan `#232328`, DockPanel yapisi.

**Ust Toolbar:** selectAll / deselectAll butonlari + renk sayisi
**Ayarlar Satiri:** Genislik (cm) TextBox, Grid CheckBox, Grid renk ComboBox, N Slider (10-100)
**Boyut Bilgisi:** DimensionInfo, StoneInfo, MouldInfo, OriginalInfo

**3 Sutunlu Liste (Katalog / Palet / Atanan):**
- Oransal genislik: `2* | 1px | 60px | 1px | 4*`
- **Katalog:** CheckBox + ID + renk swatch (16x14, texture thumbnail overlay) + CodeName
- **Palet (I):** Index + renk karesi (kirmizi baslik)
- **Atanan:** Num + CodeName + PixelCount (renkli arka plan satirlari)

#### Canvas (Col 2)
Arka plan `#2e2e34`. DragDrop destegi (gorsel surukle-birak).

- ScrollViewer icinde Image kontrolu (zoom + pan)
- Mouse tekerlegi: zoom (1.25x / 0.8x)
- Sol tik: piksel secimi / duzenleme
- Sag tik: pan (surukle)
- Orta tik: piksel duzenleme modunu toggle

**Mini Map Navigator:** Sag ustte 150x150 overlay panel. Yesil (#4ecb71) viewport cercevesi. Tikla/surukle ile navigasyon.

#### Properties Panel (Col 4)
Arka plan `#232328`. Secili tasin detaylarini gosterir.

| Bolum | Icerik |
|-------|--------|
| **RENK** | 36x36 renk swatch + codeName + ID |
| **DOKU** | 64x64 tas dokusu onizlemesi |
| **RGB** | R, G, B degerleri |
| **KOORDINAT** | Piksel (Y, X) ve Kalip (yi, xi) koordinatlari |
| **DUZENLEME** | Duzenlenen piksel sayisi + Ctrl+Z/Y ipucu (edit modu aktifken) |

## Code-Behind (MainWindow.axaml.cs)

### Constructor
`MainViewModel` olusturur, `DataContext` atar. DragDrop ve KeyDown handler'larini baglar.

### Klavye Kisayollari

| Kisayol | Islem |
|---------|-------|
| Ctrl+Z | Piksel duzenleme geri al (Undo) |
| Ctrl+Y | Piksel duzenleme yinele (Redo) |

### Event Handler'lar

| Metod | Tetikleyici | Islem |
|-------|------------|-------|
| `OnKeyDown` | KeyDown | Ctrl+Z → Undo, Ctrl+Y → Redo |
| `OnLoadImage` | Gorsel Yukle | `StorageProvider.OpenFilePickerAsync` → `_vm.LoadImage(path)` |
| `OnRunMosaic` | Mos butonu | `await _vm.RunMosaicAsync()` → `FitToWindow` |
| `OnExportImage` | Disa Aktar | Masaustu `mosairEXPORT` klasorune zaman damgali JPG → `await _vm.ExportImageAsync(path)` |
| `OnExportAsImage` | Farkli Disa Aktar | `StorageProvider.SaveFilePickerAsync` → `await _vm.ExportImageAsync(path)` |
| `OnWidthChanged` | Genislik TextBox LostFocus | `_vm.UpdateDimensions()` |
| `OnWidthKeyDown` | Genislik TextBox Enter | `_vm.UpdateDimensions()` |
| `OnResetSize` | Ekrana Sigdir | `_vm.FitToWindow(...)` |
| `OnColorCheckChanged` | Renk CheckBox | `_vm.SyncColorExclusion(item)` |
| `OnImageWheel` | Mouse tekerlek | Zoom ayarla + `UpdateNav()` |
| `OnScrollChanged` | ScrollViewer | `UpdateNav()` |
| `OnNavPointerPressed/Moved` | Mini map tiklama | `NavigateFromNav(pos)` |
| `OnImagePointerMoved` | Gorsel uzerinde hareket | Pan veya piksel bilgisi guncelleme |
| `OnImagePointerPressed` | Gorsel uzerinde tiklama | Sag: pan baslat, Sol: piksel sec, Orta: edit toggle |
| `OnImagePointerReleased` | Buton birak | Pan bitir |
| `OnSaveProject` | Kaydet | Mevcut dosyaya kaydet veya SaveAs dialog |
| `OnSaveAsProject` | Farkli Kaydet | `SaveAsDialog()` |
| `OnOpenProject` | Proje Ac | `.mos` dosya sec → `_vm.OpenProject(path)` |
| `OnSelectAll` / `OnDeselectAll` | Tumunu Sec / Kaldir | Katalog renklerini sec/kaldir |
| `OnDragOver` | Surukle uzerinde | `DragDropEffects.Copy` ayarla |
| `OnDrop` | Dosya birak | Gorsel dosyasi ise `_vm.LoadImage(path)` |

### Yardimci Metodlar

| Metod | Islem |
|-------|-------|
| `UpdateNav()` | Mini map viewport dikdortgenini gunceller |
| `NavigateFromNav(pos)` | Mini map tiklamasini scroll pozisyonuna cevirir |
| `SaveAsDialog()` | Proje kayit dialog penceresi |
| `StartMosAnim()` / `StopMosAnim()` | `IsProcessing` degisince Mos butonundaki 4 kare animasyonunu baslatir/durdurur |
| `StartExportAnim()` / `StopExportAnim()` | `IsExporting` degisince Disa Aktar ikonundaki ok animasyonunu baslatir/durdurur; buton `exporting` class'i ile devre disiyken de tam opak kalir |

## Diger Dosyalarla Iliskisi

| Dosya | Iliski |
|-------|--------|
| `ViewModels/MainViewModel.cs` | DataContext; tum binding'ler buraya gider |
| `Services/ProjectService.cs` | `CurrentFileName` kontrol edilir (kaydet islemi) |
| `App.axaml.cs` | `OnFrameworkInitializationCompleted` icinde olusturulur |
