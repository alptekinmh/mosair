# PixelEditService

> Kaynak: `mosair/Services/PixelEditService.cs` · Güncelleme: 2026-10-06

## Amaç

Mozaik üzerinde tek tek taş (hücre) değiştirmeyi yönetir: kaynak renk seçimi, hedef hücreye uygulama, düzenleme listesi, geri al / yinele ve gerçek taş dokulu görüntünün (`MosaicData.rsBitmap`) ilgili hücresinin yerinde güncellenmesi.

## Nerede kullanılır

| Çağıran | Kullanım |
|---|---|
| `MainViewModel` | Mod değiştirme, kaynak/hedef seçimi, `EditPixel`, `UndoLastEdit`, `RedoLastEdit`, `CanUndo`/`CanRedo`, `EditedPixels` sayısı |
| `MainWindow.axaml.cs` | `IsPixelEditActive` (fare/imleç davranışı) |
| `ProjectService` | `EditedPixels` kaydedilir; açılışta `Reset()` sonrası dosyadan geri yüklenir |
| `MosaicEngine` | `Reset()`: `MosaicEngine.Reset`, `ApplyOptimalK` ve stoğa göre düzeltme (`FixToStock`) mozaiği gerçekten değiştirdiğinde |

## Yapı

### `PixelEditRecord`

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `Y`, `X` | `int` | 0 | Taş koordinatı (satır, sütun) |
| `Source` | `rgb` | `new()` | Yerleştirilen renk (kaynak) |
| `Target` | `rgb` | `new()` | Tıklanan hücrenin o anki rengi |
| `Original` | `rgb` | `new()` | Hücrenin ilk, düzenleme öncesi rengi |

### `PixelEditService` (static)

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `EditedPixels` | `List<PixelEditRecord>` | boş | Etkin düzenlemeler (hücre başına bir kayıt) |
| `Current` | `PixelEditRecord?` | `null` | Hazırlanmakta olan düzenleme |
| `IsSourcePixelMode` | `bool` | `false` | Sıradaki tıklama kaynak rengi seçer |
| `IsTargetPixelMode` | `bool` | `false` | Sıradaki tıklama hedef hücreyi boyar |
| `IsPixelEditActive` | `bool` | `false` | Piksel düzenleme modu açık mı |
| `CanUndo` / `CanRedo` | `bool` | | Yığınlarda kayıt var mı |
| `_undoStack` / `_redoStack` | `Stack<PixelEditRecord>` | boş | Geri al / yinele yığınları (özel) |

Özel yardımcılar: `PatchRSRegion`, `UpdateRSForPixel`, `RestoreRSForPixel`, `FindCodeNameForColor` (aşağıda) ve `CloneRgb` (yalnızca `r/g/b/ID/codeName` alanlarını kopyalar).

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `Reset()` | Listeyi, yığınları, `Current`'ı ve mod bayraklarını sıfırlar | `MosaicEngine` |
| `TogglePixelEditMode()` | Modu açar/kapar; açılınca kaynak seçim moduna geçer ve yeni `Current` oluşturur | `MainViewModel` |
| `SetSourcePixel(y, x, r, g, b, id, codeName = "")` | `Current.Source`'u ayarlar, hedef moduna geçer | `MainViewModel` |
| `EditPixel(y, x, r, g, b, id, codeName = "")` → `string` | Hedef hücreye kaynak rengi uygular (aşağıdaki kurallar); sonucu açıklayan kısa İngilizce metin döner | `MainViewModel` |
| `UndoLastEdit()` → `string` | Son kaydı geri alır: hücreye `Original` yazılır, kayıt `EditedPixels`'ten çıkar, redo yığınına gider. Yığın boşsa `nothing to undo` | `MainViewModel` |
| `RedoLastEdit()` → `string` | Son geri alınanı yeniden uygular ve `EditedPixels`'e kopyasını ekler. Yığın boşsa `nothing to redo` | `MainViewModel` |

## Önemli davranışlar ve iş kuralları

### `EditPixel` karar tablosu

| Durum | Sonuç | Dönüş metni |
|---|---|---|
| `Current == null` | Hiçbir şey | `no source selected` |
| `Source.ID == Target.ID` | Atlanır | `same color, skipped` |
| Hücre zaten aynı kaynakla düzenlenmiş | Atlanır | `already edited with same source` |
| Hücre düzenlenmiş, yeni kaynak farklı ve `Original` değil | Kayıt güncellenir (`Source`/`Target`), undo'ya eklenir | `replaced #n` |
| Hücre düzenlenmiş, yeni kaynak hücrenin `Original` rengi | Hücre orijinale döner, kayıt silinir (undo'ya eklenmez) | `restored to original` |
| Hücre ilk kez düzenleniyor | `Original = Target` (r/g/b/ID), yeni kayıt eklenir, undo'ya eklenir | `edited pixel (n total)` |

Her başarılı değişiklikte redo yığını temizlenir.

### Yazılan veriler

- `MosaicData.dataM3[y, x, 0..2]` BGR sırasıyla yazılır (`0 = b`, `1 = g`, `2 = r`).
- `drl.dat[y, x, 3]` taş ID'si olur.
- `rsBitmap` güncellemesi `PatchRSRegion` ile yapılır (`UpdateRSForPixel` → `Source`, `RestoreRSForPixel` → `Original`):
  - Renk, `FindCodeNameForColor` ile `MosaicData.arMA`'da aynı RGB'ye sahip girişin kod adına çevrilir; bulunamazsa rengin kendi `codeName`'i kullanılır.
  - `StoneTextureService.FindFolderForCode` ile doku klasörü bulunursa rastgele `1..15.jpg` arasından biri `N×N`'e küçültülüp hücreye kopyalanır; yoksa hücre düz renkle doldurulur.
  - Hem `Rgba8888` hem BGRA bitmap'ler desteklenir; hücre bitmap dışına taşıyorsa işlem atlanır.

## Dikkat / bilinen sınırlamalar

- `PatchRSRegion` `unsafe` kod kullanır ve kaynak/doku bitmap'lerinin piksel başına 4 bayt olduğunu varsayar.
- Hücre `N×N` alanın tamamını boyar; ızgaralı (`showGrid`) üretilmiş bir `rsBitmap`'te ızgara çizgisi o hücrede kaybolur.
- Rastgele doku `new Random().Next(1, 16)` ile seçilir: `16.jpg` hiç kullanılmaz; `MosaicData.arn` güncellenmediği için RS bitmap yeniden üretilince hücrenin dokusu değişebilir.
- "restored to original" dalı undo yığınına eklenmez; daha önce aynı hücre için yığında kalan kayıt geri alındığında hücreye yine `Original` yazılır.
- "replaced" dalı aynı kayıt nesnesini yeniden undo'ya iter; geri alındığında önceki kaynağa değil `Original`'e dönülür.
- Dönüş metinleri yerelleştirilmemiştir (İngilizce sabitler).
- Stoğa göre düzeltme mozaiği değiştirirse `Reset()` çağrılır; elle yapılmış piksel düzenlemeleri ve geri al/yinele geçmişi silinir.

## İlgili dosyalar

- [ProjectService](./ProjectService.md)
- [StoneTextureService](./StoneTextureService.md)
- [MosaicEngine](./MosaicEngine.md)
- [MosaicData](../Models/MosaicData.md), [Rgb](../Models/Rgb.md), [Region](../Models/Region.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
- [MainWindow](../MainWindow.md)
