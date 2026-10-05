# StockCompareRunner

> Kaynak: `mosair/StockCompareRunner.cs` · Güncelleme: 2026-10-06 · Durum: deneme (`stok_deneme` dalı)

## Amaç
"Stoğa göre" algoritmasını arayüz olmadan, yerel bir stok tablosu kopyasıyla çevrimdışı test eden geliştirici aracı. Her görsel için düz Optimum ile stoğa göre düzeltilmiş sonucu karşılaştırır, taş taş kg tablosu ve yan yana resim üretir.

## Nerede kullanılır
`Program.Main`, `--stockcompare` argümanıyla çağırır.

## Kullanım
```
mosair --stockcompare <outDir> <widthCm> <stock.csv> <stockScale> <görsel1> [görsel2 ...]
```
| Argüman | Açıklama |
|---|---|
| `outDir` | `rapor.txt` ve `<görsel>_stok.png` buraya yazılır |
| `widthCm` | Mozaik genişliği (ör. 93.6) |
| `stock.csv` | Stok tablosunun yerel CSV kopyası (gviz çıktısı). Repoya **eklenmez**. |
| `stockScale` | Bütün "Bizdeki (kg)" değerlerini çarpar; eksiklik yaratmak için ör. 0.3 |

Ortam değişkenleri:
- `MOSAIR_PROJECT`: bu mozaiğin tablo sütunu; diğer mozaiklerin ayırdığı stoktan hariç tutulur (Stok Kontrol sonrası durumu taklit eder).
- `MOSAIR_USAGE=1`: her görsel için Optimum'un taş kullanımını listeler.

Uygulama WinExe olduğu için Windows'ta konsol çıktısı görünmeyebilir; güvenilir çıktı `rapor.txt`'dir.

## Ürettiği rapor
- Stoklu her taş için ΔE'ye göre en yakın 3 taş ve aynı taşın yüzeyleri arası mesafeler.
- Görsel başına:
  - Optimum ve stoğa göre kalite ölçüleri (`MosaicMetrics`) ve süre.
  - Kontroller: stok yeterliyse birebir aynılık; stoğu aşan taş kalmaması; `MinUsage` altında taş kalmaması.
  - Değişen taşlar tablosu (adet/kg), taşınanlar, yeni türler, az kullanıldığı için çıkarılanlar, stok kaydı olmayanlar.
- Resim panelleri: Orijinal | Optimum | Stoğa göre | Değişen taşlar.

## İlgili dosyalar
- [StockAwareAssigner](Services/StockAwareAssigner.md), [MosaicEngine](Services/MosaicEngine.md), [StockSheetService](Services/StockSheetService.md), [MosaicMetrics](Services/MosaicMetrics.md), [CompareRunner](CompareRunner.md), [Program](Program.md)
