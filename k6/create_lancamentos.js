import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  vus: 5,
  duration: '30s',
};

const LANCAMENTO_BASE = __ENV.LANCAMENTO_BASE || __ENV.ENTRY_BASE || 'http://host.docker.internal:5260';
const API_KEY = __ENV.API_KEY || 'dev-default-key';

export default function () {
  // List fluxos
  let r = http.get(`${LANCAMENTO_BASE}/api/FluxoDeCaixa`);
  check(r, { 'list fluxos 200': (res) => res.status === 200 });
  let fluxos = JSON.parse(r.body || '[]');
  if (!fluxos || fluxos.length === 0) {
    sleep(1);
    return;
  }
  // pick a random fluxo
  const fluxo = fluxos[Math.floor(Math.random() * fluxos.length)];

  // create a lancamento (tipo numeric)
  const payload = JSON.stringify({ tipo: 0, valor: Math.floor(Math.random()*1000)/10, descricao: 'k6 load test' });
  const headers = { 'Content-Type': 'application/json', 'X-Api-Key': API_KEY };
  const post = http.post(`${LANCAMENTO_BASE}/api/FluxoDeCaixa/${fluxo.id}/lancamentos`, payload, { headers });
  check(post, { 'post lanc 201': (res) => res.status === 201 });
  sleep(0.1);
}
