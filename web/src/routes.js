const ENTITY_TYPES = ['persons', 'places', 'texts', 'institutions', 'traditions', 'events'];
const STATIC_ROUTES = ['/', '/timeline', '/map', '/graph'];

export function parseRoute(pathname) {
  const path = (pathname || '/').split('?')[0].split('#')[0] || '/';
  if (STATIC_ROUTES.includes(path)) return { kind: path === '/' ? 'home' : path.slice(1), path };
  const match = path.match(/^\/([a-z]+)\/([^/]+)$/);
  if (match) {
    const plural = match[1];
    const id = decodeURIComponent(match[2]);
    if (ENTITY_TYPES.includes(plural) && id.length > 0 && id.length <= 200) {
      const singular = plural.endsWith('s') ? plural.slice(0, -1) : plural;
      return { kind: 'entity', type: singular, plural, id, path };
    }
  }
  return { kind: 'notfound', path };
}

export function buildRoute(type, id) {
  const plural = `${String(type || '').toLowerCase()}s`;
  if (!ENTITY_TYPES.includes(plural) || !id) return '/';
  return `/${plural}/${encodeURIComponent(id)}`;
}

export function detailRouteToApi(detailRoute) {
  return String(detailRoute || '').replace(/^\//, '').replace(/^api\/v1\//, '');
}

export function apiToDetailRoute(type, id) {
  return buildRoute(type, id);
}

export { ENTITY_TYPES, STATIC_ROUTES };
