// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
self.addEventListener('fetch', () => { });

// Take over when the app asks (the update bar's "reload"). Nothing is cached here, but a newly installed
// worker still waits for every tab to close, so the same hand-over is needed to test the flow at all — and
// the message must be handled by the WAITING worker, which is the newly deployed script, not the old one.
self.addEventListener('message', event => {
    if (event.data && event.data.type === 'SKIP_WAITING') self.skipWaiting();
});
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
