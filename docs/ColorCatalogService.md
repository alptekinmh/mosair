# ColorCatalogService.cs

## Genel Bakis

`ColorCatalogService` 124 farkli tas renginden olusan renk katalogunun yuklenmesi, filtrelenmesi ve UI'ya sunulmasindan sorumlu statik siniftir. Katalog verisi `Assets/colorsBas.txt` dosyasindan okunur. Kullanici arayuzunden belirli renklerin devre disi birakilmasini destekler.

## Metodlar

### `LoadCatalog(string path)`

Belirtilen yoldaki renk katalog dosyasini okur ve `MosaicData.arRGBAll` listesine yukler.

**Dosya formati:** Her satir boslukla ayrilmis alanlar icerir:
```
R G B codeName name1 name2 [name3]
```
Ornek: `168 139 112 RW020 Rosa Wood`

**Islem:**
1. Her satir icin `id` 1'den baslayarak artar
2. `rgb` nesnesi olusturulur: `r`, `g`, `b`, `codeName`, `name`, `ID`, `dis = (r+g+b)/3`
3. `arRGBAll` listesine eklenir
4. `InitArcs()` ile disari birakma durumlari baslatilir
5. `SetActiveColors()` ile aktif renk listesi olusturulur

### `LoadDefaultCatalog()`

`AppContext.BaseDirectory/Assets/colorsBas.txt` yolundaki varsayilan katalogu yukler. Uygulama baslatildiginda `MainViewModel` constructor'inda cagirilir.

### `SetLeaveOut(int index, bool leaveOut)`

Belirtilen indeksteki rengin disari birakma durumunu degistirir.

**Islem:**
1. `arRGBAll[index].boolLeaveOut = leaveOut`
2. `arcs[index] = leaveOut` (boolean izleme listesi)
3. `SetActiveColors()` ile aktif renk listesini yeniden olusturur

**Kullanim:** UI'daki CheckBox tiklandiginda `MainViewModel.SyncColorExclusion()` uzerinden cagirilir.

### `SetActiveColors()`

`arRGBAll` listesinden `boolLeaveOut=false` olan renkleri filtreleyerek `MosaicData.arRGB` aktif renk listesini olusturur. Bu liste M3 pipeline'inda ve katalog eslestirmede kullanilir.

### `InitArcs()` (private)

`arcs` boolean listesini baslatir veya senkronize eder.

**Ilk yukleme:** `arcs` bos ise her renk icin `false` (aktif) degerle doldurur.

**Yeniden yukleme:** `arcs` zaten mevcutsa her rengin `boolLeaveOut` degerini `arcs`'tan geri yukler. Bu sayede kullanici tercihleri korunur.

### `GetCatalogList() → List<CatalogColorInfo>`

UI'da gosterilmek uzere tum katalog renklerinin bilgilerini `CatalogColorInfo` DTO listesi olarak dondurur. Her eleman: Index, R, G, B, CodeName, Name, ID, IsExcluded.

## CatalogColorInfo Sinifi

UI binding icin kullanilan basit veri transfer nesnesi (DTO):

| Alan | Tip | Aciklama |
|------|-----|----------|
| `Index` | `int` | arRGBAll icindeki sira |
| `R`, `G`, `B` | `byte` | RGB renk degerleri |
| `CodeName` | `string` | Tas kodu (ornek: "RW020") |
| `Name` | `string` | Tas adi (ornek: "Rosa Wood") |
| `ID` | `int` | Sirali numara (1-124) |
| `IsExcluded` | `bool` | Disari birakildi mi |

## Diger Dosyalarla Iliskisi

| Dosya | Iliski |
|-------|--------|
| `MosaicData.cs` | `arRGBAll` (tam katalog), `arRGB` (aktif renkler), `arcs` (disari birakma listesi) |
| `MosaicEngine.cs` | `SetActiveColors()` RunMosaicAsync basinda cagirilir |
| `ColorMatcher.cs` | `arRGB` uzerinden mesafe hesabi yapar |
| `MainViewModel.cs` | `GetCatalogList()` ile UI'yi doldurur, `SetLeaveOut()` ile checkbox degisiklikleri iletir |
| `StoneTextureService.cs` | `arMA` icindeki `codeName` ile doku klasorlerini eslestirir |
| `Assets/colorsBas.txt` | Kaynak veri dosyasi (124 satir) |

## Orijinal WPF Karsiligi

- **Kaynak:** Orijinal WPF projesinde katalog yukleme kodu birden fazla dosyaya dagilmisti
- `colorsBas.txt` dosya formati degismeden tasinmistir
- Disari birakma (leaveOut) mekanizmasi korunmustur
- `GetCatalogList()` ve `CatalogColorInfo` yeni eklenmistir (WPF'te dogrudan veri yapisina erisim vardi)
