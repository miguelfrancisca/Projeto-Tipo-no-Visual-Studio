/*
    01-schema.sql — criação do banco PedidosDb (idempotente).

    Fonte de verdade do schema (ver docs/spec.md §4). Não há EF migrations:
    o EF Core apenas mapeia estas tabelas.

    Executado automaticamente pelo serviço `db-init` do docker-compose
    e pelos testes de integração (Testcontainers).
*/
SET NOCOUNT ON;
GO

IF DB_ID(N'PedidosDb') IS NULL
    CREATE DATABASE PedidosDb;
GO

USE PedidosDb;
GO

/* ---------- Clientes ---------- */
IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes
    (
        Id           INT IDENTITY(1, 1) NOT NULL,
        Nome         NVARCHAR(150)      NOT NULL,
        Email        NVARCHAR(200)      NOT NULL,
        DataCadastro DATETIME2(0)       NOT NULL CONSTRAINT DF_Clientes_DataCadastro DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Clientes PRIMARY KEY CLUSTERED (Id)
    );

    CREATE UNIQUE INDEX UX_Clientes_Email ON dbo.Clientes (Email);
END
GO

/* ---------- Produtos ---------- */
IF OBJECT_ID(N'dbo.Produtos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Produtos
    (
        Id         INT IDENTITY(1, 1) NOT NULL,
        Nome       NVARCHAR(150)      NOT NULL,
        Sku        VARCHAR(30)        NOT NULL,
        Preco      DECIMAL(18, 2)     NOT NULL,
        Estoque    INT                NOT NULL,
        RowVersion ROWVERSION         NOT NULL,   -- token de concorrência otimista (RN10)

        CONSTRAINT PK_Produtos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Produtos_Preco   CHECK (Preco >= 0),
        CONSTRAINT CK_Produtos_Estoque CHECK (Estoque >= 0)  -- última barreira contra overselling (RN10)
    );

    CREATE UNIQUE INDEX UX_Produtos_Sku ON dbo.Produtos (Sku);
END
GO

/* ---------- Pedidos ---------- */
IF OBJECT_ID(N'dbo.Pedidos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Pedidos
    (
        Id         INT IDENTITY(1, 1) NOT NULL,
        ClienteId  INT                NOT NULL,
        DataPedido DATETIME2(3)       NOT NULL,   -- UTC, atribuída pelo servidor (RN08)
        ValorTotal DECIMAL(18, 2)     NOT NULL,   -- desnormalizado: Σ itens (RN09)

        CONSTRAINT PK_Pedidos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Pedidos_Clientes FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes (Id),
        CONSTRAINT CK_Pedidos_ValorTotal CHECK (ValorTotal >= 0)
    );

    -- Filtro por cliente (+ período) na listagem; também cobre a FK.
    CREATE INDEX IX_Pedidos_ClienteId_DataPedido
        ON dbo.Pedidos (ClienteId, DataPedido)
        INCLUDE (ValorTotal);

    -- Filtro só por período, ordenação da listagem e relatório top-clientes.
    CREATE INDEX IX_Pedidos_DataPedido
        ON dbo.Pedidos (DataPedido)
        INCLUDE (ClienteId, ValorTotal);
END
GO

/* ---------- ItensPedido ---------- */
IF OBJECT_ID(N'dbo.ItensPedido', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ItensPedido
    (
        Id            INT IDENTITY(1, 1) NOT NULL,
        PedidoId      INT                NOT NULL,
        ProdutoId     INT                NOT NULL,
        Quantidade    INT                NOT NULL,
        PrecoUnitario DECIMAL(18, 2)     NOT NULL,  -- preço congelado no momento da compra (RN07)

        CONSTRAINT PK_ItensPedido PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ItensPedido_Pedidos  FOREIGN KEY (PedidoId)  REFERENCES dbo.Pedidos (Id) ON DELETE CASCADE,
        CONSTRAINT FK_ItensPedido_Produtos FOREIGN KEY (ProdutoId) REFERENCES dbo.Produtos (Id),
        CONSTRAINT CK_ItensPedido_Quantidade    CHECK (Quantidade > 0),       -- RN03
        CONSTRAINT CK_ItensPedido_PrecoUnitario CHECK (PrecoUnitario >= 0)
    );

    -- RN05 (produto não se repete no pedido) + busca dos itens de um pedido.
    -- INCLUDE cobre a soma Quantidade * PrecoUnitario por pedido (ranking da Parte 4).
    CREATE UNIQUE INDEX UX_ItensPedido_PedidoId_ProdutoId
        ON dbo.ItensPedido (PedidoId, ProdutoId)
        INCLUDE (Quantidade, PrecoUnitario);

    -- Cobre a FK para Produtos.
    CREATE INDEX IX_ItensPedido_ProdutoId
        ON dbo.ItensPedido (ProdutoId);
END
GO
