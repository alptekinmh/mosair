# Region.cs

## Genel Bakis

Mozaiklestirme isleminde bolge (region) tanimlamalarini icerir. Orijinal WPF uygulamasinda gorsel birden fazla bolgeye ayrilabiliyordu; musteri uygulamasinda tum gorsel tek bir bolge olarak islenir. `dr` sinifi tekil piksel pozisyonunu, `drl` sinifi bolge tanimini ve bolge bazli veri dizisini tasir.

## Siniflar ve Alanlar

### `dr`

Tek bir piksel pozisyonunu temsil eder.

- `x` — Sutun (column) indeksi.
- `y` — Satir (row) indeksi.
- `boolPass` — Pikselin islenip islenmedigini belirten bayrak.
- `L` — CIELAB L (aydinlik) degeri (bazi islemlerde kullanilir).

**Constructor'lar:**
- `dr()` — Bos.
- `dr(x, y)` — Koordinatli olusturma.

### `drl`

Bir bolgenin tanimini ve bolge bazli piksel takip dizisini icerir.

**Statik Alanlar (tum bolgeler arasi paylasilan):**
- `arar` — `List<drl>`: Tum bolgelerin listesi. Musteri uygulamasinda tek eleman icerir.
- `dat[R, C, 4]` — Piksel bazli bolge/renk takip dizisi:
  - `dat[i, j, 0]` — Bolge kontrol bayragi 1 (genellikle 1).
  - `dat[i, j, 1]` — Bolge kontrol bayragi 2 (genellikle 1).
  - `dat[i, j, 2]` — Bu pikselin ait oldugu bolge numarasi (musteri uygulamasinda hep 1).
  - `dat[i, j, 3]` — Bu piksele atanan renk numarasi (`rgb.u` degeri). Iteratif surec boyunca guncellenir; sonunda katalog `ID` degeriyle degistirilir.
- `datBackup[R, C, 4]` — `dat` dizisinin yedegi.

**Ornek Alanlar (her bolge icin ayri):**
- `ar` — `List<dr>`: Bu bolgeye ait piksel pozisyonlarinin listesi.
- `x1, y1` — Bolgenin sol ust kosesi (dahil).
- `x2, y2` — Bolgenin sag alt kosesi (haric).
- `rgbM` — Bu bolge icin hedef ara renk sayisi.

**Constructor'lar:**
- `drl()` — Bos.
- `drl(x1, y1, x2, y2)` — Sinir koordinatlariyla olusturma.

## Diger Dosyalarla Iliskisi

- **MosaicEngine.cs** — `CreateSingleRegion()` ile tek bir `drl` nesnesi olusturulur ve `arar`'a eklenir. `dat` dizisi tum surec boyunca guncellenir: `SetColorNumber()` renk numarasi yazar, Section 3-5 birlestirme ve katalog ID atamasi yapar.
- **MosaicData.cs** — `dat` dizisi `dataM3` ile birlikte calisir. `dataM3` piksel rengini, `dat` ise bolge ve renk numarasini tutar. Birlikte kullanilarak belirli bir pikselin hangi bolgedeki hangi renge ait oldugu belirlenir.

## Orijinal WPF Karsiligi

- **Kaynak:** `mosairWPF/ourRobotWpf/picture/lum.cs` — `dr` ve `drl` siniflari orijinalde `lum.cs` icinde tanimlanmisti. Coklu bolge destegi icin daha fazla alan vardi (boolPass gibi), musteri uygulamasinda sadeletirilmistir.
- Orijinal uygulamada kullanici gorseli birden fazla bolgeye ayirabiliyordu; her bolge farkli `rgbM` degerine sahip olabilirdi. Musteri uygulamasinda bu ozellik yoktur, tek bolge kullanilir.
