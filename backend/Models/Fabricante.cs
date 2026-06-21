using System.ComponentModel.DataAnnotations;

namespace LocadoraVeiculos.Models;

public class Fabricante
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome do fabricante é obrigatório.")]
    [StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [StringLength(50)]
    public string? PaisOrigem { get; set; }

    // Navegação: um fabricante possui vários veículos
    public ICollection<Veiculo> Veiculos { get; set; } = new List<Veiculo>();
}
