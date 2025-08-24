import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  vus: 5,
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

const LANCAMENTO_BASE = __ENV.LANCAMENTO_BASE || __ENV.ENTRY_BASE || 'http://host.docker.internal:5007';

export default function (data) {
  const token = data?.token || __ENV.ACCESS_TOKEN || null;
  const headers = token ? 
    { 'Authorization': `Bearer ${token}`, 'Accept': 'application/json' } : 
    { 'Accept': 'application/json' };

  // List fluxos
  let r = http.get(`${LANCAMENTO_BASE}/api/FluxoDeCaixa`, { headers });
  check(r, { 'list fluxos 200': (res) => res.status === 200 });
  
  if (r.status !== 200) {
    sleep(1);
    return;
  }

  let fluxos = JSON.parse(r.body || '[]');
  if (!fluxos || fluxos.length === 0) {
    sleep(1);
    return;
  }
  
  // pick a random fluxo
  const fluxo = fluxos[Math.floor(Math.random() * fluxos.length)];

  // create a lancamento
  const payload = JSON.stringify({ 
    tipo: Math.floor(Math.random() * 2), // 0 or 1 (Credito or Debito)
    valor: Math.floor(Math.random() * 1000) / 10, 
    descricao: 'k6 load test'
  });
  
  const postHeaders = token ? 
    { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' } : 
    { 'Content-Type': 'application/json' };

  const post = http.post(`${LANCAMENTO_BASE}/api/FluxoDeCaixa/${fluxo.id}/lancamentos`, payload, { headers: postHeaders });
  check(post, { 'post lanc 201': (res) => res.status === 201 });
  sleep(0.1);
}
