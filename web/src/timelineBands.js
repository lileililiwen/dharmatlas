const RANGE_SPLIT = /[–—-]|to/i;

export function parseYearToken(token, defaultBce = false) {
  if (!token) return null;
  const text = String(token).trim();
  const bce = /\bBCE\b/i.test(text) || defaultBce;
  const match = text.match(/-?\d{1,4}/);
  if (!match) return null;
  let year = Number(match[0]);
  if (bce) year = -Math.abs(year);
  return year;
}

export function parseDateBand(displayExpression, certainty) {
  const expression = displayExpression || 'Date not recorded';
  const approximate = /\b(c\.|ca\.|circa|approx)/i.test(expression);
  const traditional = String(certainty || '').toLowerCase() === 'traditional';
  const expressionBce = /\bBCE\b/i.test(String(expression));
  const parts = String(expression).split(RANGE_SPLIT).map(part => part.trim()).filter(Boolean);
  const years = parts.map(part => parseYearToken(part, expressionBce)).filter(value => value !== null);
  let lower = null;
  let upper = null;
  if (years.length >= 2) {
    lower = Math.min(years[0], years[1]);
    upper = Math.max(years[0], years[1]);
  } else if (years.length === 1) {
    lower = years[0];
    upper = years[0];
  }
  return {
    expression,
    lower,
    upper,
    approximate,
    traditional,
    isInterval: lower !== null && upper !== null && lower !== upper,
    isPoint: lower !== null && upper !== null && lower === upper,
    isUnknown: lower === null || upper === null,
  };
}

export function toBands(events) {
  return (events || []).map(event => {
    const band = parseDateBand(event.displayDate || event.DisplayDate, event.certainty || event.Certainty);
    return {
      id: event.id || event.Id,
      title: event.title || event.Title,
      category: event.category || event.Category || null,
      region: event.region || event.Region || null,
      certainty: event.certainty || event.Certainty || 'Unknown',
      detailRoute: event.detailRoute || event.DetailRoute,
      ...band,
    };
  });
}

export function laneKey(band) {
  return `${band.region || 'Region unknown'} / ${band.category || 'Uncategorized'}`;
}

export function filterBands(bands, { region = '', category = '' } = {}) {
  return bands.filter(band =>
    (!region || band.region === region) &&
    (!category || band.category === category));
}
