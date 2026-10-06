# mosair — Belgeler

**mosair**, doğal taş mozaik üretimi için bir masaüstü uygulamasıdır. Bir görseli katalogdaki gerçek taş renklerine dönüştürür ve sonucu gerçek taş dokularıyla gösterir. Piksel ve taş düzenlemeye izin verir, Google Sheets stok tablosuyla çalışır, projeleri bir Google Drive klasörüne kaydedip oradan açabilir. Projeleri robot tarafındaki WPF uygulamasıyla ortak JSON biçiminde kaydeder.

| | |
|---|---|
| Platform | Avalonia UI 12 · .NET 10 |
| Görüntü işleme | SkiaSharp 3 |
| Hedefler | Windows x64, macOS x64, macOS ARM64 |
| Güncelleme | 2026-10-06 |

## Nereden başlamalı?

| Kim için | Belge |
|---|---|
| Uygulamayı kullanan herkes | **[ARAYUZ.md](ARAYUZ.md)**: arayüzde yapılabilecek her şey tek dosyada |
| Kodu geliştiren | Aşağıdaki **kod belgeleri** ve [mimari özet](#mimari-özet) |
| Sürüm çıkaran | [kod/build-workflow.md](kod/build-workflow.md) |
| "Stoğa göre" kararının geçmişini merak eden | [STOGA_GORE_RAPOR.md](STOGA_GORE_RAPOR.md): özelliğin deneme dalındayken hazırlanan durum ve entegrasyon raporu (tarihsel; güncel davranış için ARAYUZ.md) |

## Kod belgeleri (`docs/kod/`)

Her kaynak dosyanın bir sayfası vardır; klasör yapısı `mosair/` ile aynıdır.

### Uygulama kabuğu

| Belge | Kaynak | İçerik |
|---|---|---|
| [App.md](kod/App.md) | `App.axaml(.cs)` | Uygulama, tema kaynakları, ana pencereyi açma |
| [Program.md](kod/Program.md) | `Program.cs` | Giriş noktası, `--compare` modu |
| [MainWindow.md](kod/MainWindow.md) | `MainWindow.axaml(.cs)` | Pencere yerleşimi, bütün olay işleyiciler, kısayollar, kaydet/dışa aktar klasörleri |
| [HelpWindow.md](kod/HelpWindow.md) | `HelpWindow.axaml(.cs)` | Uygulama içi kullanım kılavuzu (TR/EN) |
| [CompareRunner.md](kod/CompareRunner.md) | `CompareRunner.cs` | Algoritma karşılaştırma aracı (geliştirici) |
| [StockCompareRunner.md](kod/StockCompareRunner.md) | `StockCompareRunner.cs` | Stoğa göre düzeltme karşılaştırma aracı (geliştirici) |
| [mosair.csproj.md](kod/mosair.csproj.md) | `mosair.csproj` | Paketler, varlıklar (renk kataloğu, taş dokuları) |
| [build-workflow.md](kod/build-workflow.md) | `.github/workflows/build.yml` | Etiketle sürüm derleme ve yayınlama |

### ViewModel

| Belge | İçerik |
|---|---|
| [MainViewModel.md](kod/ViewModels/MainViewModel.md) | Arayüz durumu ve komutlar: görsel, Mos, Optimum, katalog, stok, Google Drive, piksel düzenleme, özellikler paneli |

### Servisler

| Belge | İçerik |
|---|---|
| [MosaicEngine.md](kod/Services/MosaicEngine.md) | Mozaikleştirme algoritması (M1/M3), ölçü hesabı |
| [OptimalPaletteService.md](kod/Services/OptimalPaletteService.md) | Optimum taş sayısını bulma |
| [MosaicMetrics.md](kod/Services/MosaicMetrics.md) | Mozaik kalite ölçütleri |
| [ColorMatcher.md](kod/Services/ColorMatcher.md) | Renk uzaklığı ve katalog eşleştirme |
| [ColorCatalogService.md](kod/Services/ColorCatalogService.md) | `colorsBas.txt` renk kataloğu |
| [GamutMapper.md](kod/Services/GamutMapper.md) | Gamut eşleme yardımcıları |
| [ImageService.md](kod/Services/ImageService.md) | Görsel yükleme, yeniden boyutlandırma, dışa aktarma |
| [StoneTextureService.md](kod/Services/StoneTextureService.md) | Gerçek taş dokularını yükleme, çizim anlık görüntüsü kurma |
| [MosaicRenderSource.md](kod/Services/MosaicRenderSource.md) | Taş dokulu görüntüyü (RS) istenen bölge ve detayda çizme (ekran karoları, dışa aktarma) |
| [MosaicExporter.md](kod/Services/MosaicExporter.md) | Dışa aktarma: seçilen görüntü kalitesiyle (taş başına piksel) boyut sınırı olmadan JPEG/PNG yazma (büyük PNG akışla, büyük JPEG tek tamponla), dosya boyutu tahmini |
| [PixelEditService.md](kod/Services/PixelEditService.md) | Piksel düzenleme, geri al/yinele |
| [ProjectService.md](kod/Services/ProjectService.md) | `.mos` JSON proje biçimi, WPF uyumu |
| [StockSheetService.md](kod/Services/StockSheetService.md) | Google Sheets stok işlemleri |
| [DriveService.md](kod/Services/DriveService.md) | Google Drive proje klasörü: Apps Script (`Assets/mosair-drive.gs`) üzerinden `.mos` ve orijinal görseli proje klasörüne kaydetme, listeleme, önizleme, indirme |
| [Loc.md](kod/Services/Loc.md) | TR/EN metinler, kısayol yazıları |
| [StockAwareAssigner.md](kod/Services/StockAwareAssigner.md) | Stoğa göre düzeltme algoritması (min-cost flow ile stoğa sığdırma) |
| [NewImageWatcher.md](kod/Services/NewImageWatcher.md) | İndirilenler ve Masaüstü klasörlerine gelen yeni JPEG/PNG dosyalarını fark etme ("mosair'de açılsın mı?" bildirimi) |
| [WorkCancellation.md](kod/Services/WorkCancellation.md) | İptal düğmesinin belirteci: Mos, stoğa göre düzeltme ve dışa aktarmanın uzun döngülerini kontrol noktalarında durdurma |

### Modeller, kontroller, dönüştürücüler

| Belge | İçerik |
|---|---|
| [Models/Rgb.md](kod/Models/Rgb.md) | Taş/renk modeli |
| [Models/Region.md](kod/Models/Region.md) | Bölge modelleri |
| [Models/MosaicData.md](kod/Models/MosaicData.md) | Paylaşılan mozaik verisi |
| [Controls/AlertDialog.md](kod/Controls/AlertDialog.md) | Bilgi iletişim kutusu |
| [Controls/ConfirmDialog.md](kod/Controls/ConfirmDialog.md) | Evet/Hayır iletişim kutusu |
| [Controls/StockSettingsDialog.md](kod/Controls/StockSettingsDialog.md) | Stok ayarları penceresi |
| [Controls/DriveSettingsDialog.md](kod/Controls/DriveSettingsDialog.md) | Google Drive ayarları penceresi (klasör bağlantısı, Script URL, kurulum adımları, bağlantı denemesi) |
| [Controls/DriveOpenDialog.md](kod/Controls/DriveOpenDialog.md) | Drive proje tarayıcısı: önizlemeli kartlar, arama, sıralama, yenile, Drive'da göster |
| [Controls/MosaicView.md](kod/Controls/MosaicView.md) | Mos'tan sonra taş dokulu mozaiği karolarla gösterme: yalnızca görünen kısım, zoom'a göre detay |
| [Controls/GridOverlay.md](kod/Controls/GridOverlay.md) | Izgara çizimi |
| [Controls/ActivityWave.md](kod/Controls/ActivityWave.md) | Durum çubuğundaki işlem dalgası animasyonu |
| [Converters/InvariantDoubleConverter.md](kod/Converters/InvariantDoubleConverter.md) | cm kutusu için sayı dönüştürücü |

## Mimari özet

```
MainWindow (görünüm, olaylar) ──bağlama──► MainViewModel (durum, komutlar)
                                              │
     ┌───────────────┬───────────────┬────────┴───────┬─────────────────┬────────────────┐
 MosaicEngine   OptimalPalette    PixelEdit      ProjectService   StockSheetService  StoneTexture
 (+ColorMatcher, Service           Service        (.mos JSON,      (Google Sheets)    Service
  ImageService)  (+MosaicMetrics)                  WPF uyumu)                         (RS görüntü)
     └───────────────┴───────────────┴────────────────┴──────────── MosaicData (paylaşılan veri)
```

- **Veri** statik `MosaicData` ve `drl` yapılarında tutulur; servisler bunları doğrudan okur ve yazar.
- **Yön:** mosair görüntü dizilerini çevirmeden tutar. WPF ise yatay aynalı tutar. Dosyadaki `Source = "mosair"` işareti dosyanın hangi yönde olduğunu belirtir ([ProjectService.md](kod/Services/ProjectService.md)).
- **Taş dokulu görüntü (RS)** bellekte hiçbir zaman bütün olarak tutulmaz. `StoneTextureService.CreateRenderSource` mozaik başına bir [MosaicRenderSource](kod/Services/MosaicRenderSource.md) kurar; ekranda [MosaicView](kod/Controls/MosaicView.md) yalnızca görünen kısmı arka planda çizilen karolarla gösterir, dışa aktarma ([MosaicExporter](kod/Services/MosaicExporter.md)) aynı çiziciyle bütün görüntüyü üretir; tek bitmap'e sığmayan görüntüyü taş satırı taş satırı çizip dosyaya yazar.
- **Metinler** `Loc` üzerinden gelir; dil değiştirilince anında güncellenir.

## Belgeler nasıl güncel kalır?

- **Kural:** Kökteki [`CLAUDE.md`](../CLAUDE.md) dosyasında yazılıdır. Kodda veya arayüzde yapılan her değişiklik, ilgili `docs/kod/` sayfasını, `ARAYUZ.md`'yi ve uygulama içi kılavuzu **aynı commit'te** günceller.
- **Otomatik kontrol:** [`docs-check`](../.github/workflows/docs-check.yml) iş akışı her push'ta çalışır ve belge sayfası olmayan kaynak dosyaları hata olarak bildirir.
- **İçerik doğruluğu:** Belgelerin içeriğinin doğru olup olmadığını otomatik kontrol denetlemez; bu, gözden geçirme ile sağlanır.
