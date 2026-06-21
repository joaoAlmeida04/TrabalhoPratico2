using Microsoft.EntityFrameworkCore;
using LocadoraVeiculos.Models;

namespace LocadoraVeiculos.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Fabricante> Fabricantes => Set<Fabricante>();
    public DbSet<CategoriaVeiculo> Categorias => Set<CategoriaVeiculo>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Aluguel> Alugueis => Set<Aluguel>();
    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- Restrições de integridade / índices únicos ----
        modelBuilder.Entity<Cliente>()
            .HasIndex(c => c.Cpf)
            .IsUnique();

        modelBuilder.Entity<Cliente>()
            .HasIndex(c => c.Email)
            .IsUnique();

        modelBuilder.Entity<Veiculo>()
            .HasIndex(v => v.Placa)
            .IsUnique();

        // ---- Comportamento de exclusão: evita cascata em massa ----
        modelBuilder.Entity<Veiculo>()
            .HasOne(v => v.Fabricante)
            .WithMany(f => f.Veiculos)
            .HasForeignKey(v => v.FabricanteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Veiculo>()
            .HasOne(v => v.CategoriaVeiculo)
            .WithMany(c => c.Veiculos)
            .HasForeignKey(v => v.CategoriaVeiculoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Aluguel>()
            .HasOne(a => a.Cliente)
            .WithMany(c => c.Alugueis)
            .HasForeignKey(a => a.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Aluguel>()
            .HasOne(a => a.Veiculo)
            .WithMany(v => v.Alugueis)
            .HasForeignKey(a => a.VeiculoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Pagamento>()
            .HasOne(p => p.Aluguel)
            .WithMany(a => a.Pagamentos)
            .HasForeignKey(p => p.AluguelId)
            .OnDelete(DeleteBehavior.Cascade);

        SeedData(modelBuilder);
    }

    // ---- Dados fictícios (seed) ----
    private static void SeedData(ModelBuilder mb)
    {
        mb.Entity<Fabricante>().HasData(
            new Fabricante { Id = 1, Nome = "Toyota", PaisOrigem = "Japão" },
            new Fabricante { Id = 2, Nome = "Volkswagen", PaisOrigem = "Alemanha" },
            new Fabricante { Id = 3, Nome = "Fiat", PaisOrigem = "Itália" },
            new Fabricante { Id = 4, Nome = "Chevrolet", PaisOrigem = "Estados Unidos" },
            new Fabricante { Id = 5, Nome = "Honda", PaisOrigem = "Japão" }
        );

        mb.Entity<CategoriaVeiculo>().HasData(
            new CategoriaVeiculo { Id = 1, Nome = "Econômico", Descricao = "Carros de entrada, baixo consumo", FatorPreco = 1.0m },
            new CategoriaVeiculo { Id = 2, Nome = "Intermediário", Descricao = "Sedans e hatches médios", FatorPreco = 1.3m },
            new CategoriaVeiculo { Id = 3, Nome = "SUV", Descricao = "Utilitários esportivos", FatorPreco = 1.6m },
            new CategoriaVeiculo { Id = 4, Nome = "Luxo", Descricao = "Veículos premium", FatorPreco = 2.0m }
        );

        mb.Entity<Veiculo>().HasData(
            new Veiculo { Id = 1, Modelo = "Corolla", AnoFabricacao = 2022, Quilometragem = 35000, Placa = "ABC1D23", Cor = "Prata", ValorDiariaBase = 180m, Disponivel = true, FabricanteId = 1, CategoriaVeiculoId = 2 },
            new Veiculo { Id = 2, Modelo = "Gol", AnoFabricacao = 2020, Quilometragem = 62000, Placa = "EFG4H56", Cor = "Branco", ValorDiariaBase = 90m, Disponivel = true, FabricanteId = 2, CategoriaVeiculoId = 1 },
            new Veiculo { Id = 3, Modelo = "Pulse", AnoFabricacao = 2023, Quilometragem = 18000, Placa = "IJK7L89", Cor = "Vermelho", ValorDiariaBase = 150m, Disponivel = true, FabricanteId = 3, CategoriaVeiculoId = 3 },
            new Veiculo { Id = 4, Modelo = "Onix", AnoFabricacao = 2021, Quilometragem = 47000, Placa = "MNO1P23", Cor = "Preto", ValorDiariaBase = 110m, Disponivel = false, FabricanteId = 4, CategoriaVeiculoId = 1 },
            new Veiculo { Id = 5, Modelo = "Civic", AnoFabricacao = 2023, Quilometragem = 12000, Placa = "QRS4T56", Cor = "Cinza", ValorDiariaBase = 220m, Disponivel = true, FabricanteId = 5, CategoriaVeiculoId = 4 },
            new Veiculo { Id = 6, Modelo = "Hilux", AnoFabricacao = 2022, Quilometragem = 54000, Placa = "UVW7X89", Cor = "Branco", ValorDiariaBase = 300m, Disponivel = true, FabricanteId = 1, CategoriaVeiculoId = 3 }
        );

        mb.Entity<Cliente>().HasData(
            new Cliente { Id = 1, Nome = "Ana Souza", Cpf = "111.111.111-11", Email = "ana.souza@email.com", Telefone = "31 99999-0001", Cidade = "Belo Horizonte", Saldo = 500m, DataCadastro = new DateTime(2025, 1, 10) },
            new Cliente { Id = 2, Nome = "Bruno Lima", Cpf = "222.222.222-22", Email = "bruno.lima@email.com", Telefone = "31 99999-0002", Cidade = "Contagem", Saldo = 150m, DataCadastro = new DateTime(2025, 2, 5) },
            new Cliente { Id = 3, Nome = "Carla Dias", Cpf = "333.333.333-33", Email = "carla.dias@email.com", Telefone = "31 99999-0003", Cidade = "Belo Horizonte", Saldo = 1000m, DataCadastro = new DateTime(2025, 3, 1) },
            new Cliente { Id = 4, Nome = "Diego Reis", Cpf = "444.444.444-44", Email = "diego.reis@email.com", Telefone = "31 99999-0004", Cidade = "Betim", Saldo = 0m, DataCadastro = new DateTime(2025, 3, 20) }
        );

        mb.Entity<Aluguel>().HasData(
            new Aluguel { Id = 1, ClienteId = 1, VeiculoId = 1, DataRetirada = new DateTime(2025, 5, 1), DataDevolucaoPrevista = new DateTime(2025, 5, 5), DataDevolucaoReal = new DateTime(2025, 5, 5), QuilometragemInicial = 34500, QuilometragemFinal = 35000, ValorDiaria = 234m, ValorTotal = 936m, Devolvido = true },
            new Aluguel { Id = 2, ClienteId = 2, VeiculoId = 4, DataRetirada = new DateTime(2025, 6, 10), DataDevolucaoPrevista = new DateTime(2025, 6, 15), DataDevolucaoReal = null, QuilometragemInicial = 47000, QuilometragemFinal = null, ValorDiaria = 110m, ValorTotal = 550m, Devolvido = false },
            new Aluguel { Id = 3, ClienteId = 3, VeiculoId = 5, DataRetirada = new DateTime(2025, 6, 1), DataDevolucaoPrevista = new DateTime(2025, 6, 3), DataDevolucaoReal = new DateTime(2025, 6, 3), QuilometragemInicial = 11800, QuilometragemFinal = 12000, ValorDiaria = 440m, ValorTotal = 880m, Devolvido = true }
        );

        mb.Entity<Pagamento>().HasData(
            new Pagamento { Id = 1, AluguelId = 1, Valor = 936m, Forma = FormaPagamento.Saldo, DataPagamento = new DateTime(2025, 5, 5), Confirmado = true },
            new Pagamento { Id = 2, AluguelId = 3, Valor = 880m, Forma = FormaPagamento.Pix, DataPagamento = new DateTime(2025, 6, 3), Confirmado = true }
        );
    }
}
