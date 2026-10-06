# StockCompareRunner

> Kaynak: `mosair/StockCompareRunner.cs` · Güncelleme: 2026-10-06

## Amaç
"Stoğa göre" algoritmasını arayüz olmadan, yerel bir stok tablosu kopyasıyla çevrimdışı test eden geliştirici aracı. Her görsel için düz Optimum ile stoğa göre düzeltilmiş sonucu (ya da `MOSAIR_CLASSIC=1` ile klasik Mos ve sonrasındaki düzeltmeyi) karşılaştırır, taş taş kg tablosu ve yan yana resim üretir.

## Nerede kullanılır
`Program.Main`, `--stockcompare` argümanıyla çağırır.

## Kullanım
```
mosair --stockcompare <outDir> <widthCm> <stock.csv> <stockScale> <görsel1> [görsel2 ...]
```
| Argüman | Açıklama |
|---|---|
| `outDir` | `rapor.txt` ve `<görsel>_stok.png` buraya yazılır (klasör yoksa oluşturulur) |
| `widthCm` | Mozaik genişliği (ör. 93.6; `InvariantCulture`) |
| `stock.csv` | Stok tablosunun yerel CSV kopyası (gviz çıktısı); `StockSheetService.ParseOnHandCsv` ile okunur. Repoya **eklenmez**. |
| `stockScale` | Bütün "Bizdeki (kg)" değerlerini çarpar; eksiklik yaratmak için ör. 0.3 |

Argüman sayısı 6'dan azsa kullanım metni yazılır ve 1 döner; başarıda 0 döner.

Ortam değişkenleri:
| Değişken | Etki |
|---|---|
| `MOSAIR_PROJECT` | Bu mozaiğin tablo sütunu; diğer mozaiklerin ayırdığı stoktan hariç tutulur (Stok Kontrol sonrası durumu taklit eder) |
| `MOSAIR_MINUSAGE=1` | En az kullanım kuralını açar (`MinUsage = StockAwareOptions.MinUsageFor(toplam taş)`). Verilmezse kural kapalıdır (`MinUsage = 0`), uygulamadaki gibi |
| `MOSAIR_USAGE=1` | Her görsel için Optimum'un taş kullanımını `USAGE` satırları olarak listeler |
| `MOSAIR_CLASSIC=1` | Optimum yerine klasik Mos (`RunM3`, uygulamadaki `rgbM` kuralıyla, `RgbIncrement` 10) çalıştırır ve sonucu `FixCurrentMosaicToStock` ile düzeltir (`RunClassic`) |
| `MOSAIR_EXCLUDE_ZERO=1` | Yalnızca `MOSAIR_CLASSIC=1` ile: Stok Çek gibi, kapasitesi 0 olan taşları Mos'tan önce seçimden çıkarır |

Kapasite: `⌊(OnHandKg × stockScale − OtherMosaicsKg) / StoneWeightKg⌋`, en az 0; tabloda kaydı olmayan taş `null` (sınırsız, ikame olarak kullanılmaz).

Uygulama WinExe olduğu için Windows'ta konsol çıktısı görünmeyebilir; güvenilir çıktı `rapor.txt`'dir (UTF-8, BOM'lu).

## Akış (Optimum modu)
1. Katalog yüklenir (`ColorCatalogService.LoadDefaultCatalog`), renk yakınlığı tablosu yazılır (`LogSimilarity`).
2. Her görsel için: `Reset` → `LoadImage` → `CalculateDimensions` → `RunOptimal` (dokusuz). Sonuç, ID'ler ve palet anahtarı saklanır.
3. **Kabul testi:** sınırsız stokla ve `MinUsage = 0` ile `ApplyOptimalKWithStock` çalıştırılır; `dataM3`, `drl.dat` ID'leri ve palet Optimum ile birebir aynı olmalı, `LastStockResult.Changed` false olmalıdır.
4. Gerçek kapasiteyle `ApplyOptimalKWithStock` çalıştırılır ve süre ölçülür.
5. Rapor satırları ve `<görsel>_stok.png` yazılır; sonunda `SUMMARY` tablosu (taş sayısı, ortalama ΔE, korunan kenar, L korelasyonu, stoğu aşan önce/sonra, sınır altı önce/sonra, taşınan, eklenen, birebir aynılık, kontroller).

## Ürettiği rapor
- Stoklu her taş için ΔE'ye göre en yakın 3 taş ve aynı taşın yüzeyleri arası mesafeler (en az / ortanca / en çok).
- Görsel başına:
  - Optimum ve stoğa göre kalite ölçüleri (`MosaicMetrics`) ve süre.
  - Kontroller: stok yeterliyse birebir aynılık; stoğu aşan taş kalmaması; `MinUsage` altında taş kalmaması (kural yalnızca `MOSAIR_MINUSAGE=1` ile açıktır).
  - Arama seviyesi (`LevelText`: benzer taşlar / 2× / 4× sınır / stoğu olan herhangi taş / yeni tür sınırı da aşıldı).
  - Değişen taşlar tablosu (adet/kg), taşınanlar, yeni türler, az kullanıldığı için çıkarılanlar, stok kaydı olmayanlar.
- Klasik modda ek olarak palet ID kontrolü (katalogla uyuşmayan kayıtlar) ve seçim kontrolü (aktif taş rengi olmayan pikseller, seçili olmayan kullanılan taşlar); düzeltme yapılamazsa "düzeltme YAPILAMADI" yazılır.
- Resim panelleri (`SavePanels`): Orijinal | Optimum | Stoğa göre | Değişen taşlar (değişmeyenler griye soluk). Klasik modda resim üretilmez.

## Yardımcılar (private)
`RunClassic`, `LevelText`, `LogSimilarity`, `CopyIds` (`drl.dat[..,3]` kopyası), `PaletteKey` (`ID:numOfPixel:u` dizisi), `Same` (byte/int dizileri için birebir karşılaştırma), `SavePanels`.

## Dikkat
- Tabloda gerçek stok verisi bulunduğundan `stock.csv` ve üretilen raporlar repoya konmaz.
- Statik `MosaicEngine` durumunu kullanır; uygulama arayüzü açılmaz.

## İlgili dosyalar
- [StockAwareAssigner](Services/StockAwareAssigner.md), [MosaicEngine](Services/MosaicEngine.md), [StockSheetService](Services/StockSheetService.md), [MosaicMetrics](Services/MosaicMetrics.md), [CompareRunner](CompareRunner.md), [Program](Program.md)
