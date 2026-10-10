# build-workflow

> Kaynak: `.github/workflows/build.yml` · Güncelleme: 2026-10-06

## Amaç

GitHub Actions ile Windows ve macOS (Intel + Apple Silicon) için kendi kendine yeterli (self-contained) derlemeler üretip bunları bir GitHub Release'e yükleyen iş akışı ("Build & Release").

## Nerede kullanılır

`v` ile başlayan bir etiket (tag) gönderildiğinde otomatik çalışır:

```bash
git tag v1.2.0
git push origin v1.2.0
```

Normal `push`/PR'lerde çalışmaz; elle tetikleme (`workflow_dispatch`) tanımlı değildir.

## Yapı

### Tetikleyici ve izinler

| Ad | Değer | Açıklama |
|---|---|---|
| `on.push.tags` | `'v*'` | Yalnızca etiket gönderimi. |
| `permissions.contents` | `write` | Release oluşturmak için. |

### `build` işi (matris)

| `os` | `rid` | `artifact` | Çıktı |
|---|---|---|---|
| `windows-latest` | `win-x64` | `win-x64` | `win-x64.zip` |
| `macos-latest` | `osx-x64` | `osx-x64` | `osx-x64.dmg` |
| `macos-latest` | `osx-arm64` | `osx-arm64` | `osx-arm64.dmg` |

Adımlar:

| # | Adım | Ne yapar |
|---|---|---|
| 1 | `actions/checkout@v4` | Kaynağı alır. |
| 2 | `actions/setup-dotnet@v4` | `.NET 10.0.x`. |
| 3 | Set version from tag | `VERSION=${GITHUB_REF_NAME#v}` (örn. `v1.2.0` → `1.2.0`). |
| 4 | Publish | `dotnet publish mosair/mosair.csproj -c Release -r <rid> --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:Version=$VERSION -o out` |
| 5 | Zip (Windows) | `Compress-Archive out/*` → `<artifact>.zip`. |
| 6 | Build .app bundle (macOS) | `mosair.app/Contents/MacOS` içine `out/` kopyalanır; `Info.plist` `Contents/` altına taşınır; `plutil` ile `CFBundleVersion` ve `CFBundleShortVersionString` = `$VERSION`; `sips` + `iconutil` ile `mosair-icon.png`'den `mosair.icns`; `chmod +x`; ad-hoc imza. |
| 7 | Create DMG (macOS) | `mosair.app` + `/Applications` kısayolu → `hdiutil create ... -format UDZO <artifact>.dmg`. |
| 8 | `actions/upload-artifact@v4` | zip/dmg yüklenir (`if-no-files-found: error`). |

### `release` işi

| # | Adım | Ne yapar |
|---|---|---|
| 1 | Checkout | Sürüm notlarını okuyabilmek için. |
| 2 | Find release notes | `.github/release-notes/<etiket>.md` (örn. `.github/release-notes/v1.2.0.md`) varsa yolunu çıktı olarak verir. |
| 3 | `actions/download-artifact@v4` | Tüm matris çıktılarını `artifacts/` altına indirir. |
| 4 | `softprops/action-gh-release@v2` | Release oluşturur: `body_path` = bulunan notlar, `generate_release_notes: true`, dosyalar `artifacts/**/*.zip` ve `artifacts/**/*.dmg`. |

## Public API

Yoktur.

## Önemli davranışlar ve iş kuralları

- **Sürüm etiketten gelir:** csproj'daki `<Version>` ve `Info.plist` içindeki sürümler CI'da etiket değeriyle ezilir. Etiket biçimi `vX.Y.Z` olmalıdır.
- **Sürüm notları:** Etiketi göndermeden önce `.github/release-notes/<etiket>.md` dosyası eklenmelidir (mevcut dosyalar: `v1.1.1.md`, `v1.3.1.md`, `v1.3.2.md`). Dosya yoksa release yalnızca GitHub'ın otomatik notlarıyla oluşur.
- **Ad-hoc imza:** `codesign --force --deep --sign - mosair.app` ve ardından `codesign --verify --deep --strict`. Apple Developer hesabı ve notarization yoktur; amaç macOS'un "hasarlı" uyarısı yerine "Yine de Aç" (Open Anyway) ile açılabilmesidir. Kullanıcı ilk açılışta Gatekeeper uyarısını Sistem Ayarları → Gizlilik ve Güvenlik üzerinden onaylamalıdır.
- **Tek dosya yayın:** `PublishSingleFile` + `IncludeNativeLibrariesForSelfExtract` ile yerel kütüphaneler (SkiaSharp) ikiliye gömülür; `Assets/` içeriği (`Content`) ikilinin yanında ayrı dosyalar olarak kalır.
- Windows zip'i imzasızdır; SmartScreen uyarısı beklenir.

## Dikkat / bilinen sınırlamalar

- Mevcut bir sürümü düzeltmek için aynı etiketi silip yeniden göndermek iş akışını tekrar çalıştırır; `softprops/action-gh-release` var olan release'e dosyaları ekler/günceller.
- `Info.plist` yalnızca macOS üzerinde derlenirken çıktıya girer (csproj koşulu); `.app` adımı bu dosyanın `out/` içinde olmasına dayanır.
- `macos-latest` her iki macOS RID'i için de kullanılır; `osx-x64` Apple Silicon makinede çapraz derlenir ve Intel Mac'te test edilmeden yayınlanır.

## İlgili dosyalar

- [mosair.csproj](mosair.csproj.md)
- [Program](Program.md)
