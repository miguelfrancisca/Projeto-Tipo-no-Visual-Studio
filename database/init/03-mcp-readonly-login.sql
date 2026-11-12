/*
    03-mcp-readonly-login.sql — login de PRIVILÉGIO MÍNIMO usado pelo MCP do Claude (.mcp.json).

    O servidor MCP escolhido (mssql-mcp) aceita SQL arbitrário — necessário para
    SET STATISTICS IO/TIME e planos de execução (Parte 4). Para que o Claude não
    consiga alterar dados mesmo assim, ele conecta com este login, que só pode:
      - ler tabelas            (db_datareader)
      - ver planos de execução (SHOWPLAN)
      - consultar DMVs         (VIEW DATABASE STATE / VIEW SERVER STATE)
    Qualquer INSERT/UPDATE/DELETE/DDL feito pelo MCP falha por falta de permissão.

    A senha chega como variável do sqlcmd: -v McpPassword="..." (ver docker-compose.yml).
    Apenas para desenvolvimento local.
*/
SET NOCOUNT ON;
GO

IF SUSER_ID(N'claude_mcp') IS NULL
    CREATE LOGIN claude_mcp WITH PASSWORD = N'$(McpPassword)', CHECK_POLICY = ON, DEFAULT_DATABASE = PedidosDb;
ELSE
    ALTER LOGIN claude_mcp WITH PASSWORD = N'$(McpPassword)';
GO

GRANT VIEW SERVER STATE TO claude_mcp;
GO

USE PedidosDb;
GO

IF USER_ID(N'claude_mcp') IS NULL
    CREATE USER claude_mcp FOR LOGIN claude_mcp;
GO

ALTER ROLE db_datareader ADD MEMBER claude_mcp;
GRANT SHOWPLAN            TO claude_mcp;
GRANT VIEW DATABASE STATE TO claude_mcp;
GO
