# mosair Arayüz Rehberi

> Uygulamada yapılabilecek **her şey** bu dosyadadır: menüler, araç çubuğu, paneller, fare ve klavye, stok, proje ve dışa aktarma.
> Kod tarafı için [`kod/`](kod/) klasörüne, belgelerin haritası için [README.md](README.md) dosyasına bakın.
> Uygulama içindeki kılavuz (**Yardım → Kullanım Kılavuzu**, F1) bu dosyanın kısa özetidir; ikisi birbiriyle çelişmemelidir.
>
> Güncelleme: 2026-10-06 · Kapsadığı sürüm: v1.3.1

## İçindekiler

1. [Ekran düzeni](#1-ekran-düzeni)
2. [Temel iş akışı](#2-temel-iş-akışı)
3. [Üst menü](#3-üst-menü)
4. [Araç çubuğu](#4-araç-çubuğu)
5. [Sol panel: ölçüler ve renk sütunları](#5-sol-panel-ölçüler-ve-renk-sütunları)
6. [Görsel alanı (canvas)](#6-görsel-alanı-canvas)
7. [Özellikler paneli (Properties)](#7-özellikler-paneli-properties)
8. [Durum çubuğu](#8-durum-çubuğu)
9. [Piksel düzenleme ve taş varyantı](#9-piksel-düzenleme-ve-taş-varyantı)
10. [Optimum taş sayısı](#10-optimum-taş-sayısı)
11. [Stok yönetimi (Google Sheets)](#11-stok-yönetimi-google-sheets)
12. [Proje kaydetme ve açma](#12-proje-kaydetme-ve-açma)
13. [Dışa aktarma (mosairEXPORT)](#13-dışa-aktarma-mosairexport)
14. [Tema ve dil](#14-tema-ve-dil)
15. [Klavye kısayolları](#15-klavye-kısayolları)
16. [Fare kontrolleri](#16-fare-kontrolleri)
17. [Uyarı ve hata mesajları](#17-uyarı-ve-hata-mesajları)
18. [Bilinen davranışlar ve sınırlamalar](#18-bilinen-davranışlar-ve-sınırlamalar)

---

## 1. Ekran düzeni

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│ Başlık çubuğu + Menü: Dosya  Düzenle  Görünüm  Araçlar  Yardım   ·  açık dosya adı │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Araç çubuğu: Görsel · Proje Aç │ Kaydet · Farklı Kaydet │ Mos │ Kalem │ Izgara      │
│   Interp · Detay │ Tablo ▾ · Stok Çek ▾ · Kontrol · Sil · Ekle │ ☐ Optimum         │
│   ☐ Stoğa göre  Taş ──●── N öneri K      … 📷 │ Dışa Aktar │ ☾ │ 🌐               │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Durum çubuğu: kullanılan renk │ ilerleme + durum + süre │ Ekrana Sığdır · zoom    │
├───────────────────────┬───────────────────────────────────┬──────────────────────┤
│ SOL PANEL             │ GÖRSEL ALANI (canvas)             │ ÖZELLİKLER PANELİ    │
│ cm · ölçü · alan      │                       ┌─────────┐ │ RENK                 │
│ taş / kalıp / orijinal│                       │Mini     │ │ DOKU                 │
│ [Tümünü Seç][Kaldır]  │                       │harita   │ │ VARYANTLAR           │
│ Katalog│Eşleşme│Atanan│                       └─────────┘ │ RGB · KOORDİNAT      │
│        │       │      │                                   │ DÜZENLEME            │
└───────────────────────┴───────────────────────────────────┴──────────────────────┘
```

- Uygulama ekranı kaplayacak şekilde (büyütülmüş) açılır; pencere en az 900×600 px olabilir.
- Paneller arasındaki ince çizgiler (ayırıcılar) sürüklenerek genişlik değiştirilebilir. Sol panel 220–380 px, sağ panel 160–360 px arasında ayarlanır.
- Pencere kendi başlık çubuğunu kullanır: boş bir yerinden sürükleyerek pencere taşınır, **çift tıklayınca** pencere büyür ya da eski boyutuna döner.
- Başlık çubuğunun ortasında **açık dosyanın adı** yazar: görsel yüklenince fotoğrafın adı (ör. `st1.jpg`), proje kaydedilince ya da açılınca projenin adı (ör. `st1.mos`). Farklı Kaydet ile yeni adla kaydedilince yeni ad görünür.
- Araç çubuğundaki düğmelerin üzerine gelince kısa bir ipucu (tooltip) çıkar; ipuçlarının metni [§4](#4-araç-çubuğu)'teki tabloda özetlenmiştir.

## 2. Temel iş akışı

| Adım | Ne yapılır | Nereden |
|---|---|---|
| 1 | Görsel yükle (PNG, JPG, JPEG, BMP, TIFF) | Görsel ikonu, **Dosya → Görsel Yükle**, `Ctrl/⌘+I` veya dosyayı pencereye sürükle-bırak |
| 2 | Mozaik genişliğini cm olarak gir | Sol panelin üstündeki kutu |
| 3 | Katalogda kullanılabilecek taşları seç; **Optimum** ve **Stoğa göre** kutularını gerekirse işaretle (ikisi de varsayılan kapalı) | Sol panel, araç çubuğu |
| 4 | Mozaikleştir | **Mos** düğmesi veya `Ctrl/⌘+M` |
| 5 | İncele, gerekirse taş sayısını, pikselleri ve taş varyantlarını düzenle | Taş kaydırıcısı, canvas, Özellikler paneli |
| 6 | (İsteğe bağlı) **Stok Kontrol** ile adetleri tabloya yaz | Stok ikonları |
| 7 | Projeyi kaydet, görüntüyü dışa aktar | `Ctrl/⌘+S`, `Ctrl/⌘+E` |

## 3. Üst menü

Kısayollar Windows'ta `Ctrl`, macOS'te `⌘` ile gösterilir; menüdeki yazı işletim sistemine göre kendiliğinden değişir.

### Dosya

| Öğe | Kısayol | Ne yapar | Ne zaman çalışır |
|---|---|---|---|
| Görsel Yükle | `Ctrl/⌘+I` | Görsel seçme penceresini açar | Her zaman |
| Proje Aç | `Ctrl/⌘+O` | `.mos` proje dosyası açar ([§12](#12-proje-kaydetme-ve-açma)) | Her zaman |
| Proje Kaydet | `Ctrl/⌘+S` | Projeyi `Masaüstü/mosairPROJECT/<görsel adı>/` klasörüne kaydeder | Mozaik varken |
| Proje Farklı Kaydet | `Ctrl/⌘+Shift+S` (macOS: `⌘+⇧+S`) | Konum ve ad sorarak kaydeder | Mozaik varken |
| mosairEXPORT ▸ | `Ctrl/⌘+E` | Alt menü: "Görüntü kalitesi seçiniz" ve 10 kalite seçeneği (görüntü boyutu ve tahmini dosya boyutuyla). Seçilen kaliteyle `Masaüstü/mosairEXPORT` klasörüne kaydeder ([§13](#13-dışa-aktarma-mosairexport)). Kısayol listeyi açmadan Detay ayarındaki kaliteyle kaydeder | Mozaik varken, işlem ya da dışa aktarma sürmüyorken |
| mosairEXPORT As ▸ | — | Aynı alt menü; seçilen kaliteyle konum, ad ve biçim (JPEG/PNG) sorarak dışa aktarır | mosairEXPORT ile aynı |

### Düzenle

| Öğe | Ne yapar |
|---|---|
| Tüm Renkleri Seç | Katalogdaki bütün taşları işaretler (Mos'ta kullanılabilir yapar) |
| Tüm Renkleri Kaldır | Katalogdaki bütün işaretleri kaldırır |

### Görünüm

| Öğe | Kısayol | Ne yapar |
|---|---|---|
| Ekrana Sığdır | `Ctrl/⌘+0` | Görseli pencereye sığacak şekilde yakınlaştırır/uzaklaştırır |

### Araçlar

Araçlar menüsü araç çubuğundaki bütün araçları içerir. Açık olan seçenekler ✓ ile işaretlidir; menü ile araç çubuğu her zaman aynı durumu gösterir.

| Öğe | Kısayol | Ne yapar |
|---|---|---|
| Mozaikleştir | `Ctrl/⌘+M` | Mos çalıştırır. Görsel yoksa **Mozaikleştirme** uyarısı çıkar; işlem sürerken bir şey yapmaz. |
| Piksel Düzenle | Orta fare tuşu | Piksel düzenleme modunu açar/kapatır ([§9](#9-piksel-düzenleme-ve-taş-varyantı)); mozaik yoksa pasiftir |
| Izgara Göster | — | Taşlar arası ızgara çizgilerini açar/kapatır |
| Izgara Rengi ▸ | — | 12 hazır renk; seçilen rengin 7 tonu ayrıca listelenir. Seçili renk ✓ ile işaretlidir. |
| İnterpolasyon Yöntemi ▸ | — | Area, Nearest, Linear, Cubic, Lanczos4, LinearExact, NearestExact |
| Detay Seviyesi ▸ | — | 10–100 arası (10'ar adım) taş başına piksel (N) |
| Optimum Taş Sayısı | — | Optimum modunu açar/kapatır ([§10](#10-optimum-taş-sayısı)) |
| Stoğa Göre Ayarla | — | **Stoğa göre** kutusunu açar/kapatır ([§11](#stoğa-göre-optimumun-yanındaki-kutu)) |
| Taş Sayısı ▸ Önerilen Değere Dön / Bir Taş Artır / Bir Taş Azalt | — | Optimum ile yapılmış bir Mos'tan sonra kullanılacak taş çeşidi sayısını değiştirir; başka zaman pasiftir |
| Stok ▸ | — | Stok Tablosunu Aç, Stok Ayarları..., Stok Çek ▸ (Stoğu olmayanları devre dışı bırak / Stoğu olmayanları kırmızıyla işaretle), Stok Kontrol, Bu mozaiğin sütununu temizle, Tüm mozaik sütunlarını temizle, Stok Ekle ([§11](#11-stok-yönetimi-google-sheets)). Bir stok işlemi sürerken pasiftir. |

### Yardım

| Öğe | Kısayol | Ne yapar |
|---|---|---|
| Kullanım Kılavuzu | `F1` | Uygulama içi kılavuzu açar |

**Kullanım kılavuzu penceresi:** Solda bölüm menüsü vardır (Genel Bakış, Arayüz Yapısı, Klavye Kısayolları, Fare Kontrolleri, Piksel Düzenleme, Optimum Taş Sayısı, Stok Yönetimi, Proje Yönetimi, İpuçları); bir bölüme tıklamak o bölüme kaydırır. Kılavuz o anki arayüz dilinde (TR/EN) ve temada açılır. Pencere açıkken ana pencere kullanılamaz; kılavuz kapatılınca devam edilir. Uygulamada ayrı bir "Hakkında" penceresi yoktur.

## 4. Araç çubuğu

Soldan sağa:

| Kontrol | İpucu | Tıklama | Ok (▾) / sağ tık | Not |
|---|---|---|---|---|
| Görsel Yükle | Görsel Yükle | Görsel seçer | — | Sürükle-bırak da olur |
| Proje Aç | Proje Aç (.mos) | `.mos` açar | — | |
| Proje Kaydet | Proje Kaydet | Kaydeder; ikon kısa süre yeşil ✓ olur | — | Mozaik yokken pasif |
| Proje Farklı Kaydet | Proje Farklı Kaydet | Konum sorarak kaydeder | — | Mozaik yokken pasif |
| **Mos** | Mozaikleştir | Mozaikleştirir | — | Görsel yokken, işlem ya da dışa aktarma sürerken pasif; çalışırken ikon animasyonludur |
| Kalem (Piksel Düzenle) | Piksel Düzenle (Orta Tuş), 1) Kaynak renk seç, 2) Hedef piksele uygula | Düzenleme modunu aç/kapat | Orta tuş da aynı işi yapar | Mozaik yokken pasif. Açıkken kalem turuncu olur, yanında `source → target` göstergesi çıkar |
| Izgara | Izgara Göster/Gizle | Açılır panel: Grid ON/OFF, Grid Rengi (12 renk + seçilen rengin 7 tonu) | — | Görsel yüklenince ızgara rengi görselin parlaklığına göre otomatik gri tona ayarlanır. Açma/kapama ve renk değişikliği anında uygulanır. |
| İnterpolasyon | İnterpolasyon Yöntemi | Açılır liste (7 yöntem); seçili yöntemin adı düğmede yazar | — | Varsayılan **Area** |
| Detay (N) | Detay Seviyesi (N) | Açılır kaydırıcı 10–100 (10'ar adım); değer düğmede yazar | — | Varsayılan **40**; 40'ın üstü ilk seferde performans uyarısı gösterir. Değişiklik anında uygulanır; beklenecek bir yeniden oluşturma yoktur. |
| Stok Tablosu (yeşil tablo) | Stok tablosunu tarayıcıda aç | Tabloyu tarayıcıda açar | Yanındaki **▾**: Stok Tablosunu Aç / Stok Ayarları... · Sağ tık: Stok Ayarları... | |
| Stok Çek (depo) | Stok çek: stoğu oku, stoğu biten taşları devre dışı bırak | Bizdeki (kg) okunur, stoğu olmayan taşlar devre dışı bırakılır | Yanındaki **▾**: Stoğu olmayanları devre dışı bırak / Stoğu olmayanları kırmızıyla işaretle | Tabloyu değiştirmez |
| Stok Kontrol (pano) | Stok kontrol: adetleri tabloya yaz, stoğu yetmeyenleri işaretle | Adetler tabloya yazılır, kalan okunur | — | Tabloyu değiştirir |
| Stok Sil (küp −) | Stok temizle: bu mozaiğin sütununu temizle (sağ tık: tüm mozaik sütunları) | Bu mozaiğin sütununu temizler | Sağ tık: **Bu mozaiğin sütununu temizle / Tüm mozaik sütunlarını temizle** | Onay ister |
| Stok Ekle (küp +) | Stok ekle: Tahmini Kalan'ı Bizdeki'ye taşı, mozaik sütunlarını temizle | Tahmini Kalan → Bizdeki | — | Onay ister |
| ☐ Optimum | Optimum taş sayısını otomatik bul | Optimum modunu aç/kapat | — | Varsayılan **kapalı** |
| ☐ Stoğa göre | Özelliğin açıklaması; son Mos'un stok raporu da eklenir | Stoğa göre modunu aç/kapat | — | Varsayılan **kapalı** ([§11](#stoğa-göre-optimumun-yanındaki-kutu)) |
| Taş ──●── N · öneri K | — | Taş çeşidi sayısını değiştirir | — | Yalnızca Optimum ile yapılmış bir Mos'tan sonra görünür |
| Ekran görüntüsü (kamera, dışa aktarmanın solunda) | Ekran görüntüsü: görsel alanında şu an görünen kısmı PNG olarak mosairEXPORT klasörüne kaydeder | Görsel alanında o an ne görünüyorsa (mozaik ya da yüklenen görsel, aynı zoom, ızgara dahil) yakalar; mini harita ve kaydırma çubukları girmez. Görüntü pencereden küçükse kenarlar tuval rengiyle dolar. Yüksek çözünürlüklü ekranlarda ekranın gerçek piksel yoğunluğuyla kaydedilir. Dosya: `mosairEXPORT/tarih_saat__görselAdı__ekran.png`; durum çubuğu dosya adını yazar. | — | Görsel yokken pasif |
| mosairEXPORT (sağda) | Dışa aktar: mosairEXPORT / mosairEXPORT As, görüntü kalitesi seçerek (boyut bilgisiyle). Sağ tık: klasörü aç | Liste açar: **mosairEXPORT ▸** / **mosairEXPORT As ▸**, her birinde görüntü kalitesi seçenekleri ([§13](#13-dışa-aktarma-mosairexport)); tıklama doğrudan kaydetmez | **Sağ tık: klasörü açar** | Mozaik yokken pasif; kaydederken ok animasyonu oynar |
| ☾ / ☀ | Tema Değiştir | Koyu/açık tema | — | |
| 🌐 | Dil | Açılır liste: TR Türkçe / EN English | — | |

Stok işlemi sürerken yedi stok düğmesi (beş ikon ve iki **▾** oku) ile **Araçlar → Stok** menüsü geçici olarak pasif olur.

## 5. Sol panel: ölçüler ve renk sütunları

### Ölçüler

| Alan | Açıklama |
|---|---|
| **cm kutusu** | Mozaik genişliği. Varsayılan 93,6 cm. Virgül yazılırsa noktaya çevrilir. `Enter` veya kutudan çıkınca uygulanır. Kutuya tıklayınca içerik seçilir. |
| Yuvarlama | Bir taş 1,2 cm'dir; değer en yakın taş sayısına yuvarlanır (en az 2 taş). |
| Üst sınır | Görselin piksel genişliğinden fazla taş istenirse genişlik o sınıra indirilir ve **Çözünürlük Yetersiz** uyarısı çıkar. |
| `G x Y cm` ve `m²` | Gerçek mozaik ölçüsü ve alanı |
| ■ satırı | Taş sayısı: `sütun x satır = toplam taş` |
| ▣ satırı | Kalıp sayısı: `sütun x satır = toplam kalıp` (genişlik ve yükseklik 26'şar taşa bölünerek, yukarı yuvarlanır) |
| ○ satırı | Orijinal görselin piksel ölçüsü |

### Tüm Renkleri Seç / Tüm Renkleri Kaldır

Katalogdaki bütün taşları işaretler ya da işaretleri kaldırır. Aynı işlem **Düzenle** menüsünde de vardır.

### Üç sütun

| Sütun | İçerik | Etkileşim |
|---|---|---|
| **Katalog Renk** | Bütün taşlar: kırmızı nokta (stok yetersiz) · onay kutusu · taş ID · doku küçük resmi · taş kodu | Onay kutusu taşı Mos'a dahil eder/çıkarır. Piksel düzenleme açıkken bir satıra tıklamak o taşı **kaynak renk** yapar. |
| **Eşleşme** | Mozaikte kullanılan taşların renk kutuları (Atanan sütunuyla aynı sırada) | Atanan sütunuyla birlikte kayar |
| **Atanan Renk** | Mozaikte gerçekten kullanılan taşlar: ID, kod, adet; satır arka planı taş rengidir | Eşleşme sütunuyla birlikte kayar |

**Kırmızı nokta** üç yoldan konur: **Stok Çek → Stoğu olmayanları kırmızıyla işaretle**, **Stok Kontrol** ve stoğu hâlâ yetmeyen taş bırakan bir **Stoğa göre** Mos ([§11](#11-stok-yönetimi-google-sheets)). Yeni bir Mos önceki noktaları siler.

**Katalog ipucu (tooltip):** Taşın doku küçük resminin üzerine gelince açılır.

- Başlık: `kod  ad  R G B`. Altında taşın büyük doku önizlemesi görünür (doku varsa).
- Sağ üstte stok bilgisi:
  - **İki değer de biliniyorsa:** `Bizdeki → Kalan kg`. Kalan, Stok Kontrol'den sonra tablodaki **Tahmini Kalan**'dır; Stoğa göre bir Mos'tan ya da Stoğa göre açıkken stoğu okuyup denetimini yapabilen bir Stok Kontrol'den sonra uygulamanın hesapladığı değerdir: Bizdeki − diğer mozaiklerin ayırdığı − bu mozaiğin kullandığı.
  - **Yalnız Bizdeki biliniyorsa:** `X kg`.
  - **Yalnız kalan biliniyorsa:** `X kg kaldı`.
  - Renk: her sayı 0 veya altındaysa **kırmızı**, üstündeyse **yeşil**.
  - Bizdeki kg, uygulama açılırken ve her görsel ya da proje yüklendiğinde stok ayarındaki tablodan kendiliğinden yüklenir; Stok Çek ve Stok Kontrol de günceller. Yeni bir Mos "kalan" değerini siler, Bizdeki kg'ı korur (Stoğa göre açıksa kalan yeniden hesaplanır).

**Mos'tan sonra:** Katalogda yalnızca mozaikte kullanılan taşlar işaretli kalır. Proje açınca da aynısı olur.

## 6. Görsel alanı (canvas)

| İşlem | Nasıl |
|---|---|
| Yakınlaştır/uzaklaştır | Fare tekerleği; imlecin altındaki nokta sabit kalır. Her adım ×1,25. En fazla 20×, en az ekrana sığdırma ölçüsü (ama ‰1'den az değil; bkz. [§18](#18-bilinen-davranışlar-ve-sınırlamalar)). |
| Kaydır (pan) | **Sağ tuşu basılı tutup sürükle** |
| Taş seç | **Sol tık**: Özellikler paneli o taşı gösterir |
| Piksel düzenle | Düzenleme modundayken sol tık ([§9](#9-piksel-düzenleme-ve-taş-varyantı)) |
| Düzenleme modunu aç/kapat | **Orta tuş** (mozaik varken, mozaiğin üzerinde) |
| Görsel yükle | Dosyayı pencereye sürükle-bırak (PNG, JPG, JPEG, BMP, TIFF; ilk uygun dosya alınır) |
| Ekrana sığdır | Durum çubuğundaki ⛶ düğmesi, `Ctrl/⌘+0` veya **Görünüm → Ekrana Sığdır**. Mos ve Proje Aç sonrasında kendiliğinden uygulanır. |

**Mini harita (navigator):** Sağ üstteki 150×150 küçük görüntüdür. Yeşil çerçeve ekranda görünen bölgeyi gösterir. Tıklamak ya da sol tuşla sürüklemek o bölgeye götürür. Görsel yüklüyken görünür. Mos'tan önce yüklenen görseli, Mos'tan sonra mozaiğin taş renklerini gösterir.

**Taş dokulu görüntünün çizimi:** Mos'tan sonra canvas, mozaiği gerçek taş dokularıyla gösterir. Görüntü bir bütün olarak oluşturulmaz; yalnızca ekranda görünen kısım, parça parça ve yakınlaştırmaya uygun detayla çizilir:

- Çok uzaklaştırıldığında (bir taş ekranda yalnızca birkaç piksel kaldığında) taşlar kendi renkleriyle gösterilir.
- Yakınlaştırdıkça dokular görünür ve görüntü keskinleşir; en yakın görünümde Detay (N) değerindeki doku çizilir.
- Kaydırma ya da yakınlaştırmadan sonra daha keskin parça hazırlanana kadar bir bölge kısa bir süre bulanık görünebilir; parça gelince kendiliğinden keskinleşir.
- Görüntü boyutu için bir sınır yoktur: çok büyük mozaikler de (ör. 20 m genişlik) gösterilir ve düzenlenebilir.
- Detay (N), ızgara ve ızgara rengi değişiklikleri anında uygulanır; ekran bir süre eldeki görüntüyle kalıp yeni parçalar geldikçe güncellenir.

**Performans bildirimi:** Detay (N) ilk kez 40'ın üstüne çıkarıldığında ortada bir uyarı kutusu çıkar ve **Anladım** ile kapanır. Oturum boyunca bir kez gösterilir.

## 7. Özellikler paneli (Properties)

Canvas'ta bir taşa sol tıklayınca dolar.

| Bölüm | Gösterdiği | Etkileşim |
|---|---|---|
| RENK | Renk kutusu, `#ID`, taş kodu | — |
| DOKU | Seçili taşın o pikselde kullanılan doku görüntüsü ve varyant numarası | — |
| VARYANTLAR | Aynı taşın bütün doku varyantları (küçük resimler); seçili olan yeşil çerçevelidir | **Tıkla:** o piksel için doku varyantını değiştirir. Geri alınabilir (`Ctrl/⌘+Z`). |
| RGB | Pikselin R, G, B değerleri | — |
| KOORDİNAT | Piksel `Y, X` ve kalıp içi `yi, xi` | — |
| DÜZENLEME | Düzenlenen piksel sayısı, geri al/yinele kısayolları | Yalnızca piksel düzenleme açıkken görünür |

## 8. Durum çubuğu

| Bölge | İçerik |
|---|---|
| Sol | Mos'tan sonra kullanılan renk bilgisi (ör. "X renk arasından Y renk kullanıldı") |
| Orta | İlerleme çubuğu (işlem sırasında), durum mesajı, geçen süre |
| Arka plan | **İşlem dalgası:** herhangi bir işlem sürerken (Mos, stoğa göre düzeltme, Optimum taş sayısı değişimi, proje açılırken taş görüntülerinin yüklenmesi, stok tablosu işlemleri, dışa aktarma) çubuğun başından sonuna yeşil bir dalga akar. İşlem sürdükçe devam eder, bitince yavaşça söner. |
| Sağ | **Ekrana Sığdır** düğmesi; zoom oranı (`N=2.0` biçiminde, Detay N ile karıştırılmamalı; 0,1'in altında en fazla 3 ondalıkla) ve ekrandaki görüntü boyutu (px) |

Ortadaki durum mesajında görülebilecekler:

- Açılışta ve her görsel/proje yüklendiğinde: "Stok bilgisi yüklendi: N taş (katalog ipucunda kg)" ya da "Stok bilgisi yüklenemedi: …". Stok ayarı hiç yapılmamışsa bu satır çıkmaz.
- Mos sonunda: "Tamamlandı — N renk, S s". **Stoğa göre** açıksa sonuna stok özeti eklenir: "Stok yeterli, mozaik değişmedi", "Stoğa göre: X taş türünden Y taş yer değiştirdi" ve gerekirse "Stoğu hâlâ yetmeyen: …", "Tabloda stok kaydı olmayan, kontrol edilemeyen taşlar: …". Stok ayarı var ama stok okunamadıysa "Stok tablodan okunamadı; Mos stoğa bakmadan yapıldı." eklenir (stok ayarı hiç yapılmamışsa bu not çıkmaz).
- Stok işlemlerinin sonucu ([§11](#11-stok-yönetimi-google-sheets)), kayıt ve dışa aktarma bilgisi (büyük dosyalarda yüzde olarak ilerleme dahil, [§13](#13-dışa-aktarma-mosairexport)), piksel düzenleme bilgisi.
- Dil değiştirilince mesaj "Hazır" olur.

## 9. Piksel düzenleme ve taş varyantı

### Renk değiştirme (piksel düzenleme)

1. **Modu açın:** orta tuş, araç çubuğundaki kalem ya da **Araçlar → Piksel Düzenle**. Önce Mos yapılmış olmalıdır. Kalem turuncu yanar ve `source` göstergesi aktif olur.
2. **Kaynak rengi seçin:** istediğiniz renkteki bir taşa sol tıklayın **veya** sol paneldeki katalogdan bir taş seçin. Gösterge `target`'a geçer.
3. **Hedefe uygulayın:** değiştirmek istediğiniz taşlara sol tıklayın. Renk anında değişir; hedef modunda kalınır, aynı kaynakla birden çok piksel boyanabilir. Düzenlenen taş, o hücrenin kendi doku varyantıyla, diğer taşlarla aynı biçimde (ızgara dahil) çizilir; ekranda dışa aktarılan dosyadakiyle aynı görünür ve sonradan değişmez.
4. **Geri al / yinele:** `Ctrl/⌘+Z` geri alır. `Ctrl/⌘+Y` veya `Ctrl/⌘+Shift+Z` yineler.
5. **Kapatmak için** modu tekrar tetikleyin (orta tuş/kalem). Kapanınca katalog seçimi temizlenir.

**Kataloğa geçen kaynaklar:** Düzenlemede katalogdan seçilip mozaiğin paletinde olmayan bir taş kullanılırsa, proje kaydedilirken bu taş palete eklenir. Böylece WPF'te de doğru görünür.

### Doku varyantı değiştirme

Özellikler panelindeki **VARYANTLAR** küçük resimlerinden birine tıklanınca, seçili pikselin gerçek taş dokusu o varyantla değiştirilir; yalnızca o taşın bulunduğu bölge yeniden çizilir. Bu değişiklik de `Ctrl/⌘+Z` / `Ctrl/⌘+Y` ile geri alınır ve yinelenir.

Geri alma sırası: önce taş varyantı değişiklikleri, sonra piksel renk düzenlemeleri.

### Ne zaman sıfırlanır?

Yeni görsel yüklemek, yeni Mos ve Optimum **Taş** sayısının değiştirilmesi piksel düzenlemelerini ve taş varyantı geri-al geçmişini sıfırlar. **Stoğa göre** açıkken Stok Kontrol mozaiği yeniden düzeltmek zorunda kalırsa düzenlemeler de sıfırlanır; bunun için önce onay sorulur ([§11](#stok-kontrol-ve-stoğa-göre)).

## 10. Optimum taş sayısı

- **Ne yapar:** Görsel için kaç çeşit taş kullanılacağını kendisi bulur. **Optimum** kutusu işaretliyken Mos'a basınca çalışır. Kutu uygulama açılırken işaretsizdir; işaretsizken klasik algoritma kullanılır.
- **Nasıl çalışır:** Önce katalogdaki bütün işaretli taşlarla en iyi sonuç hesaplanır. Sonra görüntüyü en az bozan taşlar tek tek çıkarılır. Renk farkı, detay ve kenarlar ile açık-koyu yapısı gözle fark edilmeyecek kadar korunurken kullanılabilecek en az taş sayısı **öneri** olarak seçilir.
- **Taş kaydırıcısı:** Optimum ile yapılmış Mos'tan sonra görünür ve önerilen değerden başlar. Değer değişip kısa bir süre (yaklaşık 0,35 sn) sabit kalınca mozaik o taş sayısıyla yeniden kurulur; yanında `öneri K` yazar. Aynı ayar **Araçlar → Taş Sayısı** menüsünde de vardır: Önerilen Değere Dön, Bir Taş Artır, Bir Taş Azalt. Yeni bir görsel, proje ya da Mos başlatılırsa bekleyen yeniden kurma iptal edilir.
- **Seçim hafızası:** Mos'tan sonra katalogda yalnızca kullanılan taşlar işaretli kalır. Kataloğa elle dokunmadıysanız bir sonraki Optimum Mos, önceki seçiminizin tamamından yeniden başlar.
- **Stokla ilişkisi:**
  - Stok Çek ile devre dışı kalan taşlar Optimum'un taş havuzundan da çıkar.
  - Son Mos **Stoğa göre** düzeltildiyse kaydırıcıyla seçilen her yeni taş sayısı da aynı stoğa göre düzeltilir. Durum çubuğuna "taş sayısı değişti, tablo için Stok Kontrol'ü tekrarlayın" eklenir; tablodaki sütun hâlâ son Stok Kontrol'ün adetlerini taşır.

## 11. Stok yönetimi (Google Sheets)

Araç çubuğunda Detay ile Optimum arasında beş stok ikonu ve ikisinin yanında birer **▾** oku vardır; aynı işlemler **Araçlar → Stok** menüsünde de bulunur. Stok ayarı yapılmamışken bir stok düğmesine basılırsa "Stok tablosu ayarlı değil…" uyarısı çıkar.

### İlk kurulum

Yeşil tablo ikonunun yanındaki **▾** okuna basıp **Stok Ayarları...**'nı seçin (ikona sağ tıklamak veya **Araçlar → Stok → Stok Ayarları...** da olur).

**Stok Tablosu Ayarları penceresi:**

| Alan | Nereden alınır |
|---|---|
| **Google Sheet ID** | Tablonun bağlantısında `/d/` ile `/edit` arasındaki kısım (`docs.google.com/spreadsheets/d/<SHEET_ID>/edit`). Bağlantının tamamı yapıştırılırsa ID kaydederken kendiliğinden ayrılır. Stok Çek ve Stok Kontrol bu tabloyu okur. |
| **Apps Script URL** | Tabloda **Uzantılar → Apps Komut Dosyası → Dağıt → Dağıtımları yönet** yolundaki Web uygulaması adresi; `/exec` ile biter. Stok Kontrol, Stok Sil ve Stok Ekle tabloya bu adres üzerinden yazar. |

- Her alanın altında açıklama ve girilecek kısmı mavi ile vurgulayan bir örnek bağlantı vardır.
- Altta **"Doğru çalışması için"** başlıklı gereksinim listesi ve güvenlik notu bulunur.
- **Kaydet** ayarları saklar ve durum çubuğuna "Stok ayarları kaydedildi" yazar; **İptal** ya da pencereyi kapatmak hiçbir şeyi değiştirmez.

**Tablonun hazır olması gerekenler:**

- Tablo "Bağlantıya sahip olan herkes" için en az **Görüntüleyen** erişimiyle paylaşılmış olmalıdır.
- İlk satırda **mos**, **Bizdeki (kg)** ve **Tahmini Kalan** sütunları bulunmalıdır. Taşlar `mos` sütunundaki numarayla katalogdaki taş ID'sine eşleşir.
- Web uygulaması "Erişimi olanlar: **Herkes**" ayarıyla dağıtılmış olmalıdır. Betik tabloya önceden kurulmuş olmalıdır; bunu genellikle tablo sahibi yapar.
- Tabloyu değiştiren işlemleri denemek için önce tablonun bir kopyasıyla çalışılması önerilir.

**Ayarların saklanması:** Ayarlar yalnızca o bilgisayarda, kullanıcının uygulama verisi klasöründeki `mosair/stock.json` dosyasında saklanır; her bilgisayarda bir kez girilir. Script URL'yi bilen herkes tabloya yazabilir; yalnızca güvenilen kişilerle paylaşılmalıdır.

### Düğmeler

| Düğme | Ne yapar | Tabloyu değiştirir mi? |
|---|---|---|
| Stok Tablosu | Tabloyu tarayıcıda açar. Yanındaki **▾** okunda "Stok Tablosunu Aç" ve "Stok Ayarları..." vardır; ikona sağ tıklamak da Stok Ayarları'nı açar. | Hayır |
| Stok Çek | Her taşın **Bizdeki (kg)** değerini okur; ipucunda Bizdeki kg görünür. İkona tıklamak veya **▾** okundaki **"Stoğu olmayanları devre dışı bırak"**: değeri 0 veya altında olan taşların işaretini kaldırır, stoğu olanları işaretler; tabloda olmayan taşlara dokunmaz. Durum: "Stok çekildi: X taş okundu, Y taş devre dışı". **"Stoğu olmayanları kırmızıyla işaretle"**: seçimi değiştirmez, stoğu olmayan taşlara yalnızca **kırmızı nokta** koyar (yeni bir Mos noktaları temizler). | Hayır |
| Stok Kontrol | Mozaiğin taş adetlerini tablodaki proje sütununa yazar, sonra **Tahmini Kalan** ve **Bizdeki** değerlerini okur. Kalanı eksiye düşen taşlara **kırmızı nokta** koyar. İpucunda `Bizdeki → Kalan` görünür. Durum: "Stok kontrol (proje): tüm taşlar için stok yeterli" ya da "… N taşın stoğu yetersiz". **Stoğa göre** açıkken önce mozaiği stoğa göre denetler ([aşağıda](#stok-kontrol-ve-stoğa-göre)). | Evet |
| Stok Sil | Bu mozaiğin sütununu temizler ve başlığını `mozaikX` yapar. Sağ tık menüsünde tüm mozaik sütunlarını temizleme seçeneği vardır. Onay ister. | Evet |
| Stok Ekle | **Tahmini Kalan** değerlerini **Bizdeki** sütununa taşır ve bütün mozaik sütunlarını temizler. Onay ister. | Evet |

**Proje adı:** Tablodaki sütun adı görsel dosyasının adıdır (ör. `7.jpg` için `7`); görsel bilinmiyorsa açılan `.mos` dosyasının adı kullanılır. İkisi de yoksa Stok Kontrol ve Stok Sil (bu mozaiğin sütunu) çalışmaz. Stok Kontrol için ayrıca Mos yapılmış (ya da proje açılmış) olmalıdır.

### Stoğa göre (Optimum'un yanındaki kutu)

Kutu uygulama açılırken **kapalıdır**; **Araçlar → Stoğa Göre Ayarla** ile de açılıp kapanır.

- **İşaretliyken** Mos (Optimum ya da klasik) önce normal yapılır, sonra sonuç tablodaki stoğa sığdırılır. Kullanılabilir stok: **Bizdeki (kg) − diğer mozaik sütunlarının ayırdığı kg** (bu projenin sütunu sayılmaz), 1 taş = 3,3 g.
- Stoğu yetmeyen taş elde olduğu kadar kullanılır; kalan yer renkçe en yakın stoklu taşla doldurulur. Değişiklikler görüntünün en az fark edilecek yerlerine yönlendirilir, kenarlar (yüz hatları, gözler gibi) korunur.
- Önce mozaikte zaten kullanılan taşlar ve neredeyse aynı renkteki taşlar denenir; gerekirse en fazla **2 yeni taş türü** eklenir. Yakın renkte stok yetmezse arama adım adım genişler (benzerlik sınırı 2 kat, 4 kat, sonra stoğu olan herhangi bir taş, en son yeni taş türü sınırı aşılır); raporda ne kadar genişletildiği yazar.
- Az kullanılan taşlar mozaikten çıkarılmaz. Stok yeterliyse mozaik hiç değişmez.
- **Stoğun okunduğu an:** Stok uygulama açılırken ve her görsel ya da proje yüklendiğinde tablodan okunur. Mos bu okunmuş stoğu kullanır; o okuma başarısız olduysa Mos sırasında bir kez daha dener. **Stok Çek bu stoğu yenilemez.** Tabloyu uygulama açıkken değiştirdiyseniz görseli/projeyi yeniden yükleyin ya da Stok Kontrol'e güvenin (o, stoğu her seferinde yeniden okur).
- Stok ayarı hiç yapılmamışsa kutu etkisizdir ve Mos her zamanki gibi yapılır; not da çıkmaz. Ayar var ama stok okunamazsa Mos stoğa bakmadan yapılır ve durum çubuğunda yazar.
- **Sonuç:** Kısa özet durum çubuğunda, ayrıntılı rapor kutunun ipucunda görünür: hangi taştan hangisine kaç taş (kg) aktarıldığı (`#ID kod ad → #ID kod ad: N taş (kg)`), yeni eklenen taş türleri, aramanın genişletilip genişletilmediği, stoğu hâlâ yetmeyen taşlar ve tabloda satırı olmadığı için denetlenemeyen taşlar. Stoğu hâlâ yetmeyen taşlara kırmızı nokta konur; katalog ipucunda `Bizdeki → kalan kg` görünür.
- **Uzun sonuç:** Büyük mozaiklerde özet durum çubuğuna sığmayacak kadar uzarsa (çok sayıda taş listesi), durum çubuğunda yalnızca sayılar kalır (ör. "stoğu hâlâ yetmeyen 3 taş türü"). Mos ve Stok Kontrol sonrasında ayrıntılı rapor bir **Stoğa Göre** penceresinde açılır; pencere uzun raporu kaydırır ve metni seçip kopyalamaya izin verir. Taş kaydırıcısı oynatılınca pencere açılmaz, durum çubuğu ayrıntıların ipucunda olduğunu yazar.
- Klasik Mos'ta bazı pikseller tek bir katalog taşına kesin eşleştirilemezse düzeltme yapılmaz, **Stoğa Göre** uyarısı çıkar ve mozaik stoğa bakılmadan kalır.
- Optimum taş kaydırıcısı değiştirilince yeni taş sayısı da aynı stoğa göre düzeltilir ([§10](#10-optimum-taş-sayısı)).
- **Stok Kontrol** son adetleri tabloya yazar.

#### Stok Kontrol ve Stoğa göre

Stoğa göre açıkken Stok Kontrol, tabloya yazmadan önce stoğu yeniden okur ve mozaiğin buna sığıp sığmadığına bakar:

- **Sığıyorsa** mozaik değişmez; adetler yazılır. Durum: "Stok yeterli, mozaik değişmedi · adetler tabloya yazıldı".
- **Sığmıyorsa** (ör. tablo değişmiş ya da piksel düzenlemeleri stoğu aşmış) mozaik yeniden düzeltilir, sonra düzeltilmiş adetler tek seferde yazılır: "… · düzeltilmiş adetler tabloya yazıldı". Bu durumda katalogda yalnızca kullanılan taşlar işaretli kalır.
  - Piksel düzenlemeleri varsa önce "Stoğa göre düzeltme N piksel düzenlemesini sıfırlayacak. Devam edilsin mi?" sorulur. **Hayır** denirse mozaik değişmez ve mevcut adetler normal Stok Kontrol gibi yazılır.
- Kırmızı noktalar ve ipucundaki kalan kg, tablonun Tahmini Kalan'ından değil okunan stoktan hesaplanır (Google'ın tabloyu yeniden hesaplaması gecikebildiği için).
- Açılmış bir projede (bu oturumda Mos yapılmamışsa) düzeltme yapılamaz: "Stoğa göre düzeltme için önce bu görselle Mos yapın" uyarısı çıkar, ardından Stok Kontrol normal haliyle devam eder. Stok okunamazsa da uyarı çıkar ve normal Stok Kontrol yapılır.

### Önerilen akış

**Stoğa göre açıkken:**

1. **Mos** yapın; mozaik stoğa göre kurulur. Gerekirse Taş kaydırıcısıyla oynayın.
2. Sonucu ve **Stoğa göre** ipucundaki raporu inceleyin.
3. **Stok Kontrol** ile adetleri tabloya yazın.
4. Üretimden sonra **Stok Ekle** ile kalan stoğu tabloya işleyin.

**Stoğa göre kapalıyken (varsayılan):**

1. **Stok Çek** ile stoğu biten taşları devre dışı bırakın.
2. **Mos** yapın.
3. **Stok Kontrol** ile adetleri tabloya yazın.
4. Kırmızı noktalı taşlar varsa onları devre dışı bırakıp tekrar Mos yapın, kontrolü tekrarlayın.
5. Üretimden sonra **Stok Ekle** ile kalan stoğu tabloya işleyin.

## 12. Proje kaydetme ve açma

| İşlem | Davranış |
|---|---|
| **Proje Kaydet** (`Ctrl/⌘+S`) | `Masaüstü/mosairPROJECT/<görsel adı>/<görsel adı>.mos` olarak kaydeder. Orijinal görsel aynı klasöre kopyalanır (yoksa). Görsel adı yoksa `mosair_project` kullanılır. Kaydedince ikon kısa süre ✓ olur ve durum çubuğunda "Kaydedildi: …" yazar. |
| **Proje Farklı Kaydet** (`Ctrl/⌘+Shift+S`) | Seçilen ad için o adda bir klasör açar ve `.mos` dosyasını içine yazar; görsel `.mos`'un yanına kopyalanır (orada yoksa). |
| **Proje Aç** (`Ctrl/⌘+O`) | `.mos` dosyasını açar. Mozaik önce taş renkleriyle görünür; taş görüntüleri arka planda yüklenince gerçek taş dokularıyla gösterilir (durum çubuğunda bu sırada "Acildi, tas dokulari yukleniyor..." yazar). Orijinal görsel `.mos` ile aynı klasördeyse o da yüklenir (ölçü bilgileri ve stok proje adı için). Stok tablodan yeniden okunur. |

**Projede saklananlar:**

- Mozaik verisi, palet ve renk atamaları, katalog seçimi
- Piksel düzenlemeleri ve taş varyantları
- Bölgeler
- Genişlik (cm), Detay (N), ızgara açık/kapalı ve rengi, interpolasyon yöntemi

Optimum analizi, Optimum ve Stoğa göre kutularının durumu ve stok değerleri projeye kaydedilmez.

**WPF uyumluluğu:**

- `.mos` dosyası JSON biçimindedir ve WPF uygulamasıyla ortaktır.
  - mosair'de kaydedilen proje WPF'te açılır: orta sütun dolu gelir, görünüm varsayılan boyuttadır, robot ilk taştan başlar.
  - WPF'te kaydedilen JSON proje mosair'de doğru yönde açılır.
- WPF'te kaydedilmiş bir projeyi mosair'de açıp kaydetmek, WPF'e özgü bilgileri (robotun kaldığı yer, görünüm alanı) korur. Bu bilgiler yeni görsel yüklenince ya da yeni Mos yapılınca bırakılır.
- Eski WPF sürümlerinin **binary** `.mos` dosyaları mosair'de açılmaz; "Proje dosyası açılamadı" uyarısı çıkar. Bu dosyalar önce güncel WPF'te açılıp yeniden kaydedilmelidir.

## 13. Dışa aktarma (mosairEXPORT)

| İşlem | Davranış |
|---|---|
| Dışa aktar ikonuna **tıklama** | Liste açılır: **mosairEXPORT ▸** ve **mosairEXPORT As ▸**. İkona tıklamak doğrudan kaydetmez; önce görüntü kalitesi seçilir. |
| **mosairEXPORT ▸** → kalite | Seçilen görüntü kalitesiyle `Masaüstü/mosairEXPORT/` klasörüne `<ay.gün.yıl>_<ss.dd.ss>__<görsel adı>__<genişlik>x<yükseklik>.jpeg` adıyla kaydeder (ölçüler cm). Görsel adı yoksa `mosair` kullanılır. JPEG bu boyutta mümkün değilse ya da belleğe sığmayacaksa (aşağıda) aynı adla `.png` kaydeder. |
| **mosairEXPORT As ▸** → kalite | Konum, ad ve biçim (JPEG/PNG) sorar, seçilen görüntü kalitesiyle kaydeder. |
| `Ctrl/⌘+E` | Liste açılmadan, Detay ayarındaki (ekrandaki) görüntü kalitesiyle mosairEXPORT. |
| **Dosya** menüsü → mosairEXPORT ▸ / mosairEXPORT As ▸ | Araç çubuğundaki listeyle aynı alt menüler. |
| Dışa aktar ikonuna **sağ tık** | `mosairEXPORT` klasörünü dosya gezgininde açar (klasör yoksa oluşturulur) |

**Görüntü kalitesi listesi:** Her iki alt menünün başında pasif bir "Görüntü kalitesi seçiniz" başlığı, altında en düşükten en yükseğe 10 görüntü kalitesi vardır. Her seçenek yalnızca görüntünün piksel boyutunu ve tahmini dosya boyutunu gösterir:

- mosairEXPORT: `13.333 × 23.688 px · JPEG ≈ 420 MB` (yazılacak biçim ve boyutu).
- mosairEXPORT As: `13.333 × 23.688 px · JPEG ≈ … · PNG ≈ …`; JPEG'in kenar sınırını aşan boyutlarda `… px · PNG ≈ … (bu boyutta JPEG olmaz)`.
- Detay ayarına (ekrandaki görüntüye) karşılık gelen seçeneğin sonunda **(ekrandaki ayar)** yazar.
- Dosya boyutları liste açılınca arka planda, en küçük kaliteden başlayarak hesaplanır; hazır olmayan seçenekte "hesaplanıyor…" yazar. Hesaplananlar mozaik, ızgara ya da ızgara rengi değişene kadar hatırlanır.

Dışa aktarılan görüntü, ekrandaki gerçek taş dokulu görüntünün tamamıdır: her taş seçilen görüntü kalitesinde, kendi doku varyantıyla çizilir. Izgara açıksa ızgarayla birlikte kaydedilir. Taş görüntüleri yüklü değilse (ör. `02_RS` klasörü yoksa) her taş kendi renginde düz olarak, aynı boyutta çizilir; dosya her durumda listede gösterilen boyuttadır.

**Boyut sınırı yok:** Görüntü her zaman seçilen görüntü kalitesiyle kaydedilir; çok büyük mozaiklerde de kendiliğinden küçültülmez. Daha küçük bir dosya için daha düşük bir görüntü kalitesi seçin. Tek sınır JPEG biçiminin kendisidir: her kenar en fazla 65.535 piksel. JPEG bu boyutu aşarsa dışa aktarma başlamaz ve "JPEG bu boyutta kaydedilemez…" uyarısı PNG ya da daha düşük bir görüntü kalitesi seçmeyi önerir.

**Büyük dosyalar** (yaklaşık 536,9 milyon pikselden büyük görüntüler):

- **PNG** parça parça yazılır: her seferinde bir taş satırı çizilip doğrudan dosyaya sıkıştırılır. Görüntü ne kadar büyük olursa olsun az bellek kullanır.
- **JPEG** bütün görüntüyü bir kez bellekte tutar: yaklaşık genişlik × yükseklik × 4 bayt RAM ister (ör. 60.000 × 60.000 px için ≈ 13,4 GB). mosairEXPORT bu yüzden JPEG tamponu kullanılabilir belleğin yarısını aşacaksa ya da JPEG mümkün değilse PNG kaydeder. mosairEXPORT As ile böyle bir JPEG seçilirse önce sorulur ("Bu boyutta JPEG için yaklaşık … bellek gerekiyor…"): **Evet** ile JPEG kaydedilir (bilgisayar yavaşlayabilir), **Hayır** ile vazgeçilir. JPEG yine de kaydedilemezse "JPEG kaydedilemedi (bellek yetmemiş olabilir)…" uyarısı çıkar.
- Durum çubuğu ilerlemeyi yüzde olarak gösterir: "Dışa aktarılıyor: <dosya adı> — %42".
- Listede gösterilen dosya boyutu, mozaiğin birkaç parçası gerçekten kodlanarak yapılan bir tahmindir; genellikle biraz fazladır (JPEG'de ≈ %4, PNG'de ≈ %6–16).

Örnek ölçümler:

| Mozaik | Görüntü | PNG | JPEG |
|---|---|---|---|
| 300 × 300 taş | 12.000 × 12.000 px | 93 MB, ≈ 9 s | 113 MB, ≈ 4 s |
| 600 × 600 taş | 24.000 × 24.000 px | 498 MB, ≈ 5 s | 450 MB, ≈ 15 s |
| 20 m, 1667 × 1667 taş | 66.680 × 66.680 px | 2,8 GB, ≈ 26 s | Yazılamaz (kenar > 65.535 px) |

Kaydetme arka planda yapılır. Dışa aktarma yarıda kalırsa (hata, disk dolu…) yarım yazılmış dosya silinir. Dışa aktarma sürerken Mos ve dışa aktarma düğmeleri pasiftir. Piksel düzenleme bu sırada kapanmaz; dışa aktarma, başladığı andaki mozaiğin kopyasını yazdığı için bu sırada yapılan düzenlemeler dosyaya girmez.

## 14. Tema ve dil

| Kontrol | Ne yapar |
|---|---|
| ☾ / ☀ | Koyu ve açık tema arasında geçiş yapar (varsayılan koyu); iletişim kutuları ve kullanım kılavuzu da temaya uyar |
| 🌐 | Açılır listeden arayüz dilini **TR Türkçe** veya **EN English** yapar; menüler, ipuçları, mesajlar ve kullanım kılavuzu anında değişir |

Tema ve dil seçimi uygulama kapanınca hatırlanmaz; uygulama koyu tema ve Türkçe ile açılır.

## 15. Klavye kısayolları

| İşlem | Windows | macOS |
|---|---|---|
| Görsel yükle | `Ctrl+I` | `⌘+I` |
| Proje aç | `Ctrl+O` | `⌘+O` |
| Proje kaydet | `Ctrl+S` | `⌘+S` |
| Proje farklı kaydet | `Ctrl+Shift+S` | `⌘+⇧+S` |
| Dışa aktar (mosairEXPORT, Detay ayarındaki görüntü kalitesiyle) | `Ctrl+E` | `⌘+E` |
| Mozaikleştir | `Ctrl+M` | `⌘+M` |
| Ekrana sığdır | `Ctrl+0` (numpad 0 da olur) | `⌘+0` |
| Geri al | `Ctrl+Z` | `⌘+Z` (Ctrl+Z da çalışır) |
| Yinele | `Ctrl+Y` veya `Ctrl+Shift+Z` | `⌘+Y` veya `⌘+⇧+Z` (Ctrl ile de çalışır) |
| Kullanım kılavuzu | `F1` | `F1` |
| cm değerini uygula | `Enter` (cm kutusundayken) | `Enter` |

Kaydet, Farklı Kaydet ve Dışa Aktar kısayolları mozaik yokken bir şey yapmaz. `F1` yalnızca başka tuş basılı değilken çalışır.

## 16. Fare kontrolleri

| Nerede | Tuş | İşlem |
|---|---|---|
| Canvas | Tekerlek | Yakınlaştır / uzaklaştır |
| Canvas | Sağ tuş + sürükle | Kaydır |
| Canvas | Sol tuş | Taş seç / piksel düzenle |
| Canvas | Orta tuş | Piksel düzenleme modunu aç/kapat |
| Mini harita | Sol tuş (tıkla/sürükle) | O bölgeye git |
| Katalog | Onay kutusu | Taşı Mos'a dahil et / çıkar |
| Katalog | Satır (düzenleme modunda) | Kaynak rengi seç |
| Katalog | Doku küçük resmi üzerinde bekle | Büyük önizleme ve stok kg ipucu |
| Özellikler → Varyantlar | Sol tuş | Doku varyantını değiştir |
| Stok Tablosu ikonu | Sağ tuş | Stok Ayarları |
| Stok Tablosu **▾** | Sol tuş | Stok Tablosunu Aç / Stok Ayarları |
| Stok Çek **▾** | Sol tuş | Devre dışı bırak / yalnızca kırmızıyla işaretle |
| Stok Sil ikonu | Sağ tuş | Bu / tüm mozaik sütunlarını temizle |
| Stoğa göre kutusu | Üzerinde bekle | Açıklama ve son stok raporu |
| Dışa aktar ikonu | Sol tuş | Liste: mosairEXPORT ▸ / mosairEXPORT As ▸ ve görüntü kalitesi seçenekleri |
| Dışa aktar ikonu | Sağ tuş | mosairEXPORT klasörünü aç |
| Başlık çubuğu | Sürükle / çift tık | Pencereyi taşı / büyüt-küçült |
| Panel ayırıcıları | Sürükle | Panel genişliğini değiştir |

## 17. Uyarı ve hata mesajları

Uyarılar ortada küçük bir pencerede çıkar ve **Anladım** (EN: OK) ile kapanır. Onay soruları **Evet / Hayır** düğmeleriyle cevaplanır.

| Başlık | Ne zaman | Ne yapılmalı |
|---|---|---|
| Görsel Yükleme | Dosya okunamadı | Desteklenen biçimde, sağlam bir görsel seçin |
| Çözünürlük Yetersiz | İstenen genişlik görselin piksel genişliğinden fazla taş gerektiriyor | Daha büyük görsel kullanın veya cm'yi küçültün |
| Mozaikleştirme | Görsel yok / hiç taş seçili değil | Görsel yükleyin / katalogda taş işaretleyin |
| Bellek Yetersiz | Mozaikleştirme (Mos) sırasında bellek yetmedi | cm değerini küçültün (mesajda Detay (N) de geçer, ama Mos'un bellek kullanımı N'ye bağlı değildir) |
| Hata | Beklenmeyen bir hata (mesajda ayrıntı yazar) | Mesajı not edin, işlemi tekrarlayın |
| Proje | Proje dosyası açılamadı (bozuk, uyumsuz ya da eski binary) | Binary dosyayı güncel WPF'te açıp yeniden kaydedin |
| Proje | Proje kaydedilemedi (disk dolu, klasöre yazma izni yok, dosya başka programda açık…) | Sorunu giderip tekrar kaydedin; uygulama açık kalır, çalışma kaybolmaz |
| (durum çubuğu) | "Renk kataloğunda okunamayan satırlar atlandı" | `colorsBas.txt` içinde belirtilen satırları düzeltin; diğer taşlar normal yüklenir |
| Dışa Aktarma | Dışa aktarılacak mozaik yok | Önce Mos yapın |
| Dışa Aktarma | JPEG bu boyutta kaydedilemez (bir kenar 65.535 pikseli aşıyor) | PNG ya da daha düşük bir görüntü kalitesi seçin |
| Dışa Aktarma | Onay: bu boyutta JPEG yaklaşık … bellek gerektiriyor (mosairEXPORT As ile büyük JPEG seçilince) | **Evet** ile JPEG kaydedilir, **Hayır** ile vazgeçilir; PNG neredeyse hiç bellek kullanmaz |
| Dışa Aktarma | "Beklenmeyen bir hata oluştu: JPEG kaydedilemedi (bellek yetmemiş olabilir)…" | PNG ya da daha düşük bir görüntü kalitesi deneyin |
| Dışa Aktarma | Dosya yazılamadı: "Beklenmeyen bir hata oluştu" ve ayrıntı (disk dolu, yazma izni yok, bellek yetmedi…) | Sorunu giderip tekrar deneyin; büyük görüntüde PNG çok daha az bellek ister |
| Stok | Ayar eksik, sütun bulunamadı, tablo boş, Script URL/yayın hatası, proje adı ya da Mos yok | Mesajdaki adımı uygulayın; [§11](#11-stok-yönetimi-google-sheets) |
| Stok Temizle / Tümünü Temizle / Stok Ekle | Onay sorusu (tabloyu değiştirmeden önce) | **Evet** ile devam edin, **Hayır** ile vazgeçin |
| Stoğa Göre | Düzeltme yapılamadı: katalog dışı renk ya da aynı renkte iki taş; açılmış projede Mos yapılmamış; stok okunamadı | Mesaja göre görselle yeniden Mos yapın ya da stok ayarını kontrol edin |
| Stoğa Göre | Onay: düzeltme piksel düzenlemelerini sıfırlayacak | **Evet** ile düzeltin, **Hayır** ile mevcut adetleri yazın |

## 18. Bilinen davranışlar ve sınırlamalar

- **Proje Kaydet** her zaman `Masaüstü/mosairPROJECT/<görsel adı>/` konumuna yazar; başka bir yerden açılmış bir projenin üzerine yazmaz. Belirli bir konuma kaydetmek için **Farklı Kaydet** kullanın.
- Tema ve dil tercihi kalıcı değildir. Optimum ve Stoğa göre kutuları da her açılışta işaretli başlar.
- Optimum **Taş** sayısı her değiştiğinde ve her yeni Mos'ta piksel düzenlemeleri sıfırlanır.
- Bir proje açıldığında Optimum **Taş** kaydırıcısı görünmez; proje dosyası Optimum analizini içermez. Kaydırıcı için Optimum açıkken yeniden Mos yapın.
- Stok değerleri (kırmızı nokta, kg) yalnızca bellektedir; projeye kaydedilmez ve yeni Mos ile silinir.
- Stoğa göre Mos, görsel/proje yüklenirken okunan stoğu kullanır; Stok Çek bu stoğu yenilemez ([§11](#stoğa-göre-optimumun-yanındaki-kutu)).
- Taş dokulu görüntü parça parça çizilir; hızlı kaydırma ve yakınlaştırmada bir bölge, daha keskin hali hazırlanana kadar kısa bir süre bulanık ya da yalnızca taş renkleriyle görünebilir.
- **Ekrana Sığdır** ve fare tekerleği en fazla ‰1 (0,001) zoom'a kadar uzaklaştırır; 20 m'lik bir mozaik N=100 ile de pencereye sığar. Zoom etiketi 0,1'in altında üç basamak gösterir.
- Dışa aktarmada boyut sınırı yoktur ve görüntü kendiliğinden küçültülmez; yalnızca JPEG kenar başına 65.535 pikselle sınırlıdır. Çok büyük bir JPEG yaklaşık genişlik × yükseklik × 4 bayt bellek ister ([§13](#13-dışa-aktarma-mosairexport)).
- Stok Kontrol, Google'ın tablo çıktısı gecikebildiği için nadiren bir önceki değeri okuyabilir; şüphede kontrolü tekrarlayın. (Stoğa göre açıkken kırmızı noktalar okunan stoktan hesaplandığı için bu durumdan etkilenmez.)
