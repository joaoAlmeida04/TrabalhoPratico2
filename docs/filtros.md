# 🔍 Documentação dos Filtros com Junções (JOINs)

Este documento descreve os **5 filtros** implementados na API que utilizam junções entre tabelas, conforme exigido pelo trabalho. São empregados **dois tipos de junção**: `INNER JOIN` (registros com correspondência em ambas as tabelas) e `LEFT JOIN` (todos os registros da tabela à esquerda, mesmo sem correspondência).

---

## Filtro 1 — Veículos disponíveis por faixa de preço

- **Endpoint:** `GET /api/veiculos/disponiveis-por-preco?precoMin={}&precoMax={}`
- **Tipo de junção:** `INNER JOIN` (Veículo × Fabricante × Categoria)
- **Descrição:** Retorna veículos disponíveis cuja diária base esteja dentro da faixa informada, já trazendo o nome do fabricante e da categoria.
- **SQL equivalente:**
  ```sql
  SELECT v.*, f.Nome AS Fabricante, c.Nome AS Categoria
  FROM Veiculos v
  INNER JOIN Fabricantes f ON v.FabricanteId = f.Id
  INNER JOIN Categorias  c ON v.CategoriaVeiculoId = c.Id
  WHERE v.Disponivel = 1
    AND v.ValorDiariaBase BETWEEN @precoMin AND @precoMax
  ORDER BY v.ValorDiariaBase;
  ```

---

## Filtro 2 — Contagem de veículos por fabricante

- **Endpoint:** `GET /api/veiculos/contagem-por-fabricante`
- **Tipo de junção:** `INNER JOIN` + `GROUP BY`
- **Descrição:** Agrupa os veículos por fabricante e conta quantos existem em cada um.
- **SQL equivalente:**
  ```sql
  SELECT f.Nome AS Fabricante, COUNT(*) AS Quantidade
  FROM Veiculos v
  INNER JOIN Fabricantes f ON v.FabricanteId = f.Id
  GROUP BY f.Nome
  ORDER BY Quantidade DESC;
  ```

---

## Filtro 3 — Clientes com resumo de aluguéis

- **Endpoint:** `GET /api/clientes/com-resumo-alugueis`
- **Tipo de junção:** `LEFT JOIN` (Cliente × Aluguel)
- **Descrição:** Lista **todos** os clientes — inclusive os que nunca alugaram — com a quantidade de aluguéis e o total gasto. O `LEFT JOIN` garante que clientes sem aluguéis apareçam com contagem zero.
- **SQL equivalente:**
  ```sql
  SELECT cl.Id, cl.Nome, cl.Cidade,
         COUNT(a.Id) AS QtdAlugueis,
         COALESCE(SUM(a.ValorTotal), 0) AS TotalGasto
  FROM Clientes cl
  LEFT JOIN Alugueis a ON a.ClienteId = cl.Id
  GROUP BY cl.Id, cl.Nome, cl.Cidade
  ORDER BY TotalGasto DESC;
  ```

---

## Filtro 4 — Relatório detalhado de aluguéis

- **Endpoint:** `GET /api/alugueis/relatorio-detalhado?de={}&ate={}`
- **Tipo de junção:** múltiplos `INNER JOIN` (Aluguel × Cliente × Veículo × Fabricante × Categoria)
- **Descrição:** Cruza quatro tabelas para montar um relatório completo de cada aluguel, com filtro opcional por período de retirada.
- **SQL equivalente:**
  ```sql
  SELECT a.Id, cl.Nome AS Cliente, v.Modelo AS Veiculo,
         f.Nome AS Fabricante, c.Nome AS Categoria,
         a.DataRetirada, a.DataDevolucaoPrevista,
         a.DataDevolucaoReal, a.ValorTotal
  FROM Alugueis a
  INNER JOIN Clientes    cl ON a.ClienteId = cl.Id
  INNER JOIN Veiculos    v  ON a.VeiculoId = v.Id
  INNER JOIN Fabricantes f  ON v.FabricanteId = f.Id
  INNER JOIN Categorias  c  ON v.CategoriaVeiculoId = c.Id
  WHERE (@de  IS NULL OR a.DataRetirada >= @de)
    AND (@ate IS NULL OR a.DataRetirada <= @ate)
  ORDER BY a.DataRetirada DESC;
  ```

---

## Filtro 5 — Faturamento por fabricante

- **Endpoint:** `GET /api/alugueis/faturamento-por-fabricante`
- **Tipo de junção:** `INNER JOIN` + `GROUP BY` (Aluguel × Veículo × Fabricante)
- **Descrição:** Soma o valor total dos aluguéis agrupando por fabricante do veículo, gerando o faturamento de cada marca.
- **SQL equivalente:**
  ```sql
  SELECT f.Nome AS Fabricante,
         COUNT(*) AS QtdAlugueis,
         SUM(a.ValorTotal) AS Faturamento
  FROM Alugueis a
  INNER JOIN Veiculos    v ON a.VeiculoId = v.Id
  INNER JOIN Fabricantes f ON v.FabricanteId = f.Id
  GROUP BY f.Nome
  ORDER BY Faturamento DESC;
  ```

---

## Resumo

| # | Filtro | Tabelas envolvidas | Tipo de junção |
|---|--------|--------------------|----------------|
| 1 | Veículos disponíveis por preço | Veículo, Fabricante, Categoria | INNER JOIN |
| 2 | Contagem por fabricante | Veículo, Fabricante | INNER JOIN + GROUP BY |
| 3 | Clientes com resumo de aluguéis | Cliente, Aluguel | **LEFT JOIN** |
| 4 | Relatório detalhado | Aluguel, Cliente, Veículo, Fabricante, Categoria | INNER JOIN (múltiplo) |
| 5 | Faturamento por fabricante | Aluguel, Veículo, Fabricante | INNER JOIN + GROUP BY |

Os filtros são implementados via LINQ no Entity Framework Core, que traduz as expressões para SQL equivalente ao mostrado acima.
