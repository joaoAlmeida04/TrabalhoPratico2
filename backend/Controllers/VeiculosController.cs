using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LocadoraVeiculos.Data;
using LocadoraVeiculos.DTOs;
using LocadoraVeiculos.Models;

namespace LocadoraVeiculos.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class VeiculosController : ControllerBase
{
    private readonly AppDbContext _db;
    public VeiculosController(AppDbContext db) => _db = db;

    // Projeção reaproveitada (faz o INNER JOIN com Fabricante e Categoria)
    private static VeiculoDTO Projetar(Veiculo v) => new(
        v.Id, v.Modelo, v.AnoFabricacao, v.Quilometragem, v.Placa, v.Cor,
        v.ValorDiariaBase, v.Disponivel,
        v.FabricanteId, v.Fabricante != null ? v.Fabricante.Nome : null,
        v.CategoriaVeiculoId, v.CategoriaVeiculo != null ? v.CategoriaVeiculo.Nome : null);

    /// <summary>
    /// Lista veículos com filtros opcionais combináveis (pesquisa com filtros personalizados).
    /// Realiza INNER JOIN entre Veiculo, Fabricante e Categoria.
    /// </summary>
    /// <param name="modelo">Filtro parcial por modelo.</param>
    /// <param name="fabricanteId">Filtro por fabricante.</param>
    /// <param name="categoriaId">Filtro por categoria.</param>
    /// <param name="disponivel">Filtro por disponibilidade.</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VeiculoDTO>>> Listar(
        [FromQuery] string? modelo,
        [FromQuery] int? fabricanteId,
        [FromQuery] int? categoriaId,
        [FromQuery] bool? disponivel)
    {
        var query = _db.Veiculos
            .Include(v => v.Fabricante)
            .Include(v => v.CategoriaVeiculo)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(modelo))
            query = query.Where(v => v.Modelo.Contains(modelo));
        if (fabricanteId.HasValue)
            query = query.Where(v => v.FabricanteId == fabricanteId.Value);
        if (categoriaId.HasValue)
            query = query.Where(v => v.CategoriaVeiculoId == categoriaId.Value);
        if (disponivel.HasValue)
            query = query.Where(v => v.Disponivel == disponivel.Value);

        var lista = await query.Select(v => Projetar(v)).ToListAsync();
        return Ok(lista);
    }

    /// <summary>Busca um veículo pelo Id (com fabricante e categoria).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<VeiculoDTO>> Obter(int id)
    {
        var v = await _db.Veiculos
            .Include(x => x.Fabricante)
            .Include(x => x.CategoriaVeiculo)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (v is null) return NotFound(new { mensagem = "Veículo não encontrado." });
        return Ok(Projetar(v));
    }

    /// <summary>
    /// FILTRO 1 (INNER JOIN): veículos disponíveis dentro de uma faixa de valor de diária,
    /// trazendo o nome do fabricante e da categoria.
    /// </summary>
    [HttpGet("disponiveis-por-preco")]
    public async Task<ActionResult<IEnumerable<VeiculoDTO>>> DisponiveisPorPreco(
        [FromQuery] decimal precoMin = 0,
        [FromQuery] decimal precoMax = decimal.MaxValue)
    {
        var lista = await _db.Veiculos
            .Include(v => v.Fabricante)
            .Include(v => v.CategoriaVeiculo)
            .AsNoTracking()
            .Where(v => v.Disponivel
                        && v.ValorDiariaBase >= precoMin
                        && v.ValorDiariaBase <= precoMax)
            .OrderBy(v => v.ValorDiariaBase)
            .Select(v => Projetar(v))
            .ToListAsync();
        return Ok(lista);
    }

    /// <summary>
    /// FILTRO 2 (INNER JOIN + agrupamento): contagem de veículos por fabricante.
    /// </summary>
    [HttpGet("contagem-por-fabricante")]
    public async Task<ActionResult<object>> ContagemPorFabricante()
    {
        var resultado = await _db.Veiculos
            .Include(v => v.Fabricante)
            .AsNoTracking()
            .GroupBy(v => v.Fabricante!.Nome)
            .Select(g => new { Fabricante = g.Key, Quantidade = g.Count() })
            .OrderByDescending(x => x.Quantidade)
            .ToListAsync();
        return Ok(resultado);
    }

    /// <summary>Cria um novo veículo. Valida existência de fabricante e categoria.</summary>
    [HttpPost]
    public async Task<ActionResult<VeiculoDTO>> Criar(VeiculoInputDTO input)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (!await _db.Fabricantes.AnyAsync(f => f.Id == input.FabricanteId))
            return BadRequest(new { mensagem = "Fabricante informado não existe." });
        if (!await _db.Categorias.AnyAsync(c => c.Id == input.CategoriaVeiculoId))
            return BadRequest(new { mensagem = "Categoria informada não existe." });

        var v = new Veiculo
        {
            Modelo = input.Modelo,
            AnoFabricacao = input.AnoFabricacao,
            Quilometragem = input.Quilometragem,
            Placa = input.Placa,
            Cor = input.Cor,
            ValorDiariaBase = input.ValorDiariaBase,
            Disponivel = input.Disponivel,
            FabricanteId = input.FabricanteId,
            CategoriaVeiculoId = input.CategoriaVeiculoId
        };
        _db.Veiculos.Add(v);
        await _db.SaveChangesAsync();

        await _db.Entry(v).Reference(x => x.Fabricante).LoadAsync();
        await _db.Entry(v).Reference(x => x.CategoriaVeiculo).LoadAsync();
        return CreatedAtAction(nameof(Obter), new { id = v.Id }, Projetar(v));
    }

    /// <summary>Atualiza um veículo existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, VeiculoInputDTO input)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var v = await _db.Veiculos.FindAsync(id);
        if (v is null) return NotFound(new { mensagem = "Veículo não encontrado." });

        if (!await _db.Fabricantes.AnyAsync(f => f.Id == input.FabricanteId))
            return BadRequest(new { mensagem = "Fabricante informado não existe." });
        if (!await _db.Categorias.AnyAsync(c => c.Id == input.CategoriaVeiculoId))
            return BadRequest(new { mensagem = "Categoria informada não existe." });

        v.Modelo = input.Modelo;
        v.AnoFabricacao = input.AnoFabricacao;
        v.Quilometragem = input.Quilometragem;
        v.Placa = input.Placa;
        v.Cor = input.Cor;
        v.ValorDiariaBase = input.ValorDiariaBase;
        v.Disponivel = input.Disponivel;
        v.FabricanteId = input.FabricanteId;
        v.CategoriaVeiculoId = input.CategoriaVeiculoId;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Remove um veículo (bloqueado se houver aluguéis vinculados).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id)
    {
        var v = await _db.Veiculos.Include(x => x.Alugueis).FirstOrDefaultAsync(x => x.Id == id);
        if (v is null) return NotFound(new { mensagem = "Veículo não encontrado." });
        if (v.Alugueis.Any())
            return Conflict(new { mensagem = "Não é possível excluir: há aluguéis vinculados a este veículo." });

        _db.Veiculos.Remove(v);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
