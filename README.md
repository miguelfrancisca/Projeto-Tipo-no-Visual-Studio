# Mini Gestão de Pedidos

> README em construção — as seções de execução, testes, decisões técnicas e uso do Claude
> serão completadas ao final da implementação. A Parte 4 já está documentada abaixo.

---

## Parte 4 — Debug da query de ranking de 2026

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

Toda a investigação foi feita pelo Claude Code através do **MCP do SQL Server** (`.mcp.json`, servidor `sqlserver-pedidos`),
conectado ao banco do `docker compose` com o login **`claude_mcp`** — `db_datareader` + `SHOWPLAN` + `VIEW STATE`, sem
permissão de escrita (`database/init/03-mcp-readonly-login.sql`). Assim o Claude pôde ler dados e planos sem nenhum risco
de alterar o banco.

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
