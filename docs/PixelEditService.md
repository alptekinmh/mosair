# PixelEditService.cs

## Genel Bakis

Mozaik uzerinde tekil piksel duzenleme islemlerini yoneten statik servis. Kaynak renk secimi (sourcePix), hedef piksele uygulama (targetPix), geri alma (undo) ve yineleme (redo) islemlerini destekler. `dataM3` dizisi ve `rsBitmap` uzerinde dogrudan degisiklik yapar.

## Veri Yapilari

### PixelEditRecord
Tek bir piksel duzenleme kaydini temsil eder.

| Ozellik | Tip | Aciklama |
|---------|-----|----------|
| `Y` | int | Piksel satir indeksi |
| `X` | int | Piksel sutun indeksi |
| `Source` | rgb | Uygulanan renk (kaynak) |
| `Target` | rgb | Degistirilen pikselin o anki rengi |
| `Original` | rgb | Pikselin ilk orijinal rengi (undo icin) |

## Statik Ozellikler

| Ozellik | Tip | Aciklama |
|---------|-----|----------|
| `EditedPixels` | List&lt;PixelEditRecord&gt; | Tum aktif duzenleme kayitlari |
| `Current` | PixelEditRecord? | Suanki islenmekte olan kayit |
| `IsSourcePixelMode` | bool | Kaynak piksel secim modu |
| `IsTargetPixelMode` | bool | Hedef piksel secim modu |
| `IsPixelEditActive` | bool | Duzenleme modu aktif mi |
| `CanUndo` | bool | Geri alinacak islem var mi |
| `CanRedo` | bool | Yinelenecek islem var mi |

## Metodlar

### TogglePixelEditMode()
Orta mouse tik ile cagirilir. Edit modu kapali ise acar (sourcePix moduna gecer). Target modundayken tekrar source moduna doner. Aktifken kapatir.

### SetSourcePixel(y, x, r, g, b, id)
Kaynak rengi kaydeder. Source modundan target moduna gecer.

### EditPixel(y, x, r, g, b, id)
Hedef piksele kaynak rengi uygular. Uc senaryo:

1. **Yeni duzenleme:** `dataM3`'e kaynak rengi yazar, `EditedPixels`'a ekler, RS bitmap'i gunceller, undo stack'e push eder
2. **Degistirme (replace):** Ayni pikselde farkli kaynak ile tekrar duzenleme. Mevcut kaydi gunceller
3. **Orijinale donus:** Kaynak renk orijinal renkle ayni ise pikseli eski haline dondurur, kaydi siler

Her basarili duzenlemede redo stack temizlenir.

### UndoLastEdit()
Undo stack'ten son kaydi alir. `dataM3`'u Original rengine dondurur, RS bitmap'i restore eder, `EditedPixels`'dan cikarir, redo stack'e push eder.

### RedoLastEdit()
Redo stack'ten son kaydi alir. `dataM3`'e Source rengini yazar, RS bitmap'i gunceller, `EditedPixels`'a ekler, undo stack'e push eder.

### ClearAllEdits()
Tum duzenlenen pikselleri orijinal renklerine dondurur ve `EditedPixels` listesini temizler.

### PatchRSRegion(record, color) [private, unsafe]
RS bitmap uzerinde N×N piksellik bir bolgede renk veya doku degisikligi yapar. Tas kodu bulunursa rastgele bir doku resmi (1-15.jpg) yuklenir ve resize edilir. Bulunamazsa duz renk uygulanir. RGBA/BGRA format farki otomatik ele alinir.

### UpdateRSForPixel / RestoreRSForPixel [private]
`PatchRSRegion`'i Source veya Original renk ile cagiran yardimci metodlar.

### FindCodeNameForColor(rgb) [private]
`MosaicData.arMA` icerisinde verilen RGB degerlerine uyan tasin `codeName`'ini bulur.

## Is Akisi

```
Kullanici orta tik → TogglePixelEditMode() → sourcePix modu
Kullanici sol tik (1. piksel) → SetSourcePixel() → targetPix modu
Kullanici sol tik (2. piksel) → EditPixel() → duzenleme uygulanir
Ctrl+Z → UndoLastEdit() → son duzenleme geri alinir
Ctrl+Y → RedoLastEdit() → geri alinan duzenleme yeniden uygulanir
```

## Diger Dosyalarla Iliskisi

| Dosya | Iliski |
|-------|--------|
| `ViewModels/MainViewModel.cs` | OnImagePressed icinden cagirilir; Undo/Redo metodlari buradan tetiklenir |
| `Models/MosaicData.cs` | `dataM3` ve `rsBitmap` dogrudan degistirilir |
| `Services/StoneTextureService.cs` | `FindFolderForCode` ile doku klasoru bulunur |
