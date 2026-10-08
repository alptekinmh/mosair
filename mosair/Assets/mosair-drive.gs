/**
 * mosair — Google Drive project folder (Apps Script web app).
 *
 * Kurulum (bir kez):
 *  1. https://script.google.com → Yeni proje → bu kodun tamamını yapıştırın → Kaydet.
 *  2. Dağıt → Yeni dağıtım → Tür: Web uygulaması
 *       Yürüten: Ben   ·   Erişimi olan: Herkes
 *  3. İzinleri onaylayın, verilen "…/exec" adresini mosair'de
 *     Dosya → Google Drive → Drive Klasörü Ayarları… penceresindeki "Apps Script URL" alanına yapıştırın.
 *  Kodu sonradan güncellerken: Dağıt → Dağıtımları yönet → düzenle → Sürüm: Yeni sürüm (adres değişmez).
 *
 * Klasör düzeni (masaüstündeki mosairPROJECT ile aynı): her proje, Drive klasörünün içinde görselin adını
 * taşıyan bir alt klasöre kaydedilir; içinde <ad>.mos ve görsel bulunur (Görsel Ayarları kullanıldıysa ayarlı
 * hâli; değiştiyse her kayıtta yenilenir). Ayarlı projede orijinal görsel "orijinal" alt klasöründe durur.
 *
 * Güvenlik: Bu adresi bilen herkes, izin verilen klasöre .mos dosyası yazabilir ve oradan okuyabilir.
 * Adresi yalnızca güvendiğiniz kişilerle paylaşın. İsterseniz Proje ayarları → Komut dosyası özellikleri'ne
 * ALLOWED_FOLDERS adlı bir özellik ekleyip değerine izin verilen klasör ID'lerini virgülle yazın; o zaman
 * script yalnızca bu klasörlerle çalışır.
 */

// The subfolder of a project folder that keeps the original image when the picture beside the project is the
// adjusted one (Görsel Ayarları); the same name mosair uses on disk.
var ORIGINAL_FOLDER = 'orijinal';

function doPost(e) {
  try {
    var req = JSON.parse(e.postData.contents);
    var folder = allowedFolder(req.folderId);

    if (req.action === 'ping') {
      return reply({ status: 'ok', folder: folder.getName() });
    }

    if (req.action === 'save') {
      // The project (and the original image, when given) arrive gzip-compressed and base64-encoded.
      var name = String(req.name || '').trim();
      if (!/\.mos$/i.test(name)) throw new Error('Dosya adı .mos ile bitmeli');
      var target = req.folderName ? subfolder(folder, String(req.folderName)) : folder;
      var file = replaceFile(target, name, unpack(req.data, name));
      // The project's picture (with Görsel Ayarları: the adjusted image). Replaced when it differs from the one
      // already there (a changed adjustment), so Drive always has the picture that goes with the project.
      if (req.image && req.imageName) {
        var imageName = String(req.imageName);
        var picture = unpack(req.image, imageName);
        var existing = target.getFilesByName(imageName);
        var same = existing.hasNext() && existing.next().getSize() === picture.getBytes().length;
        if (!same) replaceFile(target, imageName, picture);
      }
      // With Görsel Ayarları the untouched original goes to the project's "orijinal" subfolder (as on disk), so
      // the project opens again with its settings; replaced when it differs.
      if (req.original && req.originalName) {
        var originalName = String(req.originalName);
        var original = unpack(req.original, originalName);
        var originals = subfolder(target, ORIGINAL_FOLDER);
        var kept = originals.getFilesByName(originalName);
        var keptSame = kept.hasNext() && kept.next().getSize() === original.getBytes().length;
        if (!keptSame) replaceFile(originals, originalName, original);
      }
      return reply({ status: 'ok', id: file.getId(), name: file.getName(), folder: target.getName() });
    }

    if (req.action === 'list') {
      // .mos files in the folder itself and in its project folders (one level down), newest first.
      var files = [];
      addMos(folder, '', files);
      var subs = folder.getFolders();
      while (subs.hasNext()) {
        var sub = subs.next();
        addMos(sub, sub.getName(), files);
      }
      files.sort(function (a, b) { return b.modified.localeCompare(a.modified); });
      return reply({ status: 'ok', folder: folder.getName(), files: files });
    }

    if (req.action === 'get') {
      // A project file, or (with imageName) the file of that name next to it, e.g. its original image.
      var mos = DriveApp.getFileById(req.fileId);
      if (!/\.mos$/i.test(mos.getName())) throw new Error('Yalnızca .mos projeleri açılabilir');
      var parent = parentInside(mos, folder);
      if (!parent) throw new Error('Dosya bu klasörde değil');
      var wanted = mos;
      if (req.imageName) {
        // "7.jpg" next to the project, or "orijinal/7.jpg" in its originals subfolder (no other paths).
        var parts = String(req.imageName).split('/');
        var where = parent;
        if (parts.length === 2 && parts[0] === ORIGINAL_FOLDER) {
          var subs = parent.getFoldersByName(ORIGINAL_FOLDER);
          if (!subs.hasNext()) return reply({ status: 'ok', name: '', data: '' });
          where = subs.next();
        } else if (parts.length !== 1) {
          throw new Error('Geçersiz dosya yolu');
        }
        var images = where.getFilesByName(parts[parts.length - 1]);
        if (!images.hasNext()) return reply({ status: 'ok', name: '', data: '' });
        wanted = images.next();
      }
      var zipped = Utilities.gzip(wanted.getBlob());
      return reply({ status: 'ok', name: wanted.getName(), data: Utilities.base64Encode(zipped.getBytes()) });
    }

    if (req.action === 'thumbs') {
      // Small previews (Drive's own thumbnails) of the given files, base64 PNG; "" when Drive has none yet.
      var thumbs = {};
      (req.ids || []).forEach(function (id) {
        try {
          var f = DriveApp.getFileById(id);
          if (!parentInside(f, folder)) return;
          var t = f.getThumbnail();
          thumbs[id] = t ? Utilities.base64Encode(t.getBytes()) : '';
        } catch (x) { thumbs[id] = ''; }
      });
      return reply({ status: 'ok', thumbs: thumbs });
    }

    throw new Error('Bilinmeyen işlem: ' + req.action);
  } catch (err) {
    return reply({ status: 'error', message: String(err && err.message ? err.message : err) });
  }
}

function unpack(data, name) {
  var blob = Utilities.ungzip(Utilities.newBlob(Utilities.base64Decode(data), 'application/x-gzip', name + '.gz'));
  blob.setName(name);
  if (/\.mos$/i.test(name)) blob.setContentType('application/octet-stream');
  return blob;
}

// Same name = same file: the new copy is written first, then older ones go to the trash (like overwriting a
// file on disk; if writing fails, the old copy stays).
function replaceFile(target, name, blob) {
  var old = [];
  var same = target.getFilesByName(name);
  while (same.hasNext()) old.push(same.next());
  var file = target.createFile(blob);
  old.forEach(function (f) { f.setTrashed(true); });
  return file;
}

function subfolder(parent, name) {
  name = name.replace(/[\/\\]/g, '_').trim() || 'mosair_project';
  var it = parent.getFoldersByName(name);
  return it.hasNext() ? it.next() : parent.createFolder(name);
}

function addMos(f, folderName, out) {
  var mos = [], imageId = '';
  var it = f.getFiles();
  while (it.hasNext()) {
    var file = it.next();
    if (/\.mos$/i.test(file.getName())) mos.push(file);
    else if (!imageId && /^image\//.test(file.getMimeType())) imageId = file.getId();
  }
  mos.forEach(function (file) {
    // imageId: the project's original image in the same folder (for the preview), "" when there is none.
    out.push({ id: file.getId(), name: file.getName(), folder: folderName, size: file.getSize(),
               modified: file.getLastUpdated().toISOString(), imageId: folderName ? imageId : '' });
  });
}

// The file's parent when it is the folder itself or one of its project folders; otherwise null.
function parentInside(file, folder) {
  var parents = file.getParents();
  while (parents.hasNext()) {
    var p = parents.next();
    if (p.getId() === folder.getId()) return p;
    var up = p.getParents();
    while (up.hasNext()) if (up.next().getId() === folder.getId()) return p;
  }
  return null;
}

function allowedFolder(folderId) {
  if (!folderId) throw new Error('Klasör belirtilmedi');
  var allowed = PropertiesService.getScriptProperties().getProperty('ALLOWED_FOLDERS');
  if (allowed) {
    var ids = allowed.split(',').map(function (s) { return s.trim(); });
    if (ids.indexOf(folderId) < 0) throw new Error('Bu klasöre izin verilmiyor (ALLOWED_FOLDERS)');
  }
  return DriveApp.getFolderById(folderId);
}

function reply(obj) {
  return ContentService.createTextOutput(JSON.stringify(obj)).setMimeType(ContentService.MimeType.JSON);
}
