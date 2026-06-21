using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocadoraVeiculos.Models;

public class Cliente
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O CPF é obrigatório.")]
    [StringLength(14)]
    public string Cpf { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail em formato inválido.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [StringLength(20)]
    public string? Telefone { get; set; }

    [StringLength(20)]
    public string? Cidade { get; set; }

    // Saldo em carteira - usado pelas funcionalidades de recarga e pagamento (TP2)
    [Column(TypeName = "decimal(10,2)")]
    public decimal Saldo { get; set; } = 0m;

    public DateTime DataCadastro { get; set; } = DateTime.Now;

    // Navegação: um cliente pode ter vários aluguéis
    public ICollection<Aluguel> Alugueis { get; set; } = new List<Aluguel>();
}
