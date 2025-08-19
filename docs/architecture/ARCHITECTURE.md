Arquitetura — Mapeamento de requisitos e decisões

Resumo rápido
- Arquitetura atual: Auth Service (login, emissão JWT + cookie, /home), LancamentoService (API + UI lançamentos + publicação de eventos), ConsolidacaoService (consumer + cálculo saldo diário + UI relatório + API), RabbitMQ (fanout), Redis (cache ou fallback in-memory), dois bancos Postgres separados (um para lançamentos, outro para consolidação).
- UI estática em cada serviço de domínio (`wwwroot/index.html`) via `UseDefaultFiles` + `UseStaticFiles`.
- Enum `TipoLancamento` trafega como número (0=Crédito,1=Débito); front converte para labels.
- EF Core com migrations (uma por serviço) aplicadas com retry resiliente em startup (`Database.Migrate()` com loop configurável).
- Conexões e migrações resilientes: variáveis `DB_MAX_ATTEMPTS`, `DB_RETRY_DELAY_MS`, `RABBIT_MAX_ATTEMPTS`, `RABBIT_BASE_DELAY_MS` governam backoff progressivo.
- Separação física de dados: containers `postgres_lancamento` e `postgres_consolidacao` isolam escopo de cada serviço.

Mapeamento RF / RNF
- RF-01: Registrar lançamentos — OK (API + eventos + UI).
- RF-02: Persistência — OK (Postgres only; 2 bancos; migrations versionadas; SQLite removido).
- RF-03: Consulta de histórico — Parcial (sem paginação, filtros de range / tipo ainda faltam).
- RF-04: Consolidação diária — OK (consumer + handler idempotente + atualização incremental saldo).
- RF-05: Expor relatório — Parcial (GET `/api/SaldoDiario?data=YYYY-MM-DD`; falta range, export, paginação, cache-read real).
- RF-06: Autenticação centralizada — OK (Auth Service + JWT cookie).

RNFs principais
- RNF-01 (Resiliência): Parcial — retry de migração e RabbitMQ implementados; falta Outbox, DLQ, confirmação explícita de consumo.
- RNF-02 (Escalabilidade): Parcial — serviços stateless; consumer acoplado ao processo web (não separado em worker); scale horizontal limitado.
- RNF-03 (Confiabilidade): Parcial — idempotência via verificação de existência de lançamento; sem outbox/garantia at-least-once consistente.
- RNF-04 (Segurança): Básico — JWT HS256; falta TLS, rotation, refresh tokens, RBAC ampliado, rate limiting.
- RNF-05 (Manutenibilidade): Boa — separação clara + migrations; precisa testes automatizados, lint e análise estática.
- RNF-06 (Observabilidade): Inicial — apenas logs; sem tracing/metrics.
- RNF-07 (Desempenho): Adequado — índices adicionados (datas e compósitos); cache escrito mas ainda não lido pela API.

Decisões arquiteturais (principais)
- Auth Service dedicado (boundary segurança / identidade).
- Postgres segregado por serviço (isolamento de falhas e de modelo de dados).
- RabbitMQ fanout (extensibilidade de novos consumidores sem acoplamento forte).
- Retry de infraestrutura (DB migrations + conexão RabbitMQ) para robustez em cold start.
- Cache Redis write-only inicial (handler escreve; leitura futura planejada) para minimizar ajuste precoce.
- Enum numérico nas APIs (simplicidade e payload reduzido) + mapeamento na UI.
- UI estática embarcada (facilita testes sem pipeline front separado).
- Normalização de DateTime para UTC na consolidação (evita exceções Npgsql timezone).

Próximos passos priorizados
1. Endpoint range de datas (GET `/api/SaldoDiario/range` com intervalo e ordenação).
2. Ler cache no controller (cache-first + fallback DB) e política de expiração.
3. Outbox + entrega confiável (publicação atômica com persistência local + background dispatcher).
4. Separar consumer em Worker para escala independente.
5. Observabilidade (OpenTelemetry tracing + métricas Prometheus).
6. Testes (unit, integração evento→saldo, auth, carga k6 automação).
7. Segurança avançada (TLS, secrets via env seguro, refresh tokens, rate limiting, RBAC granular).
8. Pipeline CI/CD + scans (SAST/Dependency) + quality gates.
9. Endpoint export (CSV) e paginação/histórico avançado.
10. Hardening de índices adicionais (ex: índice composto tipo+data se consultas filtrarem por tipo).

Artefatos entregues
- PlantUML C4 (Context, Container, Component) atualizados para Postgres segregado.
- UIs estáticas: Auth (/login,/home), Lançamentos (/), Consolidação (/).
- Evento: LancamentoCreated (demais tipos futuros: Updated/Deleted ainda não publicados).
- Scripts infra (docker compose com healthchecks + retries).
- Postman collection + script k6 para carga.
- Migrations EF Core versionadas por serviço.

Observações adicionais
- Autenticação via cookie JWT (`fluxo_token`) lido pelo JwtBearer Events.
- Escritas usam `X-Api-Key` (hardcoded dev) — trocar por escopo/role.
- Cache inválido pode ser reconstruído a partir de lançamentos (fonte de verdade).
- DateTimes normalizados para UTC na consolidação evitam erro Npgsql (timestamp with time zone).

Observação: diagramas .png podem ser regenerados via PlantUML (ver README). Solicite se precisar exportações atualizadas.
