# mosair — çalışma kuralları

## Dokümantasyon her değişiklikle birlikte güncellenir

Kaynak kodda veya arayüzde yapılan her değişiklik, **aynı commit içinde** ilgili belgeleri de günceller:

| Değişen | Güncellenecek belge |
|---|---|
| `mosair/<klasör>/<Ad>.cs` veya `.axaml` / `.axaml.cs` | `docs/kod/<klasör>/<Ad>.md` (kök dosyalar için `docs/kod/<Ad>.md`) |
| Yeni kaynak dosya | Yeni `docs/kod/...md` ve `docs/README.md` haritasına bir satır |
| Silinen veya taşınan kaynak dosya | İlgili `docs/kod/...md` silinir/taşınır, harita güncellenir |
| Kullanıcının gördüğü her şey: menü, düğme, kısayol, ipucu, iletişim kutusu, uyarı, davranış | `docs/ARAYUZ.md` **ve** uygulama içi kılavuz `mosair/HelpWindow.axaml` (TR ve EN) |
| `mosair/mosair.csproj`, paket sürümleri | `docs/kod/mosair.csproj.md` |
| `.github/workflows/build.yml` | `docs/kod/build-workflow.md` |

Kurallar:

- Belgelerde yalnızca kodda gerçekten var olan adlar kullanılır; yazdıktan sonra adlar kaynakta aranarak doğrulanır.
- Belgelerdeki "Güncelleme" tarihi değiştirilen dosyada yenilenir.
- `docs/ARAYUZ.md` ile `HelpWindow.axaml` birbiriyle çelişmemelidir.
- `docs-check` iş akışı (`.github/workflows/docs-check.yml`), dokümanı olmayan bir kaynak dosya görürse başarısız olur.

## Güvenlik

Depo herkese açıktır. Google Sheet ID, tablo bağlantısı veya Apps Script (`script.google.com/macros/...`) adresi hiçbir dosyaya, belgeye ya da commit mesajına yazılmaz. Bu ayarlar yalnızca kullanıcının bilgisayarındaki `stock.json` dosyasında durur.

## Git

- Açıkça istenmeden push yapılmaz.
- Sürüm yayınlarken yeni sürüm açılmaz, son etiket taşınır (aksi istenmedikçe).

## Metinler

Arayüz metinleri `mosair/Services/Loc.cs` içindeki TR ve EN sözlüklerine birlikte eklenir.

## Menüler

Kullanıcının yapabildiği her işlem, araç çubuğunda ya da başka bir yerde düğmesi olsa bile, üst menüde de bulunur. Yeni bir özellik eklenince ait olduğu menünün altına da eklenir:

| Menü | Neler |
|---|---|
| Dosya | Görsel yükleme, proje aç/kaydet, dışa aktarma, ekran görüntüsü gibi dosya üreten/okuyan işlemler |
| Düzenle | Seçim, geri al/yinele, süren işlemi iptal etme gibi düzenleme işlemleri |
| Görünüm | Zoom, ekrana sığdırma gibi yalnızca görüntüyü değiştiren ayarlar |
| Araçlar | Mos, piksel düzenleme, ızgara, Optimum, Stoğa göre, stok işlemleri |
| Yardım | Kullanım kılavuzu |

Menü öğesinin kısayolu varsa menüde gösterilir; `docs/ARAYUZ.md` §3 ve `HelpWindow.axaml` menü listesi de güncellenir.
