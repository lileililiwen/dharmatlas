export function uncertaintyLabel(value) {
  const labels = {
    Certain: 'documented',
    Probable: 'probable',
    Possible: 'possible',
    Disputed: 'disputed',
    Traditional: 'traditional account',
    Unknown: 'date or evidence unknown'
  };
  return labels[value] ?? value ?? 'uncertainty not recorded';
}

export function displayDate(date) {
  if (!date) return 'Date not recorded';
  return date.displayExpression || 'Date not recorded';
}

export function apiUrl(path, params = {}) {
  const query = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') query.set(key, value);
  });
  const suffix = query.toString();
  return `/api/v1/${path}${suffix ? `?${suffix}` : ''}`;
}
