import en from './i18n/en.json' with { type: 'json' };
import zh from './i18n/zh.json' with { type: 'json' };
import ja from './i18n/ja.json' with { type: 'json' };

const catalogs = { en, zh, ja };

export function supportedLocales() {
  return Object.keys(catalogs);
}

export function resolveLocale(requested) {
  const normalized = String(requested || 'en').toLowerCase().split('-')[0];
  return catalogs[normalized] ? normalized : 'en';
}

export function t(locale, key) {
  const resolved = resolveLocale(locale);
  const hit = catalogs[resolved]?.[key];
  if (typeof hit === 'string' && hit.trim().length > 0) return hit;
  const fallback = en[key];
  if (typeof fallback === 'string' && fallback.trim().length > 0) return fallback;
  return key;
}
