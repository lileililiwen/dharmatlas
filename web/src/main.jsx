import React, { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { get } from './api.js';
import { displayDate, uncertaintyLabel } from './formatters.js';
import { applyFeatureCap } from './geo.js';
import { toBands, filterBands } from './timelineBands.js';
import { buildNeighborhood, paginateEdges, GRAPH_PAGE_SIZE, GRAPH_MAX_DEPTH } from './graph.js';
import { parseRoute, buildRoute, detailRouteToApi } from './routes.js';
import { t, resolveLocale, supportedLocales } from './i18n.js';
import { buildMeta, updateDocumentMeta } from './meta.js';
import { saveVisit, loadVisit } from './offline.js';
import { TOUR_STEPS, nextTourStep } from './onboarding.js';
import { recordLocal } from './telemetry.js';
import MapPane from './mapPane.jsx';
import TimelinePane from './timelinePane.jsx';
import GraphPane from './graphPane.jsx';
import './styles.css';

const modes = ['overview', 'timeline', 'map', 'graph'];

function navigate(path) {
  window.history.pushState({}, '', path);
  window.dispatchEvent(new PopStateEvent('popstate'));
}

function telemetryEnabled() {
  try {
    return localStorage.getItem('dharmatlas.telemetry.optout') !== '1';
  } catch {
    return true;
  }
}

function count(kind, fields) {
  if (!telemetryEnabled()) return;
  try {
    recordLocal(kind, fields);
  } catch {
    // Telemetry never breaks the surface.
  }
}

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

function Search({ onSelect, locale }) {
  const [term, setTerm] = useState('');
  const [state, setState] = useState({ status: 'idle', hits: [] });
  useEffect(() => {
    if (term.trim().length < 2) { setState({ status: 'idle', hits: [] }); return; }
    const controller = new AbortController();
    setState(current => ({ ...current, status: 'loading' }));
    const timer = setTimeout(() => get('search', { q: term.trim(), limit: 8 }, controller.signal)
      .then(data => {
        setState({ status: 'ready', hits: data.hits || [] });
        if (!(data.hits || []).length) count('search_no_hit', {});
      })
      .catch(error => { if (error.name !== 'AbortError') setState({ status: 'error', hits: [], error: error.message }); }), 180);
    return () => { clearTimeout(timer); controller.abort(); };
  }, [term]);
  return <div className="search-wrap">
    <label htmlFor="global-search">{t(locale, 'search.label')}</label>
    <input id="global-search" value={term} onChange={event => setTerm(event.target.value)} placeholder={t(locale, 'search.placeholder')} autoComplete="off" />
    {state.status === 'loading' ? <p className="status" aria-live="polite">Searching…</p> : null}
    {state.status === 'error' ? <p className="error" role="alert">{state.error}</p> : null}
    {state.status === 'ready' && !state.hits.length ? <p className="status">{t(locale, 'search.noHit')}</p> : null}
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

function Onboarding({ locale, step, onStep, onDismiss }) {
  return <section className="notice" aria-labelledby="onboarding-title">
    <h2 id="onboarding-title">{t(locale, 'onboarding.title')}</h2>
    <p>{t(locale, 'onboarding.body')}</p>
    <ol>{TOUR_STEPS.map((tStep, index) => <li key={tStep.id} aria-current={step === index ? 'step' : undefined}>{t(locale, tStep.textKey)}</li>)}</ol>
    <div className="filter-row">
      <button type="button" className="primary" onClick={() => { const next = nextTourStep(step); if (next === null) onDismiss(); else onStep(next); }}>{step === null ? t(locale, 'onboarding.dismiss') : 'Next step'}</button>
      <button type="button" className="text-button" onClick={onDismiss}>{t(locale, 'onboarding.dismiss')}</button>
    </div>
    <p className="muted">{t(locale, 'telemetry.note')}</p>
  </section>;
}

function NotFound({ locale, onHome }) {
  return <main className="content"><p className="kicker">404</p><h1>{t(locale, 'notfound.title')}</h1><p className="lead">{t(locale, 'notfound.body')}</p><button className="primary" onClick={onHome}>{t(locale, 'detail.back')}</button></main>;
}

function Detail({ route, locale, onBack, onGraph }) {
  const [state, setState] = useState({ status: 'loading', data: null, offline: false });
  useEffect(() => {
    const controller = new AbortController();
    const apiPath = detailRouteToApi(route);
    get(apiPath, {}, controller.signal)
      .then(data => {
        setState({ status: 'ready', data, offline: false });
        saveVisit(route, data);
        updateDocumentMeta(buildMeta(data, route, window.location.origin));
        count('page_view_by_type', { entityType: data.type || 'Unknown' });
      })
      .catch(error => {
        if (error.name === 'AbortError') return;
        const cached = loadVisit(route);
        if (cached.status === 'stale-ok' || cached.status === 'fresh') {
          setState({ status: 'ready', data: cached.data, offline: true });
          updateDocumentMeta(buildMeta(cached.data, route, window.location.origin));
          count('offline_reuse', { entityType: cached.data?.type || 'Unknown' });
        } else {
          setState({ status: 'error', error: error.message });
        }
      });
    return () => controller.abort();
  }, [route]);
  if (state.status === 'loading') return <main className="content"><div className="skeleton" aria-label="Loading entity" /></main>;
  if (state.status === 'error') return <main className="content"><p className="kicker">404</p><h1>{t(locale, 'notfound.title')}</h1><p className="error" role="alert">{state.error}</p><p className="lead">{t(locale, 'notfound.body')}</p><button className="text-button" onClick={onBack}>{t(locale, 'detail.back')}</button></main>;
  const entity = state.data;
  const entityId = entity.id || route.split('/').pop();
  return <main className="content detail">
    <button className="text-button" onClick={onBack}>← {t(locale, 'detail.back')}</button>
    {state.offline ? <p className="notice" role="status">{t(locale, 'offline.stale')}</p> : null}
    <p className="kicker">{entity.type}</p><h1>{entity.canonicalName}</h1>
    <div className="badge-row"><Badge tone={entity.certainty}>{uncertaintyLabel(entity.certainty)}</Badge>{entity.region ? <Badge>{entity.region}</Badge> : null}</div>
    {entity.summary ? <p className="lead">{entity.summary}</p> : null}
    <button className="primary" onClick={() => onGraph(entityId)}>Open relationship graph</button>
    <section><h2>Names</h2><ul className="name-list">{(entity.names || []).map(name => <li key={`${name.value}-${name.language}`}><strong>{name.value}</strong><span>{name.language} · {name.script}{name.isPrimary ? ' · primary' : ''}</span></li>)}</ul></section>
    <section><h2>{t(locale, 'detail.evidence')}</h2>{(entity.claims || []).length ? <ul className="claim-list">{entity.claims.map(claim => <li key={claim.id}><p>{claim.statement}</p><Badge tone={claim.interpretation}>{claim.interpretation}</Badge>{claim.sourceLocator ? <small>Locator: {claim.sourceLocator}</small> : null}<SourceList sources={claim.sources} /></li>)}</ul> : <p className="muted">{t(locale, 'detail.noClaims')}</p>}</section>
    <section><h2>{t(locale, 'detail.sources')}</h2><SourceList sources={entity.sources} /></section>
  </main>;
}

function App() {
  const [path, setPath] = useState(() => window.location.pathname);
  const [graphCenter, setGraphCenter] = useState(null);
  const [locale, setLocale] = useState(() => resolveLocale(typeof navigator !== 'undefined' ? navigator.language : 'en'));
  const [showOnboarding, setShowOnboarding] = useState(() => {
    try {
      return localStorage.getItem('dharmatlas.onboarded') !== '1';
    } catch {
      return true;
    }
  });
  const [tourStep, setTourStep] = useState(0);
  useEffect(() => {
    const onPop = () => setPath(window.location.pathname);
    window.addEventListener('popstate', onPop);
    if ('serviceWorker' in navigator) {
      navigator.serviceWorker.register('/sw.js').catch(() => {});
    }
    return () => window.removeEventListener('popstate', onPop);
  }, []);
  function go(next) {
    navigate(next);
    setPath(next);
    window.scrollTo(0, 0);
  }
  function dismissOnboarding() {
    setShowOnboarding(false);
    try {
      localStorage.setItem('dharmatlas.onboarded', '1');
    } catch {
      // Private mode: onboarding reappears, product still works.
    }
  }
  const parsed = parseRoute(path);
  function openGraph(centerId) {
    setGraphCenter(centerId);
    go('/graph');
  }
  const header = <Header locale={locale} onLocale={setLocale} onSearch={() => go('/')} onHome={() => go('/')} />;
  if (parsed.kind === 'entity') {
    return <>{header}{showOnboarding ? <main><Onboarding locale={locale} step={tourStep} onStep={setTourStep} onDismiss={dismissOnboarding} /></main> : null}<Detail route={parsed.path} locale={locale} onBack={() => go('/')} onGraph={openGraph} /><Footer locale={locale} /></>;
  }
  if (parsed.kind === 'notfound') {
    return <>{header}<NotFound locale={locale} onHome={() => go('/')} /><Footer locale={locale} /></>;
  }
  const mode = parsed.kind === 'home' ? 'overview' : parsed.kind;
  function onMode(next) {
    go(next === 'overview' ? '/' : `/${next}`);
  }
  return <>{header}<main>
    <section className="hero"><div><p className="kicker">An open historical atlas of Buddhism</p><h1>Follow people, places, and texts across time.</h1><p className="lead">{t(locale, 'app.lede')}</p></div><div className="hero-rail"><span>500 BCE</span><div className="hero-line" /><span>1000 CE</span></div></section>
    {showOnboarding ? <Onboarding locale={locale} step={tourStep} onStep={setTourStep} onDismiss={dismissOnboarding} /> : null}
    <section className="search-panel"><Search onSelect={go} locale={locale} /></section>
    <Explorer mode={mode} onMode={onMode} onSelect={go} graphCenter={graphCenter} onGraphCenter={setGraphCenter} />
    <nav className="view-links" aria-label="Explore sections">{modes.filter(item => item !== 'overview').map(item => <button key={item} onClick={() => onMode(item)}>{item === 'timeline' ? t(locale, 'nav.timeline') : item === 'map' ? t(locale, 'nav.map') : t(locale, 'nav.graph')} <span aria-hidden="true">↗</span></button>)}</nav>
  </main><Footer locale={locale} /></>;
}

function Header({ locale, onLocale, onSearch, onHome }) {
  return <header><a className="wordmark" href="/" onClick={event => { event.preventDefault(); onHome(); }}>DHARMATLAS</a><button className="header-search" onClick={onSearch}>{t(locale, 'search.label')} <span aria-hidden="true">/</span></button><label className="muted">Language <select value={locale} onChange={event => onLocale(resolveLocale(event.target.value))} aria-label="Language">{supportedLocales().map(code => <option key={code} value={code}>{code}</option>)}</select></label><span className="header-note">{t(locale, 'app.tagline')}</span></header>;
}

function Footer({ locale }) {
  return <footer><span>Dharmatlas</span><a href="/api/v1/meta">Public API</a><a href="/sitemap.xml">Sitemap</a><span>{t(locale, 'telemetry.note')}</span></footer>;
}

createRoot(document.getElementById('root')).render(<App />);
export { buildRoute };
