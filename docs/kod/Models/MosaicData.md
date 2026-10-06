# MosaicData

> Kaynak: `mosair/Models/MosaicData.cs` · Güncelleme: 2026-10-06

## Amaç

Uygulamanın tüm mozaik durumunu tutan **statik** veri deposu: renk katalogları, aşama aşama piksel dizileri, bölge bazlı renk listeleri ve SkiaSharp bitmap'leri. Servisler bu alanları doğrudan okuyup yazar; ayrı bir durum nesnesi yoktur (WPF sürümündeki global yapı korunmuştur).

## Nerede kullanılır

`ColorCatalogService`, `MosaicEngine`, `PixelEditService`, `ProjectService`, `StoneTextureService`, `MainViewModel` ve `CompareRunner`. Hangi alanın kimde kullanıldığı aşağıdaki tabloda.

## Yapı

Piksel dizileri `[satır, sütun, kanal]` düzenindedir ve kanal sırası **BGR**'dir (`[...,0]` = mavi, `[...,2]` = kırmızı). Satır/sütun sayısı taş sayısıdır (1 hücre = 1 taş).

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `arRGBAll` | `List<rgb>` | boş | Tüm katalog (`Assets/colorsBas.txt`). `ColorCatalogService.LoadCatalog` doldurur. |
| `arcs` | `List<bool>` | boş | Katalogla aynı sırada "hariç tutuldu" bayrakları (`boolLeaveOut` aynası). Proje dosyasına yazılır. |
| `arRGB` | `List<rgb>` | boş | Aktif (hariç tutulmamış) katalog renkleri. `ColorCatalogService.SetActiveColors` yeniden kurar. |
| `dataM1` | `byte[,,]` | `[3,3,3]` | İlk eşleştirme aşamasının sonucu (her piksel en yakın katalog rengine). |
| `dataM3` | `byte[,,]` | `[3,3,3]` | Nihai mozaik. Ekranda gösterilen, kaydedilen ve dokulandırılan dizi. |
| `dataM3F` | `byte[,,]` | `[3,3,3]` | Kaydedilir/yüklenir/sıfırlanır; hesaplamada kullanılmaz. |
| `dataM3Backup` | `byte[,,]` | `[3,3,3]` | Renk indirgeme sonrası `dataM3` yedeği (`MosaicEngine`). |
| `arMA` | `List<List<rgb>>` | boş | Bölge başına çalışma paleti (indirgeme bu listede yapılır). Dokular ve piksel düzenleme kaynağı. |
| `arMB` | `List<List<rgb>>` | boş | `arMA`'nın derin kopyası; birleştirme/numaralandırma adımları burada. |
| `arMBR` | `List<List<rgb>>` | boş | `arMB`'nin kopyası (referans/yedek). |
| `reducedBitmap` | `SKBitmap?` | `null` | Kaynak görüntünün taş ızgarasına küçültülmüş hali / ara sonuç. |
| `exportBitmap` | `SKBitmap?` | `null` | Düz renkli mozaik bitmap'i (dokusuz). |
| `inputBitmap` | `SKBitmap?` | `null` | Yüklenen orijinal görüntü. |
| `N` | `int` | 40 | Taş dokulu görüntüde (RS) bir taşın piksel boyutu ("Detay"). `MainViewModel.StonePixelSize` ayarlar. |
| `arn` | `int[]` | `new int[3]` | Hücre başına doku varyantı indeksi (`R*C` uzunluk). `StoneTextureService.PopulateRandomIndices` yeniden ayırır. |

Taş dokulu görüntü (RS) bu sınıfta tutulmaz (eski `rsBitmap` alanı kaldırıldı). Görüntünün tamamı hiçbir zaman bellekte durmaz: ekranda [MosaicView](../Controls/MosaicView.md) görünen kısmı karolarla, dışa aktarma ise bütün görüntüyü [MosaicRenderSource](../Services/MosaicRenderSource.md) ile `dataM3`, `arn` ve paletten çizer.

## Public API

Metot yoktur; yalnızca `public static` alanlar.

| Alan grubu | Yazan | Okuyan |
|---|---|---|
| `arRGBAll`, `arcs`, `arRGB` | `ColorCatalogService`, `ProjectService` | `MosaicEngine`, `ColorMatcher`, `MainViewModel`, `CompareRunner` |
| `dataM1`, `dataM3`, `dataM3Backup` | `MosaicEngine`, `ProjectService`, `PixelEditService` (`dataM3`) | `StoneTextureService`, `MosaicRenderSource` (`dataM3`), `MainViewModel`, `CompareRunner` |
| `arMA`, `arMB`, `arMBR` | `MosaicEngine`, `ProjectService` | `StoneTextureService`, `MosaicRenderSource` (`arMA`, `arMB`), `MainViewModel` |
| Bitmap'ler | `MosaicEngine`, `MainViewModel` | `MainViewModel` |
| `N`, `arn` | `MainViewModel`, `StoneTextureService`, `ProjectService` | `StoneTextureService`, `MosaicRenderSource` (`arn`), `MainViewModel` |

## Önemli davranışlar ve iş kuralları

- `MosaicEngine.Reset` dizileri `[3,3,3]`'e döndürür, listeleri temizler ve bitmap'leri `Dispose` edip `null` yapar. Yeni görüntü yüklemek (`MainViewModel.LoadImage`) her zaman `Reset` ile başlar.
- Bitmap'ler değiştirilirken önce eskisi `Dispose` edilir (`MosaicData.exportBitmap?.Dispose()` kalıbı). Yeni kod bu kalıbı izlemeli, aksi halde yerel bellek sızar.
- Optimum modunda (`RunOptimal` → `ApplyOptimalK`) tek bölgeli palet doğrudan `arMB` olarak kurulur, `arMA` onun kopyasıdır.
- `.mos` dosyası bu sınıftaki alanların neredeyse tamamını düzleştirerek saklar (bkz. `ProjectService`).

## Dikkat / bilinen sınırlamalar

- Tamamen statik ve iş parçacığı güvenli değil; mozaik hesaplanırken arayüzden bu alanlara yazılmamalıdır (`MainViewModel.IsProcessing` korumasına güvenilir).
- `arn` varsayılanı `new int[3]` olup hücre sayısıyla uyuşmaz; doku üretilmeden önce `PopulateRandomIndices` çağrılmış olmalıdır. `MainViewModel` bu alanı `null` da yapabiliyor (alan tipi null kabul etmez).
- `dataM3F` işlevsiz bir kalıntıdır ama `.mos` uyumluluğu için kaldırılmamalıdır.
- Kanal sırası BGR'dir; SkiaSharp `SKColor` oluştururken `r = [..,2]`, `b = [..,0]` kullanılmalıdır.

## İlgili dosyalar

- [Rgb](Rgb.md)
- [Region](Region.md)
- [MosaicEngine](../Services/MosaicEngine.md)
- [ColorCatalogService](../Services/ColorCatalogService.md)
- [StoneTextureService](../Services/StoneTextureService.md), [MosaicRenderSource](../Services/MosaicRenderSource.md)
- [ProjectService](../Services/ProjectService.md)
- [PixelEditService](../Services/PixelEditService.md)
- [MainViewModel](../ViewModels/MainViewModel.md)
