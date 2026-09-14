import { useMemo, useRef, useState } from 'react';
import { laneKey } from './timelineBands.js';

const WIDTH = 720;
const ROW_HEIGHT = 30;

export default function TimelinePane({ bands, view, onZoom, onSelect }) {
  const svgRef = useRef(null);
  const [focusIndex, setFocusIndex] = useState(0);
  const lanes = useMemo(() => {
    const ordered = [...bands].sort((a, b) => (a.lower ?? 0) - (b.lower ?? 0));
    const laneNames = [...new Set(ordered.map(laneKey))];
    return { ordered, laneNames };
  }, [bands]);

  const [from, to] = view;
  const span = Math.max(1, to - from);
  const x = value => ((Math.min(Math.max(value, from), to) - from) / span) * (WIDTH - 120) + 100;

  function onKeyDown(event) {
    if (event.key === 'ArrowRight' || event.key === 'ArrowLeft') {
      event.preventDefault();
      const delta = event.key === 'ArrowRight' ? 1 : -1;
      setFocusIndex(index => {
        const next = Math.min(Math.max(0, index + delta), Math.max(0, lanes.ordered.length - 1));
        svgRef.current?.querySelector(`[data-band-index="${next}"]`)?.focus();
        return next;
      });
    }
    if (event.key === '+' || event.key === '=') { event.preventDefault(); onZoom?.('in'); }
    if (event.key === '-' || event.key === '_') { event.preventDefault(); onZoom?.('out'); }
  }

  return (
    <div className="timeline-pane">
      <div className="timeline-controls" role="toolbar" aria-label="Timeline zoom">
        <button type="button" onClick={() => onZoom?.('out')} aria-label="Zoom timeline out">−</button>
        <button type="button" onClick={() => onZoom?.('in')} aria-label="Zoom timeline in">+</button>
        <button type="button" onClick={() => onZoom?.('reset')} aria-label="Reset timeline zoom">Reset</button>
        <span className="muted">{from < 0 ? `${Math.abs(from)} BCE` : `${from} CE`} – {to < 0 ? `${Math.abs(to)} BCE` : `${to} CE`}</span>
      </div>
      <svg ref={svgRef} viewBox={`0 0 ${WIDTH} ${Math.max(120, lanes.laneNames.length * ROW_HEIGHT + 40)}`} role="img"
        aria-label={`Timeline with ${bands.length} events as interval bands. Use arrow keys to move between events.`}
        tabIndex={0} onKeyDown={onKeyDown} className="timeline-svg">
        {lanes.laneNames.map((lane, laneIndex) => (
          <g key={lane}>
            <text x={4} y={laneIndex * ROW_HEIGHT + 24} className="lane-label">{lane}</text>
            <line x1={100} x2={WIDTH - 20} y1={laneIndex * ROW_HEIGHT + 28} y2={laneIndex * ROW_HEIGHT + 28} className="lane-line" />
          </g>
        ))}
        {lanes.ordered.map((band, index) => {
          const laneIndex = Math.max(0, lanes.laneNames.indexOf(laneKey(band)));
          const y = laneIndex * ROW_HEIGHT + 14;
          if (band.isUnknown) {
            return (
              <g key={band.id} transform={`translate(${WIDTH - 60},${y})`}>
                <text className="unknown-glyph" aria-hidden="true">?</text>
                <title>{band.title}: date unknown</title>
              </g>
            );
          }
          const x1 = x(band.lower);
          const x2 = x(band.upper);
          const isBand = band.isInterval;
          const rectX = isBand ? Math.min(x1, x2) : x1 - 2;
          const rectWidth = isBand ? Math.max(8, Math.abs(x2 - x1)) : 4;
          return (
            <g key={band.id}>
              <rect x={rectX} y={y} width={rectWidth} height={12} rx={2}
                className={`band ${band.approximate ? 'approximate' : 'exact'} certainty-${band.certainty}`}
                tabIndex={-1} data-band-index={index}
                onClick={() => onSelect?.(band.detailRoute)}
                onKeyDown={event => { if (event.key === 'Enter' && band.detailRoute) onSelect?.(band.detailRoute); }}>
                <title>{`${band.title}: ${band.expression}${band.approximate ? ' (approximate)' : ''}${band.traditional ? ' (traditional account)' : ''}`}</title>
              </rect>
              {band.approximate ? <text x={rectX} y={y - 3} className="ca-label" aria-hidden="true">ca.</text> : null}
              {band.traditional ? <text x={rectX + rectWidth + 3} y={y + 10} className="tradition-glyph" aria-hidden="true">◈<title>Traditional account</title></text> : null}
            </g>
          );
        })}
      </svg>
    </div>
  );
}
