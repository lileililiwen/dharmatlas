export const VISIT_TTL_MS = 7 * 24 * 60 * 60 * 1000;
const PREFIX = 'dharmatlas.visit:';

function store() {
  if (typeof localStorage === 'undefined') return null;
  return localStorage;
}

export function visitKey(route) {
  return `${PREFIX}${route}`;
}

export function saveVisit(route, data, now = Date.now()) {
  const s = store();
  if (!s) return false;
  try {
    s.setItem(visitKey(route), JSON.stringify({ savedAt: now, data }));
    const keys = [];
    for (let i = 0; i < s.length; i++) {
      const k = s.key(i);
      if (k && k.startsWith(PREFIX)) keys.push(k);
    }
    while (keys.length > 100) {
      const oldest = keys.shift();
      s.removeItem(oldest);
    }
    return true;
  } catch {
    return false;
  }
}

export function loadVisit(route, now = Date.now(), ttlMs = VISIT_TTL_MS) {
  const s = store();
  if (!s) return { status: 'miss' };
  try {
    const raw = s.getItem(visitKey(route));
    if (!raw) return { status: 'miss' };
    const parsed = JSON.parse(raw);
    if (!parsed || typeof parsed.savedAt !== 'number') return { status: 'miss' };
    if (now - parsed.savedAt > ttlMs) {
      s.removeItem(visitKey(route));
      return { status: 'expired' };
    }
    return { status: now - parsed.savedAt > 0 ? 'stale-ok' : 'fresh', data: parsed.data, savedAt: parsed.savedAt };
  } catch {
    return { status: 'miss' };
  }
}
