using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocadoraVeiculos.Models;

public class Aluguel
{
    [Key]
    public int Id { get; set; }

    // ---- Chaves estrangeiras ----
    [Required]
    public int ClienteId { get; set; }
    [ForeignKey(nameof(ClienteId))]
    public Cliente? Cliente { get; set; }

    [Required]
    public int VeiculoId { get; set; }
    [ForeignKey(nameof(VeiculoId))]
    public Veiculo? Veiculo { get; set; }

    // ---- Período ----
    [Required(ErrorMessage = "A data de retirada é obrigatória.")]
    public DateTime DataRetirada { get; set; }

    [Required(ErrorMessage = "A data prevista de devolução é obrigatória.")]
    public DateTime DataDevolucaoPrevista { get; set; }

    // Preenchida quando o veículo é efetivamente devolvido (null = em aberto)
    public DateTime? DataDevolucaoReal { get; set; }

    // ---- Quilometragem ----
    [Required]
    [Range(0, int.MaxValue)]
    public int QuilometragemInicial { get; set; }

    public int? QuilometragemFinal { get; set; }

    // ---- Valores ----
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorDiaria { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorTotal { get; set; }

    // false = aluguel em andamento; true = veículo devolvido
    public bool Devolvido { get; set; } = false;

    // Navegação: um aluguel pode ter um ou mais pagamentos
    public ICollection<Pagamento> Pagamentos { get; set; } = new List<Pagamento>();
}
