---
name: novo-endpoint
description: Cria um novo endpoint REST seguindo o padrão do projeto (spec → DTOs/validator → service → repository/queries → controller → testes unitários e de integração). Use quando o usuário pedir um endpoint, rota, recurso ou operação nova na API.
argument-hint: "<MÉTODO> <rota> — <o que faz> (ex.: GET /produtos/{id} — detalhe de um produto)"
---

# Skill: novo endpoint

Endpoint pedido: **$ARGUMENTS**

Siga as etapas **na ordem**. Não pule a spec nem os testes. Consulte `CLAUDE.md` para nomenclatura.
Use como referência o fluxo de pedidos, que já segue o padrão:
`Application/Pedidos/*`, `Infrastructure/Persistence/Repositories/*`, `Infrastructure/Persistence/Queries/*`, `Api/Controllers/PedidosController.cs`.

## 0. Entender e confirmar

1. Leia `docs/spec.md` e verifique se o endpoint já está especificado.
2. Classifique: é **escrita** (muda estado → Repository + UnitOfWork) ou **leitura** (consulta → `*Queries` com projeção `AsNoTracking`)?
3. Se faltar informação (formato da resposta, códigos de erro, regras), pergunte antes de codar.

## 1. Spec primeiro

Atualize `docs/spec.md`:
- regra(s) nova(s) na tabela de regras (próximo `RNxx`);
- seção do endpoint em §5 com request, response de exemplo e códigos de erro;
- critério(s) de aceite (`CAxx`) no formato *Dado/Quando/Então*.

## 2. Application (contratos + caso de uso)

Em `src/MiniGestaoPedidos.Application/<Feature>/`:
- `<Acao>Request.cs` / `<Algo>Response.cs` como `record` (ou `<Algo>Query.cs` para filtros de leitura);
- `<Acao>RequestValidator.cs` (FluentValidation) — tudo que é **formato** (obrigatório, faixas, paginação) → 400;
- interface `I<Feature>Service` + `<Feature>Service` com o caso de uso. Regras que dependem de dados (existência, estoque) lançam `RegraNegocioException` (422) ou `NaoEncontradoException` (404);
- se precisar de acesso a dados novo, declare o método em `I<Entidade>Repository` (escrita) ou `I<Feature>Queries` (leitura) — **só a interface**;
- registre o service em `Application/DependencyInjection.cs`.

Invariantes da entidade (ex.: quantidade > 0) ficam **no Domain**, lançando `DomainException`.

## 3. Testes unitários (antes de implementar a Infrastructure)

Em `tests/MiniGestaoPedidos.Application.Tests/<Feature>/` (e `tests/MiniGestaoPedidos.Domain.Tests/` se mexeu em entidade):
- um teste por regra/caminho de erro + o caminho feliz;
- validator: um teste por regra;
- mocks com **NSubstitute**, asserções com **`Assert` do xUnit**;
- nome: `Metodo_Cenario_ResultadoEsperado`.

Rode `dotnet test tests/MiniGestaoPedidos.Application.Tests` e confirme que passam.

## 4. Infrastructure

- Implemente o método em `Infrastructure/Persistence/Repositories/` ou `Infrastructure/Persistence/Queries/`.
- Leitura: `AsNoTracking()` + `Select` direto para o DTO; filtros por data como **intervalo semiaberto** (`>= inicio && < fim.AddDays(1)`) — nunca função sobre a coluna.
- Se precisar de coluna/índice novo: altere `database/01-schema.sql` **e** a `*Configuration` do EF no mesmo commit. Não crie migrations.
- Use o **MCP do SQL Server** para conferir o plano de execução da query nova contra o banco do compose.
- Registre a implementação em `Infrastructure/DependencyInjection.cs`.

## 5. Controller

Em `src/MiniGestaoPedidos.Api/Controllers/<Feature>Controller.cs`:
- `[ApiController]`, `[Route("<rota>")]`, ação `async` com `CancellationToken`;
- **sem regra de negócio e sem try/catch** — erros são tratados pelo `GlobalExceptionHandler`;
- `[ProducesResponseType]` para cada status documentado na spec;
- criação devolve `CreatedAtAction(...)` (201 + `Location`).

## 6. Teste de integração

Em `tests/MiniGestaoPedidos.Api.IntegrationTests/<Feature>EndpointsTests.cs`, usando a fixture existente (Testcontainers SQL Server):
- um teste por critério de aceite (`CAxx`) do endpoint;
- valide status HTTP, corpo **e** o estado do banco quando houver escrita.

Rode `dotnet test tests/MiniGestaoPedidos.Api.IntegrationTests` (Docker precisa estar rodando).

## 7. Frontend (se o endpoint for consumido pela UI)

- tipos em `frontend/src/api/types.ts` espelhando os DTOs;
- função em `frontend/src/api/client.ts`.

## 8. Fechamento

- `dotnet build` com 0 avisos, `dotnet test` verde.
- Commits pequenos, na ordem: `docs(spec)`, `feat(application)`, `test(application)`, `feat(infrastructure)`, `feat(api)`, `test(api)`.
- Resuma ao usuário: rota, arquivos criados, regras cobertas e testes adicionados.
