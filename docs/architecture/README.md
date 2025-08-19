Arquitetura (C4) - Fluxo de Caixa Diário

Este diretório contém diagramas C4 (PlantUML) e documentação de arquitetura para o sistema de Fluxo de Caixa Diário implementado como POC.

Arquivos:
- `C4-Context.puml` — Diagrama de Contexto (Nível 1).
- `C4-Container.puml` — Diagrama de Contêineres (Nível 2).
- `C4-Component.puml` — Diagrama de Componentes (Nível 3) focado no `ConsolidacaoService`.
- `ARCHITECTURE.md` — Mapeamento de requisitos, decisões e próximos passos.

Como gerar os diagramas

Você pode usar o PlantUML localmente (jar) ou a extensão PlantUML do VS Code. Exemplo (com docker):

```powershell
# usando o container plantuml/plantuml
docker run --rm -v ${PWD}:/workspace -w /workspace plantuml/plantuml C4-Context.puml
```

Ou na máquina com plantuml.jar:

```powershell
java -jar plantuml.jar C4-Context.puml
```

Observações
- Os diagramas usam a sintaxe C4-PlantUML. A inclusão remota via URL é usada nos arquivos .puml; se estiver offline, faça o download da biblioteca C4-PlantUML e ajuste os includes.
- Os diagramas são adequados para documentação de POC; para produção, reflita o uso de clusters/replicas e componentes gerenciados.

Autenticação Central (JWT + Cookie)

- O token agora é emitido somente pelo serviço `FluxoDeCaixa.Auth` via `/auth/token` (POST JSON { "username", "password" }) ou formulário em `/login`.
- Credenciais padrão (`ADMIN_USER`/`ADMIN_PASS`) podem ser sobrescritas por variáveis de ambiente.
- Segredo: `JWT_SECRET` (≥32 chars). Se menor, é preenchido (padding) apenas para dev.
- O token também é definido em cookie HttpOnly `fluxo_token` para facilitar navegação web.
- Serviços de domínio (Lancamento / Consolidacao) não expõem mais endpoints de login/token; apenas validam JWT.
- Redireciono automático: requisição browser GET não autenticada é redirecionada para `/login` (ou `LOGIN_BASE_URL/login` se configurado).
