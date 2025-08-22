# Desafio CI&T - Solução de Fluxo de Caixa Diário

Sistema de controle de fluxo de caixa diário implementado com arquitetura de microsserviços, utilizando .NET Core, PostgreSQL, RabbitMQ e Redis.

## Requisitos Funcionais

| Código | Descrição | Implementado (Resumo) |
|--------|-----------|------------------------|
| RF-01 | Registrar lançamentos (débito/crédito) com valor, data, descrição | API CRUD em `LancamentoService` + tipo Crédito/Débito + evento `LancamentoCreated` |
| RF-02 | Persistir lançamentos | Postgres dedicado (`lancamento_db`) via EF Core migrations |
| RF-03 | Consultar histórico de lançamentos | Endpoint `GET /api/FluxoDeCaixa` lista fluxos e lançamentos relacionados |
| RF-04 | Consolidar saldo diário | Consumer no `ConsolidacaoService` atualiza `SaldoDiario` idempotente ao receber evento |
| RF-05 | Relatório consolidado diário | Endpoint `GET /api/SaldoDiario?data=YYYY-MM-DD` + UI estática |
| RF-06 | Autenticação centralizada | Serviço Auth (login, token JWT HS256 + cookie HttpOnly) usado pelos demais serviços |

## Requisitos Não Funcionais

| Código | Descrição | Implementado (Resumo) |
|--------|-----------|-----------------------|
| RNF-01 | Resiliência | Retry DB migrations & RabbitMQ, fanout exchange, containers separados de banco |
| RNF-02 | Escalabilidade | Serviços stateless, mensagens desacopladas, bancos isolados por serviço |
| RNF-03 | Confiabilidade | Idempotência no handler (checa existência de lançamento), filas duráveis |
| RNF-04 | Segurança | Auth central (JWT + cookie)|
| RNF-05 | Manutenibilidade | Código separado por serviço, migrations versionadas, documentação C4 |
| RNF-06 | Observabilidade inicial | Logs console, healthchecks e mensagens de retry no startup |
| RNF-07 | Desempenho | Índices em datas e chave composta, cache Redis (escrita) preparado |
| RNF-08 | Infraestrutura & Deploy | Docker Compose, Postgres segregado (`postgres_lancamento` / `postgres_consolidacao`), variáveis de retry |

## Visão Técnica Sintética

- **Serviços:** Auth, LancamentoService, ConsolidacaoService
- **Mensageria:** RabbitMQ (exchange fanout `lancamentos`)
- **Bancos:** Postgres (2 containers, um por serviço)
- **Cache:** Redis (saldo diário)
- **Auth:** JWT HS256 + Cookie HttpOnly
- **Containerização:** Docker Compose (healthchecks, dependências ordenadas)

## Tabela de Decisões

| Escolha | Por que | Benefício / Impacto |
|---|---|---|
| Arquitetura por microsserviços | Isola responsabilidades (Auth, Lançamento, Consolidação) e facilita deploy/escala independentes | Facilita escalonamento e deploy independente |
| Comunicação assíncrona (RabbitMQ) | Desacopla produtores e consumidores; permite reprocessamento e buffering | Maior resiliência em picos e tolerância a falhas temporárias |
| Postgres (persistência) | Banco relacional maduro, transacional e com suporte a migrações | Consistência transacional e histórico confiável de lançamentos |
| Redis (cache) | Cache para leituras rápidas de saldo consolidado | Reduz latência em consultas críticas e alivia carga do DB |
| .NET / C# | Ecosistema maduro para Web API e EF Core; alinhado com o requisito do desafio | Desenvolvimento rápido, integração com libs .NET e facilidade de manutenção |
| k6 (teste de carga) | Ferramenta scriptável, reproduzível e integrável em CI | Permite validar requisitos de performance e automatizar testes de carga |

## Arquitetura Definida
- **[Arquitetura da Solução](./docs/ARCHITECTURE.md)** - Diagramas C4, estrutura de eventos e modelo de dados

## Alternativas Arquiteturais
- **[Alternativas Arquiteturais](./docs/ALTERNATIVES.md)** - Outras abordagens consideradas durante o planejamento

## Execução Rápida

```bash
# Iniciar todos os serviços
docker compose up -d --build

# Acessar as aplicações
# Auth: http://localhost:5080/login
# Lançamentos: http://localhost:5007/
# Consolidação: http://localhost:5260/
# RabbitMQ: http://localhost:15672

# Reset completo dos dados
docker compose down -v && docker compose up -d --build
```

## Testes de Performance
- **[Testes de Performance](./docs/TESTING.md)** - Instruções k6 e validação de requisitos

## Melhorias Futuras
- **[Melhorias Futuras](./docs/ROADMAP.md)** - Roadmap de evolução e melhorias planejadas  










