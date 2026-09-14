import React, { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { get } from './api.js';
import { displayDate, uncertaintyLabel } from './formatters.js';
import { applyFeatureCap } from './geo.js';
import { toBands, filterBands } from './timelineBands.js';
import { buildNeighborhood, paginateEdges, GRAPH_PAGE_SIZE, GRAPH_MAX_DEPTH } from './graph.js';
import MapPane from './mapPane.jsx';
import TimelinePane from './timelinePane.jsx';
import GraphPane from './graphPane.jsx';
import './styles.css';

const modes = ['overview', 'timeline', 'map', 'graph'];

function Badge({ children, tone = '' }) {
  return <span className={`badge ${tone}`}><span aria-hidden="true">·</span> {children}</span>;
}

function SourceList({ sources = [] }) {
  if (!sources.length) return <p className="muted">No public source is attached.</p>;
  return <ul className="source-list">{sources.map(source => <li key={source.id}>
    <strong>{source.title}</strong>{source.author ? <span> — {source.author}</span> : null}
    {source.identifier ? <small>{source.identifier}</small> : null}
  </li>)}</ul>;
}

function Search({ onSelect }) {
  const [term, setTerm] = useState('');
  const [state, setState] = useState({ status: 'idle', hits: [] });
  useEffect(() => {
    if (term.trim().length < 2) { setState({ status: 'idle', hits: [] }); return; }
    const controller = new AbortController();
    setState(current => ({ ...current, status: 'loading' }));
    const timer = setTimeout(() => get('search', { q: term.trim(), limit: 8 }, controller.signal)
      .then(data => setState({ status: 'ready', hits: data.hits || [] }))
      .catch(error => { if (error.name !== 'AbortError') setState({ status: 'error', hits: [], error: error.message }); }), 180);
    return () => { clearTimeout(timer); controller.abort(); };
  }, [term]);
  return <div className="search-wrap">
    <label htmlFor="global-search">Search names and aliases</label>
    <input id="global-search" value={term} onChange={event => setTerm(event.target.value)} placeholder="Try a person, place, text, or alias" autoComplete="off" />
    {state.status === 'loading' ? <p className="status" aria-live="polite">Searching…</p> : null}
    {state.status === 'error' ? <p className="error" role="alert">{state.error}</p> : null}
    {state.status === 'ready' && !state.hits.length ? <p className="status">No published match. Try another spelling or language.</p> : null}
    {state.hits.length ? <ul className="search-results" aria-label="Search results">{state.hits.map(hit => <li key={`${hit.type}-${hit.id}`}>
      <button type="button" onClick={() => onSelect(hit.detailRoute)}><span className="result-name">{hit.canonicalName}</span><span>{hit.matchedName} · {hit.type} · {uncertaintyLabel(hit.certainty)}</span></button>
    </li>)}</ul> : null}
  </div>;
}

function TimelineExplorer({ onSelect }) {
  const [year, setYear] = useState(500);
  const [view, setView] = useState([300, 700]);
  const [region, setRegion] = useState('');
  const [category, setCategory] = useState('');
  const [state, setState] = useState({ status: 'loading', data: null });
  useEffect(() => {
    const controller = new AbortController();
    setState({ status: 'loading', data: null });
    get('timeline', { fromYear: view[0], toYear: view[1], region: region || undefined, category: category || undefined, includeUnknownDates: true, limit: 100 }, controller.signal)
      .then(data => setState({ status: 'ready', data }))
      .catch(error => { if (error.name !== 'AbortError') setState({ status: 'error', data: null, error: error.message }); });
    return () => controller.abort();
  }, [view, region, category]); // eslint-disable-line react-hooks/exhaustive-deps
  const bands = toBands(state.data?.events || []);
  const visible = filterBands(bands, { region, category });
  function zoom(direction) {
    setView(([from, to]) => {
      const span = to - from;
      if (direction === 'reset') return [year - 200, year + 200];
      const next = direction === 'in' ? span / 2 : span * 2;
      const clamped = Math.min(2000, Math.max(20, next));
      const mid = (from + to) / 2;
      return [Math.round(mid - clamped / 2), Math.round(mid + clamped / 2)];
    });
  }
  return <section aria-labelledby="timeline-heading">
    <h3 id="timeline-heading">Uncertainty-honest timeline</h3>
    <label className="year-control" htmlFor="timeline-year">Reference year <output>{year < 0 ? `${Math.abs(year)} BCE` : `${year} CE`}</output></label>
    <input id="timeline-year" type="range" min="-500" max="1500" value={year} onChange={event => { const next = Number(event.target.value); setYear(next); setView([next - 200, next + 200]); }} />
    <div className="filter-row">
      <label>Region <input value={region} onChange={event => setRegion(event.target.value)} placeholder="e.g. Central Asia" /></label>
      <label>Category <input value={category} onChange={event => setCategory(event.target.value)} placeholder="e.g. Travel" /></label>
    </div>
    {state.status === 'loading' ? <div className="skeleton" aria-label="Loading timeline" /> : null}
    {state.status === 'error' ? <p className="error" role="alert">{state.error}. The source list remains available below.</p> : null}
    {state.status === 'ready' && !visible.length ? <p className="empty">No published events match this window. Try another year or clear filters.</p> : null}
    {state.status === 'ready' && visible.length ? <>
      <TimelinePane bands={visible} view={view} onZoom={zoom} onSelect={onSelect} />
      <ul className="feature-list" aria-label="Timeline results">{visible.map(band => <li key={band.id}>
        <a href={band.detailRoute} onClick={event => { event.preventDefault(); onSelect(band.detailRoute); }}>
          <strong>{band.title}</strong>
          <span>{band.expression}{band.approximate ? ' (approximate)' : ''}{band.traditional ? ' (traditional account)' : ''}</span>
          <Badge tone={band.certainty}>{uncertaintyLabel(band.certainty)}</Badge>
        </a>
      </li>)}</ul>
    </> : null}
  </section>;
}

function MapExplorer({ onSelect }) {
  const [year, setYear] = useState(650);
  const [includeUnknown, setIncludeUnknown] = useState(false);
  const [tileFailed, setTileFailed] = useState(false);
  const [state, setState] = useState({ status: 'loading', data: null });
  useEffect(() => {
    const controller = new AbortController();
    setState({ status: 'loading', data: null });
    get('map', { year, includeUnknownActivity: includeUnknown }, controller.signal)
      .then(data => setState({ status: 'ready', data }))
      .catch(error => { if (error.name !== 'AbortError') setState({ status: 'error', data: null, error: error.message }); });
    return () => controller.abort();
  }, [year, includeUnknown]);
  const features = state.data?.features || [];
  const { visible, capped, total } = applyFeatureCap(features);
  return <section aria-labelledby="map-heading">
    <h3 id="map-heading">Time-filtered map</h3>
    <label className="year-control" htmlFor="map-year">Reference year <output>{year < 0 ? `${Math.abs(year)} BCE` : `${year} CE`}</output></label>
    <input id="map-year" type="range" min="-500" max="1500" value={year} onChange={event => setYear(Number(event.target.value))} />
    <label className="check-row"><input type="checkbox" checked={includeUnknown} onChange={event => setIncludeUnknown(event.target.checked)} /> Include places with unknown activity (hatched, opt-in)</label>
    {state.status === 'loading' ? <div className="skeleton" aria-label="Loading map" /> : null}
    {state.status === 'error' ? <p className="error" role="alert">{state.error}. The source list remains available below.</p> : null}
    {state.status === 'ready' && !visible.length ? <p className="empty">No published places match this year. Try another year or opt in to unknown activity.</p> : null}
    {state.status === 'ready' && visible.length ? <div className="explore-grid">
      <MapPane features={visible} capped={capped} total={total} tileFailed={tileFailed} onTileFailed={() => setTileFailed(true)} />
      <ul className="feature-list" aria-label="Map results">{visible.map(item => <li key={item.id}>
        <a href={item.detailRoute} onClick={event => { event.preventDefault(); onSelect(item.detailRoute); }}>
          <strong>{item.title}</strong><span>{item.activityExpression}</span><Badge tone={item.certainty}>{uncertaintyLabel(item.certainty)}</Badge>
        </a>
      </li>)}</ul>
    </div> : null}
  </section>;
}

function GraphExplorer({ centerId, onCenter, onSelect }) {
  const [draft, setDraft] = useState(centerId || '');
  const [depth, setDepth] = useState(1);
  const [showAll, setShowAll] = useState(false);
  const [state, setState] = useState({ status: 'idle', relationships: [] });
  useEffect(() => { setDraft(centerId || ''); }, [centerId]);
  useEffect(() => {
    if (!centerId) return;
    const controller = new AbortController();
    setState({ status: 'loading', relationships: [] });
    async function load() {
      try {
        const [outgoing, incoming] = await Promise.all([
          get('relationships', { from: centerId, limit: 50 }, controller.signal),
          get('relationships', { to: centerId, limit: 50 }, controller.signal),
        ]);
        const merged = new Map();
        for (const rel of [...(outgoing.items || outgoing || []), ...(incoming.items || incoming || [])]) {
          const id = rel.id || rel.Id;
          if (id && !merged.has(id)) merged.set(id, rel);
        }
        let all = [...merged.values()];
        if (depth >= GRAPH_MAX_DEPTH) {
          const first = buildNeighborhood(centerId, all);
          const neighbors = first.nodes.filter(node => !node.center).slice(0, 5);
          for (const neighbor of neighbors) {
            const [a, b] = await Promise.all([
              get('relationships', { from: neighbor.id, limit: 20 }, controller.signal).catch(() => ({ items: [] })),
              get('relationships', { to: neighbor.id, limit: 20 }, controller.signal).catch(() => ({ items: [] })),
            ]);
            for (const rel of [...(a.items || a || []), ...(b.items || b || [])]) {
              const id = rel.id || rel.Id;
              if (id && !merged.has(id)) merged.set(id, rel);
            }
          }
          all = [...merged.values()];
        }
        setState({ status: 'ready', relationships: all });
      } catch (error) {
        if (error.name !== 'AbortError') setState({ status: 'error', relationships: [], error: error.message });
      }
    }
    load();
    return () => controller.abort();
  }, [centerId, depth]);
  const neighborhood = buildNeighborhood(centerId, state.relationships);
  const page = paginateEdges(neighborhood.edges, { pageSize: GRAPH_PAGE_SIZE, showAll });
  return <section aria-labelledby="graph-heading">
    <h3 id="graph-heading">Relationship graph</h3>
    <form className="filter-row" onSubmit={event => { event.preventDefault(); onCenter(draft.trim()); }}>
      <label>Center entity id <input value={draft} onChange={event => setDraft(event.target.value)} placeholder="Paste an entity id" /></label>
      <button type="submit" className="primary">Load neighborhood</button>
    </form>
    {!centerId ? <p className="empty">Enter an entity id, or open one from search or a detail page.</p> : null}
    {state.status === 'loading' ? <div className="skeleton" aria-label="Loading graph" /> : null}
    {state.status === 'error' ? <p className="error" role="alert">{state.error}. The list below remains available when data loads.</p> : null}
    {state.status === 'ready' && !neighborhood.edges.length ? <p className="empty">No published relationships connect this entity yet.</p> : null}
    {state.status === 'ready' && neighborhood.edges.length ? <>
      <GraphPane centerId={centerId} nodes={neighborhood.nodes} edges={neighborhood.edges}
        visibleEdges={page.visible} hasMore={page.hasMore} total={page.total} depth={depth}
        onShowMore={() => setShowAll(true)} onDepth={next => { setDepth(next); setShowAll(false); }}
        onSelect={id => {
          const edge = neighborhood.edges.find(entry => entry.fromId === id || entry.toId === id);
          const route = id.includes('/') ? id : `/entities/${id}`;
          onSelect(edge && (edge.fromId === id ? edge.fromName : edge.toName) ? route : route);
        }} />
      <ul className="feature-list" aria-label="Relationship results">{page.visible.map(edge => {
        const otherId = edge.fromId === centerId ? edge.toId : edge.fromId;
        const otherName = edge.fromId === centerId ? edge.toName : edge.fromName;
        return <li key={edge.key}>
          <strong>{otherName}</strong>
          <span>{edge.type} · {uncertaintyLabel(edge.certainty)}</span>
          <span className="muted">{edge.fromId} → {edge.toId} ({otherId})</span>
        </li>;
      })}</ul>
    </> : null}
  </section>;
}

function Explorer({ mode, onMode, onSelect, graphCenter, onGraphCenter }) {
  return <section className="explorer" aria-labelledby="explore-heading">
    <div className="section-header"><div><p className="kicker">Explore context</p><h2 id="explore-heading">Time, place, and connections</h2></div><div className="mode-tabs" role="tablist" aria-label="Explore view">
      {modes.map(item => <button key={item} role="tab" aria-selected={mode === item} className={mode === item ? 'selected' : ''} onClick={() => onMode(item)}>{item}</button>)}
    </div></div>
    {mode === 'overview' ? <p className="lead">Choose timeline, map, or graph. Every visual pane ships with an identical accessible list.</p> : null}
    {mode === 'timeline' ? <TimelineExplorer onSelect={onSelect} /> : null}
    {mode === 'map' ? <MapExplorer onSelect={onSelect} /> : null}
    {mode === 'graph' ? <GraphExplorer centerId={graphCenter} onCenter={onGraphCenter} onSelect={onSelect} /> : null}
  </section>;
}

function Detail({ route, onBack, onGraph }) {
  const [state, setState] = useState({ status: 'loading', data: null });
  useEffect(() => { const controller = new AbortController(); get(route.replace(/^\//, '').replace(/^api\/v1\//, ''), {}, controller.signal).then(data => setState({ status: 'ready', data })).catch(error => { if (error.name !== 'AbortError') setState({ status: 'error', error: error.message }); }); return () => controller.abort(); }, [route]);
  if (state.status === 'loading') return <main className="content"><div className="skeleton" aria-label="Loading entity" /></main>;
  if (state.status === 'error') return <main className="content"><p className="error" role="alert">{state.error}</p><button className="text-button" onClick={onBack}>Return to exploration</button></main>;
  const entity = state.data;
  const entityId = entity.id || route.split('/').pop();
  return <main className="content detail"><button className="text-button" onClick={onBack}>← Back to exploration</button><p className="kicker">{entity.type}</p><h1>{entity.canonicalName}</h1><div className="badge-row"><Badge tone={entity.certainty}>{uncertaintyLabel(entity.certainty)}</Badge>{entity.region ? <Badge>{entity.region}</Badge> : null}</div>{entity.summary ? <p className="lead">{entity.summary}</p> : null}<button className="primary" onClick={() => onGraph(entityId)}>Open relationship graph</button><section><h2>Names</h2><ul className="name-list">{(entity.names || []).map(name => <li key={`${name.value}-${name.language}`}><strong>{name.value}</strong><span>{name.language} · {name.script}{name.isPrimary ? ' · primary' : ''}</span></li>)}</ul></section><section><h2>Public evidence</h2>{(entity.claims || []).length ? <ul className="claim-list">{entity.claims.map(claim => <li key={claim.id}><p>{claim.statement}</p><Badge tone={claim.interpretation}>{claim.interpretation}</Badge>{claim.sourceLocator ? <small>Locator: {claim.sourceLocator}</small> : null}<SourceList sources={claim.sources} /></li>)}</ul> : <p className="muted">No published claims with resolvable sources.</p>}</section><section><h2>Sources</h2><SourceList sources={entity.sources} /></section></main>;
}

function App() {
  const [mode, setMode] = useState('overview'); const [detailRoute, setDetailRoute] = useState(null);
  const [graphCenter, setGraphCenter] = useState(null);
  function openGraph(centerId) { setGraphCenter(centerId); setDetailRoute(null); setMode('graph'); }
  if (detailRoute) return <><Header onSearch={() => setDetailRoute(null)} /><Detail route={detailRoute} onBack={() => setDetailRoute(null)} onGraph={openGraph} /></>;
  return <><Header onSearch={() => document.getElementById('global-search')?.focus()} /><main><section className="hero"><div><p className="kicker">An open historical atlas of Buddhism</p><h1>Follow people, places, and texts across time.</h1><p className="lead">Source-first history with uncertainty left visible.</p></div><div className="hero-rail"><span>500 BCE</span><div className="hero-line" /><span>1000 CE</span></div></section><section className="search-panel"><Search onSelect={setDetailRoute} /></section><Explorer mode={mode} onMode={setMode} onSelect={setDetailRoute} graphCenter={graphCenter} onGraphCenter={setGraphCenter} /><nav className="view-links" aria-label="Explore sections">{modes.filter(item => item !== 'overview').map(item => <button key={item} onClick={() => setMode(item)}>{item === 'timeline' ? 'Timeline' : item === 'map' ? 'Map and list' : 'Graph'} <span aria-hidden="true">↗</span></button>)}</nav></main><footer><span>Dharmatlas</span><a href="/api/v1/meta">Public API</a><span>Source-first · non-sectarian · open</span></footer></>;
}

function Header({ onSearch }) { return <header><a className="wordmark" href="/" onClick={event => { event.preventDefault(); window.location.reload(); }}>DHARMATLAS</a><button className="header-search" onClick={onSearch}>Search the atlas <span aria-hidden="true">/</span></button><span className="header-note">History with its uncertainties intact</span></header>; }

createRoot(document.getElementById('root')).render(<App />);
