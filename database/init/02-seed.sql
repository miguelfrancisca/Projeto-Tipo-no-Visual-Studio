/*
    02-seed.sql — carga de dados de exemplo (idempotente: só roda com o banco vazio).

    Totalmente set-based e DETERMINÍSTICO (sem RAND/NEWID): toda execução gera
    exatamente os mesmos dados, o que permite reproduzir os números do README.

    Volume (suficiente para o plano de execução da Parte 4 ser significativo):
      - 200 clientes   · nomes combinados de listas fixas → existem HOMÔNIMOS de propósito
                         (ex.: dois "Ana Souza" com e-mails diferentes)
                       · clientes 181–190 só têm pedidos em 2025
                       · clientes 191–200 não têm nenhum pedido
      - 50 produtos    · 3 com estoque baixo/zerado, para testar RN06 pela interface
      - 20.000 pedidos entre 2025-01-01 e 2026-11-10, concentrados nos clientes de Id menor
      - ~50.000 itens  · 1 a 4 por pedido, sem produto repetido no mesmo pedido
*/
SET NOCOUNT ON;
GO

USE PedidosDb;
GO

IF EXISTS (SELECT 1 FROM dbo.Clientes)
BEGIN
    PRINT 'Seed ignorado: o banco já possui dados.';
    SET NOEXEC ON;   -- pula os lotes seguintes; o SET NOEXEC OFF no fim reativa a execução
END
GO

BEGIN TRANSACTION;

/* ---------- Clientes (200) ---------- */
DECLARE @PrimeirosNomes TABLE (Ordem INT PRIMARY KEY, Nome NVARCHAR(50));
INSERT INTO @PrimeirosNomes (Ordem, Nome) VALUES
    (0, N'Ana'), (1, N'Bruno'), (2, N'Carla'), (3, N'Diego'), (4, N'Eduarda'),
    (5, N'Felipe'), (6, N'Gabriela'), (7, N'Henrique'), (8, N'Isabela'), (9, N'João'),
    (10, N'Larissa'), (11, N'Marcelo'), (12, N'Natália'), (13, N'Otávio'), (14, N'Paula'),
    (15, N'Rafael'), (16, N'Sofia'), (17, N'Thiago'), (18, N'Vanessa'), (19, N'William');

DECLARE @Sobrenomes TABLE (Ordem INT PRIMARY KEY, Nome NVARCHAR(50));
INSERT INTO @Sobrenomes (Ordem, Nome) VALUES
    (0, N'Souza'), (1, N'Lima'), (2, N'Oliveira'), (3, N'Santos'),
    (4, N'Pereira'), (5, N'Costa'), (6, N'Almeida'), (7, N'Ferreira');

-- 20 nomes × 8 sobrenomes = 160 combinações para 200 clientes → 40 homônimos.
SET IDENTITY_INSERT dbo.Clientes ON;

INSERT INTO dbo.Clientes (Id, Nome, Email, DataCadastro)
SELECT  s.value,
        CONCAT(pn.Nome, N' ', sn.Nome),
        CONCAT(LOWER(pn.Nome), N'.', LOWER(sn.Nome), N'.', s.value, N'@exemplo.com'),
        DATEADD(DAY, s.value, '2024-06-01')
FROM GENERATE_SERIES(1, 200) AS s
JOIN @PrimeirosNomes pn ON pn.Ordem = (s.value - 1) % 20
JOIN @Sobrenomes     sn ON sn.Ordem = ((s.value - 1) / 20) % 8;

SET IDENTITY_INSERT dbo.Clientes OFF;

/* ---------- Produtos (50) ---------- */
DECLARE @Tipos TABLE (Ordem INT PRIMARY KEY, Nome NVARCHAR(60), PrecoBase DECIMAL(18, 2), Sigla VARCHAR(4));
INSERT INTO @Tipos (Ordem, Nome, PrecoBase, Sigla) VALUES
    (0, N'Teclado',       129.90, 'TEC'), (1, N'Mouse',          59.90, 'MOU'),
    (2, N'Monitor 24"',   899.00, 'MON'), (3, N'Headset',       249.90, 'HEA'),
    (4, N'Webcam',        199.90, 'WEB'), (5, N'Cadeira',      1299.00, 'CAD'),
    (6, N'Mousepad',       39.90, 'PAD'), (7, N'Hub USB-C',     149.90, 'HUB'),
    (8, N'SSD 1TB',       459.90, 'SSD'), (9, N'Notebook Stand', 89.90, 'STD');

DECLARE @Variantes TABLE (Ordem INT PRIMARY KEY, Nome NVARCHAR(20), Fator DECIMAL(4, 2));
INSERT INTO @Variantes (Ordem, Nome, Fator) VALUES
    (0, N'Lite', 0.80), (1, N'Basic', 1.00), (2, N'Plus', 1.25), (3, N'Pro', 1.60), (4, N'Max', 2.10);

SET IDENTITY_INSERT dbo.Produtos ON;

INSERT INTO dbo.Produtos (Id, Nome, Sku, Preco, Estoque)
SELECT  s.value,
        CONCAT(t.Nome, N' ', v.Nome),
        CONCAT(t.Sigla, '-', UPPER(LEFT(v.Nome, 3)), '-', FORMAT(s.value, '000')),
        ROUND(t.PrecoBase * v.Fator, 0) - 0.10,   -- preços terminando em ,90
        CASE s.value
            WHEN 3  THEN 0      -- esgotado
            WHEN 17 THEN 2      -- estoque baixo
            WHEN 41 THEN 5      -- estoque baixo
            ELSE 200 + (s.value * 37) % 800
        END
FROM GENERATE_SERIES(1, 50) AS s
JOIN @Tipos     t ON t.Ordem = (s.value - 1) / 5
JOIN @Variantes v ON v.Ordem = (s.value - 1) % 5;

SET IDENTITY_INSERT dbo.Produtos OFF;

/* ---------- Pedidos (20.000) ---------- */
-- Janela de datas: 2025-01-01 00:00 até 2026-11-10 (≈ 679 dias = 977.760 minutos).
-- ClienteId: distribuição enviesada (quadrática) para os Ids menores → ranking com líderes claros.
SET IDENTITY_INSERT dbo.Pedidos ON;

WITH Base AS
(
    SELECT  s.value AS N,
            1 + CAST(POWER(((s.value * 7919) % 10007) / 10007.0, 2) * 190 AS INT) AS ClienteId,
            DATEADD(MINUTE, (s.value * 104729) % 977760, CAST('2025-01-01' AS DATETIME2(3))) AS Data
    FROM GENERATE_SERIES(1, 20000) AS s
)
INSERT INTO dbo.Pedidos (Id, ClienteId, DataPedido, ValorTotal)
SELECT  N,
        ClienteId,
        -- Clientes 181–190 compram só em 2025: datas de 2026 recuam um ano.
        CASE WHEN ClienteId BETWEEN 181 AND 190 AND Data >= '2026-01-01'
             THEN DATEADD(YEAR, -1, Data)
             ELSE Data
        END,
        0       -- calculado após inserir os itens
FROM Base;

SET IDENTITY_INSERT dbo.Pedidos OFF;

/* ---------- ItensPedido (1 a 4 por pedido) ---------- */
-- Produto = 1 + (N*13 + k*7) % 50 → para k = 1..4 os deslocamentos 7, 14, 21, 28 são distintos
-- módulo 50, então nunca há produto repetido no mesmo pedido (RN05).
INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario)
SELECT  p.Id,
        pr.Id,
        1 + (p.Id + k.value) % 5,
        pr.Preco
FROM dbo.Pedidos p
CROSS APPLY GENERATE_SERIES(1, 1 + p.Id % 4) AS k
JOIN dbo.Produtos pr ON pr.Id = 1 + (p.Id * 13 + k.value * 7) % 50;

/* ---------- ValorTotal = Σ itens (RN09) ---------- */
UPDATE p
SET    p.ValorTotal = t.Total
FROM   dbo.Pedidos p
JOIN  (SELECT PedidoId, SUM(Quantidade * PrecoUnitario) AS Total
       FROM dbo.ItensPedido
       GROUP BY PedidoId) t ON t.PedidoId = p.Id;

COMMIT TRANSACTION;

DECLARE @Resumo NVARCHAR(200) = CONCAT('Seed concluído: ',
    (SELECT COUNT(*) FROM dbo.Clientes),    ' clientes, ',
    (SELECT COUNT(*) FROM dbo.Produtos),    ' produtos, ',
    (SELECT COUNT(*) FROM dbo.Pedidos),     ' pedidos, ',
    (SELECT COUNT(*) FROM dbo.ItensPedido), ' itens.');
PRINT @Resumo;
GO

SET NOEXEC OFF;
GO
