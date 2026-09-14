export const MAP_FEATURE_CAP = 500;
export const MAP_CLUSTER_THRESHOLD = 60;
export const MAP_DEFAULT_CELL_DEGREES = 4;

export function applyFeatureCap(features, cap = MAP_FEATURE_CAP) {
  const list = Array.isArray(features) ? features : [];
  return {
    visible: list.slice(0, cap),
    capped: list.length > cap,
    total: list.length,
  };
}

export function repPoint(feature) {
  const geometry = feature?.geometry || feature?.Geometry;
  if (Array.isArray(geometry) && geometry.length) {
    const first = geometry[0];
    const latitude = first.latitude ?? first.Latitude ?? first.lat;
    const longitude = first.longitude ?? first.Longitude ?? first.lng ?? first.lon;
    if (Number.isFinite(latitude) && Number.isFinite(longitude)) return { latitude, longitude };
  }
  const latitude = feature?.latitude ?? feature?.Latitude;
  const longitude = feature?.longitude ?? feature?.Longitude;
  if (Number.isFinite(latitude) && Number.isFinite(longitude)) return { latitude, longitude };
  return null;
}

export function clusterGrid(features, cellSizeDegrees = MAP_DEFAULT_CELL_DEGREES) {
  if (!(cellSizeDegrees > 0)) throw new RangeError('Cell size must be positive.');
  const groups = new Map();
  for (const feature of features || []) {
    const point = repPoint(feature);
    if (!point) continue;
    const key = `${Math.floor(point.latitude / cellSizeDegrees)},${Math.floor(point.longitude / cellSizeDegrees)}`;
    if (!groups.has(key)) {
      const [latCell, lngCell] = key.split(',').map(Number);
      groups.set(key, {
        center: { latitude: (latCell + 0.5) * cellSizeDegrees, longitude: (lngCell + 0.5) * cellSizeDegrees },
        members: [],
      });
    }
    groups.get(key).members.push(feature);
  }
  return [...groups.values()];
}

export function shouldCluster(count, threshold = MAP_CLUSTER_THRESHOLD) {
  return count > threshold;
}

export function tileConfig() {
  return {
    url: (typeof import.meta !== 'undefined' && import.meta.env?.VITE_TILE_URL) || 'https://demotiles.maplibre.org/style.json',
    attribution: (typeof import.meta !== 'undefined' && import.meta.env?.VITE_TILE_ATTRIBUTION) || '© OpenStreetMap contributors © MapLibre demotiles',
  };
}
