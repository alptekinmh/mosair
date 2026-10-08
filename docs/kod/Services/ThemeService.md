# ThemeService

> Kaynak: `mosair/Services/ThemeService.cs` · Güncelleme: 2026-10-08

## Amaç

**Görünüm → Tema** menüsündeki beş renk temasını (paleti) tutmak ve uygulamak. Her palet, `App.axaml`'daki bütün renk anahtarlarını hem koyu hem açık tema için verir; koyu/açık düğmesi her palette çalışır (toplam 10 görünüm). Seçim kullanıcının kendi bilgisayarında saklanır (`%APPDATA%\mosair\ui.json`); proje dosyasına yazılmaz, yani bir projeyi açan herkes kendi temasında görür.

## Nerede kullanılır

| Çağıran | Kullanım |
|---|---|
| `App.OnFrameworkInitializationCompleted` | İlk pencereden önce `LoadSaved()`: kaydedilmiş palet ve koyu/açık uygulanır. |
| `MainWindow.OnPickTheme` | **Görünüm → Tema →** bir palet: `SetPalette(id)` (id menü öğesinin `Tag`'inde). |
| `MainWindow.OnToggleTheme` | Araç çubuğundaki ay/güneş düğmesi ve **Görünüm → Tema → Açık Tema**: `SetLight(!IsLight)`. |
| `MainWindow.OnThemeChanged` | `Changed` olayı: menü onay işaretleri, ay/güneş ikonu ve görsel alanının rengi (`ApplyImageTint`) yenilenir. |

## Paletler

| Id | Menü adı (TR / EN) | Karakter |
|---|---|---|
| `lapis` | Lapis / Lapis | Varsayılan; `App.axaml`'daki değerlerin aynısı (nötr gri, mavi vurgu, adaçayı logo) |
| `pastel` | Adaçayı ve Lavanta / Sage and Lavender | Hafif sıcak gri, pastel adaçayı yeşili vurgu (koyu temada vurgu üstü yazı koyu), lavanta logo |
| `grafit` | Grafit ve Petrol / Graphite and Teal | Saf nötr grafit, petrol yeşili vurgu |
| `traverten` | Traverten / Travertine | Sıcak gri-bej, pişmiş toprak vurgu, zeytin yeşili logo |
| `murekkep` | Mürekkep ve Leylak / Ink and Lilac | Mavimsi mürekkep, pastel leylak-mavi vurgu (koyu temada vurgu üstü yazı koyu), şeftali logo |

Her palet şu 27 anahtarı verir: `BgMain`, `BgBar`, `BgPanel`, `BgInput`, `BgCanvas`, `BgCard`, `BgHover`, `BgPressed`, `BrdrMain`, `BrdrSec`, `BrdrTer`, `FgPrimary`, `FgSecondary`, `FgMuted`, `FgDisabled`, `FgIcon`, `FgMenu`, `NavBg`, `Brand`, `BrandFill`, `AccentFill`, `AccentFillHover`, `AccentFillPressed`, `OnAccent`, `AccentText`, `AccentSubtle`, `AccentBorder` (anlamları: [App.md](../App.md)). Durum renkleri (`FgWarn`, `Success`, `Danger`, `DangerText`, `EditMode`) her palette aynıdır (`StatusDark` / `StatusLight`), çünkü anlamları değişmez.

## Public API

| Üye | Ne yapar |
|---|---|
| `Palettes` | Beş `Palette(Id, NameKey, Dark, Light)` kaydı; `Dark`/`Light` anahtar → `#RRGGBB` ya da `#AARRGGBB`. |
| `DefaultId` | `"lapis"`. |
| `CurrentId` / `IsLight` | Uygulanan palet ve koyu/açık. |
| `Changed` | Her uygulamadan sonra (UI iş parçacığında). |
| `LoadSaved()` | `ui.json`'u okur (`Theme`, `Light`); dosya yoksa, bozuksa ya da palet bilinmiyorsa Lapis / koyu. Kaydetmeden uygular. |
| `SetPalette(id)` / `SetLight(light)` | Uygular ve `ui.json`'a yazar (yazılamazsa tema yine bu oturum için değişir). |

## Uygulama (`Apply`)

1. Paletin değerleri `Application.Resources.ThemeDictionaries`'teki `Dark` ve `Light` sözlüklerine yazılır; `DynamicResource` ile bağlı her şey (ana pencere, iletişim kutuları, kılavuz) anında değişir, yeniden başlatma gerekmez.
2. Fluent temasının `Dark`/`Light` paletlerinin `Accent`'i paletin `AccentFill`'i yapılır (işaret kutusu, kaydırıcı, ilerleme çubuğu).
3. `Application.RequestedThemeVariant` = `Light` ya da `Dark`.
4. İstenmişse `ui.json` yazılır, sonra `Changed`.

## Dikkat

- `ui.json` yalnızca tema bilgisini tutar (`{"Theme":"grafit","Light":false}`); dil ve diğer tercihler hâlâ kalıcı değildir.
- Yeni bir renk anahtarı eklenirse hem `App.axaml`'a hem de buradaki `Keys` dizisine ve beş paletin iki satırına (koyu/açık) eklenmelidir; değer sayısı tutmazsa uygulama açılışta `ArgumentException` verir.
