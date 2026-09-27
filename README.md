# mosair

SCARA robot mozaik tas uretim sistemi — cross-platform musteri uygulamasi.

Gorsel yukle, genisligi gir, **Mos** butonuna bas, gercek tas dokulu mozaik sonucunu gor.

---

## Indir ve Calistir

Isletim sisteminize uygun dosyayi indirin, zipi acin ve calistirin.
**.NET kurulumu gerekmez.**

| Platform | Indir |
|----------|-------|
| **Windows (x64)** | [mosair - Windows](https://github.com/alptekinmh/mosair/releases/latest/download/mosairMac-win-x64.zip) |
| **macOS Intel (x64)** | [mosair - macOS Intel](https://github.com/alptekinmh/mosair/releases/latest/download/mosairMac-osx-x64.zip) |
| **macOS Apple Silicon (M1/M2/M3/M4)** | [mosair - macOS ARM](https://github.com/alptekinmh/mosair/releases/latest/download/mosairMac-osx-arm64.zip) |

> Tum surumler icin [Releases](https://github.com/alptekinmh/mosair/releases) sayfasina bakin.

---

## Kurulum

### Windows

1. Zip dosyasini indirin ve acin
2. `mosair.exe` dosyasini calistirin

### macOS

1. Zip dosyasini indirin ve acin
2. Terminal'de calistirma izni verin:
   ```bash
   chmod +x mosair
   ./mosair
   ```
3. Ilk acilista "tanimlanamayan gelistirici" uyarisi gelirse:
   **System Settings → Privacy & Security → Open Anyway**

---

## Kullanim

1. Gorsel yukle
2. Genislik (cm) gir
3. **Mos** butonuna bas
4. Gercek tas dokulu mozaik sonucunu gor
5. PNG/JPEG olarak disa aktar

---

## Teknoloji

| | |
|---|---|
| UI | Avalonia UI 12 |
| Goruntu Isleme | SkiaSharp |
| Runtime | .NET 10 (self-contained) |
| Platformlar | Windows x64, macOS x64, macOS ARM64 |

---

*ESCRobotics © 2026*
