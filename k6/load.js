import http from 'k6/http';
import { check } from 'k6';

// Load gate: short CI run against staging, not a sustained soak.
// Thresholds enforce the SLOs in docs/slo.md.
export const options = {
  vus: 10,
  duration: '60s',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<300'],
  },
};

const BASE = __ENV.BASE_URL || 'http://localhost:18080';

export default function () {
  const read = http.get(`${BASE}/api/v1/meta`);
  check(read, { 'meta 200': (r) => r.status === 200 });

  const search = http.get(`${BASE}/api/v1/search?q=ashoka`);
  check(search, {
    'search 200': (r) => r.status === 200,
    'search p95 under 250ms': (r) => r.timings.duration < 250,
  });

  const timeline = http.get(`${BASE}/api/v1/timeline?fromYear=-500&toYear=1000`);
  check(timeline, { 'timeline 200': (r) => r.status === 200 });
}
