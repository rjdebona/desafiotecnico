import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  vus: 10,
  duration: '30s',
};

export function setup() {
  const authBase = __ENV.AUTH_BASE || 'http://host.docker.internal:5080';
  const username = __ENV.ADMIN_USER || 'admin';
  const password = __ENV.ADMIN_PASS || 'password';
  const payload = JSON.stringify({ Username: username, Password: password });
  const res = http.post(`${authBase}/auth/token`, payload, { headers: { 'Content-Type': 'application/json' } });
  if (res.status !== 200) {
    console.error('Auth token request failed', res.status, res.body);
    return { token: null };
  }
  const body = res.body || '{}';
  let token = null;
  try { token = JSON.parse(body).access_token; } catch (e) { token = null }
  return { token };
}

export default function (data) {
  const base = __ENV.CONSOLIDACAO_BASE || 'http://host.docker.internal:5260';
  const date = __ENV.DATA || new Date().toISOString().substring(0,10);
  const url = `${base}/api/SaldoDiario?data=${date}`;
  const token = data?.token || __ENV.ACCESS_TOKEN || null;
  const params = token ? { headers: { Authorization: `Bearer ${token}`, Accept: 'application/json' } } : { headers: { Accept: 'application/json' } };
  const res = http.get(url, params);
  check(res, { 'status 200': (r) => r.status === 200 });
  sleep(0.2);
}
