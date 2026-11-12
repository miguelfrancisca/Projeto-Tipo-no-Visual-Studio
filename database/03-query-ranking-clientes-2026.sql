/*
    03-query-ranking-clientes-2026.sql — Parte 4: debug da query de ranking de clientes de 2026.

    NÃO faz parte do init automático (fica fora de database/init/): é só leitura, para
    rodar manualmente (sqlcmd, SSMS ou o MCP do Claude). A análise completa, com planos
    de execução e métricas, está no README (seção "Parte 4").
*/
USE PedidosDb;
GO

/* ------------------------------------------------------------------------------------
   ORIGINAL (com defeitos) — mantida comentada para referência

   SELECT c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
   FROM Clientes c
   LEFT JOIN Pedidos p ON p.ClienteId = c.Id
   LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
   WHERE YEAR(p.DataPedido) = 2026          -- (4) função na coluna: não usa índice por data
                                            -- (3) filtro sobre `p` anula o LEFT JOIN
   GROUP BY c.Nome                          -- (1) soma clientes homônimos como se fossem um só
   ORDER BY Total                           -- (2) crescente: quem comprou MENOS aparece primeiro
                                            -- (5) sem Id e sem desempate determinístico
------------------------------------------------------------------------------------ */


/* ------------------------------------------------------------------------------------
   CORRIGIDA — ranking pelo valor dos itens (fonte de verdade)

   - INNER JOIN explícito: o ranking é de quem COMPROU em 2026.
   - Intervalo semiaberto [2026-01-01, 2027-01-01): predicado sargable, cobre o ano
     inteiro (inclusive 31/12 às 23:59:59.999) e permite seek em IX_Pedidos_DataPedido.
   - Agrega por ClienteId ANTES do join com Clientes (180 linhas no join, não 50 mil)
     e devolve o Id: homônimos continuam sendo clientes distintos.
   - Ordem decrescente com desempate determinístico por Id.
------------------------------------------------------------------------------------ */
WITH Totais AS
(
    SELECT  p.ClienteId,
            SUM(i.Quantidade * i.PrecoUnitario) AS Total
    FROM    dbo.Pedidos p
    JOIN    dbo.ItensPedido i ON i.PedidoId = p.Id
    WHERE   p.DataPedido >= '2026-01-01'
      AND   p.DataPedido <  '2027-01-01'
    GROUP BY p.ClienteId
)
SELECT  RANK() OVER (ORDER BY t.Total DESC) AS Posicao,
        c.Id,
        c.Nome,
        t.Total
FROM    Totais t
JOIN    dbo.Clientes c ON c.Id = t.ClienteId
ORDER BY t.Total DESC, c.Id;
GO


/* ------------------------------------------------------------------------------------
   VARIANTE MAIS RÁPIDA — usa Pedidos.ValorTotal (desnormalizado, RN09)

   Mesmo resultado (verificado: 0 pedidos com ValorTotal diferente de Σ itens), mas
   não lê ItensPedido: coberta inteiramente por IX_Pedidos_ClienteId_DataPedido.
   Medido: 82 leituras lógicas / 2,8 ms de CPU, contra 368 / 13,7 ms da versão por itens.
   É a abordagem usada pelo endpoint GET /relatorios/top-clientes.
------------------------------------------------------------------------------------ */
WITH Totais AS
(
    SELECT  p.ClienteId,
            SUM(p.ValorTotal) AS Total
    FROM    dbo.Pedidos p
    WHERE   p.DataPedido >= '2026-01-01'
      AND   p.DataPedido <  '2027-01-01'
    GROUP BY p.ClienteId
)
SELECT  RANK() OVER (ORDER BY t.Total DESC) AS Posicao,
        c.Id,
        c.Nome,
        t.Total
FROM    Totais t
JOIN    dbo.Clientes c ON c.Id = t.ClienteId
ORDER BY t.Total DESC, c.Id;
GO


/* ------------------------------------------------------------------------------------
   CONFERÊNCIA — prova do bug dos homônimos (bug 1)
   A original mostra "Ana Souza" = 2.333.623,90; são dois clientes diferentes:
   Id 1 = 2.257.766,40 e Id 161 = 75.857,50.
------------------------------------------------------------------------------------ */
SELECT  c.Id, c.Nome, c.Email,
        SUM(i.Quantidade * i.PrecoUnitario) AS Total2026
FROM    dbo.Clientes c
JOIN    dbo.Pedidos p      ON p.ClienteId = c.Id
                          AND p.DataPedido >= '2026-01-01' AND p.DataPedido < '2027-01-01'
JOIN    dbo.ItensPedido i  ON i.PedidoId = p.Id
WHERE   c.Nome IN (SELECT Nome FROM dbo.Clientes GROUP BY Nome HAVING COUNT(*) > 1)
GROUP BY c.Id, c.Nome, c.Email
ORDER BY c.Nome, c.Id;
GO
