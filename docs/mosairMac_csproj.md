# mosairMac.csproj

## Genel Bakis

.NET proje dosyasi. Uygulamanin hedef framework'u, NuGet bagimliliklari, derleme ayarlari ve asset yapilandirmasini tanimlar. Cross-platform (macOS + Windows) self-contained dagitim icin yapilandirilmistir.

## Proje Ayarlari

| Ayar | Deger | Aciklama |
|------|-------|----------|
| `OutputType` | WinExe | GUI uygulamasi (konsol penceresi acilmaz) |
| `TargetFramework` | net10.0 | .NET 10 |
| `Nullable` | enable | Null referans analizi aktif |
| `AllowUnsafeBlocks` | true | `ImageService.Resize()`, `ToByteArray()`, `FromByteArray()` ve `StoneTextureService` icindeki pointer erisimi icin gerekli |
| `ApplicationManifest` | app.manifest | Yalnizca Windows'ta (`IsOSPlatform('Windows')` kosulu ile) |
| `AssemblyName` | mosairMac | Cikti dosya adi |
| `Version` | 1.0.0 | Uygulama versiyonu |
| `Description` | Mozaik Tas Uretim Sistemi — Musteri Uygulamasi | Assembly aciklamasi |

## NuGet Bagimliliklari

| Paket | Versiyon | Kullanim |
|-------|---------|----------|
| `Avalonia` | 12.1.2 | UI framework cekirdegi |
| `Avalonia.Desktop` | 12.1.2 | Masaustu platform destegi (Windows, macOS, Linux) |
| `Avalonia.Themes.Fluent` | 12.1.2 | Fluent Design tema paketi |
| `Avalonia.Fonts.Inter` | 12.1.2 | Inter yazi tipi |
| `SkiaSharp` | 3.119.4 | Goruntu isleme (resize, bitmap manipulasyonu, disa aktarma) |
| `AvaloniaUI.DiagnosticsSupport` | 2.2.3 | Yalnizca Debug modunda; F12 ile DevTools penceresi |

## Asset Yapilandirmasi

### colorsBas.txt
```xml
<None Update="Assets\colorsBas.txt">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```
124 satir iceren tas rengi katalogu dosyasi. Her satirda: R G B CodeName Name bilgisi.

### 02_RS (Gercek Tas Doku Fotograflari)
```xml
<Content Include="Assets\02_RS\**\*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <Link>Assets\02_RS\%(RecursiveDir)%(Filename)%(Extension)</Link>
</Content>
```
- 124 klasor, ~2013 JPG dosya, toplam ~15MB
- `**\*` ile recursive olarak tum alt klasor ve dosyalar dahil edilir
- `Link` ile cikti dizininde `Assets/02_RS/` altina duz kopyalanir
- `PreserveNewest` ile yalnizca degisen dosyalar kopyalanir

## Dagitim (Publish) Komutlari

```bash
# macOS ARM64 (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true

# macOS x64 (Intel)
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true

# Windows x64
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

| Platform | Cikti Boyutu | Aciklama |
|----------|-------------|----------|
| osx-arm64 | ~121 MB | Apple M1/M2/M3 islemciler |
| osx-x64 | ~114 MB | Intel Mac'ler |
| win-x64 | ~214 MB | Windows 64-bit |

Self-contained: .NET runtime dahildir, hedef makinede SDK kurulumu gerekmez.

## Diger Dosyalarla Iliskisi

| Dosya | Iliski |
|-------|--------|
| `Program.cs` | WinExe OutputType bu dosyanin giris noktasi olmasini saglar |
| `Services/ImageService.cs` | AllowUnsafeBlocks bu servisin unsafe pointer erisimi icin gerekli |
| `Services/StoneTextureService.cs` | 02_RS Content include bu servisin doku dosyalarina erismesini saglar |
| `Services/ColorCatalogService.cs` | colorsBas.txt asset'i bu servis tarafindan okunur |
| `Assets/` | Tum runtime asset'lerin kaynak konumu |

## Orijinal WPF Karsiligi

WPF versiyonu .NET Framework 4.7.2 hedefliyordu ve EmguCV 4.5.2 (OpenCV .NET wrapper) bagimliligi vardi. Bu projede EmguCV tamamen cikarilmis, yerine SkiaSharp 3.119.4 konulmustur. Avalonia UI, WPF'nin cross-platform alternatifi olarak secilmistir.
