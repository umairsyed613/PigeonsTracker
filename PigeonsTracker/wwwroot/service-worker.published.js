// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));
self.addEventListener('beforeinstallprompt', event => event.respondWith(beforeInstallPrompt(event)));

const cacheNamePrefix = 'pt-offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpeg$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];

async function onInstall(event) {
    console.info('Service worker: Install');

    // Fetch and cache all matching items from the assets manifest
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash }));

    await caches.open(cacheName).then(cache => {
        cache.add(new Request('index.html'));
        let requestsToFetch = [];
        Promise.all(assetsRequests.map(async (request) => {
            const response = await caches.match(request);
            if (response) {
                return cache.put(request, response);
            }
            else {
                requestsToFetch.push(request);
                return Promise.resolve();
            }
        })).then(() => {
            return cache.addAll(requestsToFetch);
        });
    });
  //  await caches.open(cacheName).then(cache => cache.addAll(assetsRequests)).catch(reason => console.log(reason));
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Delete unused caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    let cachedResponse = null;
    if (event.request.method === 'GET') {
        // For all navigation requests, try to serve index.html from cache
        // If you need some URLs to be server-rendered, edit the following check to exclude those URLs
        const shouldServeIndexHtml = event.request.mode === 'navigate';

        const request = shouldServeIndexHtml ? 'index.html' : event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }

    return cachedResponse || fetch(event.request);
}

async function beforeInstallPrompt(e){
    // Prevent Chrome 67 and earlier from automatically showing the prompt
    e.preventDefault();
    // Stash the event so it can be triggered later.
    window.PWADeferredPrompt = e;
    // Show custom installation prompt (which will trigger the built-in one, of course, when confirmed by the user)
    const checkExist = setInterval(function () {
        const indexPage = document.getElementById("indexPage");
        if (!!indexPage) {
            showAddToHomeScreen();
            clearInterval(checkExist);
        }
    }, 100);
}

function showAddToHomeScreen() {
    //console.log("Calling Install Prompt!");
    const blazorAssembly = 'PigeonsTracker';
    const blazorInstallMethod = 'PWAInstallable';

    DotNet.invokeMethodAsync(blazorAssembly, blazorInstallMethod)
        .then(function () { }, function (er) {
            //console.log(er);
            //setTimeout(showAddToHomeScreen, 1000);
        });
}
function OnPwaInstallClick() {
    if (window.PWADeferredPrompt) {
        // Fires the browser prompt. Invoked from the custom installation UI through JS Interop
        window.PWADeferredPrompt.prompt();
        window.PWADeferredPrompt.userChoice
            .then(function (choiceResult) {
                window.PWADeferredPrompt = null;
            });
    }
}
// changed 08:07