using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocadoraVeiculos.Models;

public class Veiculo
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "O modelo é obrigatório.")]
    [StringLength(100)]
    public string Modelo { get; set; } = string.Empty;

    [Required(ErrorMessage = "O ano de fabricação é obrigatório.")]
    [Range(1950, 2100, ErrorMessage = "Ano de fabricação inválido.")]
    public int AnoFabricacao { get; set; }

    [Required(ErrorMessage = "A quilometragem é obrigatória.")]
    [Range(0, int.MaxValue, ErrorMessage = "A quilometragem não pode ser negativa.")]
    public int Quilometragem { get; set; }

    [StringLength(8)]
    public string? Placa { get; set; }

    [StringLength(30)]
    public string? Cor { get; set; }

    [Required(ErrorMessage = "O valor da diária é obrigatório.")]
    [Range(0.01, 100000, ErrorMessage = "O valor da diária deve ser positivo.")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorDiariaBase { get; set; }

    // true = disponível para aluguel
    public bool Disponivel { get; set; } = true;

    // ---- Chaves estrangeiras ----
    [Required]
    public int FabricanteId { get; set; }
    [ForeignKey(nameof(FabricanteId))]
    public Fabricante? Fabricante { get; set; }

    [Required]
    public int CategoriaVeiculoId { get; set; }
    [ForeignKey(nameof(CategoriaVeiculoId))]
    public CategoriaVeiculo? CategoriaVeiculo { get; set; }

    // Navegação: um veículo pode ter vários aluguéis (ao longo do tempo)
    public ICollection<Aluguel> Alugueis { get; set; } = new List<Aluguel>();
}
