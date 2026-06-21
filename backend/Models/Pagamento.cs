using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocadoraVeiculos.Models;

public enum FormaPagamento
{
    Saldo = 0,
    CartaoCredito = 1,
    CartaoDebito = 2,
    Pix = 3,
    Dinheiro = 4
}

public class Pagamento
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int AluguelId { get; set; }
    [ForeignKey(nameof(AluguelId))]
    public Aluguel? Aluguel { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Valor { get; set; }

    [Required]
    public FormaPagamento Forma { get; set; } = FormaPagamento.Saldo;

    public DateTime DataPagamento { get; set; } = DateTime.Now;

    // true = pagamento confirmado
    public bool Confirmado { get; set; } = true;
}
