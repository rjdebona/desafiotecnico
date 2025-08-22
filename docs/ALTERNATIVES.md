
## Alternativas Arquiteturais Consideradas

Durante o planejamento, foram avaliadas diferentes abordagens para atender aos requisitos do desafio. 

### Alternativa 1: Monolito com Background Service

**Implementação:**
- **1 única aplicação .NET** com `BackgroundService` para consolidação
- **1 banco PostgreSQL** compartilhado entre funcionalidades
- **In-Memory Event Bus** ou **Outbox Pattern** para eventos
- **Redis** opcional para cache de saldos

**Como atende aos requisitos:**
- ✅ **RNF-01 (Resiliência)**: Background Service processa independente da API de lançamentos
- ✅ **RNF-02 (Escalabilidade)**: Cache Redis + índices otimizados + stateless app
- ✅ **RNF-03 (Confiabilidade)**: Transações ACID + retry no Background Service
- ✅ **RNF-04 (Segurança)**: Auth middleware integrado
- ✅ **RNF-05 (Manutenibilidade)**: Código organizado em services, deploy único

**Desvantagens:**
- ❌ Scaling menos granular (toda a app escala junto)
- ❌ Tecnologias acopladas (tudo deve ser .NET)
- ❌ Falha única point (se a app cair, tudo para)

### Alternativa 2: Serverless (Azure Functions)

**Implementação:**
- **Azure Functions** HTTP triggers para APIs
- **Timer Function** para consolidação (executa periodicamente)
- **Service Bus** para eventos entre functions
- **Azure SQL** para persistência transacional
- **Table Storage** para cache de saldos

**Como atende aos requisitos:**
- ✅ **RNF-01 (Resiliência)**: Functions completamente independentes
- ✅ **RNF-02 (Escalabilidade)**: Auto-scaling automático e ilimitado
- ✅ **RNF-03 (Confiabilidade)**: Retry policies nativas
- ✅ **RNF-04 (Segurança)**: Azure AD + Key Vault integrados
- ✅ **RNF-05 (Manutenibilidade)**: Functions isoladas, deploy independente

**Desvantagens:**
- ❌ primeiras requisições mais lentas
- ❌ específico para Azure
- ❌ ambiente distribuído
- ❌ 5-10 minutos por function


