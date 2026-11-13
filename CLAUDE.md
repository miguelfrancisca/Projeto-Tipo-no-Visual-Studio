# CLAUDE.md — Mini Gestão de Pedidos

Convenções do projeto para o Claude Code (e para humanos). Leia antes de alterar código.

## Fonte de verdade

- **Regras de negócio, endpoints, modelo de dados e critérios de aceite:** `docs/spec.md` (RN01–RN14, CA01–CA22). Toda mudança de comportamento começa atualizando a spec.
- **Schema do banco:** `database/*.sql`. **Não existem EF migrations** — o EF Core apenas mapeia as tabelas criadas pelos scripts. Mudou o schema? Altere o script e o mapeamento (`Infrastructure/Persistence/Configurations`) no mesmo commit.
- **Plano de implementação:** `docs/plan.md`.

## Arquitetura (Clean Architecture)

```
src/MiniGestaoPedidos.Domain          Entidades e regras invariantes. Zero dependências.
src/MiniGestaoPedidos.Application     Casos de uso (Services), DTOs, validadores, interfaces de repositório/queries.
src/MiniGestaoPedidos.Infrastructure  EF Core (AppDbContext, mapeamentos), repositórios, queries de leitura.
src/MiniGestaoPedidos.Api             Controllers finos, Program.cs, tratamento global de erros.
frontend/                   React + Vite + TypeScript.
database/                   Scripts SQL (schema, seed, query da Parte 4).
```

Regra de dependência (verificada nos `.csproj`):
`Api → Application, Infrastructure` · `Infrastructure → Application, Domain` · `Application → Domain` · `Domain → nada`.

- **Domain** não conhece EF, HTTP nem DTOs. Invariantes ficam nas entidades e lançam `DomainException`.
- **Application** define interfaces (`IPedidoRepository`, `IPedidoQueries`...) e a **Infrastructure** implementa.
- **Controllers não têm regra de negócio**: recebem a request, chamam um service, devolvem o resultado.
  Exceção: leituras puras sem validação nem regra (ex.: `ICatalogoQueries`) podem ser chamadas direto pelo controller — um service que só repassa a chamada é cerimônia.
- Escrita usa repositórios + `IUnitOfWork`; leitura (listagens/relatórios) usa interfaces `*Queries` que projetam direto para DTO com `AsNoTracking`.

## Tratamento de erros (HTTP)

Erros viram `ProblemDetails` em `Api/Errors/GlobalExceptionHandler.cs`. **Não** faça try/catch em controllers.

| Exceção | Status |
|---|---|
| `FluentValidation.ValidationException` | 400 |
| `NaoEncontradoException` | 404 |
| `ConflitoConcorrenciaException` (a Infrastructure converte `DbUpdateConcurrencyException`) | 409 |
| `DomainException`, `RegraNegocioException` | 422 |

## Nomenclatura

- **Idioma do domínio: português** (`Pedido`, `Cliente`, `BaixarEstoque`, `ValorTotal`). Termos técnicos em inglês quando consagrados (`Repository`, `Service`, `Handler`, `DbContext`).
- Sufixos: `*Service` (caso de uso), `*Repository` (escrita), `*Queries` (leitura), `*Request` / `*Response` (DTOs de entrada/saída), `*Query` (filtros de leitura), `*Validator` (FluentValidation), `*Configuration` (mapeamento EF).
- Interfaces com prefixo `I`. Métodos assíncronos terminam em `Async` e recebem `CancellationToken`.
- Uma classe pública por arquivo; arquivo com o nome da classe. Namespaces = pastas (file-scoped).
- Organização por **feature** dentro de cada camada: `Application/Pedidos/`, `Application/Relatorios/`...
- Testes: `Metodo_Cenario_ResultadoEsperado` (ex.: `CriarAsync_ClienteInexistente_LancaRegraNegocioException`). Arrange/Act/Assert separados por linha em branco.
- Frontend: componentes em `PascalCase.tsx`, hooks `useAlgo.ts`, tipos da API em `src/api/types.ts` espelhando os DTOs.

## Comandos

```bash
dotnet build                                            # build (warnings são erros)
dotnet test tests/MiniGestaoPedidos.Domain.Tests                  # unitários – domínio
dotnet test tests/MiniGestaoPedidos.Application.Tests             # unitários – application
dotnet test tests/MiniGestaoPedidos.Api.IntegrationTests          # integração (precisa do Docker: Testcontainers)
dotnet test                                             # tudo

docker compose up --build                               # stack completa: web :3000, api :5000, sql :1433
docker compose down -v                                  # derruba e apaga o volume do banco

cd frontend && npm run dev                              # frontend em dev (proxy /api → localhost:5000)
cd frontend && npx tsc --noEmit                         # type-check
cd frontend && npx playwright test                      # E2E (stack do compose rodando)
```

## Regras para o Claude

- Siga TDD para regras de negócio: escreva/ajuste o teste unitário antes da implementação.
- Para criar um endpoint novo, use a skill `/novo-endpoint`.
- O hook `PostToolUse` roda `dotnet build` após editar `.cs`/`.csproj` e `tsc` após editar `.ts`/`.tsx`. Se ele falhar, **corrija o erro antes de seguir**.
- Use o MCP do SQL Server (`.mcp.json`) para inspecionar schema/dados e planos de execução em vez de supor.
- Não use FluentAssertions (licença comercial a partir da v8): use `Assert` do xUnit.
- Não adicione pacotes NuGet sem registrar a versão em `Directory.Packages.props`.
- Commits pequenos, Conventional Commits (`feat(domain): ...`, `test(api): ...`). Nunca reescreva histórico.
- Não exponha segredos: a senha SA do compose é **apenas de desenvolvimento**.
