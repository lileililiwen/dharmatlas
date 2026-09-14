# Design: Public Web Productization

## Routing

React Router (or equivalent): `/`, `/timeline`, `/map`, `/persons/:id`, `/places/:id`, `/texts/:id`, `/institutions/:id`, `/traditions/:id`, `/events/:id`. `detailRoute` from API is the single source of truth; unknown IDs render a sourced 404 with search suggestion. All routes survive reload via host fallback.

## SEO / sharing

Prerendered meta per entity (title = canonical name + type, description = summary + certainty, canonical URL, OG tags) plus `/sitemap.xml`. Host serves prerender or SSR head injection without exposing drafts; unpublished IDs are absent from sitemap.

## I18n and PWA

`web/src/i18n/*.json` for chrome strings (en + stub zh/ja); fallback chain `requested → en`. Claim/source text never auto-translated. Service worker caches app shell + visited GETs (`CacheFirst` for shell, `StaleWhileRevalidate` for entity reads, TTL-bounded); writes and search never cached offline as authoritative.

## Onboarding and telemetry

First-visit orientation card + 3-step year tour, dismissible and keyboard accessible. Anonymous counters only: `page_view_by_type`, `search_no_hit_rate`, offline-hit reuse. No cookies, no IDs; schema test asserts PII-free payload.

## Verification

Router tests for deep-link parity; Lighthouse SEO/a11y thresholds; airplane-mode revisit test; i18n fallback test (missing key → en, never blank); telemetry schema test rejects PII fields.
