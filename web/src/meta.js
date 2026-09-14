export function buildMeta(entity, route, origin = '') {
  const name = entity?.canonicalName || entity?.id || 'Dharmatlas entity';
  const type = entity?.type || 'entity';
  const certainty = entity?.certainty ? ` (${String(entity.certainty).toLowerCase()})` : '';
  const title = `${name} — ${type}${certainty} | Dharmatlas`;
  const summary = entity?.summary || 'A source-first record in the open historical atlas of Buddhism.';
  const description = summary.length > 300 ? `${summary.slice(0, 297)}...` : summary;
  const canonical = `${origin}${route}`;
  return { title, description, canonical };
}

export function updateDocumentMeta(meta) {
  if (typeof document === 'undefined') return;
  document.title = meta.title;
  setMeta('description', meta.description);
  setMeta('og:title', meta.title);
  setMeta('og:description', meta.description);
  setMeta('og:url', meta.canonical);
  setLink('canonical', meta.canonical);
}

function setMeta(name, content) {
  const selector = name.startsWith('og:') ? `meta[property="${name}"]` : `meta[name="${name}"]`;
  let tag = document.head.querySelector(selector);
  if (!tag) {
    tag = document.createElement('meta');
    if (name.startsWith('og:')) tag.setAttribute('property', name);
    else tag.setAttribute('name', name);
    document.head.appendChild(tag);
  }
  tag.setAttribute('content', content);
}

function setLink(rel, href) {
  let tag = document.head.querySelector(`link[rel="${rel}"]`);
  if (!tag) {
    tag = document.createElement('link');
    tag.setAttribute('rel', rel);
    document.head.appendChild(tag);
  }
  tag.setAttribute('href', href);
}
