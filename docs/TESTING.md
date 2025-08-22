## k6 Testes (do README)

### 1) Executar (um único passo):

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

