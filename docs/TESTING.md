# Testes de Performance

## Requisitos Funcionais Testados

| Código | Descrição | Requisito | Como é Testado |
|--------|-----------|-----------|----------------|
| RF-01 | Registrar lançamentos (débito/crédito) com valor, data, descrição | O sistema deve permitir o registro de lançamentos de débito e crédito com valor, data, e descrição | Script `create_lancamentos.js` realiza POST para criar lançamentos |
| RF-02 | Persistir lançamentos | O sistema deve armazenar os lançamentos de forma persistente | Lançamentos criados são consultados e validados nos testes |
| RF-03 | Consultar histórico de lançamentos | O sistema deve permitir a consulta do histórico de lançamentos | Script `script.js` consulta saldos consolidados baseados em lançamentos |
| RF-04 | Consolidar saldo diário | O sistema deve ser capaz de processar todos os lançamentos de um dia para gerar um saldo consolidado | Script `script.js` valida endpoint de saldo consolidado |
| RF-05 | Relatório consolidado diário | O serviço de consolidação deve expor uma API para que o relatório possa ser consumido | Testes fazem GET em `/api/SaldoDiario` validando resposta 200 |

## Requisitos Não Funcionais Testados

| Código | Descrição | Requisito | Como é Testado |
|--------|-----------|-----------|----------------|
| RNF-02 | Escalabilidade | O serviço de consolidado diário deve ser capaz de receber 50 requisições por segundo em picos | Script padrão usa 50 VUs por 30s. **Com Cache Redis: 233 req/s** (366% acima do requisito) |
| RNF-03 | Confiabilidade | O serviço de consolidado diário deve garantir uma perda máxima de 5% de requisições em picos | k6 valida taxa de sucesso (checks). **Com Cache Redis: 0% falhas** (100% success rate) |
| RNF-04 | Segurança | O serviço deve implementar mecanismos de autenticação e autorização para o registro e consulta de lançamentos | Setup obtém token JWT do serviço Auth e usa em todas as requisições |

## k6 Testes

### 1) Pré-requisitos:

```powershell
# Certificar que Docker Desktop está rodando
docker compose up -d --build

# Permitir execução de scripts (uma vez por sessão)
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
```

### 2) Executar (um único passo):

```powershell
# a partir da raiz do repositório (independente do caminho local)
.\k6\run-load-tests.ps1
```

O comando acima inicia o cenário de teste definido nos scripts em `k6/`.

### O que será testado
- `k6/script.js`: faz chamadas concorrentes ao endpoint `GET /api/SaldoDiario?data=YYYY-MM-DD` (padrão: data de hoje). Ele obtém um token de `Auth` no setup e valida respostas 200; configuração padrão: 10 VUs por 30s.
- `k6/create_lancamentos.js` (opcional): cria lançamentos via `POST /api/FluxoDeCaixa/{fluxoId}/lancamentos` para exercitar o caminho de escrita (configuração: 5 VUs por 30s).

### Como ler os resultados
- Resumo no console: ao final do k6 você verá métricas principais (VUs, requisições, erros, `http_req_duration` com percentis p(90)/p(95)/p(99)). Esse resumo geralmente atende para verificar performance e regressões.
- JSON detalhado (se gerado): o script pode salvar um resumo em `k6/k6-summary.json`. Para abrir:

```powershell
Get-Content .\k6\k6-summary.json | ConvertFrom-Json | Format-List
```
