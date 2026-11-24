// Kho lưu trữ khóa–giá trị trong IndexedDB của trình duyệt (dung lượng lớn hơn localStorage nhiều).
// Được gọi từ C# (PlannerStore) qua JS interop.
window.plannerDb = (function () {
  let dbPromise = null;

  function open() {
    if (!dbPromise) {
      dbPromise = new Promise(function (resolve, reject) {
        const req = indexedDB.open('planning-table', 1);
        req.onupgradeneeded = function () { req.result.createObjectStore('kv'); };
        req.onsuccess = function () { resolve(req.result); };
        req.onerror = function () { reject(req.error); };
      });
    }
    return dbPromise;
  }

  async function run(mode, action) {
    const db = await open();
    return new Promise(function (resolve, reject) {
      const tx = db.transaction('kv', mode);
      const req = action(tx.objectStore('kv'));
      tx.oncomplete = function () { resolve(req && req.result !== undefined ? req.result : null); };
      tx.onerror = function () { reject(tx.error); };
      tx.onabort = function () { reject(tx.error); };
    });
  }

  return {
    get: function (key) { return run('readonly', function (s) { return s.get(key); }); },
    set: function (key, value) { return run('readwrite', function (s) { return s.put(value, key); }); },
    remove: function (key) { return run('readwrite', function (s) { return s.delete(key); }); },

    // Mở file đính kèm (lưu dạng data URL) trong tab mới.
    openFile: async function (dataUrl) {
      const blob = await (await fetch(dataUrl)).blob();
      const url = URL.createObjectURL(blob);
      window.open(url, '_blank');
      setTimeout(function () { URL.revokeObjectURL(url); }, 60000);
    }
  };
})();
