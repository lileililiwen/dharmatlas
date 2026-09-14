import { useEffect, useRef } from 'react';
import { clusterGrid, repPoint, shouldCluster, tileConfig, MAP_DEFAULT_CELL_DEGREES } from './geo.js';

export default function MapPane({ features, capped, total, tileFailed, onTileFailed }) {
  const containerRef = useRef(null);
  const mapRef = useRef(null);
  const config = tileConfig();
  const points = (features || []).map(feature => ({ feature, point: repPoint(feature) })).filter(entry => entry.point);
  const clustered = shouldCluster(points.length);
  const clusters = clustered ? clusterGrid(features, MAP_DEFAULT_CELL_DEGREES) : [];

  useEffect(() => {
    let cancelled = false;
    let map = null;
    async function init() {
      if (!containerRef.current || tileFailed) return;
      const { default: maplibre } = await import('maplibre-gl');
      await import('maplibre-gl/dist/maplibre-gl.css');
      if (cancelled || !containerRef.current) return;
      const reduced = typeof window !== 'undefined' && window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
      try {
        map = new maplibre.Map({
          container: containerRef.current,
          style: config.url,
          center: [85, 28],
          zoom: 3,
          attributionControl: { compact: true },
          fadeDuration: reduced ? 0 : 300,
        });
        map.addControl(new maplibre.NavigationControl({ visualizePitch: false }), 'top-right');
        map.on('error', () => onTileFailed?.());
      } catch {
        onTileFailed?.();
      }
      mapRef.current = map;
    }
    init();
    return () => { cancelled = true; try { map?.remove(); } catch { /* noop */ } mapRef.current = null; };
  }, [tileFailed]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    const map = mapRef.current;
    if (!map || tileFailed) return;
    const markers = [];
    function render() {
      markers.forEach(marker => marker.remove());
      markers.length = 0;
      if (clustered) {
        import('maplibre-gl').then(({ default: maplibre }) => {
          for (const cluster of clusters.slice(0, 200)) {
            const element = document.createElement('div');
            element.className = 'cluster-marker';
            element.textContent = String(cluster.members.length);
            element.setAttribute('aria-hidden', 'true');
            markers.push(new maplibre.Marker({ element }).setLngLat([cluster.center.longitude, cluster.center.latitude]).addTo(map));
          }
        });
        return;
      }
      import('maplibre-gl').then(({ default: maplibre }) => {
        for (const { feature, point } of points.slice(0, 200)) {
          const element = document.createElement('div');
          element.className = 'feature-marker';
          element.setAttribute('aria-hidden', 'true');
          element.title = feature.title || feature.Title;
          markers.push(new maplibre.Marker({ element }).setLngLat([point.longitude, point.latitude]).addTo(map));
        }
      });
    }
    if (map.loaded()) render();
    else map.once('load', render);
    return () => markers.forEach(marker => marker.remove());
  });

  return (
    <div className="map-pane">
      {!tileFailed ? (
        <div ref={containerRef} className="maplibre-surface" role="application" aria-label={`Map of ${points.length} features. The accessible list below holds the same data.`} data-tile-url={config.url} />
      ) : (
        <p className="notice" role="status">Map tiles are unavailable ({config.url}). The accessible feature list below remains fully usable.</p>
      )}
      {clustered && !tileFailed ? (
        <p className="muted" role="status">{clusters.length} clusters group {points.length} features. Zoom the map or refine the year to separate them.</p>
      ) : null}
      {capped ? (
        <p className="muted" role="status">Showing the first 500 of {total} features. Refine the year or viewport to see more.</p>
      ) : null}
      <p className="muted tile-attribution">{config.attribution}</p>
    </div>
  );
}
