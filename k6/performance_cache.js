import http from 'k6/http';
import { check } from 'k6';

const BASE_URL = 'http://consolidacao:8080';

export function setup() {
  // Autenticação
  const authResponse = http.post('http://auth:8080/auth/token', JSON.stringify({
    username: 'admin',
    password: 'password'
  }), { headers: { 'Content-Type': 'application/json' } });
  
  if (authResponse.status === 200) {
    const authData = JSON.parse(authResponse.body);
    return { token: authData.access_token };
  }
  return { token: null };
}

export default function(data) {
  const headers = data.token ? 
    { 'Authorization': `Bearer ${data.token}` } : 
    {};
    
  // Consultar saldo de hoje repetidamente para testar performance
  const response = http.get(`${BASE_URL}/api/SaldoDiario?data=2025-08-28`, { headers });
  
  check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
    'from cache': (r) => {
      try {
        const body = JSON.parse(r.body);
        return body.fromCache === true;
      } catch {
        return false;
      }
    }
  });
}
