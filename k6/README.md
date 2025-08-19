k6 scripts for ProjetoCIandT

create_lancamentos.js - simulates the Fluxo de Caixa UI: lists fluxos and creates lancamentos.

Usage (local Docker k6):
- LANCAMENTO_BASE should point to host.docker.internal:5260 for LancamentoService (legacy ENTRY_BASE still accepted)
- API_KEY default is dev-default-key

Example (docker):
 docker run --rm -i -v "C:\\ProjetoCIandT\\k6:/scripts" -w /scripts grafana/k6 run create_lancamentos.js

Or with env overrides:
 docker run --rm -e LANCAMENTO_BASE=http://host.docker.internal:5260 -e API_KEY=dev-default-key -v "C:\\ProjetoCIandT\\k6:/scripts" -w /scripts grafana/k6 run create_lancamentos.js

PowerShell convenience script (Windows)

You can run the provided PowerShell helper which wraps the docker k6 image:

```powershell
# From repository root
cd k6
.\run-load-tests.ps1 -Target consolidacao -Vus 50 -Duration 30s
```

Or run a lancamento workload:

```powershell
.\run-load-tests.ps1 -Target lancamento -Vus 40 -Duration 30s -ApiKey dev-default-key

Authenticated requests (Consolidacao)

The consolidacao endpoint is protected by JWT. The PowerShell runner will perform an auth request to the Auth service using the following defaults:

- AUTH_BASE: http://host.docker.internal:5080
- ADMIN_USER: admin
- ADMIN_PASS: password

You can override these by editing the `run-load-tests.ps1` or setting environment variables when invoking Docker directly.
```
