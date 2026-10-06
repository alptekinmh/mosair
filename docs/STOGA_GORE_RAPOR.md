# "Stoğa Göre" Özelliği — Durum Raporu ve Entegrasyon Planı

> Dal: `stok-deneme` (yerel, push edilmedi) · Rapordaki kod durumu: `e5b869c` · Tarih: 2026-10-06
> Bu rapor, yarın verilecek karar için hazırlandı. Kodda bu rapordan sonra değişiklik yapılmadı.

**İsteğin okunuşu:** "Stok kontrolünü ayrı bir fonksiyon olarak arayüze ekleyeceğim" ifadesini şöyle anladım: stoğa göre düzeltme, Stok Kontrol'ün sonuna otomatik eklenmeyecek; arayüzde **kendi düğmesi olan ayrı bir işlem** olacak. Rapordaki ana öneri buna göre hazırlandı. Şu anki kutu yapısı "bugünkü durum" olarak anlatılıyor.

---

## 1. Özet

**Ne yapıyor?** Optimum'un seçtiği taşlardan biri stokta yetmiyorsa, o taş ancak elde olduğu kadar kullanılır. Fazlası, renkçe en yakın ve stoğu olan taşa aktarılır. Örneğin Toros Siyah cilalı (C130) bitmişse, neredeyse aynı renkteki Alexander Black cilalı (C110) kullanılır. Stok yeterliyse mozaik **hiç değişmez**. (Az kullanılan taşları çıkaran "en az kullanım" kuralı 2026-10-06'da kapatıldı; bkz. 4.6.)

**Bugünkü durum:**
- Kod `stok_deneme` dalında, ayrı bir klasörde (`D:\dev\mosair-stok-deneme`). `feature`, `main` ve kullanıcıların sürümü (v1.3.0) etkilenmedi.
- Bugün bir **kutu** var: Optimum'un yanında "Stoğa göre", varsayılan olarak açık. Kutu açıkken:
  1. Stok uygulama açılırken ve her görsel ya da proje yüklendiğinde tablodan okunur (Bizdeki − diğer mozaikler).
  2. **Mos** (Optimum ya da klasik) sonucu hemen bu stoğa göre düzeltilir: yetmeyen taşlar elde olduğu kadar kullanılır, az kullanılan taşlar çıkarılır. Stok okunamazsa Mos stoğa bakmadan yapılır ve durum çubuğunda yazar.
  3. **Stok Kontrol** son adetleri tabloya **tek seferde** yazar (gerekirse önce bir kez daha düzeltir); kırmızı noktalar hesaplanan stoktan konur.
  4. Düzeltme yapılamazsa sebebi bir pencerede gösterilir; Stok Kontrol normal haliyle devam eder.
- Stok ayarı şu an **"mosair deneme" (TEST) tablosuna** bağlı. Bu ayar bilgisayardaki **bütün** mosair'ler için ortak; kurulu v1.3.0 da şu an test tablosunu kullanıyor.

**Test durumu (dürüst not):**
- **Test edildi:** Algoritma, komut satırından çalışan karşılaştırma aracıyla 4 görselde test edildi; bütün kontroller geçti.
- **Test edilmedi:** Uygulama içindeki **"Mos → stoğa göre düzeltme → Stok Kontrol ile yazma"** akışı tarafımdan arayüzde baştan sona çalıştırılmadı; kullanıcı denemesi bekleniyor.

---

## 2. Önerilen yerleşim: ayrı bir "Stoğa Göre Düzelt" işlemi

### Arayüz
- **Araç çubuğu:** Beş stok ikonunun yanına yeni bir düğme gelir: **"Stoğa Göre Düzelt"**. İkon önerisi: küp + çift yönlü ok.
- **Menü:** Aynı işlem **Araçlar → Stok** altında da bulunur.
- **Kutu kalkar:** Optimum'un yanındaki "Stoğa göre" kutusu ve Stok Kontrol'ün sonundaki otomatik çağrı kaldırılır. Tetikleyici tek olur.
- **Mos:** Her zaman düz Optimum yapar.
- **Düğme ne zaman etkin olur?** Yalnızca Optimum ile Mos yapılmışken ve başka bir stok işlemi sürmüyorken. Proje açıldıktan, klasik Mos'tan ya da yeni görsel yüklendikten sonra pasif kalır; üzerine gelince "Önce Optimum ile Mos yapın" yazar.

### Düğmeye basınca
1. **Tablo okunur.** Her taş için kullanılabilir stok:
   **Bizdeki (kg) − diğer mozaik sütunlarının ayırdığı kg.** Bu projenin kendi sütunu hesaba katılmaz.
2. **Piksel düzenlemesi kontrolü:** Düzenleme varsa "sıfırlanacak, devam edilsin mi?" diye sorulur.
3. **Düzeltme yapılır;** gerçek taş dokulu görüntü yenilenir.
4. **Rapor penceresi açılır** (yalnızca ipucunda değil):
   - hangi taştan hangi taşa kaç taş ve kaç kg aktarıldığı,
   - yeni eklenen taş türleri,
   - az kullanıldığı için çıkarılan taşlar,
   - hâlâ yetmeyen taşlar,
   - stok kaydı olmayan taşlar.
5. **"Geri al" seçeneği:** Rapor penceresinde düz Optimum sonucuna tek tıkla dönülebilir. Ucuz bir işlem, çünkü Optimum analizi zaten bellekte.

### Karar: düğme tabloya yazsın mı?
| Seçenek | Nasıl | Artı | Eksi |
|---|---|---|---|
| **(a) Yazmasın** — önerilen | Rapor "Şimdi Stok Kontrol'e basın" der | Tabloya yalnızca tek bir işlem (Stok Kontrol) yazar; daha güvenli ve anlaşılır | Bir tık fazla |
| (b) Yazsın | Düzeltilmiş adetleri proje sütununa kendisi yazar | Tek tık | Tabloya iki ayrı işlem yazar; "neden tablom değişti?" karışıklığı olabilir |

---

## 3. Tetikleme seçeneklerinin karşılaştırması

| Yöntem | Durum | Kazanç | Kayıp |
|---|---|---|---|
| Mos sırasında (ilk sürüm) | Kaldırıldı | Tek adım | Yalnızca Bizdeki'ye bakıyordu, başka mozaiklerin ayırdığı stoğu görmüyordu; her Mos'ta tablo okunuyordu |
| Stok Kontrol'den sonra, kutu ile (bugün) | Çalışıyor | Diğer mozaikler hesaba katılıyor; kırmızılar düzeltmeden sonra kalkıyor | Stok Kontrol "gizlice" mozaiği değiştiriyor; kutu açık unutulursa sürpriz olabiliyor |
| **Ayrı düğme** (önerilen) | Planlandı | Ne zaman çalıştığı açık; rapor ve "Geri al" doğal; Mos ve Stok Kontrol eski davranışında kalıyor | Kullanıcının bir düğmeye daha basması gerekiyor |

---

## 4. Algoritma — ne kullanıldı, neden?

### 4.1 Stok → taş adedi
- **1 taş = 3,3 g (0,0033 kg).** Tablodaki "Kullanılacaklar (kg)" sütunu, kullanılan 65 taşın hepsinde tam olarak adet × 0,0033'e eşit çıktı.
- **Kapasite** = ⌊(Bizdeki − diğer mozaikler) / 0,0033⌋. Eksi değerler 0 sayılır.
- **Mozaik sütunlarının tespiti:** Tablonun kendi betiğiyle aynı kural kullanılır. "Bizdeki" başlığından sonraki ve "13." başlığından önceki sütunlar mozaik sütunu sayılır; "#" ve boş başlıklar atlanır.
- **Neden Tahmini Kalan kullanılmıyor?** İki nedeni var:
  - Tablo yazmadan hemen sonra eski veriyi döndürebiliyor.
  - #2, #3 ve #5 satırlarında Kalan formülü adetlere tepki vermiyor.
- **Sayı biçimi:** Türkçe sayılar ("1.027,00") doğru okunuyor.

### 4.2 "Benzer renk" nasıl ölçülüyor?
- **İkame kararı:** Lab renk uzayında düz renk farkı kullanılır (**ΔE76**; açıklık ile renk eşit ağırlıkta).
- **Neden Optimum'un ölçüsü değil?** Optimum palet seçerken açıklığı **3 kat** ağır tartar; desenin okunabilirliğini korumak için doğru olan budur. Aynı ölçü ikame için kullanılınca siyahın yerine **koyu kırmızı** (Rosso Anatolia) seçiliyordu, çünkü "koyuluğu aynı" görünüyordu. ΔE76'ya geçince siyahlar yine siyah ya da koyu gri taşlara gidiyor.
- **Seçim kriteri:** Kararı renk yakınlığı veriyor, taşın adı değil. Aynı taşın farklı yüzeyleri (cilalı, honlu, AH) arasındaki farklar tutarlı değil: en az 0,9, en çok ~19 ΔE. Aynı taş ailesine yalnızca küçük bir öncelik veriliyor (`SameFamilyFactor` = 0,85).

### 4.3 Kaç taşın nereye gideceği: en ucuz akış (min-cost flow)
- **Ne yapar?** Fazlalıkları aktarmanın toplam renk değişimi en küçük yolunu **kesin olarak** hesaplar. Tahmin ya da deneme-yanılma değildir; her çalıştırmada aynı sonucu verir.
- **Ağ yapısı:** kaynak → stoğu aşan taş → o taşın renk grupları → aday ikame taşlar (piksel başına maliyet) → hedef (adayın boştaki stoğu).
- **Çözüm yöntemi:** Ardışık en kısa yol, potansiyelli Dijkstra ile. Başlangıç potansiyelleri Bellman-Ford ile bulunur.
- **Maliyet** = kenar ağırlığı × (yeni taşa uzaklık² − eski taşa uzaklık²).
  - **Kenar ağırlığı:** Optimum'daki Sobel ağırlığıyla aynı, 1 + 2·min(1, kenar/eşik). Düz alanlar kenarlardan önce taşınır, böylece desen bozulmaz.
- **Hızlandırmalar:**
  - Benzer tonlar gruplanır (`GroupStep` = 2 ΔE).
  - Her taş için en yakın 8 aday değerlendirilir (`MaxCandidates` = 8).
  - Ölçülen süreler: 7 ms – 1,5 sn.

### 4.4 Bir alan iki taşa bölünürse hangi pikseller gider?
- Her pikselin 3×3 komşuluğunun ortalama rengine bakılır. Komşuluğu yeni taşa daha yakın olan pikseller önce gider; geçiş doğal tonu izler.
- Eşitlik durumunda Bayer düzeni kullanılır. Değişim alana dağılır, ortada keskin bir ek yeri oluşmaz.

### 4.5 İkizler, genişleme adımları, yeni taş türü sınırı
- **İkiz:** Renk farkı ΔE ≤ 3 olan taş (`TwinTolerance`); pratikte aynı renk. Örnek: C130 ↔ C110 (1,1).
  - Her zaman kullanılabilir.
  - Her taş için yalnızca en yakın tek ikiz kullanılır; küçük bir alan iki ikize bölünmez.
- **Benzerlik sınırı:** 10 ΔE (`SimilarityTolerance`). Yakın renkte stok yetmezse arama adım adım genişler ve rapor bunu söyler:
  1. 10 → 20 → 40
  2. stoklu herhangi bir taş
  3. yeni taş türü sınırını aşmak
- **Yeni taş türü:**
  - En fazla 2 tane, ikizler hariç (`MaxExtraStones`).
  - Yeni bir tür ancak toplam değişimi en az %20 küçültüyorsa eklenir (`NewStoneGain` = 0,8). Maliyet eksiyse de doğru çalışacak şekilde düzeltildi.
- **Stok sert sınırdır:** Hiçbir durumda aşılmaz. Yalnızca hiçbir stoklu taş kalmazsa "hâlâ yetmiyor" yazılır.

### 4.6 En az kullanım kuralı (kapalı)
- **Durum:** 2026-10-06'da kullanıcı isteğiyle **kapatıldı**. Uygulama `MinUsage = 0` ile çalışır; az kullanılan taşlar artık çıkarılmaz. Mekanizma kodda duruyor, karşılaştırma aracında `MOSAIR_MINUSAGE=1` ile açılabilir. Aşağısı kural açıkken nasıl çalıştığını anlatır.
- **Sınır:** max(10, toplam taşın %0,05'i). 78×78'lik bir mozaikte 10 taş, 16 m²'lik bir mozaikte yaklaşık 56 taş (`MinUsageFor`).
- **Uygulama:** Turlar halinde yapılır. Sınırın altında kalan her taşın kapasitesi 0 sayılır ve pikselleri aynı akış yöntemiyle taşınır. Yeni küçük taş çıkmayana kadar tekrarlanır.
- **Rapor:** Çıkarılan taşlar adetleriyle birlikte listelenir.

### 4.7 Güvenceler
- **Birebir aynılık:** Stoğu aşan taş yoksa sonuç düz Optimum'la **bayt bayt aynıdır**. Her test görselinde otomatik kontrol ediliyor.
- **Optimum'a dokunulmadı:** Optimum ve klasik Mos algoritması hiç değiştirilmedi. Düzeltme Optimum'dan **sonra** ayrı bir adım olarak çalışır.
- **WPF uyumu:** Sonuç normal bir mozaiktir; WPF ile aynı dosya biçiminde kaydedilir.

---

## 5. Test sonuçları (TEST tablosundaki senaryo stoklarıyla)

**Senaryo:** Bütün taşlar 20 kg, ama görsellerde çok kullanılan 8 taş kıt: Toros Siyah C 3 kg, Sarı Trv C 2 kg, Rosso Anatolia H 1,5 kg, Alexander Black H, Uşak Yeşil AH, Carrara H ve Ottoman 1'er kg, Teos1 Yeşil C 0 kg.

| Görsel | Aktarılan taş | Yeni tür | Ortalama ΔE (Optimum → Stoğa göre) | Süre | Kontroller |
|---|---|---|---|---|---|
| 7 | 1.677 | 2 | 15,43 → 16,86 | 76 ms | geçti |
| st1 | 3.546 | 4 | 5,96 → 6,06 | 221 ms | geçti |
| n7 | 391 | 0 | 17,49 → 17,54 | 24 ms | geçti |
| dali | 305 | 3 | 6,99 → 6,99 | 7 ms | geçti |

**Kontrollerin anlamı:** Stok hiçbir taşta aşılmadı; sonuçta en az kullanımın altında taş kalmadı; düzeltilecek bir şey olmayan durumda sonuç Optimum'la birebir aynıydı.

**Görsel bazında gözlemler:**
- **7:** Sarı Trv cilalı → honlu (1.376 taş). Bu iki yüzey arasında 7,4 ΔE fark var; değişim görülebilir, ortalama ΔE'deki +1,4 artış bundan geliyor.
- **st1:** Taşınanların büyük kısmı ikiz taşlara gitti (siyahlar). Ortalama ΔE yalnızca +0,1 arttı.
- **n7:** Rosso Anatolia honlu → cilalı (387 taş). Fark çok küçük.
- **dali:** Ottoman'ın 305 taşı açık tonlu 3 yeni taşa dağıldı. Görüntüde fark yok, ama robot için 3 yeni renk değişimi demek.

---

## 6. Faydalar ve zararlar

### Faydalar
- **Stok gerçekçi:** Stokta olmayan taşla tasarım yapılmaz. Başka mozaiklerin ayırdığı stok da düşülür.
- **Görünüm korunur:** Çoğu durumda değişim gözle seçilmez; ikizler ve aynı taşın diğer yüzeyleri kullanılır.
- **Robot için daha az renk değişimi:** Az kullanılan taşlar çıkarılır.
- **Kararlar şeffaf:** Rapor her kararı kg cinsinden gösterir.
- **Hızlı ve tekrarlanabilir:** Aynı girdiyle her zaman aynı sonuç çıkar.

### Zararlar ve riskler
- **Yeni taş türü:** Her yeni tür, robot için bir renk değişimi daha demek. Testlerde mozaik başına 0–4 yeni tür eklendi (ikizler dahil).
- **Detay kaybı riski:** En az kullanım kuralı, Optimum'un küçük detaylar için bilerek tuttuğu az kullanılan taşları da çıkarır. Test görsellerinde böyle bir taş çıkmadı, ama ince detaylı görsellerde detay kaybı olabilir.
- **Uzak ikame görülebilir:** Yakın renkte stok yoksa arama genişler ve fark görünür hale gelebilir. Örneğin gerçek stokta siyah azsa siyahlar koyu griye döner.
- **Katalog renklerine bağımlılık:** "Benzer" kararı katalogdaki RGB değerlerine dayanır. Örneğin 48/49 "Teos1 Yeşil" katalogda gri görünüyor. Katalog renkleri gerçek taşlarla uyuşmazsa ikameler de yanlış olur.
- **Kontrol edilemeyen taşlar:** Tabloda kaydı olmayan **#68–124** taşlar stokla kontrol edilemiyor. Raporda ayrıca listeleniyorlar ve Optimum bunları sıkça kullanıyor (görsel başına 4–8 taş).
- **Piksel düzenlemeleri sıfırlanır.**
- **Yalnızca Optimum ile çalışır:** Taze bir Optimum Mos gerekir; proje açıldıktan sonra ya da klasik Mos ile çalışmaz.
- **Kaydırıcıdan sonra tablo eskir:** Taş sayısı kaydırıcısı son okunan stoğu kullanır. Kaydırıcıdan sonra tablodaki adetler eski kalır; Stok Kontrol'ün tekrarlanması gerekir.
- **Tablonun tuhaflıkları:** #2/#3/#5'te Kalan formülü, eksi stoklar, Google'ın birkaç saniye eski veri döndürmesi.
- **Yapılmayan:** "Önce/Sonra" karşılaştırma düğmesi.

---

## 7. `feature`'a birleştirme planı

1. **Arayüzü ayrı düğmeye çevir** (Bölüm 2): kutuyu ve otomatik çağrıyı kaldır, düğme + menü + rapor penceresi + "Geri al" ekle.
2. **Engelleyici — belgeler:** `docs-check` iş akışı, yeni iki kaynak dosyanın (`Services/StockAwareAssigner.cs`, `StockCompareRunner.cs`) belge sayfası yoksa başarısız olur. `docs/kod/...md` sayfaları yazılmalı.
3. **Belgeleri güncelle:**
   - `docs/ARAYUZ.md` ve uygulama içi kılavuz (`HelpWindow`, TR/EN): yeni düğme, rapor, kurallar.
   - `docs/kod`: StockSheetService, MosaicEngine, MainViewModel, Loc.
4. **Kod temizliği:** `RebuildFromAssignment` içindeki palet kurma kodu `ApplyOptimalK` ile ortak bir yardımcıya taşınmalı; ardından birebir aynılık testi tekrar çalıştırılmalı.
5. **Karşılaştırma aracı** (`--stockcompare`): uygulamayla birlikte gelsin mi, ayrı bir geliştirici aracı olarak mı kalsın?
6. **Uygulama içi uçtan uca deneme:** TEST tablosunda Mos → Stoğa Göre Düzelt → Stok Kontrol akışı en az bir kez denenmeli.
7. **Stok ayarını geri al:** Testler bitince `stock.real.json` geri yüklenmeli (gerçek tablo).
8. **Sürüm:** Kullanıcı onayıyla `feature` → `main` ve sürüm etiketi her zamanki gibi taşınır.

---

## 8. Karar soruları

1. **Tetikleyici:** Ayrı düğme (önerilen) mi, bugünkü kutu mu?
2. **Tabloya yazma:** Düğme tabloya yazmasın, "Stok Kontrol'e basın" desin (önerilen); yoksa kendisi mi yazsın?
3. **En az kullanım:** max(10, %0,05) uygun mu? Kural her zaman mı uygulansın, yoksa yalnızca stok düzeltmesi gerektiğinde mi?
4. **Yeni taş türü sınırı:** 2 mi, 1 mi? İkizler bu sınırın dışında kalsın mı?
5. **Stok kaydı olmayan taşlar (#68–124):** Sınırsız mı sayılsın, uyarıyla mı kullanılsın, yoksa hiç mi kullanılmasın?
6. **Katalog renkleri:** Birkaç taş çiftinin (ör. 48/49, C119/H119) katalog RGB değerleri gerçek taşlarla karşılaştırılsın mı?
