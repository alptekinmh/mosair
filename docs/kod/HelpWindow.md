# HelpWindow

> Kaynak: `mosair/HelpWindow.axaml`, `mosair/HelpWindow.axaml.cs` · Güncelleme: 2026-10-07

## Amaç

Uygulama içi kullanım kılavuzu penceresi. Türkçe ve İngilizce içeriği aynı XAML'da iki ayrı panel olarak taşır; sol kenar çubuğundaki gezinme düğmeleriyle ilgili bölüme kaydırır.

## Nerede kullanılır

`MainWindow.ShowHelp()` → `new HelpWindow().ShowDialog(this)`. Tetikleyiciler: Yardım → Kullanım Kılavuzu menüsü (`OnShowHelp`) ve değiştiricisiz F1 (`OnKeyDown`).

## Yapı

### Pencere

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `Title` | bağlama | `{Binding [MenuUserGuide], Source={x:Static svc:Loc.Instance}}` | Dile göre başlık. |
| Boyut | — | 720 × 680 (`MinWidth` 500, `MinHeight` 400) | `CanResize="True"`, `CenterOwner`, arka plan `{DynamicResource BgMain}`. |
| Stiller | `TextBlock.h1`, `h2`, `body`, `shortcut-key`, `shortcut-desc`; `Border.section-card`, `icon-badge` | — | Bölüm başlığı, gövde, kısayol satırı ve kart görünümleri. |

### Yerleşim

```
DockPanel
├─ [Top] Başlık: logo + "mosair Kullanim Kilavuzu" (IsTr) / "mosair User Guide" (IsEn)
├─ [Left] Kenar çubuğu (160 px)
│   ├─ TR gezinme StackPanel (IsTr) — Button Tag="0..8" Click="OnNavClick"
│   └─ EN gezinme StackPanel (IsEn) — aynı Tag'ler
└─ contentScroll (ScrollViewer)
    └─ Panel
        ├─ contentPanel   (IsTr): sec0 … sec8
        └─ contentPanelEn (IsEn): sec0en … sec8en
```

### Bölümler

Bölüm kimliği = `x:Name` sonundaki sayı = gezinme düğmesinin `Tag` değeri = kod tarafındaki dizi indeksi. Görüntülenme sırası sayısal sıra **değildir** (7 ve 8 sonradan eklenip 4'ün arkasına yerleştirilmiştir).

| Görünüm sırası | `Tag` / indeks | TR `x:Name` | EN `x:Name` | TR başlık (kaynaktaki haliyle) | EN başlık |
|---|---|---|---|---|---|
| 1 | 0 | `sec0` | `sec0en` | `Genel Bakis` | `Overview` |
| 2 | 1 | `sec1` | `sec1en` | `Arayuz Yapisi` | `Interface Layout` |
| 3 | 2 | `sec2` | `sec2en` | `Klavye Kisayollari` | `Keyboard Shortcuts` |
| 4 | 3 | `sec3` | `sec3en` | `Fare Kontrolleri` | `Mouse Controls` |
| 5 | 4 | `sec4` | `sec4en` | `Piksel Duzenleme` | `Pixel Editing` |
| 6 | 7 | `sec7` | `sec7en` | `Optimum Tas Sayisi` | `Optimum Stone Count` |
| 7 | 8 | `sec8` | `sec8en` | `Stok Yonetimi (Google Sheet)` | `Stock Management (Google Sheet)` |
| 8 | 5 | `sec5` | `sec5en` | `Proje Yonetimi` | `Project Management` |
| 9 | 6 | `sec6` | `sec6en` | `Ipuclari` | `Tips` |

Klavye kısayolları bölümündeki tuş metinleri sabit değil, `Loc.Instance` üzerindeki `KeyModI`, `KeyModO`, `KeyModS`, `KeyModShiftS`, `KeyModE`, `KeyModM`, `KeyMod0`, `KeyModZ`, `KeyModY` özelliklerine bağlıdır; macOS'ta ⌘, diğerlerinde Ctrl görünür. `F1`, `F4` (Özellikler panelini gizle / göster) ve `Esc` (süren işi iptal et) satırları sabit metindir.

### Code-behind alanları

| Ad | Tip | Varsayılan | Açıklama |
|---|---|---|---|
| `_sectionsTr` | `TextBlock[]` | `{ sec0, …, sec8 }` | İndeks = `Tag`. |
| `_sectionsEn` | `TextBlock[]` | `{ sec0en, …, sec8en }` | İndeks = `Tag`. |

## Public API

| Metot | Ne yapar | Kimden çağrılır |
|---|---|---|
| `HelpWindow()` | `InitializeComponent` ve iki bölüm dizisini kurar. | `MainWindow.ShowHelp` |
| `OnNavClick` (private) | Düğmenin `Tag`'ini `int`'e çevirir; `Loc.Instance.Lang == "en"` ise `_sectionsEn`, değilse `_sectionsTr` dizisinden ilgili başlığa `BringIntoView()` uygular. Geçersiz/aralık dışı `Tag` sessizce yok sayılır. | Kenar çubuğu düğmeleri |

## Önemli davranışlar ve iş kuralları

- Dil değişimi canlıdır: paneller `IsTr`/`IsEn` bağlamasıyla gizlenip gösterilir, pencere yeniden açılmaz.
- Stok Yönetimi bölümü (`sec8` / `sec8en`) araç çubuğundaki açılır okları (Tablo: tabloyu aç / stok ayarları; Stok Çek: stoğu olmayanları kırmızıyla işaretle) ve Optimum'un yanındaki "Stoğa göre" kutusunu da anlatır.
- Proje Yönetimi bölümünün (`sec5` / `sec5en`) `.mos` kartında, kısayol satırının altında **Google Drive** alt başlığı vardır: Drive ikonu ve menüsü, bir kezlik Apps Script kurulumu, Drive'a Kaydet (mosairPROJECT gibi proje klasörü + orijinal görsel) / Drive'dan Aç (önizlemeli kartlar, arama, sıralama, Yenile, Drive'da göster), güvenlik (`ALLOWED_FOLDERS`), boyut sınırı ve uygulama güncellenince script'in "Dağıtımları yönet → Yeni sürüm" ile güncellenmesi. Arayüz Yapısı bölümündeki toolbar ve Dosya menüsü satırları ile Fare Kontrolleri'ndeki Sol Tuş / Ok (▾) satırları da Drive ikonunu anar.
- Arayüz Yapısı bölümünün menü metni Görünüm menüsündeki **Yumuşak Fare Hareketi** öğesini, Canvas kartı ve Tekerlek kısayol satırı yumuşak zoom ile sağ tuşla kaydırma sonrası süzülmeyi anlatır (TR ve EN).
- Arayüz Yapısı bölümünün (`sec1` / `sec1en`) "Sag Panel — Properties" kartı, taş seçili değilken görünen görsel bilgisi kartlarını (önizleme ve dosya adı, Ayrıntılar: çözünürlük, megapiksel, en-boy oranı, dosya boyutu, tarih; renk çubuğuyla baskın renkler; en çok kullanılan taşlar; ✕ ile seçime dönüş) ve panelin gizlenip gösterilmesini (başlıktaki düğme, F4, Görünüm → Özellikler Paneli, sağ kenardaki şerit) de anlatır; menü paragrafında Görünüm menüsü Özellikler Paneli'ni de sayar. Dosya menüsü paragrafı **Yeni Görselleri Bildir**'i sayar; İpuçları bölümünde (`sec6` / `sec6en`) İndirilenler / Masaüstüne gelen yeni JPEG/PNG için çıkan "mosair'de açılsın mı?" bildirimini anlatan bir ipucu vardır.
- Metinlerin tamamı XAML'a gömülüdür (`Loc` sözlüğünde değildir); yalnızca pencere başlığı ve kısayol tuşları `Loc`'tan gelir.

### Yeni bölüm ekleme

1. Bir sonraki boş numarayı seçin (şu an `9`).
2. `contentPanel` içinde istenen konuma `<TextBlock x:Name="sec9" Classes="h1" Text="..."/>` ve ardından içerik kartını (`Border Classes="section-card"`) ekleyin.
3. Aynısını İngilizce olarak `contentPanelEn` içine `sec9en` adıyla, **aynı konuma** ekleyin.
4. Her iki gezinme `StackPanel`'ine (TR ve EN) `Tag="9"` ve `Click="OnNavClick"` olan bir `Button` ekleyin; diğer düğmelerin stil özniteliklerini kopyalayın.
5. `HelpWindow()` yapıcısında `_sectionsTr` dizisinin sonuna `sec9`, `_sectionsEn` dizisinin sonuna `sec9en` ekleyin. Dizi sırası görüntü sırasına değil `Tag` numarasına göredir.
6. Aynı içeriği [docs/ARAYUZ.md](../ARAYUZ.md) belgesine de işleyin.

## Dikkat / bilinen sınırlamalar

- **Tutarlılık:** Bu pencere ile [ARAYUZ.md](../ARAYUZ.md) aynı kullanıcı bilgisini taşır. Kısayol, menü veya stok davranışı değiştiğinde üç yer birlikte güncellenmelidir: TR paneli, EN paneli ve `ARAYUZ.md`. Kısayolların gerçek davranışı için kaynak `MainWindow.OnKeyDown`'dur.
- Türkçe metinler kaynakta Türkçe karakter kullanmadan yazılmıştır (`Genel Bakis`, `Ipuclari` …). Düzeltilecekse tüm panel birlikte ele alınmalıdır.
- `Tag` ↔ dizi indeksi eşlemesi elle tutulur; bir `TextBlock` yeniden adlandırılır ya da dizi sırası bozulursa düğme yanlış bölüme kaydırır (derleme hatası vermez).
- Renkler tema anahtarlarına (`DynamicResource`) bağlıdır; açık temada kılavuz da açık görünür. Bölüm rozetleri, başlık yeşili ve kısayol rozeti bilerek sabit renklidir.

## İlgili dosyalar

- [MainWindow](MainWindow.md)
- [Loc](Services/Loc.md)
- [Kullanıcı arayüzü (ARAYUZ)](../ARAYUZ.md)
