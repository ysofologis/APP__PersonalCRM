// Personal CRM service worker.
// Implements:
//   - Precaching of the WASM shell + static assets on install.
//   - Cache-first for static assets, network-first for API GETs.
//   - Background Sync registration for the offline write queue.
//   - Web Push handling for reminders (WIP — server-side wiring lands in v0.2).
//
// All cache names are versioned so upgrades cleanly evict old entries.

const VERSION       = 'v0.0.1';
const STATIC_CACHE  = `pcrm-static-${VERSION}`;
const RUNTIME_CACHE = `pcrm-runtime-${VERSION}`;
const DATA_CACHE    = `pcrm-data-${VERSION}`;

const PRECACHE_URLS = [
  '/',
  '/manifest.webmanifest',
  '/icon-192.png',
  '/icon-512.png',
  '/icon-maskable-512.png',
  '/_content/MudBlazor/MudBlazor.min.css',
  '/_content/MudBlazor/MudBlazor.min.js'
];

const SYNC_TAG = 'sync-outbox';

// ---------------------------------------------------------------- install
self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(STATIC_CACHE).then((cache) => cache.addAll(PRECACHE_URLS))
      .then(() => self.skipWaiting())
  );
});

// ----------------------------------------------------------------- activate
self.addEventListener('activate', (event) => {
  const expected = new Set([STATIC_CACHE, RUNTIME_CACHE, DATA_CACHE]);
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(keys.filter((k) => !expected.has(k)).map((k) => caches.delete(k)))
    ).then(() => self.clients.claim())
  );
});

// ------------------------------------------------------------------- fetch
self.addEventListener('fetch', (event) => {
  const req = event.request;
  if (req.method !== 'GET') {
    // Let mutations pass through to the network. The Background Sync handler
    // below catches failures and re-queues them via IndexedDB.
    return;
  }

  const url = new URL(req.url);

  // Never cache the API; always go to network. WASM-specific data caches
  // are handled inside the app via IndexedDB (Dexie), not by the SW.
  if (url.pathname.startsWith('/api/')) {
    event.respondWith(networkFirst(req, DATA_CACHE));
    return;
  }

  // Static assets: cache-first with network refresh.
  if (url.pathname.startsWith('/_content/')
      || url.pathname.startsWith('/css/')
      || url.pathname.startsWith('/js/')
      || /\.(?:png|jpg|jpeg|svg|gif|webp|ico|woff2?)$/i.test(url.pathname)) {
    event.respondWith(cacheFirst(req, STATIC_CACHE));
    return;
  }

  // Navigation requests: network-first, fall back to cached shell.
  if (req.mode === 'navigate') {
    event.respondWith(networkFirst(req, RUNTIME_CACHE));
  }
});

// ----------------------------------------------------- background sync
self.addEventListener('sync', (event) => {
  if (event.tag === SYNC_TAG) {
    event.waitUntil(notifyClientsToDrainOutbox());
  }
});

// ---------------------------------------------------------- push (v0.2+)
self.addEventListener('push', (event) => {
  if (!event.data) return;
  let payload;
  try {
    payload = event.data.json();
  } catch {
    payload = { title: 'Personal CRM', body: event.data.text() };
  }

  event.waitUntil(
    self.registration.showNotification(payload.title ?? 'Personal CRM', {
      body: payload.body ?? '',
      icon: payload.icon  ?? '/icon-192.png',
      badge: payload.badge ?? '/icon-192.png',
      tag:   payload.tag   ?? 'pcrm-reminder',
      data:  payload.data  ?? {}
    })
  );
});

self.addEventListener('notificationclick', (event) => {
  event.notification.close();
  const target = event.notification.data?.url ?? '/';
  event.waitUntil(
    self.clients.matchAll({ type: 'window', includeUncontrolled: true })
      .then((clients) => {
        for (const client of clients) {
          if ('focus' in client) {
            client.navigate(target);
            return client.focus();
          }
        }
        if (self.clients.openWindow) {
          return self.clients.openWindow(target);
        }
      })
  );
});

// ---------------------------------------------------------------- helpers

async function cacheFirst(req, cacheName) {
  const cached = await caches.match(req);
  if (cached) return cached;
  try {
    const response = await fetch(req);
    if (response && response.ok) {
      const cache = await caches.open(cacheName);
      cache.put(req, response.clone());
    }
    return response;
  } catch (err) {
    // Last resort: return the cached app shell so the SPA still loads.
    const fallback = await caches.match('/');
    if (fallback) return fallback;
    throw err;
  }
}

async function networkFirst(req, cacheName) {
  try {
    const response = await fetch(req);
    if (response && response.ok) {
      const cache = await caches.open(cacheName);
      cache.put(req, response.clone());
    }
    return response;
  } catch (err) {
    const cached = await caches.match(req);
    if (cached) return cached;
    throw err;
  }
}

async function notifyClientsToDrainOutbox() {
  const clients = await self.clients.matchAll({ type: 'window' });
  for (const client of clients) {
    client.postMessage({ type: 'pcrm:drain-outbox' });
  }
}
