# Plano de implementação — Mini Gestão de Pedidos

> Documento da Parte 1 (SDD). Registra o plano aprovado em *plan mode*, como ele foi executado,
> as divergências e correções, e as decisões da autora. Fonte de verdade das regras: `docs/spec.md`.

**Legenda de estado**

- ✅ concluída
- 🔧 concluída com ajustes
- ⏳ pendente
- 📝 em andamento

**Legenda de origem**

- 🅕 decisão ou pedido explícito da Francisca
- 🅒 decisão técnica do Claude
- 🅣 achado de teste ou verificação

## 1. Como o plano foi produzido

1. A especificação foi escrita **antes** do plano e do código: `5197d4f docs: add feature specification (SDD)`.
2. O Claude Code entrou em **plan mode** (somente leitura) e fez quatro perguntas. Respostas da Francisca 🅕:

   | Pergunta | Resposta |
   |---|---|
   | Framework do frontend | React + Vite |
   | Acesso a dados | EF Core |
   | Projeto original (Class Library vazia) | Remover |
   | Diferenciais opcionais | GitHub Actions e Playwright E2E (sem deploy online) |

3. O plano abaixo foi apresentado com `ExitPlanMode` e **aprovado pela Francisca** antes de qualquer código.

## 2. Plano original (texto aprovado, sem alterações)

> **Nota:** o texto abaixo é reproduzido exatamente como foi aprovado. Os nomes `Projeto.*` e
> `Projeto Tipo no Visual Studio.sln` foram renomeados depois para `MiniGestaoPedidos.*` e
> `MiniGestaoPedidos.sln` no commit `449529d` (ver D11).

<details>
<summary>Plano aprovado em plan mode (clique para expandir)</summary>

<!-- INÍCIO DO PLANO ORIGINAL (cópia byte a byte) -->
# Plano de implementação — Mini Gestão de Pedidos

## Contexto

Teste técnico: entregar ponta a ponta uma feature de **cadastro e consulta de Pedidos**. A entrega inclui:
- SDD (spec + plano);
- configuração AI First (CLAUDE.md, hook, skill, MCP para SQL Server);
- backend .NET 8 em Clean Architecture com SQL Server;
- frontend React;
- debug de uma query de ranking;
- testes unitários, de integração e E2E;
- Docker Compose (sobe com um comando) e CI.

O histórico de commits é avaliado, por isso o trabalho vai em **commits pequenos e frequentes, sem squash**.

**Ponto de partida:**
- Commit `2c3fe19`: scaffold com `src/Projeto.{Domain,Application,Infrastructure,Api}` e `tests/Projeto.{Domain,Application}.Tests`. Tudo em net8.0 e só com arquivos de modelo (`Class1.cs`, `UnitTest1.cs`, `WeatherForecast*`).
- Commit `5197d4f`: `docs/spec.md`, que é a **fonte de verdade** das regras RN01–RN13, dos endpoints, do modelo de dados e dos critérios de aceite CA01–CA17.

**Ambiente verificado:**
- SDKs .NET 5/8/9/10 instalados, com o **10 como padrão**.
- Node 22, Docker 28 com Compose v2 (Docker Desktop iniciado), Python 3.13 e `sqlcmd` local.
- Não há SQL Server local, nem `gh`, nem `uv`.
- Remote `origin` = GitHub, branch `master`.

**Decisões da Francisca (respostas às perguntas do plan mode):**
- React + Vite.
- EF Core.
- **Remover** o projeto original.
- Incluir **GitHub Actions** e **Playwright**.
- **Sem** deploy online.

---

## Decisões técnicas

| Tema | Decisão | Motivo |
|---|---|---|
| SDK | `global.json` fixando o 8.0.408 (`rollForward: latestFeature`) | O SDK padrão da máquina é o 10. Fixar garante build idêntico local, no Docker e na CI. |
| Build | `Directory.Build.props` com `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors` e `AnalysisLevel latest` | Regras de qualidade definidas num único sítio. |
| Schema | **Scripts SQL em `database/` são a fonte de verdade.** Sem EF migrations. O EF só mapeia, com Fluent API. | O teste pede scripts. Uma só fonte evita divergência. Os testes de integração rodam **os mesmos scripts**, o que também os valida. |
| Erros | Exceções tipadas mapeadas para ProblemDetails por um `IExceptionHandler`: `DomainException` e `RegraNegocioException` → 422, `NaoEncontradoException` → 404, `DbUpdateConcurrencyException` → 409. Erros de validação → 400. | É o caminho idiomático no .NET 8, com pouco código. Um Result pattern seria mais cerimónia do que o escopo pede. |
| Validação | FluentValidation na Application (`CriarPedidoRequestValidator`, `ListarPedidosQueryValidator`, `TopClientesQueryValidator`) | Os validadores ficam testáveis isoladamente nos testes unitários. |
| Concorrência (RN10) | `RowVersion` em `Produtos` como token de concorrência, `SaveChanges` numa transação e `CHECK (Estoque >= 0)` | Evita overselling sem lock pessimista. Em caso de conflito, a API devolve 409. |
| Leitura | Interfaces de leitura (`IPedidoQueries`, `IRelatorioQueries`) que projetam direto para DTO com `AsNoTracking` | Listagem e relatório não precisam de agregados. A query fica eficiente e coberta pelos índices. |
| Tempo | `TimeProvider` injetado (RN08) | Data determinística nos testes. |
| Asserções | xUnit `Assert` puro (sem FluentAssertions) e NSubstitute para mocks | A partir da v8, o FluentAssertions passou a ter licença comercial. |
| Integração | `Testcontainers.MsSql` + `WebApplicationFactory<Program>` + Respawn para limpar dados entre testes | Usa um SQL Server real e descartável. Funciona igual localmente e na CI (os runners Ubuntu têm Docker). |
| Frontend | Vite + React + TS, `fetch` tipado, sem lib de estado. É servido por **nginx**, que faz proxy de `/api` → `api:8080`, e o Vite dev usa o mesmo proxy. | Sem CORS, com a mesma URL relativa em dev e em produção. |
| Init do banco | Serviço `db-init` com a mesma imagem `mssql/server:2022-latest` (que já traz `/opt/mssql-tools18/bin/sqlcmd`). Roda `database/*.sql` em ordem depois do `db` ficar healthy. A `api` usa `depends_on: db-init: service_completed_successfully`. | A imagem oficial não tem init automático. Os scripts são idempotentes (`IF DB_ID(...) IS NULL`, `IF OBJECT_ID(...) IS NULL`). |
| Segredos | Senha SA de desenvolvimento com default no compose: `${MSSQL_SA_PASSWORD:-Pedidos_Dev_2026!}` | Requisito "sobe sem ajustes manuais". O README diz que é só para desenvolvimento. |

---

## Etapas e commits (em ordem)

Cada etapa termina com `dotnet build` e `dotnet test` verdes antes do commit. Os prefixos seguem Conventional Commits.

### 1. Limpeza e base
1. `chore: remove empty template project` — `dotnet sln remove` e apagar `Projeto Tipo no Visual Studio/`.
2. `chore: remove template sample files` — `Class1.cs`, `UnitTest1.cs`, `WeatherForecast*`, `Projeto.Api.http`.
3. `build: pin .NET 8 SDK and centralize build settings` — `global.json`, `Directory.Build.props`.

### 2. Configuração AI First (cedo, para ser *usada* durante o desenvolvimento)
4. `docs: add CLAUDE.md with project conventions`, com:
   - arquitetura e regra de dependência;
   - nomenclatura (pt-BR no domínio, `*Service`, `*Repository`, `*Queries`, `*Request/*Response`, `*Validator`, `I*`; testes `Metodo_Cenario_Resultado`);
   - comandos de build, testes, compose e frontend;
   - "o que não fazer" (sem migrations, sem lógica em controller, sem FluentAssertions).
5. `chore(claude): add post-edit build hook`:
   - `.claude/settings.json` com `PostToolUse` para `Edit|Write`, que chama `node .claude/hooks/post-edit-check.mjs`.
   - O script lê o JSON do stdin. Se o arquivo for `.cs` ou `.csproj`, roda `dotnet build --no-restore -v q`. Se for `.ts` ou `.tsx`, roda `npx tsc --noEmit` em `frontend/`.
   - Em caso de erro, devolve **exit 2** com as mensagens no stderr, para o Claude corrigir na hora.
   - Usa Node em vez de bash e jq para funcionar no Windows e no Linux.
   - Acrescenta `permissions.allow` para `dotnet build/test`, `npm`, `docker compose`.
6. `chore(claude): add novo-endpoint skill` — `.claude/skills/novo-endpoint/SKILL.md`. É um passo a passo do padrão: Request/Response + Validator → interface e Service na Application → Repository/Queries na Infrastructure → Controller → teste unitário + teste de integração → atualizar a spec.
7. `chore(claude): configure SQL Server MCP server` — `.mcp.json` (escopo de projeto) apontando para `localhost,1433` do compose.
   - **Escolha do servidor a confirmar nesta etapa:** preferência pelo MSSQL MCP oficial da Microsoft (`Azure-Samples/SQL-AI-samples`). Se não houver pacote executável direto, uso um servidor da comunidade via `npx` ou `pip`. Verifico qual realmente funciona antes do commit e não invento nomes de pacote.
   - ⚠️ O MCP só fica disponível para o Claude **depois de reiniciar a sessão** e aprovar o servidor de projeto. Nessa altura peço à Francisca para reiniciar. A partir daí uso o MCP para inspecionar o schema, conferir o seed e, sobretudo, para os planos de execução da Parte 4.

### 3. Banco de dados
8. `feat(db): add schema creation script` — `database/01-schema.sql`, exatamente com as tabelas, PK, FK, CHECK e índices da spec §4. Ajuste: o índice único `UX_ItensPedido_PedidoId_ProdutoId` ganha `INCLUDE (Quantidade, PrecoUnitario)` para cobrir a query da Parte 4. A spec é atualizada no mesmo commit.
9. `feat(db): add sample data seed script` — `database/02-seed.sql`, set-based e determinístico:
   - ~200 clientes, incluindo **homónimos** e clientes **sem pedidos em 2026**, para expor os bugs da Parte 4;
   - ~50 produtos com estoque alto;
   - ~20 mil pedidos em 2025–2026 com ~50 mil itens;
   - `ValorTotal` consistente com os itens.
   - O volume é suficiente para o plano de execução ser significativo, e o seed continua a rodar em poucos segundos.
10. `feat(docker): add SQL Server with automatic schema/seed init` — `docker-compose.yml` com `db` (healthcheck via sqlcmd) e `db-init`. Verificação: `docker compose up db db-init` e contagens por tabela.

### 4. Domínio (TDD)
11. `feat(domain): add Cliente, Produto, Pedido and ItemPedido entities`, com testes em `Projeto.Domain.Tests`:
    - `Produto.BaixarEstoque(qtd)` lança exceção se `qtd <= 0` ou se for maior que o estoque (RN06).
    - `Pedido.Criar(clienteId, data, itens)` valida pelo menos 1 item (RN02), quantidade > 0 (RN03) e produto não repetido (RN05), e calcula `ValorTotal` (RN09).
    - `ItemPedido.Subtotal`.
    - Setters privados, construtores protegidos para o EF e `DomainException`.

### 5. Application (TDD)
12. `feat(application): add pedido creation use case with validation`:
    - `CriarPedidoRequest/Response`, `CriarPedidoRequestValidator`, `IPedidoService`/`PedidoService`;
    - interfaces `IClienteRepository`, `IProdutoRepository`, `IPedidoRepository`, `IUnitOfWork`;
    - `DependencyInjection.AddApplication()`.
    - Testes com NSubstitute: cliente inexistente (RN01), produto inexistente (RN04), estoque insuficiente sem baixar nenhum estoque (RN06/CA06), preço vindo do produto (RN07), data vinda do `TimeProvider` (RN08), e cada regra do validator.
13. `feat(application): add pedido listing and top-clientes report contracts`:
    - `ListarPedidosQuery` + validator (RN11/RN12), `PaginaResultado<T>`, `IPedidoQueries`;
    - `TopClientesQuery` + validator, `IRelatorioQueries`;
    - consultas de clientes e produtos para o frontend.
    - Testes dos validators.

### 6. Infrastructure
14. `feat(infrastructure): add EF Core DbContext and entity mappings`:
    - `AppDbContext` e `IEntityTypeConfiguration<>` por entidade (nomes de tabela e colunas iguais aos do script, `RowVersion` com `IsRowVersion()`);
    - `DependencyInjection.AddInfrastructure(config)`.
15. `feat(infrastructure): add repositories and read queries`:
    - repositórios e `UnitOfWork` (transação);
    - `PedidoQueries` com filtro de período inclusivo (`>= inicio AND < fim+1 dia`, CA11), ordenação `DataPedido DESC, Id DESC`, `Count` + `Skip/Take`;
    - `RelatorioQueries` com `GroupBy` em `Pedidos` (`ValorTotal`), `Take(5)` e desempate por `ClienteId`.

### 7. API
16. `feat(api): add pedidos, relatorios, clientes and produtos endpoints`:
    - controllers finos (só chamam os services);
    - `Program.cs` com `AddProblemDetails`, `GlobalExceptionHandler`, Swagger, health check `/health` (com check do banco) e `public partial class Program` para a `WebApplicationFactory`;
    - `appsettings` com a connection string de dev.
    - Verificação manual com `curl` contra o compose.

### 8. Testes de integração
17. `test(api): add integration tests with Testcontainers SQL Server`:
    - projeto novo `tests/Projeto.Api.IntegrationTests`, adicionado à solução;
    - fixture `IAsyncLifetime` que sobe `MsSqlContainer`, executa `database/01-schema.sql` (separando por `GO`), configura a `WebApplicationFactory` com a connection string do container, e usa Respawn entre testes com dados mínimos por teste;
    - um teste por CA da API: CA01–CA14, incluindo a verificação de que o estoque **não** muda em CA06.

### 9. Docker da API
18. `feat(docker): containerize API` — `src/Projeto.Api/Dockerfile` multi-stage (sdk:8.0 → aspnet:8.0, porta 8080), mais o serviço `api` no compose com `depends_on db-init`.

### 10. Frontend
19. `feat(frontend): scaffold React + Vite + TypeScript app` — `frontend/`, com proxy `/api` no `vite.config.ts`.
20. `feat(frontend): add pedidos list with filters and pagination` — `api/client.ts` (tipos espelham os DTOs), `PedidosPage`, `FiltrosPedidos`, `TabelaPedidos`, `Paginacao` (CA15).
21. `feat(frontend): add create pedido modal` — `NovoPedidoModal`: itens dinâmicos, total em tela, erros 400/422/409 exibidos sem perder o que foi preenchido, recarga da lista ao salvar (CA16).
22. `feat(docker): containerize frontend with nginx` — Dockerfile multi-stage (node → nginx) e `nginx.conf` com fallback SPA e proxy `/api/`. Serviço `web` na porta 3000. Verificação: `docker compose up --build` do zero (CA17).

### 11. Parte 4 — Debug da query
23. `fix(db): correct and optimize 2026 customer ranking query` — `database/03-query-ranking-clientes-2026.sql` com a query original comentada e a corrigida.

Problemas já identificados na leitura:

| # | Problema | Efeito | Correção |
|---|---|---|---|
| 1 | `WHERE YEAR(p.DataPedido) = 2026` sobre um `LEFT JOIN` | Descarta as linhas com `p` NULL, por isso o LEFT JOIN vira INNER JOIN implicitamente. A intenção fica ambígua. | Num ranking de quem comprou, usar `INNER JOIN` explícito. |
| 2 | `YEAR()` aplicado à coluna | Torna o predicado não-sargable: scan da tabela inteira, sem seek no índice. | `p.DataPedido >= '2026-01-01' AND p.DataPedido < '2027-01-01'`. |
| 3 | `GROUP BY c.Nome` | Junta **clientes homónimos** num só, o que dá **total errado**. | `GROUP BY c.Id, c.Nome`. |
| 4 | `ORDER BY Total` | Ordem ascendente, por isso quem comprou menos aparece primeiro. | `ORDER BY Total DESC`, com desempate determinístico. |
| 5 | Sem índice adequado | Scan em `Pedidos` e em `ItensPedido`. | `IX_Pedidos_DataPedido INCLUDE (ClienteId)` e índice de `ItensPedido` por `PedidoId` com `INCLUDE (Quantidade, PrecoUnitario)`. |
| 6 | (Observação) | Pode-se usar `Pedidos.ValorTotal` para não ler os itens. | Fica como alternativa mais rápida. Mantenho a soma por itens como fonte de verdade e comparo as duas. |

Investigação, feita via MCP ou `sqlcmd` contra o compose:
- `SET STATISTICS IO, TIME ON` e plano de execução real, antes e depois;
- registar as leituras lógicas e o tempo;
- provar o bug do homónimo com uma query de conferência por `Id`.

Os números reais vão para o README.

### 12. E2E e CI
24. `test(e2e): add Playwright test for create-and-list pedido flow` — `frontend/e2e/`, com `baseURL` `http://localhost:3000` e a stack do compose a correr.
25. `ci: add GitHub Actions workflow` — `.github/workflows/ci.yml` com três jobs:
    - **backend**: setup-dotnet 8, restore, build, testes unitários e de integração (Testcontainers);
    - **frontend**: `npm ci`, `tsc`, build;
    - **e2e**: `docker compose up -d --build`, esperar pelo `/health`, Playwright, `compose down`.

### 13. Documentação
26. `docs: add implementation plan` — `docs/plan.md` = este plano + a secção **"Ajustes e discordâncias"**.
    - Essa secção regista as decisões que a Francisca tomou no plan mode (React, EF Core, remoção do projeto, sem deploy) e os desvios que surgirem durante a execução.
    - O que ela **discordou** do plano tem de ser escrito por ela. Deixo o espaço marcado e não invento opiniões.
27. `docs: add README` com:
    - como rodar com Docker;
    - como rodar os testes;
    - as decisões técnicas (tabela acima);
    - a Parte 4 com os números medidos;
    - como o MCP foi usado;
    - "Como usei o Claude" com factos reais da sessão (o que funcionou, o que o hook apanhou, o que foi corrigido).
    - O vídeo e o e-mail com o link ficam a cargo da Francisca.

**Push:** não faço `git push` sem confirmação explícita. O remote é público e o push é uma ação externa.

---

## Arquivos críticos

- `Projeto Tipo no Visual Studio.sln`, `global.json`, `Directory.Build.props`
- `CLAUDE.md`, `.claude/settings.json`, `.claude/hooks/post-edit-check.mjs`, `.claude/skills/novo-endpoint/SKILL.md`, `.mcp.json`
- `database/01-schema.sql`, `02-seed.sql`, `03-query-ranking-clientes-2026.sql`
- `src/Projeto.Domain/Entities/*.cs`, `src/Projeto.Application/Pedidos/PedidoService.cs`, `src/Projeto.Infrastructure/Persistence/AppDbContext.cs`, `src/Projeto.Api/Program.cs`
- `tests/Projeto.Api.IntegrationTests/` (projeto novo)
- `frontend/src/**`, `frontend/nginx.conf`, `frontend/e2e/*.spec.ts`
- `docker-compose.yml`, `.github/workflows/ci.yml`, `README.md`, `docs/plan.md`

## Verificação

1. `dotnet build` → 0 avisos e 0 erros (`TreatWarningsAsErrors`).
2. `dotnet test tests/Projeto.Domain.Tests tests/Projeto.Application.Tests` → testes unitários verdes.
3. `dotnet test tests/Projeto.Api.IntegrationTests` → verde contra o SQL Server do Testcontainers.
4. `docker compose down -v && docker compose up --build` → `http://localhost:3000` abre a listagem com dados do seed; `http://localhost:5000/swagger` funciona.
5. Fluxo manual: criar um pedido válido, receber erro de estoque com o modal preservado e confirmar o novo pedido no topo da lista.
6. `npx playwright test` (em `frontend/`) → verde com o compose a correr.
7. Parte 4: comparar o resultado da query original com o da corrigida e as estatísticas de IO antes e depois.
8. `git log --oneline` → ~27 commits pequenos e coerentes, sem reescrita de histórico.
<!-- FIM DO PLANO ORIGINAL -->

</details>

## 3. Execução comparada com o plano (27 etapas)

| # | Etapa (título do plano) | Commit(s) | Estado | Observação |
|---|---|---|---|---|
| 1 | `chore: remove empty template project` | `9a78b5f` | ✅ | |
| 2 | `chore: remove template sample files` | `3020d82` | ✅ | |
| 3 | `build: pin .NET 8 SDK and centralize build settings` | `198bce0` | 🔧 | Acrescentado `Directory.Packages.props` (gestão central de pacotes) 🅒 |
| 4 | `docs: add CLAUDE.md with project conventions` | `b1e0a92` | ✅ | Atualizado depois em `326b9e5`, `1a16411`, `449529d` e `2d07a53` |
| 5 | `chore(claude): add post-edit build hook` | `0e9d22c`, `daf28b2` | 🔧 | O hook pedia "correções" quando a DLL estava em uso por `dotnet run`; passou a só avisar (D6) 🅣 |
| 6 | `chore(claude): add novo-endpoint skill` | `d0a9ba7` | ✅ | |
| 7 | `chore(claude): configure SQL Server MCP server` | `fd9de8f` | 🔧 | Executada **depois** das etapas 8–10 (o login do MCP precisa do banco); `mssql-mcp@3.0.0` (D2) e login somente leitura (D1) |
| 8 | `feat(db): add schema creation script` | `f55a855` | 🔧 | Caminho `database/init/01-schema.sql` (D1); o ajuste do `INCLUDE` e a atualização da spec seguiram o plano |
| 9 | `feat(db): add sample data seed script` | `77a9ce6` | 🔧 | Caminho `database/init/` (D1); volume conforme o plano (200 / 50 / 20.000 / 50.000) |
| 10 | `feat(docker): add SQL Server with automatic schema/seed init` | `705c2cc` | ✅ | |
| 11 | `feat(domain): add Cliente, Produto, Pedido and ItemPedido entities` | `a0feb48` | ✅ | 17 testes unitários |
| 12 | `feat(application): add pedido creation use case with validation` | `0623b40` | 🔧 | Inclui também os contratos da **listagem** (previstos na etapa 13) e a `ConflitoConcorrenciaException` (D4) |
| 13 | `feat(application): add pedido listing and top-clientes report contracts` | `326b9e5` | 🔧 | A listagem já entrou na etapa 12; aqui ficaram o relatório e o catálogo (D5) |
| 14 | `feat(infrastructure): add EF Core DbContext and entity mappings` | `bba4eb9` | ✅ | |
| 15 | `feat(infrastructure): add repositories and read queries` | `2f5cea8` | ✅ | |
| 16 | `feat(api): add pedidos, relatorios, clientes and produtos endpoints` | `cc7779f` | 🔧 | Truncagem de `DataPedido` a milissegundos e chaves de erro em camelCase, encontradas ao testar com curl (D6) 🅣 |
| 17 | `test(api): add integration tests with Testcontainers SQL Server` | `503d827` | 🔧 | Executa `database/init/01-schema.sql` (D1); helper corrigido para não gravar como `DATETIME` (D6) 🅣 |
| 18 | `feat(docker): containerize API` | `bdb69f0`, `32ed779` | 🔧 | Feita no mesmo commit da etapa 22, após o frontend; override opcional de CA (D3) |
| 19 | `feat(frontend): scaffold React + Vite + TypeScript app` | `7146bf6` | ✅ | |
| 20 | `feat(frontend): add pedidos list with filters and pagination` | `0fa0654` | ✅ | |
| 21 | `feat(frontend): add create pedido modal` | `cb5f0d0` | 🔧 | CSS do modal corrigido após screenshots do Playwright (D6) 🅣 |
| 22 | `feat(docker): containerize frontend with nginx` | `bdb69f0` | 🔧 | Mesmo commit da etapa 18 |
| 23 | `fix(db): correct and optimize 2026 customer ranking query` | `e117b45` | ✅ | Investigação via MCP; resultado no README (`ff7a6d1`) (D7, D13) |
| 24 | `test(e2e): add Playwright test for create-and-list pedido flow` | `f1db7c2`, `039dbf1`, `7eeca47` | 🔧 | Ampliado de 1 fluxo para 10 testes (D8), mais 3 por fuso (D9); instabilidade corrigida (D12) |
| 25 | `ci: add GitHub Actions workflow` | `5577936` | ✅ | Três jobs, como previsto; o frontend também roda lint e Vitest e o backend inclui `Infrastructure.Tests` (D10). Primeira execução no GitHub (run `43460540401`, commit `cb97bb4`): Backend, Frontend e Playwright com sucesso. Gatilho automático restrito à `main` em `9026d02`; após a publicação (run `43468527578`, commit `f814221`), os três jobs passaram novamente |
| 26 | `docs: add implementation plan` | `bdc1c28` | ✅ | Atualizado depois com o estado da CI e do contrato 400. Os textos da autora na seção 6 estão pendentes |
| 27 | `docs: add README` | `ff7a6d1`, `0d6b0dc`, `cb97bb4` | ✅ | Parte 4 primeiro (D13), restante em `0d6b0dc`. As seções de avaliação pessoal e vídeo são da autora e estão pendentes |

**Commits fora das 27 etapas**

| Commit(s) | Descrição |
|---|---|
| `f60d9e3`, `d498771` | Commits iniciais do repositório (anteriores à sessão) |
| `2c3fe19`, `5197d4f` | Scaffold da Clean Architecture e spec (anteriores ao plano) |
| `1a16411`, `3a54eb7`, `2d469ff`, `039dbf1` | RN14 — período no fuso de quem consulta (D9) |
| `7b424c2`, `a813f9e` | Testes do conversor UTC e do instante gravado (D10) |
| `449529d` | Renomeação para `MiniGestaoPedidos` (D11) |
| `7eeca47` | Correção da instabilidade do E2E (D12) |
| `bde5a7c`, `e8f02a6` | Atualizações de segurança de dependências de desenvolvimento do frontend (`@playwright/test` 1.55.1, `vitest` 4.1.12) |
| `e69c067`, `dc9643c` | Contrato HTTP 400: spec §5 e CA23; correção e testes (D14) |

**Verificação do plano** (seção "Verificação" do texto aprovado)

| # | Previsto | Situação |
|---|---|---|
| 1 | Build sem avisos | ✅ 0 avisos, 0 erros |
| 2 | Testes unitários | ✅ Domínio 17, Application 56; mais Infrastructure 9 e Vitest 11 (não previstos, D10) |
| 3 | Integração com Testcontainers | ✅ 63 (48 + 15 do contrato HTTP 400, D14); também na CI do GitHub |
| 4 | `down -v` e `up --build`; `:3000` e `:5000/swagger` | ✅ verificado nesta máquina com o override de CA (D3); na CI do GitHub, `up -d --build` sem o override, com `/health` saudável |
| 5 | Fluxo manual de criar pedido e erro de estoque | ✅ |
| 6 | Playwright | ✅ 13/13 em dois ciclos com a stack recriada do zero; ✅ na CI do GitHub com `--retries=0 --fail-on-flaky-tests` |
| 7 | Parte 4 antes e depois | ✅ (D7) |
| 8 | "~27 commits" | 🔧 38 commits até a criação deste documento; 48 até `2d07a53`: etapas mais granulares e trabalho não previsto (D9–D14) |

## 4. Divergências e correções

| # | Divergência | Origem | Motivo | Evidência |
|---|---|---|---|---|
| D1 | Scripts em `database/init/`, com o login somente leitura `03-mcp-readonly-login.sql` (não previsto) | 🅒 | Ordem de execução no `db-init`; privilégio mínimo para o MCP, que aceita SQL livre | `f55a855`, `fd9de8f` |
| D2 | MCP da comunidade `mssql-mcp@3.0.0` | 🅒 | `@modelcontextprotocol/server-mssql` e `@azure/mssql-mcp-server` retornam 404 no npm; foram avaliados `mssql-mcp-server` 1.3.0 e `mssql-mcp` 3.0.0 (este último aceita SQL livre, necessário para planos de execução). Alternativa já prevista no plano | `fd9de8f` |
| D3 | `docker-compose.ca.yml`: certificado raiz como BuildKit secret, usado só no restore e no `npm ci` | 🅣 + 🅕 | O Avast intercepta o HTTPS e os containers não confiam na raiz dele. A Francisca **recusou** exclusões e a desativação da verificação HTTPS, pediu alternativas seguras e aprovou a "alternativa A" | `32ed779` (mensagem do commit) e sessão |
| D4 | O 409 vem de `ConflitoConcorrenciaException` (Application), convertida na Infrastructure, e não de `DbUpdateConcurrencyException` tratada na API | 🅒 | A API não precisa conhecer o EF | `0623b40`, `2f5cea8` |
| D5 | Controllers usam `ICatalogoQueries` sem service intermediário | 🅒 | Um service só repassaria a chamada; exceção documentada no `CLAUDE.md` | `326b9e5` |
| D6 | Correções encontradas durante a execução | 🅣 | Hook bloqueando com DLL em uso; `DataPedido` com precisão maior que `datetime2(3)`; `AddWithValue` gravando `DATETIME` (23:59:59.999 virava o dia seguinte); CSS do modal | `daf28b2`, `cc7779f`, `503d827`, `cb5f0d0` |
| D7 | Na Parte 4, a query corrigida por itens faz **mais** leituras lógicas (368 × 295) | 🅣 | 45% dos pedidos são de 2026; com essa seletividade o scan é a escolha correta; o ganho está em CPU e memória. A variante por `ValorTotal` faz 82 leituras | `e117b45`, `ff7a6d1` |
| D8 | E2E com 10 testes, cobrindo 7 cenários | 🅕 | Pedido da Francisca: listagem, filtros por cliente e período, paginação, modal, criação e erros, com banco real e testes reproduzíveis | `f1db7c2` |
| D9 | **RN14**: período interpretado no fuso do navegador (não previsto) | 🅣 + 🅕 | Encontrado nos E2E: o filtro usava dias UTC e a tela, hora local. A Francisca escolheu a alternativa B e aprovou com condições (seção 6, F5) | `1a16411`, `3a54eb7`, `2d469ff`, `039dbf1` |
| D10 | Testes não previstos: Vitest (frontend) e projeto `Infrastructure.Tests` | 🅒; 🅒 + 🅕 | **Vitest**: decisão do Claude durante a RN14, para testar horário de verão em vários fusos sem navegador. **Infrastructure.Tests**: proposto pelo Claude e autorizado pela Francisca (item 2b), para testar o conversor sem Docker. O teste do instante gravado (item 2a) também foi autorizado por ela | `039dbf1`, `7b424c2`, `a813f9e` |
| D11 | Renomeação para `MiniGestaoPedidos` | 🅕 | Pedido da Francisca, em commit único preservando o histórico | `449529d` |
| D12 | Instabilidade do E2E corrigida | 🅣 | O teste lia as linhas antes da resposta filtrada chegar | `7eeca47` |
| D13 | A Parte 4 do README foi escrita antes do restante | 🅒 | Documentar a análise logo após realizá-la | `ff7a6d1` |
| D14 | Contrato HTTP 400 unificado (não previsto): todo 400 em `application/problem+json`, com `type` e `traceId`, chaves em camelCase e mensagens em português, sem nada interno do .NET | 🅣 | Regressão do `2d469ff` encontrada em revisão somente leitura (itens 3a–3f, adiados em F7). A spec ganhou a §5 "Formato do 400" e o CA23 antes da correção. 404 e 415 continuam no formato padrão (F7) | `e69c067`, `dc9643c` |

## 5. Problemas conhecidos e pendências

- **Contrato HTTP 400 (3a–3f) — corrigido.** O `2d469ff` fez as respostas 400 de binding perderem `type` e
  `traceId`, saindo como `application/json`, em inglês e com nomes de tipos .NET. Corrigido em `dc9643c`, com o
  contrato definido antes na spec (`e69c067`, §5 e CA23) (D14).
- **Commit intermediário `3a54eb7`:** a API aceitava `inicio`/`fim` sem offset (usando o fuso do servidor) até o
  `2d469ff`. O frontend ainda não usava esses parâmetros e o commit nunca foi publicado. Não é corrigível sem
  reescrever o histórico, o que é proibido. **Lição:** a validação da entrada entra junto com o contrato novo,
  ou antes dele.
- **Histórico de dois arquivos pequenos:** só aparecem como renomeados com `git log --follow -M30%`
  (`IPedidoRepository.cs` 44%, `MiniGestaoPedidos.Infrastructure.csproj` 47%).
- **404 e 415:** mantêm o formato padrão do ASP.NET, por decisão da Francisca 🅕.
- **Resolvidos:** CI (etapa 25), README (etapa 27), branch principal publicada como `main` e publicação de
  `9026d02` e `2d07a53` (push até `f814221`; CI `43468527578` com sucesso nos três jobs).
- **Pendentes:**
  - `.mcp.json` com `cmd /c npx`, que só funciona no Windows (P2 no README);
  - `npm audit` com vulnerabilidades em dependências de desenvolvimento (ver README §11);
  - textos da autora (seção 6 deste documento e as seções de avaliação pessoal e vídeo do README).

## 6. Ajustes e discordâncias da Francisca

Fatos registrados na sessão de trabalho (pedidos, recusas e condições explícitas). As **razões** são da autora
e ficam nos espaços marcados abaixo.

| # | O que a Francisca decidiu ou exigiu | Evidência |
|---|---|---|
| F1 | Respostas do plan mode: React + Vite, EF Core, remover o projeto original, GitHub Actions e Playwright, sem deploy | Plan mode (seção 1) |
| F2 | Recusou exclusões no Avast e a desativação da verificação HTTPS; pediu diagnóstico e alternativas seguras; aprovou a alternativa A | Sessão; `32ed779` |
| F3 | Parte 4: investigação somente leitura via MCP, sem alterar dados, tabelas ou índices, com aprovação antes de implementar | Sessão; `e117b45` |
| F4 | E2E com 7 cenários definidos por ela, banco real, sem mocks, reproduzíveis | Sessão; `f1db7c2` |
| F5 | Fuso horário: escolheu a alternativa B e exigiu offset explícito; validar o **texto bruto** da query string, e não só o tipo `DateTimeOffset`; manter `dataInicio`/`dataFim` documentados como UTC; rejeitar a mistura das formas; aplicar ao relatório; offset próprio por limite, inclusive no horário de verão; nenhuma conversão dependente do fuso do servidor; preservar os testes existentes | Sessão; `1a16411`…`039dbf1` |
| F6 | Pediu revisão técnica somente leitura de quatro pontos antes de seguir, com atenção especial ao conversor de `DateTime` | Sessão |
| F7 | Autorizou os itens 4a, 2b e 2a; adiou 3a–3f para revisão; decidiu não uniformizar 404 e 415 | Sessão; `7b424c2`, `a813f9e` |
| F8 | Renomeação: commit único, histórico preservado, sem alterar lógica, `ItemPedido.cs` fora dos commits; aceitou `--follow -M30%` para os dois arquivos pequenos | Sessão; `449529d` |
| F9 | Recusou encerrar processos à força quando as pastas estavam bloqueadas; fechou o Visual Studio | Sessão |
| F10 | Exigiu aprovação manual para cada operação, sem troca de branch e sem push | Sessão (todas as etapas) |
| F11 | Publicação: escolheu `main` como branch principal, renomeando a `master` local; autorizou o primeiro push só de `main`, sem `--force` e sem tags; depois, o segundo push (`cb97bb4..f814221`), também sem `--force`; consulta da CI somente leitura, pela API pública | Sessão; `origin/main` em `cb97bb4` e depois em `f814221` |

### Por que (texto da Francisca)

<!-- A preencher pela Francisca: razões das decisões F1–F10. -->

### O que eu ajustei ou discordei do plano ou do código gerado (texto da Francisca)

<!-- A preencher pela Francisca. -->

## 7. Como o Claude Code foi usado no processo SDD

1. **Spec primeiro:** `docs/spec.md` com regras, endpoints, modelo de dados e critérios de aceite, antes de qualquer código.
2. **Plan mode:** exploração somente leitura, perguntas sobre decisões que cabiam à autora e plano aprovado antes da execução.
3. **Execução em commits pequenos**, apoiada pela configuração do projeto:
   - **hook** `PostToolUse`, que roda `dotnet build` / `tsc -b` após cada edição e devolve os erros ao Claude;
   - **skill** `novo-endpoint`, a receita para criar endpoints seguindo o padrão do projeto;
   - **MCP** do SQL Server com login somente leitura, para inspecionar o banco e os planos de execução.
4. **Revisões técnicas somente leitura** antes das mudanças sensíveis, com aprovação explícita da autora.

O detalhe — o que funcionou bem e o que precisou ser corrigido no código gerado — está no README.
