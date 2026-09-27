# mosairMac

SCARA robot mozaik tas uretim sisteminin **Kullanici odakli** cross-platform versiyonu.

Orijinal WPF uygulamasindan (`mosairWPF`) mozaiklestirme algoritmasinin tamami tasinarak Avalonia UI + SkiaSharp uzerine yeniden insa edilmistir. Robot kontrolu, kamera, seri port, 3D gorsellestime gibi uretim tarafli ozellikler kapsam disidir.

## Kullanici Senaryosu

1. Gorsel yukle
2. Genislik (cm) gir
3. **Mos** butonuna bas
4. Gercek tas dokulu mozaik sonucunu gor
5. PNG/JPEG olarak disa aktar

## Teknoloji

| Katman | Teknoloji |
|--------|-----------|
| UI Framework | Avalonia UI 12.1.2 |
| Goruntu Isleme | SkiaSharp 3.119.4 |
| Runtime | .NET 10 |
| Hedef Platformlar | macOS (ARM64 + x64), Windows x64 |

Orijinal WPF uygulamasi EmguCV 4.5.2 (OpenCV .NET wrapper) kullaniyordu. Tum EmguCV islemleri SkiaSharp ile yeniden yazildi — hicbir native OpenCV bagimliligi yoktur.

## Proje Yapisi

```
mosairMac/                          # Solution root
├── .gitignore
├── README.md
├── mosairMac.sln                   # Solution dosyasi
│
├── docs/                           # Dokumantasyon
│   ├── App.md
│   ├── MainWindow.md
│   ├── MainViewModel.md
│   ├── MosaicEngine.md
│   ├── ImageService.md
│   ├── ColorCatalogService.md
│   ├── ColorMatcher.md
│   ├── StoneTextureService.md
│   ├── MosaicData.md
│   ├── Rgb.md
│   ├── Region.md
│   ├── Np.md
│   ├── Program.md
│   └── mosairMac_csproj.md
│
├── publish/                        # Dagitim ciktilari
│   ├── mosairMac-arm64.zip
│   ├── mosairMac-osx-arm64.tar.gz
│   ├── mosairMac-osx-x64.tar.gz
│   └── mosairMac-x64.zip
│
└── mosairMac/                      # Proje klasoru
    ├── mosairMac.csproj
    ├── Program.cs                  # Giris noktasi
    ├── App.axaml / App.axaml.cs    # Avalonia uygulama tanimlamasi
    ├── MainWindow.axaml / .cs      # Ana pencere (dark theme UI)
    ├── app.manifest
    │
    ├── Models/
    │   ├── Rgb.cs                  # rgb, cooo renk siniflari
    │   ├── MosaicData.cs           # Statik mozaik verileri (dataM1/M3, arMA/arMB, bitmap'ler)
    │   ├── Region.cs               # dr, drl bolge siniflari
    │   └── Np.cs                   # np yardimci sinifi
    │
    ├── Services/
    │   ├── MosaicEngine.cs         # M3 mozaiklestirme akisinin tamami
    │   ├── ColorMatcher.cs         # Renk uzakligi hesaplama, katalog eslestirme
    │   ├── ImageService.cs         # SkiaSharp: yukle, resize, flip, overlay, export
    │   ├── ColorCatalogService.cs  # colorsBas.txt renk katalogu yonetimi
    │   ├── StoneTextureService.cs  # Gercek tas doku yukleme ve render
    │   ├── PixelEditService.cs     # sourcePix/targetPix piksel duzenleme
    │   └── ProjectService.cs       # Proje kaydet/yukle (.mos)
    │
    ├── ViewModels/
    │   └── MainViewModel.cs        # MVVM ViewModel, async mozaik islemi
    │
    └── Assets/
        ├── colorsBas.txt           # 124 tas rengi katalogu
        └── 02_RS/                  # Gercek tas doku fotograflari (124 klasor, ~2000 JPG)
```

## M3 Mozaiklestirme Algoritmasi

M3, iteratif renk indirgemesi yapan cekirdek algoritmadir. Akisi:

```
Mos butonu
  → CreateSingleRegion()        # Tum gorseli tek bolge yap
  → GenerateInitialPalette()    # RGBInc adimlarla tum RGB kombinasyonlarini uret
  → RunM1()                     # Ilk eslestirme (her pikseli en yakin renge esle)
  → CopyM1ToM3()                # M1 sonuclarini M3'e kopyala
  → while (renkSayisi > rgbM):  # Iteratif renk indirgemesi
      RemoveMinimalColors()     #   Az pikselli renkleri sil
      ProcessM3()               #   Kalan renklerle yeniden esle
  → Katalog rengi atama         # Her ara rengi en yakin tas rengine esle
  → Ayni renkleri birlestir     # uc kodlarini ata
  → Gercek tas doku render      # 02_RS fotograflariyla final bitmap olustur
```

### rgbM Hesaplamasi

`rgbM` (hedef ara renk sayisi) Mos butonuna basildiginda otomatik hesaplanir:

```
rgbM = floor(0.1 × genislik × yukseklik / 10) × 10
```

Ornek: 78×156 = 12168 tas → %10'u = 1216 → 10'a yuvarla → **rgbM = 1210**

### Gercek Tas Doku Sistemi

Mozaiklestirme sonrasi her piksel pozisyonuna gercek tas fotograflari yerlestirilir:

1. `02_RS` klasorundeki her tas tipi icin 16 farkli JPG foto yuklenir
2. Fotograflar N×N piksele boyutlandirilir (N=20, area-weighted interpolation)
3. Her piksel icin rastgele bir foto secilir (1-15 arasi, orijinalle ayni)
4. N×N bloklar buyuk bitmap'e yerlestirilir → final gercek tas gorunumu

### BGR vs RGB

Dahili veri dizileri (`dataM1`, `dataM3`, `dataRS`) BGR siralamasinda calisir — orijinal EmguCV davranisiyla uyumlu. Donusum yalnizca sinir noktalarinda yapilir:
- `ImageService.ToByteArray()`: SKBitmap → BGR byte[,,]
- `ImageService.FromByteArray()`: BGR byte[,,] → SKBitmap (Rgba8888)

### Area-Weighted Resize

OpenCV `Inter.Area` interpolasyonu SkiaSharp'ta mevcut degildir. `ImageService.Resize()` icinde area-weighted averaging manuel olarak implement edilmistir (unsafe pointer erisimi ile yuksek performans).

## Varsayilan Parametreler

| Parametre | Deger | Aciklama |
|-----------|-------|----------|
| Genislik | 93.6 cm | Tas boyutlandirma referansi |
| rgbInc | 10 | Baslangic renk adimi |
| minRgbInc | 2 | Minimum renk silme artisi |
| rgbM | 15 → otomatik | Mos'a basilinca hesaplanir |
| N | 20 | Tas doku buyutme katsayisi |

## Derleme ve Calistirma

```bash
# Gelistirme
cd mosairMac
dotnet build
dotnet run --project mosairMac

# Release build
dotnet build -c Release
```

## Dagitim (Publish)

```bash
# macOS ARM64 (Apple Silicon — M1/M2/M3)
dotnet publish mosairMac -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true -o publish/osx-arm64

# macOS x64 (Intel)
dotnet publish mosairMac -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true -o publish/osx-x64

# Windows x64
dotnet publish mosairMac -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/win-x64
```

Cikti boyutlari (self-contained, .NET runtime dahil):

| Platform | Boyut |
|----------|-------|
| macOS ARM64 | ~121 MB |
| macOS x64 | ~114 MB |
| Windows x64 | ~214 MB |

### macOS Kurulum

```bash
tar -xzf mosairMac-osx-arm64.tar.gz -C mosairMac
chmod +x mosairMac/mosairMac
./mosairMac/mosairMac
```

Ilk acilista "tanimlanamayan gelistirici" uyarisi gelirse: **System Settings → Privacy & Security → Open Anyway**.

## Orijinal Projeden Goc

| Kaynak (WPF) | Hedef (Avalonia) | Islem |
|---------------|------------------|-------|
| `picture/im.cs` | `Services/MosaicEngine.cs` + `Services/ColorMatcher.cs` + `Services/ImageService.cs` | M3 akisi, renk eslestirme, gorsel I/O ayrildi |
| `picture/lum.cs` | `Models/MosaicData.cs` + `Models/Rgb.cs` + `Models/Region.cs` | Veri modelleri temizlendi |
| `picture/np.cs` | `Models/Np.cs` | Degisiklik yok |
| `picture/realStone.cs` | `Services/StoneTextureService.cs` | Gercek tas doku sistemi yeniden yazildi |
| `picture/editedPixel.cs` | `Services/PixelEditService.cs` | Piksel duzenleme yeniden yazildi |
| `picture/savep.cs` | `Services/ProjectService.cs` | Proje kaydet/yukle yeniden yazildi |
| `colorsBas.txt` | `Assets/colorsBas.txt` | Degisiklik yok |
| `02_RS/` klasoru | `Assets/02_RS/` | Kopyalandi |
| EmguCV (OpenCV) | SkiaSharp | Tum gorsel islemler yeniden yazildi |
| WPF UI | Avalonia AXAML | Dark theme, zoom destegi ile yeniden tasarlandi |
| `gc<T>.DeepCopy` (BinaryFormatter) | Manuel klon metodlari | .NET 10 uyumlu |

### Tasinmayan Dosyalar

Robot kontrolu, kamera, seri port, 3D gorsellestime, Word export, Windows Registry islemleri — Kullanici uygulamasi icin gereksiz oldugu icin alinmamistir.

## Gereksinimler

- .NET 10 SDK (gelistirme icin)
- Dagitim icin runtime gerekmez (self-contained)
