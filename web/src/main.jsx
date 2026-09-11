import React, { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { get } from './api.js';
import { displayDate, uncertaintyLabel } from './formatters.js';
import './styles.css';

const modes = ['overview', 'timeline', 'map'];

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

function Explorer({ mode, onMode, onSelect }) {
  const [year, setYear] = useState(500);
  const [state, setState] = useState({ status: 'loading', data: null });
  useEffect(() => {
    const controller = new AbortController(); setState({ status: 'loading', data: null });
    const path = mode === 'timeline' ? 'timeline' : 'map';
    const params = mode === 'timeline' ? { fromYear: year - 200, toYear: year + 200, includeUnknownDates: true, limit: 20 } : { year, includeUnknownActivity: true };
    get(path, params, controller.signal).then(data => setState({ status: 'ready', data })).catch(error => { if (error.name !== 'AbortError') setState({ status: 'error', data: null, error: error.message }); });
    return () => controller.abort();
  }, [mode, year]);
  const items = mode === 'timeline' ? state.data?.events || [] : state.data?.features || [];
  return <section className="explorer" aria-labelledby="explore-heading">
    <div className="section-header"><div><p className="kicker">Explore context</p><h2 id="explore-heading">Time and place, held together</h2></div><div className="mode-tabs" role="tablist" aria-label="Explore view">
      {modes.map(item => <button key={item} role="tab" aria-selected={mode === item} className={mode === item ? 'selected' : ''} onClick={() => onMode(item)}>{item}</button>)}
    </div></div>
    <label className="year-control" htmlFor="explore-year">Reference year <output>{year < 0 ? `${Math.abs(year)} BCE` : `${year} CE`}</output></label>
    <input id="explore-year" type="range" min="-500" max="1500" value={year} onChange={event => setYear(Number(event.target.value))} />
    {state.status === 'loading' ? <div className="skeleton" aria-label="Loading exploration" /> : null}
    {state.status === 'error' ? <p className="error" role="alert">{state.error}. The source list remains available below.</p> : null}
    {state.status === 'ready' && !items.length ? <p className="empty">No published {mode === 'timeline' ? 'events' : 'places'} match this year. Try another year.</p> : null}
    {state.status === 'ready' && items.length ? <div className="explore-grid"><div className="map-surface" aria-label="Map visualization with accessible list fallback"><span className="map-note">Map view is paired with the accessible feature list.</span>{mode === 'map' ? <div className="map-dots" aria-hidden="true">{items.slice(0, 12).map((item, index) => <i key={item.id} style={{ left: `${12 + (index * 17) % 75}%`, top: `${18 + (index * 29) % 62}%` }} />)}</div> : <div className="timeline-line" aria-hidden="true" />}</div><ul className="feature-list" aria-label={`${mode} results`}>{items.map(item => <li key={item.id}><a href={item.detailRoute} onClick={event => { event.preventDefault(); onSelect(item.detailRoute); }}><strong>{item.title}</strong><span>{mode === 'timeline' ? displayDate({ displayExpression: item.displayDate }) : item.activityExpression}</span><Badge tone={item.certainty}>{uncertaintyLabel(item.certainty)}</Badge></a></li>)}</ul></div> : null}
  </section>;
}

function Detail({ route, onBack }) {
  const [state, setState] = useState({ status: 'loading', data: null });
  useEffect(() => { const controller = new AbortController(); get(route.replace(/^\//, '').replace(/^api\/v1\//, ''), {}, controller.signal).then(data => setState({ status: 'ready', data })).catch(error => { if (error.name !== 'AbortError') setState({ status: 'error', error: error.message }); }); return () => controller.abort(); }, [route]);
  if (state.status === 'loading') return <main className="content"><div className="skeleton" aria-label="Loading entity" /></main>;
  if (state.status === 'error') return <main className="content"><p className="error" role="alert">{state.error}</p><button className="text-button" onClick={onBack}>Return to exploration</button></main>;
  const entity = state.data; return <main className="content detail"><button className="text-button" onClick={onBack}>← Back to exploration</button><p className="kicker">{entity.type}</p><h1>{entity.canonicalName}</h1><div className="badge-row"><Badge tone={entity.certainty}>{uncertaintyLabel(entity.certainty)}</Badge>{entity.region ? <Badge>{entity.region}</Badge> : null}</div>{entity.summary ? <p className="lead">{entity.summary}</p> : null}<section><h2>Names</h2><ul className="name-list">{(entity.names || []).map(name => <li key={`${name.value}-${name.language}`}><strong>{name.value}</strong><span>{name.language} · {name.script}{name.isPrimary ? ' · primary' : ''}</span></li>)}</ul></section><section><h2>Public evidence</h2>{(entity.claims || []).length ? <ul className="claim-list">{entity.claims.map(claim => <li key={claim.id}><p>{claim.statement}</p><Badge tone={claim.interpretation}>{claim.interpretation}</Badge>{claim.sourceLocator ? <small>Locator: {claim.sourceLocator}</small> : null}<SourceList sources={claim.sources} /></li>)}</ul> : <p className="muted">No published claims with resolvable sources.</p>}</section><section><h2>Sources</h2><SourceList sources={entity.sources} /></section></main>;
}

function App() {
  const [mode, setMode] = useState('overview'); const [detailRoute, setDetailRoute] = useState(null);
  if (detailRoute) return <><Header onSearch={() => setDetailRoute(null)} /><Detail route={detailRoute} onBack={() => setDetailRoute(null)} /></>;
  return <><Header onSearch={() => document.getElementById('global-search')?.focus()} /><main><section className="hero"><div><p className="kicker">An open historical atlas of Buddhism</p><h1>Follow people, places, and texts across time.</h1><p className="lead">Source-first history with uncertainty left visible.</p></div><div className="hero-rail"><span>500 BCE</span><div className="hero-line" /><span>1000 CE</span></div></section><section className="search-panel"><Search onSelect={setDetailRoute} /></section>{mode !== 'overview' ? <Explorer mode={mode} onMode={setMode} onSelect={setDetailRoute} /> : <section className="orientation"><div><p className="kicker">How to read this atlas</p><h2>Evidence stays attached to the story.</h2></div><p>Dates may be approximate or interval-based. Traditional accounts and documented history are labeled separately. Open a result to inspect the names, claims, and sources behind it.</p><button className="primary" onClick={() => setMode('timeline')}>Explore the timeline</button></section>}<nav className="view-links" aria-label="Explore sections">{modes.filter(item => item !== 'overview').map(item => <button key={item} onClick={() => setMode(item)}>{item === 'timeline' ? 'Timeline' : 'Map and list'} <span aria-hidden="true">↗</span></button>)}</nav></main><footer><span>Dharmatlas</span><a href="/api/v1/meta">Public API</a><span>Source-first · non-sectarian · open</span></footer></>;
}

function Header({ onSearch }) { return <header><a className="wordmark" href="/" onClick={event => { event.preventDefault(); window.location.reload(); }}>DHARMATLAS</a><button className="header-search" onClick={onSearch}>Search the atlas <span aria-hidden="true">/</span></button><span className="header-note">History with its uncertainties intact</span></header>; }

createRoot(document.getElementById('root')).render(<App />);
