# mosair Arayüz Rehberi

> Uygulamada yapılabilecek **her şey** bu dosyadadır: menüler, araç çubuğu, paneller, fare ve klavye, stok, proje ve dışa aktarma.
> Kod tarafı için [`kod/`](kod/) klasörüne, belgelerin haritası için [README.md](README.md) dosyasına bakın.
> Uygulama içindeki kılavuz (**Yardım → Kullanım Kılavuzu**, F1) bu dosyanın kısa özetidir; ikisi birbiriyle çelişmemelidir.
>
> Güncelleme: 2026-10-04 · Kapsadığı sürüm: `feature` dalı (v1.3.0 sonrası)

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
│ Başlık çubuğu + Menü: Dosya  Düzenle  Görünüm  Araçlar  Yardım                    │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Araç çubuğu: Görsel · Proje Aç · Kaydet · Farklı Kaydet │ Mos │ Kalem │ Izgara      │
│   Interp · Detay │ 5 stok ikonu │ ☐ Optimum  Taş ──●── öneri N   … Dışa Aktar ▾ ☾ 🌐 │
├──────────────────────────────────────────────────────────────────────────────────┤
│ Durum çubuğu: kullanılan renk │ ilerleme + durum + süre │ Ekrana Sığdır · zoom %  │
├───────────────────────┬───────────────────────────────────┬──────────────────────┤
│ SOL PANEL             │ GÖRSEL ALANI (canvas)             │ ÖZELLİKLER PANELİ    │
│ cm · ölçü · alan      │                       ┌─────────┐ │ RENK                 │
│ taş / kalıp / orijinal│                       │Mini     │ │ DOKU                 │
│ [Tümünü Seç][Kaldır]  │                       │harita   │ │ VARYANTLAR           │
│ Katalog│Eşleşme│Atanan│                       └─────────┘ │ RGB · KOORDİNAT      │
│        │       │      │                                   │ DÜZENLEME            │
└───────────────────────┴───────────────────────────────────┴──────────────────────┘
```

- Paneller arasındaki ince çizgiler (ayırıcılar) sürüklenerek genişlik değiştirilebilir. Sol panel 220–380 px, sağ panel 160–360 px arasında ayarlanır.
- Pencere kendi başlık çubuğunu kullanır: boş bir yerinden sürükleyerek pencere taşınır, **çift tıklayınca** pencere büyür ya da eski boyutuna döner.

## 2. Temel iş akışı

| Adım | Ne yapılır | Nereden |
|---|---|---|
| 1 | Görsel yükle (PNG, JPG, JPEG, BMP, TIFF) | Görsel ikonu, **Dosya → Görsel Yükle**, `Ctrl/⌘+I` veya dosyayı pencereye sürükle-bırak |
| 2 | Mozaik genişliğini cm olarak gir | Sol panelin üstündeki kutu |
| 3 | Katalogda kullanılabilecek taşları seç (isteğe bağlı: **Stok Çek**) | Sol panel, stok ikonları |
| 4 | Mozaikleştir | **Mos** düğmesi veya `Ctrl/⌘+M` |
| 5 | İncele, gerekirse piksel ve taş varyantı düzenle | Canvas, Özellikler paneli |
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
| mosairEXPORT | `Ctrl/⌘+E` | Görüntüyü `Masaüstü/mosairEXPORT` klasörüne JPEG olarak kaydeder | Mozaik varken |
| mosairEXPORT As | — | Konum, ad ve biçim (JPEG/PNG) sorarak dışa aktarır | Mozaik varken |

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
| Mozaikleştir | `Ctrl/⌘+M` | Mos çalıştırır |
| Piksel Düzenle | Orta fare tuşu | Piksel düzenleme modunu açar/kapatır ([§9](#9-piksel-düzenleme-ve-taş-varyantı)); mozaik yoksa pasiftir |
| Izgara Göster | — | Taşlar arası ızgara çizgilerini açar/kapatır |
| Izgara Rengi ▸ | — | 12 hazır renk; seçilen rengin 7 tonu ayrıca listelenir |
| İnterpolasyon Yöntemi ▸ | — | Area, Nearest, Linear, Cubic, Lanczos4, LinearExact, NearestExact |
| Detay Seviyesi ▸ | — | 10–100 arası (10'ar adım) taş başına piksel (N) |
| Optimum Taş Sayısı | — | Optimum modunu açar/kapatır ([§10](#10-optimum-taş-sayısı)) |
| Taş Sayısı ▸ Önerilen Değere Dön / Bir Taş Artır / Bir Taş Azalt | — | Optimum Mos'tan sonra kullanılacak taş çeşidi sayısını değiştirir |
| Stok ▸ | — | Stok Tablosunu Aç, Stok Ayarları..., Stok Çek, Stok Kontrol, Bu mozaiğin sütununu temizle, Tüm mozaik sütunlarını temizle, Stok Ekle ([§11](#11-stok-yönetimi-google-sheets)) |

### Yardım

| Öğe | Kısayol | Ne yapar |
|---|---|---|
| Kullanım Kılavuzu | `F1` | Uygulama içi kılavuzu açar (TR/EN, solda bölüm menüsü) |

## 4. Araç çubuğu

Soldan sağa:

| Kontrol | Tıklama | Sağ tık / ek | Not |
|---|---|---|---|
| Görsel Yükle | Görsel seçer | — | Sürükle-bırak da olur |
| Proje Aç | `.mos` açar | — | |
| Proje Kaydet | Kaydeder; ikon kısa süre ✓ olur | — | Mozaik yokken pasif |
| Proje Farklı Kaydet | Konum sorarak kaydeder | — | Mozaik yokken pasif |
| **Mos** | Mozaikleştirir | — | Görsel yokken veya işlem sürerken pasif; çalışırken ikon animasyonludur |
| Kalem (Piksel Düzenle) | Düzenleme modunu aç/kapat | Orta tuş da aynı işi yapar | Açıkken kalem turuncu, yanında `source → target` göstergesi çıkar |
| Izgara | Açılır panel: Grid ON/OFF, Grid Rengi (12 renk + 7 ton) | — | Görsel yüklenince ızgara rengi görselin parlaklığına göre otomatik gri tona ayarlanır |
| İnterpolasyon | Açılır liste (7 yöntem) | — | Varsayılan **Area** |
| Detay (N) | Açılır kaydırıcı 10–100 | — | Varsayılan **40**; 40'ın üstü ilk seferde performans uyarısı gösterir |
| Stok Tablosu (yeşil tablo) | Tabloyu tarayıcıda açar | **Stok Ayarları...** | |
| Stok Çek | Bizdeki (kg) okunur | — | Tabloyu değiştirmez |
| Stok Kontrol | Adetler tabloya yazılır, kalan okunur | — | Tabloyu değiştirir |
| Stok Sil (küp −) | Bu mozaiğin sütununu temizler | **Bu mozaiğin sütununu temizle / Tüm mozaik sütunlarını temizle** | Onay ister |
| Stok Ekle (küp +) | Tahmini Kalan → Bizdeki | — | Onay ister |
| ☐ Optimum | Optimum modunu aç/kapat | — | Varsayılan **açık** |
| Taş ──●── N · öneri K | Taş çeşidi sayısını değiştirir | — | Yalnızca Optimum ile yapılmış bir Mos'tan sonra görünür |
| mosairEXPORT (sağda) | Masaüstü/mosairEXPORT'a JPEG | **Sağ tık: klasörü açar** | Kaydederken ok animasyonu oynar |
| ▾ (dışa aktar yanındaki) | mosairEXPORT / mosairEXPORT As | — | |
| ☾ / ☀ | Koyu/açık tema | — | |
| 🌐 | Dil: Türkçe / English | — | |

Stok işlemi sürerken beş stok düğmesi geçici olarak pasif olur.

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
| **Eşleşme** | Mozaiğin ara paletindeki renkler (indeks sırasıyla) | Atanan sütunuyla birlikte kayar |
| **Atanan Renk** | Mozaikte gerçekten kullanılan taşlar: ID, kod, adet; satır arka planı taş rengidir | Eşleşme sütunuyla birlikte kayar |

**Katalog ipucu (tooltip):** Taşın doku küçük resminin üzerine gelince açılır.

- Başlık: `kod  ad  R G B`. Altında taşın büyük doku önizlemesi görünür (doku varsa).
- Sağ üstte stok bilgisi:
  - **İki değer de biliniyorsa:** `Bizdeki → Tahmini Kalan kg`.
  - **Yalnız Bizdeki biliniyorsa:** `X kg`.
  - **Yalnız kalan biliniyorsa:** `X kg kaldı`.
  - Renk: her sayı 0 veya altındaysa **kırmızı**, üstündeyse **yeşil**.
  - Elimizdeki kg, uygulama açılırken ve her görsel ya da proje yüklendiğinde stok ayarındaki tablodan kendiliğinden yüklenir; Stok Çek ve Stok Kontrol de günceller. Yeni bir Mos kırmızı noktaları ve "kalan" değerini siler, elimizdeki kg'ı korur.

**Mos'tan sonra:** Katalogda yalnızca mozaikte kullanılan taşlar işaretli kalır.

## 6. Görsel alanı (canvas)

| İşlem | Nasıl |
|---|---|
| Yakınlaştır/uzaklaştır | Fare tekerleği; imlecin altındaki nokta sabit kalır. Her adım ×1,25. En fazla 20×, en az ekrana sığdırma ölçüsü. |
| Kaydır (pan) | **Sağ tuşu basılı tutup sürükle** |
| Taş seç | **Sol tık**: Özellikler paneli o taşı gösterir |
| Piksel düzenle | Düzenleme modundayken sol tık ([§9](#9-piksel-düzenleme-ve-taş-varyantı)) |
| Düzenleme modunu aç/kapat | **Orta tuş** |
| Görsel yükle | Dosyayı pencereye sürükle-bırak (PNG, JPG, JPEG, BMP, TIFF; ilk uygun dosya alınır) |
| Ekrana sığdır | Durum çubuğundaki ⛶ düğmesi, `Ctrl/⌘+0` veya **Görünüm → Ekrana Sığdır**. Mos ve Proje Aç sonrasında kendiliğinden uygulanır. |

**Mini harita (navigator):** Sağ üstteki 150×150 küçük görüntüdür. Yeşil çerçeve ekranda görünen bölgeyi gösterir. Tıklamak ya da sürüklemek o bölgeye götürür. Görsel yüklüyken görünür.

**Performans bildirimi:** Detay (N) ilk kez 40'ın üstüne çıkarıldığında ortada bir uyarı kutusu çıkar ve **Anladım** ile kapanır. Oturum boyunca bir kez gösterilir.

## 7. Özellikler paneli (Properties)

Canvas'ta bir taşa sol tıklayınca dolar.

| Bölüm | Gösterdiği | Etkileşim |
|---|---|---|
| RENK | Renk kutusu, `#ID`, taş kodu | — |
| DOKU | Seçili taşın o pikselde kullanılan doku görüntüsü ve varyant numarası | — |
| VARYANTLAR | Aynı taşın bütün doku varyantları (küçük resimler) | **Tıkla:** o piksel için doku varyantını değiştirir. Geri alınabilir (`Ctrl/⌘+Z`). |
| RGB | Pikselin R, G, B değerleri | — |
| KOORDİNAT | Piksel `Y, X` ve kalıp içi `yi, xi` | — |
| DÜZENLEME | Düzenlenen piksel sayısı, geri al/yinele kısayolları | Yalnızca piksel düzenleme açıkken görünür |

## 8. Durum çubuğu

| Bölge | İçerik |
|---|---|
| Sol | Mos'tan sonra kullanılan renk bilgisi (ör. "X renk arasından Y renk kullanıldı") |
| Orta | İlerleme çubuğu (işlem sırasında), durum mesajı, geçen süre |
| Sağ | **Ekrana Sığdır** düğmesi; zoom oranı (`N=2.0` biçiminde, Detay N ile karıştırılmamalı) ve ekrandaki görüntü boyutu (px) |

## 9. Piksel düzenleme ve taş varyantı

### Renk değiştirme (piksel düzenleme)

1. **Modu açın:** orta tuş, araç çubuğundaki kalem ya da **Araçlar → Piksel Düzenle**. Önce Mos yapılmış olmalıdır. Kalem turuncu yanar ve `source` göstergesi aktif olur.
2. **Kaynak rengi seçin:** istediğiniz renkteki bir taşa sol tıklayın **veya** sol paneldeki katalogdan bir taş seçin. Gösterge `target`'a geçer.
3. **Hedefe uygulayın:** değiştirmek istediğiniz taşlara sol tıklayın. Renk anında değişir; hedef modunda kalınır, aynı kaynakla birden çok piksel boyanabilir.
4. **Geri al / yinele:** `Ctrl/⌘+Z` geri alır. `Ctrl/⌘+Y` veya `Ctrl/⌘+Shift+Z` yineler.
5. **Kapatmak için** modu tekrar tetikleyin (orta tuş/kalem). Kapanınca katalog seçimi temizlenir.

**Kataloğa geçen kaynaklar:** Düzenlemede katalogdan seçilip mozaiğin paletinde olmayan bir taş kullanılırsa, proje kaydedilirken bu taş palete eklenir. Böylece WPF'te de doğru görünür.

### Doku varyantı değiştirme

Özellikler panelindeki **VARYANTLAR** küçük resimlerinden birine tıklanınca, seçili pikselin gerçek taş dokusu o varyantla değiştirilir. Bu değişiklik de `Ctrl/⌘+Z` / `Ctrl/⌘+Y` ile geri alınır ve yinelenir.

Geri alma sırası: önce taş varyantı değişiklikleri, sonra piksel renk düzenlemeleri.

### Ne zaman sıfırlanır?

Yeni görsel yüklemek, yeni Mos ve Optimum **Taş** sayısının değiştirilmesi piksel düzenlemelerini ve taş varyantı geri-al geçmişini sıfırlar.

## 10. Optimum taş sayısı

- **Ne yapar:** Görsel için kaç çeşit taş kullanılacağını kendisi bulur. **Optimum** kutusu işaretliyken (varsayılan) Mos'a basınca çalışır. İşaret kaldırılırsa klasik algoritma kullanılır.
- **Nasıl çalışır:** Önce katalogdaki bütün işaretli taşlarla en iyi sonuç hesaplanır. Sonra görüntüyü en az bozan taşlar tek tek çıkarılır. Renk farkı, detay ve kenarlar ile açık-koyu yapısı gözle fark edilmeyecek kadar korunurken kullanılabilecek en az taş sayısı **öneri** olarak seçilir.
- **Taş kaydırıcısı:** Optimum ile yapılmış Mos'tan sonra görünür ve önerilen değerden başlar. Değer değişip kısa bir süre (yaklaşık 0,35 sn) sabit kalınca mozaik o taş sayısıyla yeniden kurulur; yanında `öneri K` yazar. Aynı ayar **Araçlar → Taş Sayısı** menüsünde de vardır: Önerilen Değere Dön, Bir Taş Artır, Bir Taş Azalt.
- **Seçim hafızası:** Mos'tan sonra katalogda yalnızca kullanılan taşlar işaretli kalır. Kataloğa elle dokunmadıysanız bir sonraki Optimum Mos, önceki seçiminizin tamamından yeniden başlar.
- **Stokla ilişkisi:** Stok Çek ile devre dışı kalan taşlar Optimum'un taş havuzundan da çıkar.

## 11. Stok yönetimi (Google Sheets)

Araç çubuğunda Detay ile Optimum arasında beş ikon vardır; aynı işlemler **Araçlar → Stok** menüsünde de bulunur.

### İlk kurulum

Yeşil tablo ikonuna **sağ tıklayın** (veya **Araçlar → Stok → Stok Ayarları...**). Açılan pencerede her alanın altında açıklama ve vurgulu bir örnek bağlantı vardır.

| Alan | Nereden alınır |
|---|---|
| **Google Sheet ID** | Tablonun bağlantısında `/d/` ile `/edit` arasındaki kısım. Bağlantının tamamı yapıştırılırsa ID kaydederken kendiliğinden ayrılır. |
| **Apps Script URL** | Tabloda **Uzantılar → Apps Komut Dosyası → Dağıt → Dağıtımları yönet** yolundaki Web uygulaması adresi; `/exec` ile biter. |

**Tablonun hazır olması gerekenler:**

- Tablo "Bağlantıya sahip olan herkes" için en az **Görüntüleyen** erişimiyle paylaşılmış olmalıdır.
- İlk satırda **mos**, **Bizdeki (kg)** ve **Tahmini Kalan** sütunları bulunmalıdır.
- Web uygulaması "Erişimi olanlar: **Herkes**" ayarıyla dağıtılmış olmalıdır. Betik tabloya önceden kurulmuş olmalıdır; bunu genellikle tablo sahibi yapar.
- Tabloyu değiştiren işlemleri denemek için önce tablonun bir kopyasıyla çalışılması önerilir.

**Ayarların saklanması:** Ayarlar yalnızca o bilgisayarda saklanır; her bilgisayarda bir kez girilir. Script URL'yi bilen herkes tabloya yazabilir; yalnızca güvenilen kişilerle paylaşılmalıdır.

### Düğmeler

| Düğme | Ne yapar | Tabloyu değiştirir mi? |
|---|---|---|
| Stok Tablosu | Tabloyu tarayıcıda açar | Hayır |
| Stok Çek | Her taşın **Bizdeki (kg)** değerini okur. Değeri 0 veya altında olan taşların işaretini kaldırır, stoğu olanları işaretler; tabloda olmayan taşlara dokunmaz. İpucunda Bizdeki kg görünür. | Hayır |
| Stok Kontrol | Mozaiğin taş adetlerini tablodaki proje sütununa yazar, sonra **Tahmini Kalan** ve **Bizdeki** değerlerini okur. Kalanı eksiye düşen taşlara **kırmızı nokta** koyar; **seçimi değiştirmez**. İpucunda `Bizdeki → Kalan` görünür. | Evet |
| Stok Sil | Bu mozaiğin sütununu temizler ve başlığını `mozaikX` yapar. Sağ tık menüsünde tüm mozaik sütunlarını temizleme seçeneği vardır. Onay ister. | Evet |
| Stok Ekle | **Tahmini Kalan** değerlerini **Bizdeki** sütununa taşır ve bütün mozaik sütunlarını temizler. Onay ister. | Evet |

**Proje adı:** Tablodaki sütun adı görsel dosyasının adıdır (ör. `7.jpg` için `7`); görsel bilinmiyorsa açılan `.mos` dosyasının adı kullanılır. İkisi de yoksa Stok Kontrol ve Stok Sil çalışmaz. Stok Kontrol için ayrıca Mos yapılmış olmalıdır.

### Stoğa göre (Optimum'un yanındaki kutu)

- **İşaretliyken** Mos (Optimum ya da klasik) yalnızca stokta olan taşlarla yapılır. Kullanılabilir stok: **Bizdeki (kg) − diğer mozaik sütunlarının ayırdığı kg** (bu projenin sütunu sayılmaz), 1 taş = 3,3 g.
- Stoğu yetmeyen taş elde olduğu kadar kullanılır; kalan yer renkçe en yakın stoklu taşla doldurulur. Çok az kullanılacak taşlar (en az 10, büyük görsellerde toplamın ‰0,5'i) hiç kullanılmaz.
- Stok, uygulama açılırken ve her görsel ya da proje yüklendiğinde tablodan okunur; Mos sırasında tablo okunamazsa Mos stoğa bakmadan yapılır ve durum çubuğunda yazar.
- Sonuç ve değişen taşlar durum çubuğunda ve kutunun ipucunda görünür. **Stok Kontrol** son adetleri tabloya yazar.
- Optimum taş kaydırıcısı değiştirilince yeni taş sayısı da aynı stoğa göre düzeltilir.

### Önerilen akış

1. **Stok Çek** ile stoğu biten taşları devre dışı bırakın.
2. **Mos** yapın.
3. **Stok Kontrol** ile adetleri tabloya yazın.
4. Kırmızı noktalı taşlar varsa onları devre dışı bırakıp tekrar Mos yapın, kontrolü tekrarlayın.
5. Üretimden sonra **Stok Ekle** ile kalan stoğu tabloya işleyin.

## 12. Proje kaydetme ve açma

| İşlem | Davranış |
|---|---|
| **Proje Kaydet** (`Ctrl/⌘+S`) | `Masaüstü/mosairPROJECT/<görsel adı>/<görsel adı>.mos` olarak kaydeder. Orijinal görsel aynı klasöre kopyalanır (yoksa). Görsel adı yoksa `mosair_project` kullanılır. |
| **Proje Farklı Kaydet** (`Ctrl/⌘+Shift+S`) | Seçilen ad için o adda bir klasör açar ve `.mos` dosyasını içine yazar; görsel `.mos`'un yanına kopyalanır. |
| **Proje Aç** (`Ctrl/⌘+O`) | `.mos` dosyasını açar ve gerçek taş dokulu görüntüyü yeniden oluşturur. Orijinal görsel `.mos` ile aynı klasördeyse o da yüklenir (ölçü bilgileri ve stok proje adı için). |

**Projede saklananlar:**

- Mozaik verisi, palet ve renk atamaları, katalog seçimi
- Piksel düzenlemeleri ve taş varyantları
- Bölgeler
- Genişlik (cm), Detay (N), ızgara açık/kapalı ve rengi, interpolasyon yöntemi

**WPF uyumluluğu:**

- `.mos` dosyası JSON biçimindedir ve WPF uygulamasıyla ortaktır.
  - mosair'de kaydedilen proje WPF'te açılır: orta sütun dolu gelir, görünüm varsayılan boyuttadır, robot ilk taştan başlar.
  - WPF'te kaydedilen JSON proje mosair'de doğru yönde açılır.
- WPF'te kaydedilmiş bir projeyi mosair'de açıp kaydetmek, WPF'e özgü bilgileri (robotun kaldığı yer, görünüm alanı) korur. Bu bilgiler yeni görsel yüklenince ya da yeni Mos yapılınca bırakılır.
- Eski WPF sürümlerinin **binary** `.mos` dosyaları mosair'de açılmaz; "Proje dosyası açılamadı" uyarısı çıkar. Bu dosyalar önce güncel WPF'te açılıp yeniden kaydedilmelidir.

## 13. Dışa aktarma (mosairEXPORT)

| İşlem | Davranış |
|---|---|
| **mosairEXPORT** (`Ctrl/⌘+E`) | `Masaüstü/mosairEXPORT/` klasörüne `<ay.gün.yıl>_<ss.dd.ss>__<görsel adı>__<genişlik>x<yükseklik>.jpeg` adıyla kaydeder (ölçüler cm). |
| **mosairEXPORT As** | Konum ve biçim sorar: JPEG veya PNG. |
| Dışa aktar ikonuna **sağ tık** | `mosairEXPORT` klasörünü dosya gezgininde açar |

Dışa aktarılan görüntü, ekrandaki gerçek taş dokulu görüntüdür. Izgara açıksa ızgarayla birlikte kaydedilir. Kaydetme arka planda yapılır; bu sırada yapılan düzenlemeler dosyaya karışmaz.

## 14. Tema ve dil

| Kontrol | Ne yapar |
|---|---|
| ☾ / ☀ | Koyu ve açık tema arasında geçiş yapar (varsayılan koyu); iletişim kutuları ve kullanım kılavuzu da temaya uyar |
| 🌐 | Arayüz dilini **Türkçe** veya **English** yapar; menüler, ipuçları, mesajlar ve kullanım kılavuzu anında değişir |

Tema ve dil seçimi uygulama kapanınca hatırlanmaz; uygulama koyu tema ve Türkçe ile açılır.

## 15. Klavye kısayolları

| İşlem | Windows | macOS |
|---|---|---|
| Görsel yükle | `Ctrl+I` | `⌘+I` |
| Proje aç | `Ctrl+O` | `⌘+O` |
| Proje kaydet | `Ctrl+S` | `⌘+S` |
| Proje farklı kaydet | `Ctrl+Shift+S` | `⌘+⇧+S` |
| Dışa aktar (mosairEXPORT) | `Ctrl+E` | `⌘+E` |
| Mozaikleştir | `Ctrl+M` | `⌘+M` |
| Ekrana sığdır | `Ctrl+0` (numpad 0 da olur) | `⌘+0` |
| Geri al | `Ctrl+Z` | `⌘+Z` (Ctrl+Z da çalışır) |
| Yinele | `Ctrl+Y` veya `Ctrl+Shift+Z` | `⌘+Y` veya `⌘+⇧+Z` (Ctrl ile de çalışır) |
| Kullanım kılavuzu | `F1` | `F1` |

Kaydet, Farklı Kaydet ve Dışa Aktar kısayolları mozaik yokken bir şey yapmaz.

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
| Stok Sil ikonu | Sağ tuş | Bu / tüm mozaik sütunlarını temizle |
| Dışa aktar ikonu | Sağ tuş | mosairEXPORT klasörünü aç |
| Başlık çubuğu | Sürükle / çift tık | Pencereyi taşı / büyüt-küçült |
| Panel ayırıcıları | Sürükle | Panel genişliğini değiştir |

## 17. Uyarı ve hata mesajları

| Başlık | Ne zaman | Ne yapılmalı |
|---|---|---|
| Görsel Yükleme | Dosya okunamadı | Desteklenen biçimde, sağlam bir görsel seçin |
| Çözünürlük Yetersiz | İstenen genişlik görselin piksel genişliğinden fazla taş gerektiriyor | Daha büyük görsel kullanın veya cm'yi küçültün |
| Mozaikleştirme | Görsel yok / hiç taş seçili değil | Görsel yükleyin / katalogda taş işaretleyin |
| Değer Çok Büyük, Bellek Yetersiz | Detay (N) veya cm çok büyük, bellek yetmiyor | N veya cm değerini küçültün |
| Proje | Proje dosyası açılamadı (bozuk, uyumsuz ya da eski binary) | Binary dosyayı güncel WPF'te açıp yeniden kaydedin |
| Proje | Proje kaydedilemedi (disk dolu, klasöre yazma izni yok, dosya başka programda açık…) | Sorunu giderip tekrar kaydedin; uygulama açık kalır, çalışma kaybolmaz |
| (durum çubuğu) | "Renk kataloğunda okunamayan satırlar atlandı" | `colorsBas.txt` içinde belirtilen satırları düzeltin; diğer taşlar normal yüklenir |
| Dışa Aktarma | Dışa aktarılacak mozaik yok | Önce Mos yapın |
| Stok | Ayar eksik, sütun bulunamadı, Script URL/yayın hatası, proje adı ya da Mos yok | Mesajdaki adımı uygulayın; [§11](#11-stok-yönetimi-google-sheets) |

## 18. Bilinen davranışlar ve sınırlamalar

- **Proje Kaydet** her zaman `Masaüstü/mosairPROJECT/<görsel adı>/` konumuna yazar; başka bir yerden açılmış bir projenin üzerine yazmaz. Belirli bir konuma kaydetmek için **Farklı Kaydet** kullanın.
- Tema ve dil tercihi kalıcı değildir.
- Optimum **Taş** sayısı her değiştiğinde ve her yeni Mos'ta piksel düzenlemeleri sıfırlanır.
- Bir proje açıldığında Optimum **Taş** kaydırıcısı görünmez; proje dosyası Optimum analizini içermez. Kaydırıcı için Optimum açıkken yeniden Mos yapın.
- Stok değerleri (kırmızı nokta, kg) yalnızca bellektedir; projeye kaydedilmez ve yeni Mos ile silinir.
- Stok Kontrol, Google'ın tablo çıktısı gecikebildiği için nadiren bir önceki değeri okuyabilir; şüphede kontrolü tekrarlayın.
