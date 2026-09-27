# mosairMac - Dokumantasyon

## Proje Hakkinda

**mosairMac**, dogal tas mozaik uretim surecinde kullanilan musteri odakli bir masaustu uygulamadir. Gorsel yukleme, mozaiklestirme ve gercek tas dokulu cikti uretmeye odaklanir.

**Platform:** Avalonia UI (.NET 10)
**Goruntu Isleme:** SkiaSharp 3.119.4
**Hedef:** macOS (ARM64, x64), Windows x64

---

## Dokumantasyon Haritasi

| Dosya | Icerik |
|-------|--------|
| [App.md](App.md) | Avalonia uygulama giris noktasi ve tema yapisi |
| [Program.md](Program.md) | Program.cs giris noktasi |
| [MainWindow.md](MainWindow.md) | Ana pencere AXAML yapisi ve event handler'lar |
| [MainViewModel.md](MainViewModel.md) | MVVM ViewModel, property'ler ve komutlar |
| [MosaicEngine.md](MosaicEngine.md) | M3 mozaiklestirme algoritmasi |
| [ColorMatcher.md](ColorMatcher.md) | Renk uzakligi hesaplama ve katalog eslestirme |
| [ColorCatalogService.md](ColorCatalogService.md) | colorsBas.txt renk katalogu yonetimi |
| [ImageService.md](ImageService.md) | SkiaSharp gorsel islemleri (resize, overlay, export) |
| [StoneTextureService.md](StoneTextureService.md) | Gercek tas doku yukleme ve RS bitmap render |
| [PixelEditService.md](PixelEditService.md) | Piksel duzenleme, undo/redo servisi |
| [ProjectService.md](ProjectService.md) | .mos proje kaydet/yukle servisi |
| [MosaicData.md](MosaicData.md) | Statik veri yapilari (dataM1, dataM3, arMA, bitmap'ler) |
| [Rgb.md](Rgb.md) | rgb, cooo renk sinif tanimlari |
| [Region.md](Region.md) | dr, drl bolge sinif tanimlari |
| [mosairMac_csproj.md](mosairMac_csproj.md) | Proje dosyasi ve bagimliliklar |

---

## Klasor Yapisi

```
mosairMac/                    # Solution root
├── mosairMac.sln
├── docs/                     # Bu klasor
└── mosairMac/                # Proje klasoru
    ├── Models/               # Veri modelleri
    ├── Services/             # Is mantigi servisleri
    ├── ViewModels/           # MVVM ViewModel
    └── Assets/               # Renk katalogu ve tas dokulari
```
