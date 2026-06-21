using System.ComponentModel.DataAnnotations;
using LocadoraVeiculos.Models;

namespace LocadoraVeiculos.DTOs;

// ---------- Fabricante ----------
public record FabricanteDTO(int Id, string Nome, string? PaisOrigem);
public class FabricanteInputDTO
{
    [Required] [StringLength(100)] public string Nome { get; set; } = string.Empty;
    [StringLength(50)] public string? PaisOrigem { get; set; }
}

// ---------- Categoria ----------
public record CategoriaDTO(int Id, string Nome, string? Descricao, decimal FatorPreco);
public class CategoriaInputDTO
{
    [Required] [StringLength(50)] public string Nome { get; set; } = string.Empty;
    [StringLength(200)] public string? Descricao { get; set; }
    [Range(0.1, 10.0)] public decimal FatorPreco { get; set; } = 1.0m;
}

// ---------- Veiculo ----------
public record VeiculoDTO(
    int Id, string Modelo, int AnoFabricacao, int Quilometragem,
    string? Placa, string? Cor, decimal ValorDiariaBase, bool Disponivel,
    int FabricanteId, string? FabricanteNome,
    int CategoriaVeiculoId, string? CategoriaNome);

public class VeiculoInputDTO
{
    [Required] [StringLength(100)] public string Modelo { get; set; } = string.Empty;
    [Range(1950, 2100)] public int AnoFabricacao { get; set; }
    [Range(0, int.MaxValue)] public int Quilometragem { get; set; }
    [StringLength(8)] public string? Placa { get; set; }
    [StringLength(30)] public string? Cor { get; set; }
    [Range(0.01, 100000)] public decimal ValorDiariaBase { get; set; }
    public bool Disponivel { get; set; } = true;
    [Required] public int FabricanteId { get; set; }
    [Required] public int CategoriaVeiculoId { get; set; }
}

// ---------- Cliente ----------
public record ClienteDTO(
    int Id, string Nome, string Cpf, string Email,
    string? Telefone, string? Cidade, decimal Saldo, DateTime DataCadastro);

public class ClienteInputDTO
{
    [Required] [StringLength(150)] public string Nome { get; set; } = string.Empty;
    [Required] [StringLength(14)] public string Cpf { get; set; } = string.Empty;
    [Required] [EmailAddress] [StringLength(150)] public string Email { get; set; } = string.Empty;
    [Phone] [StringLength(20)] public string? Telefone { get; set; }
    [StringLength(20)] public string? Cidade { get; set; }
}

public class RecargaDTO
{
    [Range(0.01, 100000, ErrorMessage = "O valor da recarga deve ser positivo.")]
    public decimal Valor { get; set; }
}

// ---------- Aluguel ----------
public record AluguelDTO(
    int Id, int ClienteId, string? ClienteNome,
    int VeiculoId, string? VeiculoModelo,
    DateTime DataRetirada, DateTime DataDevolucaoPrevista, DateTime? DataDevolucaoReal,
    int QuilometragemInicial, int? QuilometragemFinal,
    decimal ValorDiaria, decimal ValorTotal, bool Devolvido);

public class AluguelInputDTO
{
    [Required] public int ClienteId { get; set; }
    [Required] public int VeiculoId { get; set; }
    [Required] public DateTime DataRetirada { get; set; }
    [Required] public DateTime DataDevolucaoPrevista { get; set; }
    [Range(0, int.MaxValue)] public int QuilometragemInicial { get; set; }
}

public class DevolucaoDTO
{
    [Required] public DateTime DataDevolucaoReal { get; set; }
    [Range(0, int.MaxValue)] public int QuilometragemFinal { get; set; }
}

// ---------- Pagamento ----------
public record PagamentoDTO(
    int Id, int AluguelId, decimal Valor, FormaPagamento Forma,
    DateTime DataPagamento, bool Confirmado);

public class PagamentoInputDTO
{
    [Required] public int AluguelId { get; set; }
    [Required] [Range(0.01, 100000)] public decimal Valor { get; set; }
    [Required] public FormaPagamento Forma { get; set; } = FormaPagamento.Saldo;
}
