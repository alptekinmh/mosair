# CompareRunner

> Kaynak: `mosair/CompareRunner.cs` · Güncelleme: 2026-10-04

## Amaç

Arayüz açmadan, bir veya birden çok görüntü üzerinde **üç mozaik algoritmasını yan yana karşılaştıran** geliştirici aracı. Algoritma değişikliklerinin kaliteye etkisini sayısal (ΔE, kenar koruma, açıklık korelasyonu) ve görsel (yan yana PNG) olarak ölçmek için kullanılır. Son kullanıcıya yönelik değildir.

Karşılaştırılan varyantlar:

| Başlık | Nasıl üretilir |
|---|---|
| `WPF` | `MosaicEngine.WpfStyleRemoval = true` ile `MosaicEngine.RunM3` (eski WPF'teki renk eleme davranışı). |
| `mosair` | `WpfStyleRemoval = false` ile `MosaicEngine.RunM3`. |
| `Optimum` | `MosaicEngine.RunOptimal` (optimum taş sayısı paleti). |

## Nerede kullanılır

`Program.Main`, ilk argüman `--compare` ise `CompareRunner.Run(args)` çağırır ve dönüş değerini çıkış kodu yapar.

## Nasıl çalıştırılır

```bash
# Depo kökünden (Release önerilir; RunM3 yavaştır)
dotnet run --project mosair -c Release -- --compare <çıktıKlasörü> <genişlikCm> <görüntü1> [görüntü2 ...]

# Örnek
dotnet run --project mosair -c Release -- --compare ./karsilastirma 100 foto1.jpg foto2.png

# Yayınlanmış ikili ile
./mosair --compare ./karsilastirma 100 foto1.jpg
```

| Argüman | Açıklama |
|---|---|
| `--compare` | Modu seçer (her zaman ilk argüman). |
| `<outDir>` | Çıktı klasörü; yoksa oluşturulur. |
| `<widthCm>` | Mozaik genişliği (cm). `InvariantCulture` ile ayrıştırılır: ondalık ayırıcı **nokta** olmalı (`120.5`). |
| `<image...>` | Bir veya daha fazla görüntü yolu. |

| Ortam değişkeni | Etki |
|---|---|
| `MOSAIR_LW` | Ayarlanırsa `OptimalPaletteService.LightnessWeight` (varsayılan 3.0) bu değerle değiştirilir; Optimum varyantındaki açıklık ağırlığını denemek için. Nokta ondalıklı yazılmalı. |

Çıktılar:

| Dosya | İçerik |
|---|---|
| `<outDir>/<görüntüAdı>_karsilastirma.png` | Dört panel yan yana: `Orijinal`, `WPF · N taş`, `mosair · N taş`, `Optimum · N taş`. Her taş `cell` piksellik kare (`cell = max(2, 420 / max(R, C))`). |
| `<outDir>/rapor.txt` | Konsola yazılan tüm satırlar (UTF-8, BOM'lu): katalog bilgisi, her görüntü için varyant metrikleri ve süreleri, Optimum'un önerdiği `k` (`KOptimal`, `KKnee`, `KThreshold`) ve sonda sekmeyle ayrılmış `SUMMARY` tablosu (`AVERAGE` satırı dahil). |

## Yapı

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `Variant` | `private sealed record` | — | `Title`, `Data` (`byte[,,]`, BGR), `Q` (`MosaicQuality`), `Seconds`. |
| `rgbInc` | `const int` (yerel) | 10 | `RunM3` başlangıç paleti adımı. |
| `target` | `int` (yerel) | — | `RunM3` hedef renk sayısı ≈ taş sayısının %10'u (10'un katına yuvarlanır), `[2, toplam taş]` aralığına ve başlangıç paletinin `steps³ - 1` sınırına kırpılır. |

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `Run(string[] args)` | Argümanları doğrular (en az 4), kataloğu yükler (`ColorCatalogService.LoadDefaultCatalog`), her görüntü için üç varyantı üretir, `MosaicMetrics.Evaluate` ile ölçer, panelleri ve raporu yazar. Başarıda 0, eksik argümanda 1 döner. | `Program.Main` |
| `SavePanels(file, R, C, panels)` (private) | Başlıklı panelleri tek PNG'ye çizer (SkiaSharp). | `Run` |

## Önemli davranışlar ve iş kuralları

- Her varyanttan önce `MosaicEngine.Reset` + `LoadImage` + `CalculateDimensions` çağrılır; motor statik durum kullandığı için varyantlar arası sızıntı böyle önlenir.
- Tüm varyantlar `InterpolationMethod.Area` ve `prepareTextures: false` ile çalışır; taş dokuları üretilmez, yalnızca `MosaicData.dataM3` kopyalanır.
- Metrik anlamları (rapordaki açıklama satırı): ΔE düşük daha iyi, `edgesKept` yüksek daha iyi, `Lcorr` 1'e yakın daha iyi.
- Yüklenemeyen görüntü `[ad] load failed` olarak raporlanır ve atlanır.

## Dikkat / bilinen sınırlamalar

- Proje `OutputType=WinExe` olarak derlenir. Windows'ta GUI alt sistemi konsola bağlanmadığı için `Console.WriteLine` çıktısı terminalde görünmeyebilir; güvenilir çıktı `rapor.txt` dosyasıdır. macOS'ta çıktı normal görünür.
- Katalog `AppContext.BaseDirectory/Assets/colorsBas.txt` yolundan okunur; `dotnet run` ve yayın çıktısı bu dosyayı kopyalar, başka bir klasöre elle kopyalanan ikili ile çalıştırılırsa katalog boş kalır.
- `widthCm` veya `MOSAIR_LW` geçersizse `double.Parse` istisna fırlatır; argüman doğrulaması yalnızca sayı adedini denetler.
- Büyük görüntü/genişlikte `RunM3` iki kez çalıştığı için süre uzundur.
- Panel başlıkları Türkçe sabittir (`Orijinal`, `taş`); yerelleştirilmez.

## İlgili dosyalar

- [Program](Program.md)
- [MosaicEngine](Services/MosaicEngine.md)
- [MosaicMetrics](Services/MosaicMetrics.md)
- [OptimalPaletteService](Services/OptimalPaletteService.md)
- [ColorCatalogService](Services/ColorCatalogService.md)
- [MosaicData](Models/MosaicData.md)
