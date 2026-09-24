window.idbCache = (() => {
    const DB_NAME = 'PigeonsTrackerCache';
    const STORE = 'cache';
    let dbPromise = null;

    function open() {
        if (!dbPromise) {
            dbPromise = new Promise((resolve, reject) => {
                const req = indexedDB.open(DB_NAME, 1);
                req.onupgradeneeded = e => e.target.result.createObjectStore(STORE, { keyPath: 'key' });
                req.onsuccess = e => resolve(e.target.result);
                req.onerror = e => { dbPromise = null; reject(e.target.error); };
            });
        }
        return dbPromise;
    }

    async function txGet(key) {
        const db = await open();
        return new Promise((resolve, reject) => {
            const r = db.transaction(STORE, 'readonly').objectStore(STORE).get(key);
            r.onsuccess = () => resolve(r.result ?? null);
            r.onerror = e => reject(e.target.error);
        });
    }

    return {
        get: txGet,
        set: async (key, data, cachedAt, lastSyncAt) => {
            const db = await open();
            return new Promise((resolve, reject) => {
                const r = db.transaction(STORE, 'readwrite').objectStore(STORE).put({ key, data, cachedAt, lastSyncAt });
                r.onsuccess = () => resolve();
                r.onerror = e => reject(e.target.error);
            });
        },
        updateMeta: async (key, lastSyncAt) => {
            const db = await open();
            return new Promise((resolve, reject) => {
                const tx = db.transaction(STORE, 'readwrite');
                const store = tx.objectStore(STORE);
                const getReq = store.get(key);
                getReq.onsuccess = () => {
                    if (!getReq.result) { resolve(); return; }
                    const putReq = store.put({ ...getReq.result, lastSyncAt });
                    putReq.onsuccess = () => resolve();
                    putReq.onerror = e => reject(e.target.error);
                };
                getReq.onerror = e => reject(e.target.error);
            });
        },
        remove: async key => {
            const db = await open();
            return new Promise((resolve, reject) => {
                const r = db.transaction(STORE, 'readwrite').objectStore(STORE).delete(key);
                r.onsuccess = () => resolve();
                r.onerror = e => reject(e.target.error);
            });
        },
        has: async key => (await txGet(key)) !== null
    };
})();

function isDevice() {
    return /android|webos|iphone|ipad|ipod|blackberry|iemobile|opera mini|mobile/i.test(navigator.userAgent);
}

function resizeImageToBase64(dataUrl, maxWidth, maxHeight) {
    return new Promise(function (resolve) {
        var img = new Image();
        img.onload = function () {
            var width = img.width;
            var height = img.height;
            var ratio = Math.min(maxWidth / width, maxHeight / height, 1);
            width = Math.round(width * ratio);
            height = Math.round(height * ratio);
            var canvas = document.createElement('canvas');
            canvas.width = width;
            canvas.height = height;
            canvas.getContext('2d').drawImage(img, 0, 0, width, height);
            resolve(canvas.toDataURL('image/jpeg', 0.8));
        };
        img.src = dataUrl;
    });
}