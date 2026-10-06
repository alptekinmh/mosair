# ActivityWave

> Kaynak: `mosair/Controls/ActivityWave.cs` · Güncelleme: 2026-10-06

## Amaç

Durum çubuğunun arka planında, bir işlem sürdüğü sürece soldan sağa akan iki katmanlı, yarı saydam bir deniz dalgası çizen hafif bir `Control`. İşlem ne kadar uzun sürerse dalga o kadar akar; işlem başlarken yumuşakça belirir, bitince söner. Çok kısa işlemlerde yalnızca kısa bir parıltı görünür.

## Nerede kullanılır

`MainWindow.axaml` içinde durum çubuğu `Border`'ının içindeki `Panel`'de, yazıların bulunduğu `Grid`'in arkasında:

```xml
<ctrl:ActivityWave IsActive="{Binding IsBusy}"/>
```

`IsBusy` = `IsProcessing || IsStockBusy || IsExporting` ([MainViewModel](../ViewModels/MainViewModel.md)). Böylece Mos, stoğa göre düzeltme, Optimum kaydırıcısıyla yeniden kurma, proje açılırken taş görüntülerinin yüklenmesi, stok tablosu işlemleri ve dışa aktarma sırasında dalga akar.

## Yapı

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `IsActive` | `bool` (StyledProperty) | false | true olunca zamanlayıcı başlar ve dalga belirir; false olunca söner, tamamen sönünce zamanlayıcı durur |
| `WaveColor` | `Color` (StyledProperty) | `#4ecb71` | Dalga rengi (durum yazısıyla aynı yeşil); `AffectsRender` |
| `FadeSeconds` | const | 0,35 | Belirme ve sönme süresi |
| `SpeedPxPerSecond` | const | 90 | Ön dalganın sağa akış hızı (px/sn); arka dalga bunun 0,6 katı |
| `Wavelength` | const | 140 | Ön dalganın tepeleri arası mesafe (px); arka dalga 1,7 katı |

Yapıcıda `IsHitTestVisible = false`: fare olayları alttaki düğmelere geçer.

## Çalışma

1. `IsActive` true olunca (veya kontrol görsel ağaca eklendiğinde zaten true ise) 33 ms'lik (`DispatcherPriority.Render`) bir `DispatcherTimer` başlar.
2. Her tikte görünürlük düzeyi (`_level`, 0–1) `IsActive`'e göre `FadeSeconds` sürede artar ya da azalır; `InvalidateVisual` çağrılır. `IsActive` false ve düzey 0 olunca zamanlayıcı durur, kontrol hiçbir şey çizmez.
3. `Render`: iki dalga çizilir. Her biri iki sinüsün toplamıdır (ikinci, küçük harmonik dalgayı düz sinüsten ayırır); faz, `Stopwatch` zamanıyla kaydırıldığı için akış zamanlayıcı gecikmelerinden etkilenmez. Dalga çizgisinin altı, yukarıdan aşağı solan yarı saydam bir degrade ile doldurulur. Opaklık `_level` ile çarpılır (arka dalga en çok 34, ön dalga en çok 52 / 255).
4. Kontrol görsel ağaçtan çıkınca zamanlayıcı durdurulur.

## Dikkat

- Genişlik değişince (pencere boyutu) dalga kendiliğinden tüm çubuğa yayılır; sabit piksel genişliği yoktur.
- Renk temadan bağımsızdır; açık ve koyu temada aynı yeşil kullanılır. Yazıların okunur kalması için opaklık düşük tutulmuştur.
- İşlem durumunu yalnızca `IsBusy` belirler; ilerleme yüzdesini göstermez (o iş için ortadaki `ProgressBar` var).

## İlgili dosyalar

- [MainWindow](../MainWindow.md), [MainViewModel](../ViewModels/MainViewModel.md), [GridOverlay](./GridOverlay.md)
