# Mini Gestão de Pedidos

Cadastro e consulta de pedidos de clientes: criação de pedidos com validação de cliente e de estoque,
listagem com filtros e paginação, e relatório dos 5 clientes de maior faturamento num período.

| Camada | Tecnologia |
|---|---|
| Backend | .NET 8 (SDK fixado em 8.0.408 via `global.json`), ASP.NET Core, EF Core 8, FluentValidation |
| Banco | SQL Server 2022 (scripts em `database/`, sem migrations) |
| Frontend | React 19 + TypeScript + Vite 6, servido por nginx |
| Testes | xUnit, NSubstitute, Testcontainers, Respawn, Vitest, Playwright |
| Infra | Docker Compose |

- **Especificação** (regras RN01–RN14, endpoints, critérios de aceite): [`docs/spec.md`](docs/spec.md)
- **Plano de implementação**, divergências e decisões: [`docs/plan.md`](docs/plan.md)

---

## 2. Como rodar com Docker

### Pré-requisitos
- Docker Desktop em execução.

### Subir a aplicação completa (um comando)
```powershell
docker compose up --build
```

| Serviço | Endereço | O que é |
|---|---|---|
| `web` | http://localhost:3000 | Frontend (nginx), com proxy de `/api` para a API |
| `api` | http://localhost:5000 · Swagger em http://localhost:5000/swagger | API .NET |
| `api` | http://localhost:5000/health | Health check (inclui o banco) |
| `db` | `localhost,1433` | SQL Server (usuário `sa`, senha de desenvolvimento) |

Ordem de inicialização (automática): `db` saudável → `db-init` executa `database/init/*.sql`
(schema → dados de exemplo → login de leitura do MCP) e termina → `api` saudável → `web`.
Os scripts são idempotentes: subir de novo não duplica dados.

### Parar
```powershell
docker compose down        # para os containers e MANTÉM os dados do banco
```

> ⚠️ **`docker compose down -v` apaga o volume `pedidos_sqldata` e todos os dados do banco**,
> inclusive pedidos criados pela interface. No próximo `up`, schema e dados de exemplo são recriados do zero.
> Use `-v` apenas quando quiser começar do zero.

### Senhas
As senhas do `docker-compose.yml` (`MSSQL_SA_PASSWORD`, `MCP_DB_PASSWORD`) têm valores padrão **apenas
para desenvolvimento**, para o projeto subir sem passos manuais. Podem ser sobrescritas por variáveis de
ambiente ou arquivo `.env`.

### Rede com inspeção TLS (antivírus/proxy corporativo)
Se o build falhar com erro de certificado no `dotnet restore` ou no `npm ci` (por exemplo, com o Avast
inspecionando HTTPS), passe o certificado raiz como BuildKit secret. Ele é usado **só** durante o restore
e o `npm ci` e **não fica nas imagens**:
```powershell
$env:EXTRA_CA_CERT = "C:\ProgramData\Avast Software\Avast\wscert.pem"
docker compose -f docker-compose.yml -f docker-compose.ca.yml up --build
```
Sem inspeção TLS, use apenas `docker compose up --build`.

---

## 3. Como testar — passo a passo

### Resumo das suítes

| Suíte | Testes | Requer Docker | Comando |
|---|---|---|---|
| Domínio (`MiniGestaoPedidos.Domain.Tests`) | 17 | não | `dotnet test tests/MiniGestaoPedidos.Domain.Tests` |
| Application (`MiniGestaoPedidos.Application.Tests`) | 56 | não | `dotnet test tests/MiniGestaoPedidos.Application.Tests` |
| Infrastructure (`MiniGestaoPedidos.Infrastructure.Tests`) | 9 | não | `dotnet test tests/MiniGestaoPedidos.Infrastructure.Tests` |
| Integração (`MiniGestaoPedidos.Api.IntegrationTests`) | 63 | **sim** (Testcontainers) | `dotnet test tests/MiniGestaoPedidos.Api.IntegrationTests` |
| Frontend — Vitest | 11 | não | `npm test` (em `frontend/`) |
| E2E — Playwright | 13 | **sim** (stack do compose rodando) | `npx playwright test` (em `frontend/`) |
| **Total** | **169** | | |

O total conta cada teste **uma vez** (testes distintos). Execuções repetidas, como `--repeat-each=3` no
Playwright, não entram nesse número.

### Passo 1 — Conferir os pré-requisitos (PowerShell)
```powershell
dotnet --version     # 8.0.408 ou superior na linha 8.0.4xx (global.json)
docker version       # Docker Desktop em execução
node --version       # Node 22 (o mesmo da imagem do frontend)
```

> Dica (Windows PowerShell 5.1): se acentos aparecerem trocados na saída (ex.: "Jo�o"), é só a
> codificação do console — os dados estão corretos. Para exibir corretamente:
> `[Console]::OutputEncoding = [Text.Encoding]::UTF8`

### Passo 2 — Build do backend
```powershell
dotnet build         # usa MiniGestaoPedidos.sln; avisos são tratados como erros
```
Esperado: `0 Aviso(s)` / `0 Erro(s)`.

### Passo 3 — Testes unitários do backend (sem Docker)
```powershell
dotnet test tests/MiniGestaoPedidos.Domain.Tests
dotnet test tests/MiniGestaoPedidos.Application.Tests
dotnet test tests/MiniGestaoPedidos.Infrastructure.Tests
```

### Passo 4 — Testes de integração (SQL Server real via Testcontainers)
Com o Docker Desktop em execução (não é preciso subir o compose: cada execução cria um container próprio,
roda o mesmo `database/init/01-schema.sql` e o descarta no fim):
```powershell
dotnet test tests/MiniGestaoPedidos.Api.IntegrationTests
```
Para rodar **todas** as suítes .NET de uma vez: `dotnet test`.

### Passo 5 — Frontend: tipos, lint e testes unitários
```powershell
cd frontend
npm ci
npx tsc -b           # verificação de tipos (use -b; ver "Limitações", P1)
npm run lint
npm test             # Vitest
cd ..
```

### Passo 6 — Subir a stack e conferir que está saudável
```powershell
docker compose up -d --build
Invoke-RestMethod http://localhost:5000/health          # esperado: Healthy
```

### Passo 7 — Teste manual pela interface (http://localhost:3000)
1. A listagem abre com 10 pedidos e a paginação mostra o total.
2. **Filtro por cliente:** escolha um cliente e clique em *Filtrar* — todas as linhas são desse cliente.
3. **Filtro por período:** preencha *De* e *Até* (ex.: 10/03/2025 a 10/03/2025) — todas as linhas mostram
   essa data no fuso do seu navegador.
4. **Paginação:** *Próxima →* / *← Anterior*.
5. **Novo pedido:** *+ Novo pedido* → cliente, produto, quantidade → *Salvar pedido*. O pedido aparece no
   topo da lista, destacado.
6. **Erro de estoque:** no modal, escolha um produto com "0 em estoque" e salve — a mensagem da API aparece
   e o formulário continua preenchido.

### Passo 8 — Teste manual da API (PowerShell)
```powershell
# Listagem com filtro de período no fuso de São Paulo (offset explícito)
Invoke-RestMethod "http://localhost:5000/pedidos?inicio=2025-03-10T00:00:00-03:00&fim=2025-03-11T00:00:00-03:00"

# Top 5 clientes de 2026
Invoke-RestMethod "http://localhost:5000/relatorios/top-clientes?dataInicio=2026-01-01&dataFim=2026-12-31"

# Criar um pedido (ATENÇÃO: grava no banco local e baixa estoque)
$pedido = @{ clienteId = 50; itens = @(@{ produtoId = 1; quantidade = 2 }) } | ConvertTo-Json -Depth 3
Invoke-RestMethod -Method Post -Uri http://localhost:5000/pedidos -ContentType "application/json" -Body $pedido

# Ver um erro de validação (400: valor de data sem offset é rejeitado).
# Use curl.exe: mostra status, Content-Type e corpo mesmo em erro.
curl.exe -s -i "http://localhost:5000/pedidos?inicio=2025-03-10T00:00:00"
```
> No Windows PowerShell 5.1, `curl` (sem `.exe`) é um alias de `Invoke-WebRequest`; use `curl.exe`.
> Nessa versão, `Invoke-RestMethod` lança exceção em respostas 4xx e `$_.ErrorDetails.Message` pode vir
> vazio — por isso o exemplo de erro usa `curl.exe`.

### Passo 9 — Testes E2E (Playwright) contra a stack real
Com a stack do passo 6 rodando:
```powershell
cd frontend
npx playwright install chromium     # apenas na primeira vez
npx playwright test
cd ..
```
Os testes usam o banco real (sem mocks) e são reproduzíveis: o seed é determinístico e as verificações
de filtro e paginação usam um período passado (março/2025), que pedidos novos — sempre gravados com a
data atual — nunca alteram.

### Passo 10 — Encerrar
```powershell
docker compose down        # mantém os dados
# docker compose down -v   # ⚠️ apaga o banco (ver seção 2)
```

---

## 4. Desenvolvimento local

Fora do Docker, com o SQL Server do compose:
```powershell
docker compose up -d db db-init                                        # só o banco
dotnet run --project src/MiniGestaoPedidos.Api --launch-profile http   # API em http://localhost:5000
cd frontend; npm run dev                                               # http://localhost:5173 (proxy /api → :5000)
```
A connection string de desenvolvimento está em `src/MiniGestaoPedidos.Api/appsettings.Development.json`.

> ⚠️ A API local (Visual Studio ou `dotnet run`) e o serviço `api` do Docker Compose usam a **mesma porta
> 5000**. Se a stack completa estiver ativa, pare apenas a API do compose antes de iniciar a local:
> ```powershell
> docker compose stop api
> ```
> Ao iniciar a API do compose de novo (`docker compose start api` ou `up`), o `db-init` roda outra vez
> como dependência. Isso é seguro: os scripts são idempotentes e os dados existentes não são alterados.

---

## 5. Arquitetura

Clean Architecture, com a regra de dependência verificada nos `.csproj`:

```
MiniGestaoPedidos.Api ──► Application ──► Domain
        │                      ▲
        └──► Infrastructure ───┘──► Domain
```

| Projeto | Responsabilidade |
|---|---|
| `src/MiniGestaoPedidos.Domain` | Entidades e invariantes (`Pedido.Criar`, `Produto.BaixarEstoque`). Sem dependências. |
| `src/MiniGestaoPedidos.Application` | Casos de uso (`*Service`), DTOs, validadores, interfaces de repositório e de consultas. |
| `src/MiniGestaoPedidos.Infrastructure` | EF Core (mapeamentos, repositórios, consultas de leitura), Unit of Work. |
| `src/MiniGestaoPedidos.Api` | Controllers finos, tratamento global de erros, binder de datas com offset. |
| `frontend/` | React + Vite. |
| `database/` | Scripts SQL (schema, seed, login do MCP, query da Parte 4). |

### Endpoints

| Método e rota | Descrição | Status |
|---|---|---|
| `POST /pedidos` | Cria pedido com itens (valida cliente e estoque, baixa estoque) | 201, 400, 409, 422 |
| `GET /pedidos` | Lista com filtros (`clienteId`, período) e paginação | 200, 400 |
| `GET /pedidos/{id}` | Detalhe do pedido | 200, 404 |
| `GET /relatorios/top-clientes` | 5 clientes com maior valor no período | 200, 400 |
| `GET /clientes`, `GET /produtos` | Listas de apoio ao frontend | 200 |
| `GET /health` | Saúde da API e do banco | 200 |

Erros de regra de negócio (404, 409, 422) seguem RFC 7807 (`application/problem+json`). O **400** tem um
formato único para erros de validação e de binding, definido em `docs/spec.md` §5 (CA23):
`application/problem+json` com `type`, `title`, `status`, `detail`, `errors` (chaves públicas em camelCase;
`corpo` para erros do corpo inteiro) e `traceId`, com mensagens em português. O 404 de rota inexistente e o
415 mantêm o formato gerado pelo próprio ASP.NET.

### Filtros de período (RN14)

O período é interpretado **no fuso de quem consulta**. A API aceita **uma** de duas formas:

| Forma | Parâmetros | Semântica |
|---|---|---|
| **Instantes com offset** (usada pelo frontend) | `inicio` (inclusivo), `fim` (**exclusivo**) | ISO 8601 com offset **obrigatório**: `Z` ou `±hh:mm`. Ex.: dia 10/03/2025 em São Paulo = `inicio=2025-03-10T00:00:00-03:00&fim=2025-03-11T00:00:00-03:00` |
| **Dias em UTC** (compatibilidade) | `dataInicio`, `dataFim` (`yyyy-MM-dd`, inclusivos) | Dias inteiros **em UTC**: `[dataInicio 00:00Z, dataFim+1 00:00Z)` |

Regras:
- **Offset explícito:** `inicio`/`fim` sem offset (ex.: `2025-03-10T00:00:00`) ou só com a data são
  rejeitados com **400**. A validação é feita sobre o **texto recebido** na query string, antes de qualquer
  conversão — a API nunca assume o fuso do servidor.
- **Não misturar:** enviar `inicio`/`fim` junto com `dataInicio`/`dataFim` retorna **400**.
- `fim` deve ser **posterior** a `inicio` (o fim é exclusivo); só um dos limites também é aceito na listagem.
- Na URL, offsets positivos precisam ser codificados: `+05:30` → `%2B05:30`.
- A conversão para UTC acontece na Application (`Periodo.ParaUtc`), **antes** da consulta ao banco.
- **A mesma regra vale para o relatório** `GET /relatorios/top-clientes`, que exige o período completo
  em uma das duas formas.
- No frontend, o dia escolhido vira a meia-noite **local** do primeiro dia e a meia-noite local do dia
  **seguinte** ao último, cada limite com o seu próprio offset (correto inclusive em dias com mudança de
  horário de verão).

---

## 6. Principais decisões técnicas

| Decisão | Motivo |
|---|---|
| Scripts SQL como fonte de verdade do schema; EF Core só mapeia (sem migrations) | O teste pede scripts; uma única fonte; os testes de integração executam o mesmo script |
| Exceções tipadas → ProblemDetails num `IExceptionHandler`; controllers sem try/catch | Pouco código, contrato de erro centralizado |
| FluentValidation na Application | Validadores testáveis isoladamente |
| `rowversion` em `Produtos` + `CHECK (Estoque >= 0)` + uma transação por pedido (RN10) | Sem overselling e sem lock pessimista; conflito vira 409 |
| `ValorTotal` desnormalizado em `Pedidos` | Listagem e relatório não precisam ler `ItensPedido` |
| `TimeProvider` injetado; datas gravadas em UTC truncadas a milissegundos | Datas determinísticas nos testes e iguais à precisão da coluna `datetime2(3)` |
| Período com offset explícito, convertido para UTC na Application (RN14) | Filtro coerente com a hora exibida, independente do fuso do servidor |
| Testcontainers + Respawn | SQL Server real e descartável nos testes de integração |
| nginx fazendo proxy de `/api` | Mesma origem, sem CORS; mesma URL relativa em dev e em produção |
| `global.json`, `TreatWarningsAsErrors`, gestão central de pacotes | Build reproduzível e sem avisos |

---

## 7. Banco de dados

| Tabela | Destaques |
|---|---|
| `Clientes` | `Email` único |
| `Produtos` | `Sku` único; `Preco >= 0`; `Estoque >= 0`; `RowVersion` (concorrência) |
| `Pedidos` | FK para `Clientes`; `DataPedido datetime2(3)` em UTC; `ValorTotal >= 0`; índices `(ClienteId, DataPedido) INCLUDE (ValorTotal)` e `(DataPedido) INCLUDE (ClienteId, ValorTotal)` |
| `ItensPedido` | FK para `Pedidos` (cascade) e `Produtos`; `Quantidade > 0`; índice único `(PedidoId, ProdutoId) INCLUDE (Quantidade, PrecoUnitario)` |

**Dados de exemplo** (`database/init/02-seed.sql`): determinísticos (sem `RAND`/`NEWID`), mesmos dados em
toda execução — 200 clientes (incluindo **homônimos de propósito**, clientes sem compras em 2026 e sem
nenhum pedido), 50 produtos (alguns com estoque baixo ou zerado), 20.000 pedidos entre 01/2025 e
11/2026 e 50.000 itens.

---

## 8. Parte 4 — Debug da query de ranking de 2026

Query recebida (deveria retornar o ranking dos clientes que mais compraram em 2026):

```sql
SELECT c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
LEFT JOIN Pedidos p ON p.ClienteId = c.Id
LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
WHERE YEAR(p.DataPedido) = 2026
GROUP BY c.Nome
ORDER BY Total
```

Versão corrigida e conferências: [`database/03-query-ranking-clientes-2026.sql`](database/03-query-ranking-clientes-2026.sql).

### Como investiguei

A investigação foi feita pelo MCP com o login de privilégio mínimo `claude_mcp` (`db_datareader`, `SHOWPLAN` e permissões de visualização de estado, conforme a seção 9). Os testes realizados demonstraram que operações de escrita foram negadas pelo SQL Server.

1. **Estrutura:** contagem de linhas, índices (chave + `INCLUDE`) e FKs via `sys.indexes`, `sys.index_columns`, `sys.foreign_keys`.
2. **Executar a original** e olhar o resultado: 160 linhas, em ordem crescente.
3. **Hipóteses provadas com consultas de conferência** (abaixo).
4. **Planos e métricas reais:** cada versão executada com um comentário-marcador; leituras lógicas, CPU e memória concedida lidas de `sys.dm_exec_query_stats`, e operadores do plano extraídos do XML de `sys.dm_exec_text_query_plan` (scan × seek, índice usado, joins).
5. **Índices faltantes:** `sys.dm_db_missing_index_details` — vazio.

### O que estava errado

| # | Problema | Tipo | Evidência |
|---|---|---|---|
| 1 | `GROUP BY c.Nome` soma **clientes homônimos** como se fossem um só | Resultado errado | O seed tem 40 nomes repetidos. "Ana Souza" saía com **2.333.623,90** = Id 1 (2.257.766,40) + Id 161 (75.857,50). 180 clientes compraram em 2026, mas a query devolvia **160 linhas**. |
| 2 | `ORDER BY Total` é **crescente** | Resultado errado | O 1º do "ranking" era quem **menos** comprou (Otávio Ferreira, 43 mil). |
| 3 | `LEFT JOIN` com filtro sobre `p` no `WHERE` | Lógica enganosa | `YEAR(NULL) = 2026` é falso: as linhas sem pedido são descartadas e o LEFT JOIN vira um INNER JOIN implícito. Os 20 clientes sem compra em 2026 não aparecem — o que é o comportamento certo para um ranking, mas o código diz outra coisa. |
| 4 | `YEAR(p.DataPedido) = 2026` — função sobre a coluna | Desempenho | Predicado **não sargable**: o plano lê os 20.000 pedidos e filtra depois (`Filter`); nunca faz seek em `IX_Pedidos_DataPedido`. |
| 5 | Sem `Id` na saída e sem desempate | Qualidade | Impossível distinguir homônimos; empates saem em ordem não determinística. |

### Correção

```sql
WITH Totais AS (
    SELECT p.ClienteId, SUM(i.Quantidade * i.PrecoUnitario) AS Total
    FROM Pedidos p
    JOIN ItensPedido i ON i.PedidoId = p.Id
    WHERE p.DataPedido >= '2026-01-01' AND p.DataPedido < '2027-01-01'
    GROUP BY p.ClienteId
)
SELECT RANK() OVER (ORDER BY t.Total DESC) AS Posicao, c.Id, c.Nome, t.Total
FROM Totais t
JOIN Clientes c ON c.Id = t.ClienteId
ORDER BY t.Total DESC, c.Id;
```

- agrupa por **`ClienteId`** (homônimos continuam distintos) e devolve o `Id`;
- `INNER JOIN` explícito: o ranking é de quem comprou;
- intervalo **semiaberto** `[2026-01-01, 2027-01-01)`: sargable e inclui 31/12 até 23:59:59.999;
- agrega **antes** de juntar com `Clientes` (o join recebe 180 linhas, não 50 mil);
- `ORDER BY Total DESC` com desempate por `Id`.

O top 5 resultante é idêntico ao do endpoint `GET /relatorios/top-clientes`
(Ana Souza Id 1 · Bruno Souza · Carla Souza · Diego Souza · Felipe Souza).

Há também uma **variante que soma `Pedidos.ValorTotal`** (coluna desnormalizada, RN09) e não lê `ItensPedido`.
Conferi que **0 pedidos** têm `ValorTotal` diferente da soma dos itens; é a abordagem usada pela API.

### Comparação (plan cache, última execução, base do seed: 20 mil pedidos / 50 mil itens)

| Versão | Linhas | Leituras lógicas | CPU | Memória concedida | Acesso a dados |
|---|---|---|---|---|---|
| Original | 160 ❌ | 295 | 41,3 ms | 7.440 KB | Scan `PK_ItensPedido` (50 mil) + scan `Pedidos` (20 mil) + `Filter` + 2 Hash Match |
| Corrigida (itens) | 180 ✅ | 368 | 13,7 ms | 2.224 KB | Scan `PK_Pedidos` com predicado + scan no índice estreito `UX_ItensPedido_PedidoId_ProdutoId` + Merge Join |
| Corrigida (`ValorTotal`) | 180 ✅ | **82** | **2,8 ms** | 1.024 KB | Só `IX_Pedidos_ClienteId_DataPedido` (cobre a query) |
| Março/2026 com `YEAR`/`MONTH` | — | 363 | 7,7 ms | — | Scan em `PK_Pedidos` |
| Março/2026 com intervalo | — | 285 | 6,0 ms | — | **Index Seek** em `IX_Pedidos_DataPedido` |

**Leitura crítica dos números.** A versão corrigida por itens faz *mais* leituras lógicas que a original (368 × 295), e
para o ano inteiro o otimizador escolheu scan, não seek. Investiguei antes de concluir: **2026 concentra 45% dos pedidos**
(9.003 de 20.000), e com essa seletividade varrer a tabela é mais barato que seek + lookups — escolha correta do otimizador.
O ganho no ano inteiro está em **CPU (3×)** e **memória (3,3×)**. O benefício do predicado sargable aparece em períodos
seletivos (linhas de março: scan → seek) e cresce com o histórico: numa base com vários anos, "2026" seria uma fração
pequena e o seek passaria a valer também no ranking anual.

### Índices

Nenhum índice novo foi necessário — o DMV de índices faltantes não sugere nada. Os índices que a query precisa já foram
modelados no `database/init/01-schema.sql` pensando nesta consulta:

- `IX_Pedidos_DataPedido (DataPedido) INCLUDE (ClienteId, ValorTotal)` — filtro por período;
- `IX_Pedidos_ClienteId_DataPedido (ClienteId, DataPedido) INCLUDE (ValorTotal)` — cobre a variante por `ValorTotal`;
- `UX_ItensPedido_PedidoId_ProdutoId (PedidoId, ProdutoId) INCLUDE (Quantidade, PrecoUnitario)` — soma por pedido sem tocar a tabela base.

O problema da original não era falta de índice: era o `YEAR()` sobre a coluna, que impedia o uso deles.

---

## 9. Configuração AI First

| Item | Arquivo | Função |
|---|---|---|
| Regras | `CLAUDE.md` | Arquitetura, nomenclatura, tratamento de erros, comandos e regras para o Claude |
| Hook | `.claude/settings.json` + `.claude/hooks/post-edit-check.mjs` | Após cada edição: `dotnet build` do projeto (`.cs`/`.csproj`) ou `tsc -b` (`.ts`/`.tsx`); em caso de erro devolve-o ao Claude para correção imediata |
| Skill | `.claude/skills/novo-endpoint/SKILL.md` | Receita para criar endpoint seguindo o padrão (spec → contratos → testes → infra → controller → integração) |
| MCP | `.mcp.json` | Servidor `sqlserver-pedidos` (`mssql-mcp@3.0.0`) conectado ao SQL Server do compose |

### MCP do SQL Server

**Privilégios do login `claude_mcp`** (`database/init/03-mcp-readonly-login.sql`):
- `db_datareader` no `PedidosDb` — leitura de todas as tabelas;
- `SHOWPLAN` — planos de execução;
- `VIEW DATABASE STATE` e `VIEW SERVER STATE` — DMVs (estatísticas do plan cache, índices faltantes).

O servidor MCP aceita SQL arbitrário (necessário para planos e estatísticas), por isso se conecta com
esse login de privilégio mínimo. O MCP, utilizando o login de privilégio mínimo, permitiu investigar o
banco e os planos de execução. Os testes realizados demonstraram que operações de escrita foram negadas
pelo SQL Server: um `SELECT` funcionou, enquanto um `DELETE` (via MCP) e um `UPDATE` (via `sqlcmd`)
foram negados (`The DELETE permission was denied…`).

> Isso é **evidência dos testes realizados, não uma garantia absoluta de segurança**. O login continua
> podendo ler **todos** os dados (inclusive e-mails), ver informações do servidor via `VIEW SERVER STATE`
> e executar consultas pesadas. A senha padrão está no repositório e é **apenas de desenvolvimento**.
> As permissões valem enquanto a configuração do login for mantida.

**Como o MCP foi usado no desenvolvimento:**
- inspecionar schema, índices e chaves estrangeiras e conferir o seed;
- Parte 4: executar a query original, provar o bug dos homônimos, medir leituras/CPU/memória no plan cache
  e extrair os operadores do plano (scan × seek);
- levantar os valores fixos dos testes E2E (31 pedidos em 10/03/2025, 58 do cliente 50 em 2025, pedido
  #5808) e os pedidos próximos da meia-noite UTC usados para provar o bug de fuso (RN14).

Limitação: ver P2 em **Limitações e pendências** (comando só para Windows).

---

## 10. Como usei o Claude Code

**Fluxo:** spec antes do código → plan mode com perguntas sobre decisões que eram minhas → execução em
commits pequenos com hook, MCP e testes → revisões técnicas somente leitura antes das mudanças sensíveis.
Detalhes e evidências em [`docs/plan.md`](docs/plan.md).

### O que funcionou bem
- O **hook** apontou erros de compilação logo após cada edição (inclusive na fase "red" do TDD e um `using`
  faltando), e o `tsc` acusou a ausência dos tipos do Node no `vite.config.ts`.
- O MCP, utilizando o login de privilégio mínimo, permitiu investigar o banco e os planos de execução.
  Os testes realizados demonstraram que operações de escrita foram negadas pelo SQL Server.
- **Testes contra banco real** (Testcontainers e compose) cobriram o que mocks não cobririam: o teste de
  concorrência RN10 com SQL Server real, o arredondamento `DATETIME` na borda do CA11 (um problema real,
  encontrado dessa forma) e a reexecução do `db-init` sobre a base temporária.
- **Revisões somente leitura** antes das mudanças encontraram problemas antes de serem gravados
  (ex.: regressão do contrato 400, comando do passo 8 que falhava no PowerShell 5.1).

### Problemas encontrados no código gerado
- `DataPedido` com precisão maior que a coluna `datetime2(3)`: a resposta do POST não batia com o GET
  (encontrado com curl; corrigido com truncagem a milissegundos).
- CSS do modal estourando a largura (encontrado em screenshots).
- Filtro de período em dias UTC enquanto a tela mostra a hora local (identificado ao escrever os testes E2E;
  os testes por fuso foram criados depois para provar o problema e a correção; deu origem à RN14).
- Conversor do EF com `ToUniversalTime()`, que dependeria do fuso do servidor (encontrado durante a
  implementação da RN14, ao procurar conversões dependentes do fuso do servidor).
- Regressão do contrato HTTP 400 no commit `2d469ff` (encontrada em revisão somente leitura; **corrigida no
  commit `dc9643c`**, ver seção 11).

### Problemas de testes e ferramentas
- Parte dos erros apontados pelo hook foi transitória: foram provocados pela ordem de edição (ex.: usar um
  import antes de acrescentá-lo, ou escrever o teste antes da classe na fase "red") e não representavam
  defeitos reais do código.
- O hook pedia "correções" quando a DLL estava travada por um `dotnet run` em execução.
- O helper dos testes de integração gravava datas como `DATETIME` (23:59:59.999 virava o dia seguinte).
- O Avast intercepta HTTPS e os containers não confiavam na raiz dele (resolvido sem reduzir a proteção).
- O `toBeDisabled()` do Playwright 1.52 não reconhece `<option disabled>`; o teste 7b passou a verificar o
  atributo `disabled` diretamente.
- O Visual Studio e o C# Dev Kit travavam as pastas durante a renomeação dos projetos.
- Um teste E2E lia as linhas antes da resposta filtrada chegar (instável com a API "fria").
- Uma primeira tentativa do V3b, feita manualmente antes da execução autorizada da validação (V0–V6),
  falhou porque `EXTRA_CA_CERT` não estava definido. Depois de corrigida essa configuração, as validações
  prosseguiram.

### Erros cometidos durante as verificações e a documentação
- Um caminho `/app` foi convertido pelo Git Bash num caminho do Windows.
- Um `docker inspect` leu o container antigo em vez da imagem nova.
- Uma contagem de CRLF com `grep` estava errada: o arquivo utilizava LF, e não CRLF. A interpretação
  incorreta levou a uma conclusão equivocada sobre as terminações de linha.
- O `CLAUDE.md` documentava um comando de verificação de tipos que não realizava a verificação esperada (P1,
  corrigido em `2d07a53`).
- O número de vulnerabilidades do npm foi informado desatualizado (7) antes de ser conferido (12).
- O comando de erro do passo 8 (`$_.ErrorDetails.Message`) estava errado na proposta do README: no
  PowerShell 5.1 a mensagem veio vazia. Só foi corrigido (para `curl.exe -s -i`) depois de executado na
  validação V3.
- Falhas das próprias ferramentas de verificação: o Node não encontrou um caminho `/tmp` do Git Bash; o
  script PowerShell salvo sem BOM corrompeu os textos acentuados e não chegou a executar; uma verificação
  com `grep -P` era inválida; e um `git diff --no-index` com `<(...)` não funcionou no Windows.
- Na correção do contrato 400, a proposta de acrescentar `application/problem+json` à resposta não teria
  efeito: no cenário testado, o `ObjectResult` não preservava o Content-Type desejado devido ao
  `[Produces("application/json")]` configurado nos controllers. Os testes do contrato, escritos antes da
  correção, detectaram a falha; a resposta passou a ser um `JsonResult`.
- Na mesma correção, uma linha errada (`AllowTrailingCommas = false`, sem relação com o pedido) foi escrita
  no `Program.cs` e substituída pela linha aprovada (`AllowInputFormatterExceptionMessages = false`) antes de
  qualquer execução de testes.

### Skill `novo-endpoint`
A skill foi criada no commit `d0a9ba7`, **mas não foi utilizada nesta sessão**: os endpoints foram
implementados diretamente.

### Avaliação pessoal

<!-- A preencher pela Francisca. -->

---

## 11. Limitações e pendências

- **Contrato HTTP 400 — corrigido** (commit `dc9643c`; `docs/spec.md` §5 e CA23). A regressão introduzida
  no `2d469ff` foi corrigida. Os cenários cobrem erros de validação (FluentValidation) e de binding (JSON
  malformado, tipo errado, corpo vazio ou `null`, parâmetro de query inválido, data sem offset). Os cenários
  de HTTP 400 previstos no contrato e cobertos pelos testes seguem o mesmo formato:
  - `Content-Type: application/problem+json`, com exatamente `type`, `title`, `status`, `detail`, `errors`
    e `traceId`;
  - chaves de `errors` com o nome público do campo em camelCase (`itens[0].quantidade`, `clienteId`,
    `inicio`); erros sobre o corpo inteiro usam a chave `corpo`;
  - mensagens em português. Nos cenários testados, as chaves e mensagens não expõem nomes de parâmetros C#,
    nomes de tipos .NET ou posições do leitor de JSON.

  Os formatos do **404** e do **415** foram **preservados** (decisão da autora) e estão fixados por testes de
  caracterização: o 404 de rota inexistente e o 415 vêm do próprio ASP.NET; o 404 de recurso inexistente
  (ex.: `GET /pedidos/{id}`) vem do `GlobalExceptionHandler`, com título e detalhe em português.
  Validação após a correção: .NET **145/145** (incluindo 15 testes novos de contrato), Vitest **11/11** e
  Playwright E2E **39/39** execuções (13 testes × 3, sem retries), contra a imagem reconstruída da API.
- **P1 — `CLAUDE.md` — corrigido** (commit `2d07a53`). Documentava `npx tsc --noEmit`, que, com a
  configuração de referências do TypeScript, não realiza a verificação esperada; passou a `npx tsc -b`, o
  mesmo comando do hook e da CI. A lista de comandos agora inclui `Infrastructure.Tests`, Vitest e lint.
- **P2 — `.mcp.json`:** usa `cmd /c npx`, que **só funciona no Windows**. Não alterado.
- **`npm audit`:** **7 vulnerabilidades moderadas**, todas em dependências de desenvolvimento e todas com
  a mesma origem: `minimatch` (negação de serviço), trazido pelo `typescript-eslint` (o pacote e os seus 5
  subpacotes `@typescript-eslint/*`). `npm audit --omit=dev` → 0. Não afetam a imagem final do frontend.
  Não corrigido. Os avisos anteriormente identificados para Playwright e Vitest deixaram de aparecer na
  auditoria atual: Playwright atualizado para **1.55.1**
  (commit `bde5a7c`; o 1.52 baixava navegadores sem verificar o certificado SSL) e Vitest para **4.1.12**
  (commit `e8f02a6`; avisos do `tinypool` e do `@vitest/mocker`).
- Senhas padrão só de desenvolvimento; sem deploy online.
- **CI (GitHub Actions):** criada em `.github/workflows/ci.yml` (commit `5577936`) e validada localmente com
  o `actionlint`. Primeira execução no GitHub (run `43460540401`, commit `cb97bb4`): Backend, Frontend e
  Playwright com sucesso. O gatilho automático foi restrito à `main` no commit local `9026d02`,
  ainda aguardando publicação.

---

## 12. Vídeo e entrega

<!-- A preencher pela Francisca: link do vídeo e observações da entrega. -->
