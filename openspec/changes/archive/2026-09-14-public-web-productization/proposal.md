# Proposal: Public Web Productization

## What Changes

Turn the single-file SPA into a shareable, indexable, multilingual product: client routes with deep links (`/persons/{id}` etc.), SSR or prerendered meta for SEO, EN + fallback i18n shell (zh/ja/sa display names, UI strings), PWA offline for visited entities, onboarding/empty-state curation, and privacy-respecting usage telemetry behind an explicit notice.

## Why

Without deep links, SEO, translations, and offline, the atlas cannot be shared, found, or used in classrooms and field conditions. Current `main.jsx` reloads to lose state and has no indexable content.

## Scope (Boundary)

In scope: router + `detailRoute` parity with API, meta/OG tags per entity, sitemap, i18n string catalog with English fallback, PWA service worker caching visited reads only, first-visit orientation + guided year tour, anonymous aggregate counts (page views per type, search-no-hit rate) with opt-out note.
Out of scope: user accounts in the client, comments/social, push notifications, advertising, full UI translation of scholarly content (names/claims stay authored).

## Use cases

- UC1: A teacher shares `/places/bodh-gaya` and the link opens the same entity with evidence intact.
- UC2: A search engine indexes entity pages with titles, descriptions, and canonical URLs.
- UC3: A visitor with intermittent connectivity reopens a visited place offline and sees its cached claims/sources.

## Non-goals / Exceptions

- No tracking cookies or fingerprinting; no per-user history leaves the device.
- No machine-translated historical claims presented as authoritative; UI chrome may translate, claim text does not auto-translate.
- No app-store release; PWA install prompt only.
- No breaking of accessible list fallbacks; every visual has a text equivalent.

## Dependencies

Builds on `public-atlas-web-experience`, `public-read-api-completion`, `provenance-claims-and-evidence`. Needs corpus and search fidelity for full value but ships independently.

## Success criteria

Deep links resolve and survive reload/share; Lighthouse SEO + a11y gates pass; offline revisit works airplane-mode; i18n fallback never blanks; telemetry contains zero PII by schema test.
