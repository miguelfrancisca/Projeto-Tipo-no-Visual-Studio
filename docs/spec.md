# Especificação — Mini Gestão de Pedidos

> Status: **aprovada para implementação** · Versão 1.0 · 2026-11-12

## 1. Objetivo

Permitir o **cadastro e a consulta de Pedidos de Clientes**, com validação de cliente e de estoque, listagem filtrada e paginada, e um relatório dos 5 clientes de maior faturamento num período.

A aplicação é composta por:

- uma **API REST** em .NET 8 (Clean Architecture);
- um **banco SQL Server**;
- um **frontend React + TypeScript**;
- tudo orquestrado via **Docker Compose** (`docker compose up`).

### Fora de escopo

- Autenticação/autorização.
- CRUD completo de Clientes e Produtos (são carregados via script de seed; a API só os **lista** para alimentar o frontend).
- Edição e cancelamento de pedidos (e, portanto, devolução de estoque).
- Multi-moeda, impostos, frete, descontos.

## 2. Glossário

| Termo | Significado |
|---|---|
| Pedido | Compra feita por um Cliente, composta por 1..N Itens. |
| Item de pedido | Produto + quantidade + preço unitário congelado no momento da compra. |
| Valor total do pedido | Σ (Quantidade × PrecoUnitario) de todos os itens. |
| Período | Intervalo de datas **inclusivo nos dois extremos** (`dataInicio` 00:00 até `dataFim` 23:59:59.9999999). |

## 3. Regras de negócio

| Id | Regra | Onde é garantida |
|---|---|---|
| **RN01** | Um pedido só pode ser criado para um **cliente existente**. | Application (consulta ao repositório) |
| **RN02** | Um pedido deve ter **pelo menos 1 item**. | Domain (`Pedido`) + validação da request |
| **RN03** | A **quantidade** de cada item deve ser **> 0**. | Domain (`ItemPedido`) + validação da request + `CHECK` no banco |
| **RN04** | Cada item deve referenciar um **produto existente**. | Application |
| **RN05** | Não pode haver **produto repetido** no mesmo pedido (o cliente deve somar a quantidade numa única linha). | Domain + `UNIQUE (PedidoId, ProdutoId)` no banco |
| **RN06** | Deve haver **estoque suficiente** para cada item. Ao criar o pedido, o estoque é **baixado**. | Domain (`Produto.BaixarEstoque`) |
| **RN07** | O **preço unitário** do item é o preço do produto **no momento da compra** — nunca é enviado pelo cliente. | Application |
| **RN08** | A **data do pedido** é atribuída pelo servidor (UTC) no momento da criação. | Application (`TimeProvider`) |
| **RN09** | O **valor total** do pedido é calculado e persistido no pedido. | Domain |
| **RN10** | A criação do pedido + baixa de estoque é **atômica** (tudo ou nada). Duas compras concorrentes do mesmo produto não podem deixar o estoque negativo. | Infrastructure (transação + token de concorrência `rowversion` em `Produtos`) + `CHECK (Estoque >= 0)` |
| **RN11** | Na listagem, `dataInicio` não pode ser posterior a `dataFim`. | Validação da request |
| **RN12** | Paginação: `pagina ≥ 1`; `tamanhoPagina` entre 1 e 100 (padrão 10). | Validação da request |
| **RN13** | Relatório top-clientes: período **obrigatório**; retorna no máximo 5 clientes, ordenados por valor total desc (desempate por `ClienteId` asc); clientes sem pedidos no período **não** aparecem. | Infrastructure (query) |

## 4. Modelo de dados

```
Clientes 1 ──── N Pedidos 1 ──── N ItensPedido N ──── 1 Produtos
```

### Clientes
| Coluna | Tipo | Restrições |
|---|---|---|
| Id | INT IDENTITY | PK |
| Nome | NVARCHAR(150) | NOT NULL |
| Email | NVARCHAR(200) | NOT NULL, **UNIQUE** (`UX_Clientes_Email`) |
| DataCadastro | DATETIME2(0) | NOT NULL, DEFAULT SYSUTCDATETIME() |

### Produtos
| Coluna | Tipo | Restrições |
|---|---|---|
| Id | INT IDENTITY | PK |
| Nome | NVARCHAR(150) | NOT NULL |
| Sku | VARCHAR(30) | NOT NULL, **UNIQUE** (`UX_Produtos_Sku`) |
| Preco | DECIMAL(18,2) | NOT NULL, CHECK (Preco >= 0) |
| Estoque | INT | NOT NULL, CHECK (Estoque >= 0) |
| RowVersion | ROWVERSION | token de concorrência otimista (RN10) |

### Pedidos
| Coluna | Tipo | Restrições |
|---|---|---|
| Id | INT IDENTITY | PK |
| ClienteId | INT | NOT NULL, **FK → Clientes(Id)** |
| DataPedido | DATETIME2(3) | NOT NULL |
| ValorTotal | DECIMAL(18,2) | NOT NULL, CHECK (ValorTotal >= 0) |

Índices:
- `IX_Pedidos_ClienteId_DataPedido (ClienteId, DataPedido) INCLUDE (ValorTotal)` — filtro por cliente + período e o relatório.
- `IX_Pedidos_DataPedido (DataPedido) INCLUDE (ClienteId, ValorTotal)` — filtro só por período, ordenação da listagem e o relatório.

### ItensPedido
| Coluna | Tipo | Restrições |
|---|---|---|
| Id | INT IDENTITY | PK |
| PedidoId | INT | NOT NULL, **FK → Pedidos(Id) ON DELETE CASCADE** |
| ProdutoId | INT | NOT NULL, **FK → Produtos(Id)** |
| Quantidade | INT | NOT NULL, CHECK (Quantidade > 0) |
| PrecoUnitario | DECIMAL(18,2) | NOT NULL, CHECK (PrecoUnitario >= 0) |

Índices:
- `UX_ItensPedido_PedidoId_ProdutoId (PedidoId, ProdutoId) INCLUDE (Quantidade, PrecoUnitario)` UNIQUE — RN05, busca dos itens de um pedido e cobertura da soma por pedido (query da Parte 4).
- `IX_ItensPedido_ProdutoId (ProdutoId)` — FK (evita scan ao validar deleção de produto).

> **Por que `ValorTotal` desnormalizado em `Pedidos`?** A listagem e o relatório agregam por pedido; com o total gravado, nenhum dos dois precisa ler `ItensPedido`, e os índices acima cobrem as consultas. Como pedidos não são editados (fora de escopo), não há risco de divergência.

## 5. Endpoints

Todas as respostas de erro seguem **RFC 7807 (`application/problem+json`)**.

| Status | Quando |
|---|---|
| 400 | Request malformada ou inválida (validação de formato: lista vazia, quantidade ≤ 0, paginação, período invertido). Corpo com `errors` por campo. |
| 404 | Recurso da URL não existe (`GET /pedidos/{id}`). |
| 409 | Conflito de concorrência no estoque (RN10) — o cliente pode tentar de novo. |
| 422 | Regra de negócio violada com request bem formada (cliente/produto inexistente, estoque insuficiente). |

### 5.1 `POST /pedidos` — cria um pedido

Request:
```json
{
  "clienteId": 1,
  "itens": [
    { "produtoId": 3, "quantidade": 2 },
    { "produtoId": 5, "quantidade": 1 }
  ]
}
```

Resposta `201 Created` + header `Location: /pedidos/{id}`:
```json
{
  "id": 42,
  "clienteId": 1,
  "clienteNome": "Ana Souza",
  "dataPedido": "2026-11-12T14:03:22.123Z",
  "valorTotal": 359.70,
  "itens": [
    { "produtoId": 3, "produtoNome": "Teclado Mecânico", "quantidade": 2, "precoUnitario": 149.90, "subtotal": 299.80 },
    { "produtoId": 5, "produtoNome": "Mouse Pad",        "quantidade": 1, "precoUnitario": 59.90,  "subtotal": 59.90 }
  ]
}
```

Erros: 400 (RN02, RN03, RN05), 422 (RN01, RN04, RN06), 409 (RN10).

### 5.2 `GET /pedidos` — lista pedidos

Query string (todos opcionais): `clienteId`, `dataInicio` (yyyy-MM-dd), `dataFim` (yyyy-MM-dd), `pagina` (padrão 1), `tamanhoPagina` (padrão 10, máx. 100).

Ordenação: `DataPedido DESC, Id DESC`.

Resposta `200 OK`:
```json
{
  "itens": [
    { "id": 42, "clienteId": 1, "clienteNome": "Ana Souza", "dataPedido": "2026-11-12T14:03:22.123Z", "valorTotal": 359.70, "quantidadeItens": 2 }
  ],
  "pagina": 1,
  "tamanhoPagina": 10,
  "totalItens": 1,
  "totalPaginas": 1
}
```

Erros: 400 (RN11, RN12).

### 5.3 `GET /relatorios/top-clientes` — top 5 clientes do período

Query string (**obrigatórios**): `dataInicio`, `dataFim`.

Resposta `200 OK`:
```json
[
  { "clienteId": 7, "clienteNome": "Carlos Lima", "valorTotal": 12450.00, "quantidadePedidos": 9 }
]
```

Erros: 400 (período ausente ou invertido).

### 5.4 Endpoints de apoio (para o frontend)

| Endpoint | Retorno |
|---|---|
| `GET /pedidos/{id}` | Pedido com itens (mesmo formato do 201). 404 se não existir. Alvo do `Location` do POST. |
| `GET /clientes` | `[{ id, nome, email }]` ordenado por nome. |
| `GET /produtos` | `[{ id, nome, sku, preco, estoque }]` ordenado por nome. |

## 6. Frontend

- **Listagem de pedidos**: tabela paginada com filtros por cliente (select) e período (dois campos de data); botão "Novo pedido".
- **Modal de criação**: select de cliente; linhas de item (produto + quantidade, com preço e estoque visíveis); total calculado em tela; botão salvar. Erros da API (400/422/409) exibidos no modal. Ao salvar com sucesso, o modal fecha e a listagem é recarregada, mostrando o novo pedido no topo.

## 7. Correção de query (Parte 4)

Entregar em `database/` a versão corrigida e otimizada da query de ranking de clientes de 2026, com a explicação no README (o que estava errado, como foi investigado, plano de execução e índices).

## 8. Critérios de aceite

### Criação de pedido
- **CA01** — *Dado* um cliente e produtos existentes com estoque suficiente, *quando* envio `POST /pedidos`, *então* recebo 201, o `Location` aponta para o pedido, o `valorTotal` = Σ(qtd × preço atual do produto) e o estoque de cada produto diminui na quantidade comprada.
- **CA02** — *Dado* um `clienteId` inexistente, *quando* envio `POST /pedidos`, *então* recebo 422 e nada é gravado.
- **CA03** — *Dada* uma lista de itens vazia (ou ausente), *quando* envio `POST /pedidos`, *então* recebo 400.
- **CA04** — *Dado* um item com quantidade ≤ 0, *então* recebo 400.
- **CA05** — *Dado* um `produtoId` inexistente, *então* recebo 422.
- **CA06** — *Dado* um item cuja quantidade excede o estoque, *então* recebo 422 indicando o produto, **nenhum** item é gravado e **nenhum** estoque é alterado (inclusive dos outros itens válidos).
- **CA07** — *Dado* o mesmo produto em duas linhas do pedido, *então* recebo 400.
- **CA08** — O `precoUnitario` gravado é o do produto no momento da compra; alterações posteriores de preço não afetam o pedido.

### Listagem
- **CA09** — Sem filtros, `GET /pedidos` retorna a 1ª página com 10 pedidos, do mais recente para o mais antigo, e `totalItens` com o total geral.
- **CA10** — Com `clienteId`, retorna apenas pedidos daquele cliente.
- **CA11** — Com `dataInicio`/`dataFim`, retorna pedidos cuja data está no intervalo, **incluindo os do dia `dataFim` inteiro**.
- **CA12** — `dataInicio > dataFim`, `pagina < 1` ou `tamanhoPagina` fora de 1..100 → 400.

### Relatório
- **CA13** — `GET /relatorios/top-clientes` retorna no máximo 5 clientes, ordenados por valor total desc, considerando apenas pedidos do período.
- **CA14** — Sem `dataInicio` ou `dataFim` → 400.

### Frontend
- **CA15** — Na tela de listagem consigo filtrar por cliente e período e navegar entre páginas.
- **CA16** — Consigo criar um pedido pelo modal e vê-lo na listagem logo em seguida; se a API recusar, vejo a mensagem de erro sem perder o que preenchi.

### Infra
- **CA17** — `docker compose up` (com Docker instalado, sem outros passos) sobe banco com schema e dados de exemplo, API e frontend funcionando.
