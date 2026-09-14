const ALLOWED = new Set(['page_view_by_type', 'search_no_hit', 'offline_reuse']);
const FORBIDDEN_KEYS = ['email', 'userId', 'user_id', 'identity', 'sub', 'query', 'claimText', 'claim_text', 'sourceText', 'name', 'ip'];

export function buildEvent(kind, fields = {}) {
  return { kind, ...fields, at: new Date().toISOString() };
}

export function validateTelemetry(event) {
  const errors = [];
  if (!event || typeof event !== 'object') return ['event must be an object'];
  if (!ALLOWED.has(event.kind)) errors.push(`kind must be one of ${[...ALLOWED].join(',')}`);
  for (const key of FORBIDDEN_KEYS) {
    if (key in event) errors.push(`forbidden field: ${key}`);
  }
  for (const [key, value] of Object.entries(event)) {
    if (typeof value === 'string' && /[\w.+-]+@[\w-]+\.[\w.]+/.test(value)) errors.push(`possible identity in ${key}`);
    if (typeof value === 'string' && value.length > 500) errors.push(`field too long: ${key}`);
  }
  return errors;
}

export function recordLocal(kind, fields = {}) {
  const event = buildEvent(kind, fields);
  const errors = validateTelemetry(event);
  if (errors.length > 0) throw new Error(`Telemetry rejected: ${errors.join('; ')}`);
  try {
    const raw = localStorage.getItem('dharmatlas.telemetry') || '[]';
    const list = JSON.parse(raw);
    list.push({ kind: event.kind, at: event.at, entityType: event.entityType || null });
    localStorage.setItem('dharmatlas.telemetry', JSON.stringify(list.slice(-200)));
  } catch {
    // Telemetry never breaks the product surface.
  }
  return event;
}
