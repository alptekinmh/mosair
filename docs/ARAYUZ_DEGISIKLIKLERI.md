# Arayüz değişiklikleri

> Tarih: 2026-10-07, 2026-10-08 · Kapsam: yalnızca görünüm (renk, yazı, tutarlılık) ve yeni Görsel Ayarları paneli. Davranış, algoritmalar ve proje/robot (WPF) dosya biçimi değişmedi (yalnızca projeye isteğe bağlı `ImageAdjust` alanı eklendi; WPF onu yok sayar).

Bu notta 2026-10-07'de arayüzde değişen her şey ve bilerek sonraya bırakılanlar listelenir. Ayrıntılı kullanım: [ARAYUZ.md](ARAYUZ.md); tema anahtarları: [kod/App.md](kod/App.md).

## 2026-10-09: Görsel Ayarları'nda görüntü kaymıyor

- Kalıp Dolgu açıkken bir mozaik varken yapılan her Mos'ta (Anlık Mos dahil) görüntü alanı Mos süresince bir kalıp payı daha büyüyüp sonra eski boyutuna dönüyor, görüntü kayıyordu. Dolgu önizlemesi artık yalnızca görünüm henüz görselin boyutundayken (ilk Mos'tan önce) uygulanır.

## 2026-10-09: Anlık Mos onay kutusu ve renk seçenekleri

- **Anlık Mos** sekme düğmelerine benzeyen bir düğme yerine sekmelerin altında bir onay kutusu oldu.
- İşaretliyken altında iki seçenek: **Tüm renkleri kullan** (katalogdaki bütün taşlar) ve **Seçili renkleri kullan** (kullanıcının işaretledikleri; Mos'tan sonra kendiliğinden daralan işaretler değil). Araçlar menüsüne de eklendi.

## 2026-10-09: Optimum Renk Sayısı

- Araç çubuğundaki **Optimum** kutusunun adı **Optimum Renk Sayısı** (EN: Optimum Colour Count) oldu; **Stoğa göre** ile arasındaki boşluk açıldı (2 px → 14 px).

## 2026-10-08: Kalıp Dolgu açıkken cm kutusu dolgulu ölçüyü gösterir

- Kalıp Dolgu açıkken girilen genişlik görselin genişliğidir; kutu dolgudan sonraki toplamı gösterir (150 → 156, 5 kalıp). Yükseklik ve alan da dolgulu. Kalıp Dolgu kapatılınca kutu görselin genişliğine döner.

## 2026-10-08: Mos'tan önce dışa aktarma

- Dışa aktarma (mosairEXPORT, mosairEXPORT As, `Ctrl/⌘+E`) artık Mos'tan önce de kullanılabilir: görsel, Görsel Ayarları uygulanmış hâliyle kendi çözünürlüğünde, Kalıp Dolgu açıksa dolgusuyla birlikte kaydedilir. Listede kalite seçenekleri yerine tek bir `Görsel: W × H px` seçeneği görünür.

## 2026-10-08: Kalıp Dolgu Mos'tan önce görselde çalışır

- Görsel ekrandayken **Kalıp Dolgu**'ya basınca Mos yapılmadan görselin sağına ve altına tam kalıba kadar dolgu alanı eklenir ve dolgu taşının rengiyle boyanır (önceden dolgu yalnızca Mos'tan sonra mozaiğe ekleniyordu). Mos dolgu alanını hep bu taşla doldurur; görselin mozaiği değişmez.
- Dolgu taşı artık **görselin renklerine en uzak** katalog taşıdır ve **stok aranmaz** (önceden mozaikte kullanılan taşlara en uzak, stoğu yeten taştı; stok yoksa dolgu yapılmıyordu).
- Mozaik ekrandayken düğme eskisi gibi dolguyu hemen ekler/kaldırır.

## 2026-10-08: Ekran görüntüsü yalnızca görseli kaydeder

- Ekran görüntüsü artık görselin (ya da mozaiğin) çevresindeki boş tuval alanını kaydetmez; yalnızca görselin görünen kısmı kaydedilir.

## 2026-10-08: Anlık Mos elle seçilen taş sayısını korur

- Optimum açıkken taş kaydırıcısıyla önerilenden farklı bir sayı seçildiyse, sonraki Anlık Mos'lar analizi yenileyip o sayıyla mozaik kurar (önceden her seferinde önerilen sayıya dönüyordu). Sağ tıkla önerilene dönülünce yine önerilen sayı izlenir.

## 2026-10-08: Özellikler paneli kapalı başlar

- Uygulama açılınca Özellikler paneli kapalı (sağ kenarda şerit) gelir; şerit, F4 ya da **Görünüm → Özellikler Paneli** ile açılır; görselde bir taş seçilince de (normal tıklama ya da piksel düzenleme) kendiliğinden açılır.

## 2026-10-08: Taş ve kalıp tek kartta

- Sol paneldeki yan yana iki kart (`250 × 444 = 111,000 …` kesiliyordu) tek karta, iki hizalı satıra dönüştü: **Taş** `250 × 444 = 111.000`, **Kalıp** `10 × 18 = 180`. Sayılar sağa hizalı, 8 haneli taş toplamı da sığar.

## 2026-10-08: Optimum taş kaydırıcısı

- Araç çubuğunda **Stoğa göre** ile **Optimum** yer değiştirdi (önce Stoğa göre, sonra Optimum).
- Taş kaydırıcısı Optimum'un hemen sağında, yalnızca Optimum işaretliyken (ve Optimum ile yapılmış bir Mos'tan sonra) görünür. Görsel Ayarları'ndaki üçgenli kaydırıcı kullanılıyor; seçili taş sayısı üçgenin üstünde yazar, sağ tık önerilen sayıya döner.
- Kaydırıcının yanındaki "Taş" etiketi, sayı ve `öneri K` yazısı kaldırıldı.

## 2026-10-08: Özellikler panelinden iki kart kaldırıldı

- Taş seçili değilken görünen **Baskın Renkler** ve **En Çok Kullanılan Taşlar** kartları kaldırıldı; panelde önizleme ve Ayrıntılar kalıyor. (Aşağıdaki eski notlarda bu kartlardan söz edilen yerler o günün durumunu anlatır.)

## 2026-10-08: Anlık Mos sürüklemeyi bekler

- Anlık Mos açıkken bir Görsel Ayarları kaydırıcısı sürüklenirken artık her adımda Mos yapılmıyor; fare bırakılınca son değerle bir kez yapılıyor.
- Yeni mozaik hazırlanırken önceki mozaik ekranda kalıyor (donmuş görüntü); arada görsel görünüp kaybolmuyor.

## 2026-10-08: Renk temaları (Görünüm → Tema)

- **Görünüm → Tema** alt menüsü: beş renk teması — **Lapis** (varsayılan, önceki renkler), **Adaçayı ve Lavanta** (pastel), **Grafit ve Petrol**, **Traverten**, **Mürekkep ve Leylak**; seçili olanda ✓. Her biri koyu ve açık çalışır (araç çubuğundaki ☾/☀ ya da aynı alt menüdeki **Açık Tema**). Renkler anında değişir.
- Seçim (palet ve koyu/açık) hatırlanır: `%APPDATA%\mosair\ui.json`. Proje dosyasına yazılmaz. Durum renkleri (kaydedildi, uyarı, stok eksikliği, piksel düzenleme) her temada aynı.
- Kod: [ThemeService](kod/Services/ThemeService.md).

## 2026-10-08: Görsel Ayarları kendi sütununda, Photoshop gibi

- **Drive'da orijinal ve ayarlar:** Görsel Ayarları kullanılan proje Drive'a kaydedilirken dokunulmamış orijinal de gönderiliyor ve Drive'daki proje klasörünün `orijinal` alt klasörüne konuyor; Drive'dan açınca orijinal de iniyor ve proje son kaydedilen ayarlarla açılıyor (kaydırıcılar kaldığı yerde). Script yeniden dağıtılmalı.
- **Drive'da görsel yenileme:** Drive script'i artık Drive'daki proje görseli farklıysa (ör. Görsel Ayarları değiştiyse) onu yenisiyle değiştiriyor; önceden ilk kayıttaki görsel kalıyordu. Script yeniden dağıtılmalı (Dağıt → Dağıtımları yönet → düzenle → Yeni sürüm).

- **Yer:** Görsel Ayarları, Özellikler panelinin altından çıkıp görsel alanı ile Özellikler arasında **kendi sütununa** taşındı. Sütun 300 px açılır, solundaki ayırıcıyla **260–600 px** genişletilebilir (uzun kaydırıcı = daha ince ayar); Özellikler paneli bundan etkilenmez. Başlıktaki ▬ ya da **Görünüm → Görsel Ayarları** sütunu 24 px'lik şeride indirir; şeride tıklayınca eski genişliğiyle açılır. Özellikler şeridi yine tek sekme.
- **İki sekme** (Photoshop'un Light ve Hue/Saturation panelleri gibi):
  - **Işık:** Pozlama (±2.00 EV, iki ondalık), Parlaklık, Kontrast, **Parlak Alanlar**, **Gölgeler**, **Beyazlar**, **Siyahlar**, Gama.
  - **Ton/Doygunluk:** **Ana** ve altı renk aralığı (Kırmızılar, Sarılar, Yeşiller, Camgöbekleri, Maviler, Eflatunlar) için **Ton** (±180°), **Doygunluk**, **Açıklık**; kendi ayarı olan aralıkta nokta; **Renklendir** (görselin tamamı tek tonda).
- **Satır düzeni:** solda ad, sağda yazılabilir **değer kutusu** (`Enter` ile uygulanır), altında tam genişlikte kaydırıcı.
- **Yeni kaydırıcı** (`AdjustSlider`): ince iz ve altında **küçük üçgen tutamaç** (Fluent kaydırıcısının yuvarlak tutamacı yerine). Ton, Doygunluk, Açıklık ve Pozlama izleri renk geçişlidir. `Shift` + sürükle dört kat yavaş ince ayar; tekerlek ±1 (`Ctrl` ±10); ok tuşları; sağ tık ya da `Delete` sıfırlar (ilk sürümde çift tıktı). Değer kutusuna yazılan sayı `Enter`'la ya da başka bir yere tıklanınca uygulanır.
- **Anlık Mos:** sekmelerin yanında (ince bir çizgiden sonra) Kalıp Dolgu gibi basılı/basılı değil görünen bir düğme; **Araçlar → Anlık Mos** ile de açılır, her açılışta kapalıdır. Açıkken her ayar değişikliğinden sonra Mos kendiliğinden yapılır (Mos sürerken yapılan değişiklik sıraya girer; kaydırıcılar bu sırada kullanılabilir kalır; ilk mozaik pencereye sığdırılır). Kapalıyken mozaik varsa bir ayar değişince eskimiş mozaik yerine ayarlanmış görsel gösterilir ve bir sonraki Mos'a kadar öyle kalır.
- **Ayarlı görsel kaydı:** ayar kullanılıyorsa Kaydet / Farklı Kaydet / Drive'a Kaydet `.mos`'un yanına görselin **ayarlı hâlini** orijinalin adıyla yazar (WPF ve robot proje klasöründeki ilk görseli kullandığı için onu görür); dokunulmamış orijinal `orijinal/<ad>` alt klasörüne kopyalanır ve projeye `OriginalPictureFileName` yazılır. Ayarsız kayıt orijinali geri koyar. Proje açılınca orijinal temel alınıp ayarlar yeniden uygulanır; `orijinal` kopyası yoksa (ör. Drive) ayarlı görsel temel alınır ve kaydırıcılar sıfırdan başlar.
- **Kaydedilen dosya bildirimi:** sağ alttaki bildirim kutusu artık mosair'in yazdığı dosyalar için de çıkar (7 sn, fare üzerindeyken durur, ✕ kapatır). Dışa aktarma bitince **DIŞA AKTARILDI**, ekran görüntüsünden sonra **EKRAN GÖRÜNTÜSÜ KAYDEDİLDİ**: dosya adı, klasörün yolu, **Klasörü aç** (dosyayı Dosya Gezgini / Finder'da seçili gösterir) ve **Aç** (varsayılan programla açar); 64 MB'a kadar dosyalarda önizleme. Proje kaydından sonra **PROJE KAYDEDİLDİ**: **Kapat** ve **Klasörü aç**, önizleme olarak mozaiğin küçük görüntüsü. Drive'a kayıt için çıkmaz. Ayrıntı: [ARAYUZ.md → Kaydedilen dosya bildirimi](ARAYUZ.md#kaydedilen-dosya-bildirimi).
- Proje dosyasına yeni `Adjust` nesnesi yazılır (ayar yoksa yazılmaz); ilk sürümün `ImageAdjust` dizisi hâlâ okunur. WPF ikisini de yok sayar.
- Ayrıntı: [ARAYUZ.md → Görsel Ayarları](ARAYUZ.md#görsel-ayarları); kod: [ImageAdjustService](kod/Services/ImageAdjustService.md), [AdjustSlider](kod/Controls/AdjustSlider.md), [AdjustParam](kod/ViewModels/AdjustParam.md).
- **Sol panel ölçü bölümü:** tek satır `[genişlik] × yükseklik cm = alan m²` (hepsi 16 px, kutudaki gibi noktalı; ör. `93.6 × 93.6 cm = 0.88 m²`), altında iki kart: `78 × 78 = 6.084 taş` ve `3 × 3 = 9 kalıp`. Eski ■ ▣ ○ satırları, kalıp satırındaki parantezli cm ölçüsü ve orijinal piksel ölçüsü (`orj im = …`) kaldırıldı; piksel ölçüsü Özellikler panelinde. Görsel yüklenmeden yalnızca kutu ve "cm" görünür.
- **Proje Aç:** dosya seçme penceresi, varsa Masaüstü/mosairPROJECT klasöründe açılır.
- **Görsele uyan arka plan:** görsel ya da proje yüklenince görsel alanı ve ölçü bölümü, görselin ortalama renginin sakin bir tonunu alır (tonu korunur, doygunluk düşük; koyu temada koyu, açık temada açık), 0,4 sn'lik yumuşak geçişle; tema değişince uyar, görsel yokken tema renkleri. Her zaman açık. Ekran görüntüsünün saydam kenarları da bu renkle dolar.

## 2026-10-07

## 1. Vurgu rengi: yeşilden "Lapis" mavisine

Önceden aynı anda dört vurgu rengi vardı: ana penceredeki nane yeşili `#4ecb71`, iletişim kutularındaki mavi `#3a7bfd`, "Tüm Renkleri Seç" yazısındaki `#4a9eff` ve işletim sisteminin vurgu rengi (işaret kutuları, kaydırıcılar, ilerleme çubuğu Windows/macOS ayarına göre renk alıyordu). Artık tek vurgu vardır:

| Anahtar | Koyu tema | Açık tema | Kullanım |
|---|---|---|---|
| `AccentFill` | `#2D6BD9` | `#1F5FCC` | Mos düğmesi, iletişim kutularının ana düğmeleri, bildirimdeki **Aç**, ilerleme çubukları, işlem dalgası, Kalıp Dolgu açıkken |
| `AccentFillHover` / `AccentFillPressed` | `#3672DE` / `#255DC0` | `#2766D4` / `#1A50AD` | Üzerine gelme / basılı |
| `AccentText` | `#6FA3FF` | `#1D5BC4` | Izgara AÇIK ikonu, "Tüm Renkleri Seç", bildirim başlığı, kılavuz başlığı, Görsel Ayarları noktası |
| `AccentBorder` / `AccentSubtle` | `#6FA3FF` / `#2D6BD9` %15 | `#1F5FCC` / `#1F5FCC` %12 | Mini harita çerçevesi ve dolgusu, seçili taş varyantı |

Neden mavi: lapis klasik bir mozaik taşıdır; doğal taş fotoğrafları çoğunlukla sıcak tonlu olduğundan mavi çerçeve ve seçimler görüntünün üstünde hep seçilir; yeşil yalnızca "başarılı" anlamında kalır. Fluent temasının kendi vurgu rengi de aynı maviye sabitlendi (`App.axaml` → `FluentTheme.Palettes`).

**Değişen yerler (eski → yeni):**

- Mos düğmesi: `#4ecb71` zemin + `#1a1a1e` yazı → `AccentFill` + beyaz; üzerine gelme `#5dda80` → `AccentFillHover`, basılı durumu eklendi (`AccentFillPressed`).
- İletişim kutularının onay düğmeleri (Uyarı, Onay, Stok Ayarları, Drive Ayarları, Drive'dan Aç) ve bildirimdeki **Aç**: `#3a7bfd` / `#4ecb71` → paylaşılan `Button.primary` stili (vurgu mavisi, üzerine gelince ve basınca koyulaşır, pasifken gri). Beyaz yazının okunurluğu 3,87'den 4,98 / 5,89'a çıktı.
- İşlem dalgası, bildirim geri sayım çubuğu, en çok kullanılan taşlar çubuğu: yeşil → `AccentFill`.
- Mini harita çerçevesi `#4ecb71` → `AccentBorder`, dolgusu `#224ecb71` → `AccentSubtle`; seçili varyant çerçevesi → `AccentBorder`.
- Izgara AÇIK ikonu ve "ON" yazısı, ölçüler kutusundaki ■ işareti, "Tüm Renkleri Seç" (`#4a9eff`), bildirim başlığı → `AccentText`.
- Kalıp Dolgu düğmesi: işletim sistemi rengi yerine `ToggleButton.chip` stili (kapalıyken nötr, açıkken `AccentFill`).

## 2. Yeşil, kırmızı, turuncu artık anlam taşıyor

| Anahtar | Koyu | Açık | Kullanım |
|---|---|---|---|
| `Success` | `#4CC27A` | `#17703D` | Kaydet ✓ işareti |
| `Danger` | `#E53935` | `#E53935` | Stok eksik kırmızı noktaları, İptal düğmesi çerçevesi ve ✕ |
| `DangerText` | `#F2665E` | `#B71C1C` | İptal yazısı, "Görsel dosyası bulunamadı" |
| `EditMode` | `#FF7A29` | `#B23A0A` | Piksel düzenleme kalemi, kaynak/hedef (önceden `#FF6600`) |
| `Brand` / `BrandFill` | `#6FAF6F` / `#3F7A3F` | `#356B35` / `#3F7A3F` | "mosair" yazısı (logodaki adaçayı yeşili; önceden nane yeşili `#4ecb71`), kılavuz başlığındaki logo kutusu |

Google Drive ve Sheets ikonlarının kendi renkleri değişmedi.

## 3. Nötr metinler ve okunurluk

- Durum çubuğundaki kullanılan renk bilgisi, durum metni ve zoom yeşilden `FgSecondary`'ye, geçen süre `FgMuted`'a; alan (m²), Optimum taş sayısı ve en çok kullanılan taşların yüzdesi `FgPrimary`'ye döndü. Vurgu rengi yalnızca anlamı olan yerlerde kalır.
- Açık temada yeşil yazılar 1,5:1 okunurluktaydı (en az 4,5 gerekir); artık hiçbir metin koda gömülü renk kullanmıyor, iki temada da okunur.
- Soluk yazı `FgMuted`: koyu `#686870` → `#9294A0` (2,83 → ≥4,6), açık `#707078` → `#5C5F68`.
- Kenarlık renkleri artık yazı rengi olarak kullanılmıyor: "cm", ızgara KAPALI, ○ işareti, geri al/yinele ipucu, "görsel yok" simgesi → `FgMuted`; pasif kaynak/hedef → `FgDisabled`.
- Diğer tema değerleri de hafifçe yenilendi (yüzeyler biraz daha ayrışık, `BrdrTer` 3:1'e çıktı, yeni `BgPressed`, `FgDisabled`). Görsel alanı (`BgCanvas`) nötr gri kaldı; taş renkleri yanıltmasın diye renk katılmadı.
- Kılavuz: başlık yeşilden `AccentText`'e, kısayol rozeti `#9898a0` zemin → `BgHover`/`FgPrimary`; numaralı adım rozetleri beyaz rakamlar okunsun diye koyulaştırıldı (`#43A047` → `#2E7D32`, `#FF8F00` → `#B45309`, `#E53935` → `#C62828`, `#FF6600` → `#C2410C`).

## 4. Yazı boyutları ve tutarlılık

- 9–10 px olan başlık ve açıklamalar 11 px'e çıktı: sütun başlıkları (Katalog Renk, Eşleşme, Atanan Renk), Izgara rengi etiketi, Properties ve Görsel Ayarları başlıkları, taş ipucu, geri al/yinele ipucu, dosya türü rozeti, en çok kullanılan taşların sayısı, bildirimin başlığı ve geri sayımı, durum çubuğundaki süre, "cm". Yoğun liste satırları (katalog, eşleşme, atanan) bilerek 9 px kaldı.
- Bütün panel başlıkları tek stilde: 11 px SemiBold `FgMuted` (`section-title`); önceden üç farklı stil vardı. Kalın (Bold) yazı azaltıldı (alan, Optimum sayısı, zoom → SemiBold).
- Eş aralıklı yazı tipi tek kaynakta (`MonoFont`: JetBrains Mono, Cascadia Mono, Consolas, Menlo, monospace); 38 ayrı tanımın yerini aldı ve macOS'te Menlo kullanılır.
- Küçük temizlikler: araç çubuğundaki iki kısa ayırıcı (16 px) diğerleri gibi 24 px; dil menüsü öğelerinin dolgusu 8,5; genişlik kutusu ve bildirim önizlemesinin köşe yuvarlaklığı 4.

## 5. Yeni: Görsel Ayarları paneli

Sağ panelin altında, kendi başlığıyla açılıp kapanan **GÖRSEL AYARLARI** bölümü: Parlaklık, Kontrast, Doygunluk, Gama (−100…+100). Çift tık bir ayarı, **Sıfırla** hepsini sıfırlar; ayar varken başlıkta ve gizli panel şeridindeki yeni **Görsel Ayarları** sekmesinde mavi nokta yanar. **Görünüm → Görsel Ayarları** ile de açılır. Mos ayarlanmış görselden yapılır; dosya değişmez, ayarlar projeyle kaydedilir. Ayrıntı: [ARAYUZ.md → Görsel Ayarları](ARAYUZ.md#görsel-ayarları), kod: [ImageAdjustService](kod/Services/ImageAdjustService.md).

## 6. Bilerek sonraya bırakılanlar

- Yoğun listelerde (katalog, eşleşme, atanan) 16 px satır ve 9 px yazı: 11 px'e çıkarmak görünen satır sayısını ~%25 azaltır; sahibin kararı.
- Kodda sabit kalan ve temayı izlemeyen birkaç renk: ipucundaki kg yeşil/kırmızı (`MainViewModel.KgOkBrush`/`KgShortBrush`), Stok/Drive ayar pencerelerindeki örnek adres vurgusu (`#3a7bfd`), ızgara rengi menüsündeki gri çerçeve.
- Bütün düğmeler için ortak stil seti (`secondary`, `ghost`, menü öğesi), köşe ve yükseklik ölçeğinin (2/4/6/8 ve 24/28/32) her yere uygulanması.
- Tek panel başlığı bileşeni (sabit yükseklik); Türkçe arayüzde "Properties" başlığının çevirisi.
- Uygulama genelinde varsayılan yazı tipi (Windows ve macOS aynı görünsün diye) ve eş aralıklı yazı tipinin pakete eklenmesi.
- macOS başlık çubuğu: pencere düğmelerinin "mosair" yazısıyla çakışıp çakışmadığı denetlenmeli.
- Kullanım kılavuzunda Türkçe karakterler, kenar menüsünde seçili durum.
- Durum mesajlarının sonuca göre renklenmesi (başarılı / uyarı / hata).
- Bildirim gölgesinin açık temada hafifletilmesi.
- Üzerine gelme / basılı görünümleri elle denenmedi; yalnızca derlendi ve koyu tema ekran görüntüsüyle kontrol edildi.
