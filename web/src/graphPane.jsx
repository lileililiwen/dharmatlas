import { useEffect, useRef } from 'react';
import { GRAPH_MAX_DEPTH } from './graph.js';

export default function GraphPane({ centerId, nodes, edges, visibleEdges, hasMore, total, depth, onShowMore, onDepth, onSelect }) {
  const containerRef = useRef(null);

  useEffect(() => {
    let cancelled = false;
    let cy = null;
    async function init() {
      if (!containerRef.current) return;
      const { default: cytoscape } = await import('cytoscape');
      if (cancelled || !containerRef.current) return;
      const reduced = typeof window !== 'undefined' && window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
      cy = cytoscape({
        container: containerRef.current,
        elements: [
          ...nodes.map(node => ({ data: { id: node.id, label: node.name || node.id, center: Boolean(node.center) } })),
          ...visibleEdges.map(edge => ({ data: { id: edge.key, source: edge.fromId, target: edge.toId, label: `${edge.type} · ${edge.certainty}` } })),
        ],
        layout: { name: 'cose', animate: !reduced, animationDuration: reduced ? 0 : 400 },
        style: [
          { selector: 'node', style: { label: 'data(label)', 'font-size': 11, 'background-color': '#e3ebe5', 'border-color': '#145c43', 'border-width': 1, 'text-wrap': 'wrap', 'text-max-width': 120 } },
          { selector: 'node[center]', style: { 'background-color': '#17231e', color: '#fff', 'border-color': '#17231e' } },
          { selector: 'edge', style: { label: 'data(label)', 'font-size': 9, color: '#61716a', 'curve-style': 'bezier', 'target-arrow-shape': 'triangle', 'line-color': '#b4482d', 'target-arrow-color': '#b4482d' } },
        ],
      });
      cy.on('tap', 'node', event => {
        const id = event.target.id();
        const node = nodes.find(entry => entry.id === id);
        if (node && !node.center) onSelect?.(id);
      });
    }
    init();
    return () => { cancelled = true; try { cy?.destroy(); } catch { /* noop */ } };
  }, [centerId, nodes, visibleEdges]); // eslint-disable-line react-hooks/exhaustive-deps

  return (
    <div className="graph-pane">
      <div className="graph-controls" role="toolbar" aria-label="Relationship graph controls">
        <span className="muted">Center: {centerId}</span>
        <button type="button" onClick={() => onDepth?.(1)} aria-pressed={depth === 1}>1-hop</button>
        <button type="button" onClick={() => onDepth?.(GRAPH_MAX_DEPTH)} aria-pressed={depth === GRAPH_MAX_DEPTH}>2-hop</button>
      </div>
      <div ref={containerRef} className="cytoscape-surface" role="application"
        aria-label={`Relationship graph around ${centerId} with ${edges.length} edges. The accessible list below holds the same connections.`} />
      <p className="muted">
        Showing {visibleEdges.length} of {total} connections{hasMore ? ' (disputed relations stay as parallel edges).' : '.'}
      </p>
      {hasMore ? <button type="button" className="primary" onClick={() => onShowMore?.()}>Show more connections</button> : null}
    </div>
  );
}
