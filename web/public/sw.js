// Dharmatlas PWA worker: shell CacheFirst, visited entity GETs StaleWhileRevalidate with TTL.
const SHELL = 'dharmatlas-shell-v1';
const READS = 'dharmatlas-reads-v1';
const READ_TTL_MS = 7 * 24 * 60 * 60 * 1000;
const SHELL_PATHS = ['/', '/index.html', '/manifest.webmanifest'];

self.addEventListener('install', (event) => {
  event.waitUntil(caches.open(SHELL).then((cache) => cache.addAll(SHELL_PATHS).catch(() => [])));
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(clients.claim());
});

function isEntityRead(url) {
  return url.origin === self.location.origin && url.pathname.startsWith('/api/v1/') && !url.pathname.includes('/contributions');
}

self.addEventListener('fetch', (event) => {
  const url = new URL(event.request.url);
  if (event.request.method !== 'GET') return;
  if (SHELL_PATHS.includes(url.pathname) || url.pathname === '/') {
    event.respondWith(
      caches.match(event.request).then((hit) => hit || fetch(event.request).then((res) => {
        const copy = res.clone();
        caches.open(SHELL).then((cache) => cache.put(event.request, copy));
        return res;
      })),
    );
    return;
  }
  if (isEntityRead(url)) {
    event.respondWith(
      caches.open(READS).then(async (cache) => {
        const cached = await cache.match(event.request);
        const network = fetch(event.request).then((res) => {
          if (res.ok) {
            const stamped = res.clone();
            cache.put(event.request, stamped);
          }
          return res;
        }).catch(() => cached);
        if (cached) {
          const date = cached.headers.get('date');
          const age = date ? Date.now() - new Date(date).getTime() : 0;
          if (age < READ_TTL_MS) return cached;
        }
        return network || cached;
      }),
    );
  }
});
