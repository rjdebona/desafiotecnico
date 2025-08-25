# Desafio CI&T - Solução de Fluxo de Caixa Diário

Sistema de controle de fluxo de caixa diário implementado com arquitetura de microsserviços, utilizando .NET Core, PostgreSQL, RabbitMQ e Redis.

## Requisitos Funcionais

| Código | Descrição | Requisito | Implementado (Resumo) |
|--------|-----------|-----------|------------------------|
| RF-01 | Registrar lançamentos (débito/crédito) com valor, data, descrição | O sistema deve permitir o registro de lançamentos de débito e crédito com valor, data, e descrição | API CRUD em `LancamentoService` + tipo Crédito/Débito + evento `LancamentoCreated` |
| RF-02 | Persistir lançamentos | O sistema deve armazenar os lançamentos de forma persistente | Postgres dedicado (`lancamento_db`) via EF Core migrations |
| RF-03 | Consultar histórico de lançamentos | O sistema deve permitir a consulta do histórico de lançamentos | Endpoint `GET /api/FluxoDeCaixa` lista fluxos e lançamentos relacionados |
| RF-04 | Consolidar saldo diário | O sistema deve ser capaz de processar todos os lançamentos de um dia para gerar um saldo consolidado | Consumer no `ConsolidacaoService` atualiza `SaldoDiario` idempotente ao receber evento |
| RF-05 | Relatório consolidado diário | O serviço de consolidação deve expor uma API para que o relatório possa ser consumido | Endpoint `GET /api/SaldoDiario?data=YYYY-MM-DD` + UI estática |

## Requisitos Não Funcionais

| Código | Descrição | Requisito | Implementado (Resumo) |
|--------|-----------|-----------|-----------------------|
| RNF-01 | Resiliência | O serviço de controle de lançamento não deve ficar indisponível se o sistema de consolidado diário falhar | Retry DB migrations & RabbitMQ, fanout exchange, containers separados de banco |
| RNF-02 | Escalabilidade | O serviço de consolidado diário deve ser capaz de receber 50 requisições por segundo em picos | Serviços stateless, mensagens desacopladas, bancos isolados por serviço |
| RNF-03 | Confiabilidade | O serviço de consolidado diário deve garantir uma perda máxima de 5% de requisições em picos | Idempotência no handler (checa existência de lançamento), filas duráveis |
| RNF-04 | Segurança | O serviço deve implementar mecanismos de autenticação e autorização para o registro e consulta de lançamentos | Auth central (JWT + cookie)|
| RNF-05 | Manutenibilidade | O código deve ser modular, com documentação clara e testes | Código separado por serviço, migrations versionadas, documentação C4 |

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

**Justificativa:**
A escolha por microsserviços foi motivada pelos requisitos específicos do desafio, especialmente o RNF-01 (Resiliência) que exige que "o serviço de controle de lançamento não deve ficar indisponível se o sistema de consolidado diário falhar". Esta arquitetura garante que cada domínio funcional seja independente, permitindo que falhas em um serviço não afetem os demais.

**Benefícios alcançados:**
- **Isolamento de falhas**: O serviço de lançamentos continua operacional mesmo se a consolidação falhar
- **Escalabilidade independente**: Cada serviço pode ser escalado conforme sua demanda específica
- **Deploy independente**: Atualizações podem ser feitas em um serviço sem afetar os outros
- **Responsabilidade única**: Cada serviço tem uma responsabilidade bem definida

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










