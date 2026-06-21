using System.ComponentModel.DataAnnotations;

namespace LocadoraVeiculos.Models;

public class CategoriaVeiculo
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome da categoria é obrigatório.")]
    [StringLength(50)]
    public string Nome { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Descricao { get; set; }

    // Fator multiplicador sobre a diária base do veículo (ex.: luxo = 1.5)
    [Range(0.1, 10.0)]
    public decimal FatorPreco { get; set; } = 1.0m;

    // Navegação: uma categoria possui vários veículos
    public ICollection<Veiculo> Veiculos { get; set; } = new List<Veiculo>();
}
