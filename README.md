# 🚗 Sistema de Locadora de Veículos

Trabalho Prático — Sistema completo de gerenciamento de uma locadora de veículos, com **back-end** em ASP.NET Core + Entity Framework Core (TP1) e **front-end** em HTML + CSS + JavaScript puro (TP2).

## 📂 Estrutura do projeto

```
locadora-veiculos/
├── backend/          # API RESTful em C# / ASP.NET Core (TP1)
│   ├── Controllers/  # Endpoints REST
│   ├── Models/       # Entidades (6)
│   ├── Data/         # DbContext + seed
│   ├── DTOs/         # Objetos de transferência
│   └── Program.cs    # Configuração da aplicação
├── frontend/         # Interface web (TP2)
│   ├── index.html
│   ├── css/style.css
│   └── js/api.js, app.js
└── docs/             # Relatório LaTeX (modelo SBC) + documentação dos filtros
```

## ✅ Pré-requisitos

- [.NET SDK 8.0+](https://dotnet.microsoft.com/download)
- [SQL Server Express](https://www.microsoft.com/sql-server/sql-server-downloads) (ou LocalDB)
- Um navegador moderno
- (Opcional) Uma distribuição LaTeX para compilar o relatório (TeX Live / MiKTeX)

## ▶️ Como executar o back-end (TP1)

1. Abra um terminal na pasta `backend/`.
2. Confira a *connection string* em `appsettings.json`. O padrão é:
   ```
   Server=localhost\SQLEXPRESS;Database=LocadoraVeiculos;Trusted_Connection=True;TrustServerCertificate=True
   ```
   Ajuste o `Server` se a sua instância do SQL tiver outro nome.
3. Restaure os pacotes e rode:
   ```bash
   dotnet restore
   dotnet run
   ```
4. Na primeira execução, o banco e as tabelas são criados automaticamente (`EnsureCreated`) e populados com dados de exemplo.
5. Acesse a documentação interativa (Swagger) em:
   ```
   http://localhost:5000/swagger
   ```

## ▶️ Como executar o front-end (TP2)

O front-end consome a API do back-end, então **o back-end precisa estar rodando** primeiro.

- **Forma simples:** abra o arquivo `frontend/index.html` diretamente no navegador.
- **Forma recomendada** (evita qualquer restrição de CORS/arquivo local): sirva a pasta com um servidor estático:
  ```bash
  cd frontend
  python -m http.server 5500
  ```
  E acesse `http://localhost:5500`.

> Se a API estiver em outra porta/endereço, ajuste a constante `BASE_URL` no início de `frontend/js/api.js`.

## 🔍 Funcionalidades

- CRUD completo de **Veículos, Clientes, Aluguéis, Pagamentos, Fabricantes e Categorias**.
- Registro de aluguel com cálculo automático de valor (diária base × fator da categoria × dias).
- Registro de devolução com recálculo por atraso e atualização de quilometragem.
- Recarga de saldo e pagamento com débito de carteira.
- **Dashboard** com indicadores consolidados.
- **5 filtros com junções** entre tabelas (ver `docs/filtros.md`).
- Filtros de pesquisa personalizados em todas as telas.

## 📄 Relatório

O relatório técnico está em `docs/relatorio.tex`, no modelo **SBC**. Para compilar:

```bash
cd docs
pdflatex relatorio.tex
pdflatex relatorio.tex   # segunda passada (referências)
```

O arquivo `docs/relatorio_preview.pdf` é uma prévia já compilada do layout.

## 👤 Autoria

Cecília [Sobrenome] — PUC Minas — [Disciplina] — 2026.
