import { apiUrl } from './formatters.js';

export async function get(path, params, signal) {
  const response = await fetch(apiUrl(path, params), { signal, headers: { Accept: 'application/json' } });
  if (!response.ok) {
    const error = await response.json().catch(() => ({}));
    throw new Error(error.detail || `Request failed (${response.status})`);
  }
  return response.json();
}
