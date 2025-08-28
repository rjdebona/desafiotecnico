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
    
  // Consultar saldo de hoje para testar cache
  const response = http.get(`${BASE_URL}/api/SaldoDiario?data=2025-08-28`, { headers });
  
  check(response, {
    'status is 200': (r) => r.status === 200,
    'has fromCache field': (r) => {
      try {
        const body = JSON.parse(r.body);
        return body.hasOwnProperty('fromCache');
      } catch {
        return false;
      }
    }
  });
  
  console.log(`Response: ${response.body}`);
}
