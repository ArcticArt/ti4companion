// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.webmanifest$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];

// Replace with your base path if you are hosting on a subfolder. Ensure there is a trailing '/'.
const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall(event) {
    console.info('Service worker: Install');

    // Take over as soon as the new version is cached, instead of waiting for every tab of the origin
    // to close (2026-08-14). This reverses an earlier decision, and the reason it was made no longer
    // holds:
    //
    //   * The old caution was that activating drops the cache the running version reads from, and that
    //     its FINGERPRINTED framework files would be gone from the server too, breaking a game
    //     mid-deploy. But `WasmFingerprintAssets` is false here — exactly one of the 67 files under
    //     _framework carries a hash — so the names are stable and a late request resolves to the new
    //     file rather than a 404.
    //   * The cost of waiting turned out to be the worse half: an installed home-screen app on iOS is
    //     essentially never fully closed, so a device sat on a pre-2.0 build across several deploys.
    //     An old client against a new API gets 400s, and the store treats those as "refresh, do not
    //     change" — the table taps and nothing happens.
    //
    // The page that is CURRENTLY open keeps running its old code; only the next launch is new. That is
    // deliberate: nobody gets yanked out of a turn, and this app's state lives on the server anyway, so
    // a relaunch costs seconds and loses nothing. UpdateNotice still offers the immediate reload.
    //
    // Note this rescues clients whose INSTALLED worker predates the change: skipWaiting is called by
    // the new worker during its own install, not by the old one.
    self.skipWaiting();


    // Fetch and cache all matching items from the assets manifest
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Delete unused caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));

    // Control the already-open pages too, not only the ones opened from here on. Without this, a page
    // loaded under the previous worker stays uncontrolled until it navigates — on an installed app
    // that can be days.
    await self.clients.claim();
}

// Switch over NOW, because the app asked. A newly installed worker otherwise waits until every tab of the
// origin is closed — which is why a deploy kept serving the old app to anyone who just pressed reload. The
// request only ever comes from the user tapping "reload" on the update bar (see UpdateNotice.razor): the
// activation below clears the previous cache, so it must not happen behind the back of a running game.
self.addEventListener('message', event => {
    if (event.data && event.data.type === 'SKIP_WAITING') self.skipWaiting();
});

async function onFetch(event) {
    let cachedResponse = null;
    if (event.request.method === 'GET') {
        // For all navigation requests, try to serve index.html from cache,
        // unless that request is for an offline resource.
        // If you need some URLs to be server-rendered, edit the following check to exclude those URLs
        const shouldServeIndexHtml = event.request.mode === 'navigate'
            && !manifestUrlList.some(url => url === event.request.url);

        const request = shouldServeIndexHtml ? 'index.html' : event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }

    return cachedResponse || fetch(event.request);
}
// ---------------------------------------------------------------------------
// Web Push: "you're up". Payload is the JSON from Services/PushService.cs.
// A service worker is the only thing that can show a notification while the tab
// is closed or the phone is locked, which is the whole point of the feature.
// ---------------------------------------------------------------------------
self.addEventListener('push', event => {
    let d = {};
    try { d = event.data ? event.data.json() : {}; } catch (e) { d = {}; }
    // One pending "your turn" per device: the tag makes a newer notification REPLACE the older one
    // instead of stacking six of them after a long turn.
    const options = {
        body: d.body || '',
        icon: 'icon-192.png',
        badge: 'icon-192.png',
        tag: d.tag || 'ti4',
        renotify: true,
        data: { code: d.code || '' }
    };
    event.waitUntil(self.registration.showNotification(d.title || 'TI4 Companion', options));
});

// Tapping it should land in the game, and reuse an already open tab rather than piling up windows.
self.addEventListener('notificationclick', event => {
    event.notification.close();
    const code = (event.notification.data && event.notification.data.code) || '';
    const path = code ? '/s/' + code : '/';
    event.waitUntil((async () => {
        const windows = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
        for (const w of windows) {
            if (w.url.indexOf(path) !== -1) { await w.focus(); return; }
        }
        await self.clients.openWindow(path);
    })());
});
