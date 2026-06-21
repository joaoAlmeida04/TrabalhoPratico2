using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LocadoraVeiculos.Data;
using LocadoraVeiculos.DTOs;
using LocadoraVeiculos.Models;

namespace LocadoraVeiculos.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FabricantesController : ControllerBase
{
    private readonly AppDbContext _db;
    public FabricantesController(AppDbContext db) => _db = db;

    /// <summary>Lista todos os fabricantes. Filtro opcional por nome.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FabricanteDTO>>> Listar([FromQuery] string? nome)
    {
        var query = _db.Fabricantes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(nome))
            query = query.Where(f => f.Nome.Contains(nome));

        var lista = await query
            .Select(f => new FabricanteDTO(f.Id, f.Nome, f.PaisOrigem))
            .ToListAsync();
        return Ok(lista);
    }

    /// <summary>Busca um fabricante pelo Id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FabricanteDTO>> Obter(int id)
    {
        var f = await _db.Fabricantes.FindAsync(id);
        if (f is null) return NotFound(new { mensagem = "Fabricante não encontrado." });
        return Ok(new FabricanteDTO(f.Id, f.Nome, f.PaisOrigem));
    }

    /// <summary>Cria um novo fabricante.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FabricanteDTO>> Criar(FabricanteInputDTO input)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var f = new Fabricante { Nome = input.Nome, PaisOrigem = input.PaisOrigem };
        _db.Fabricantes.Add(f);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Obter), new { id = f.Id },
            new FabricanteDTO(f.Id, f.Nome, f.PaisOrigem));
    }

    /// <summary>Atualiza um fabricante existente.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(int id, FabricanteInputDTO input)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var f = await _db.Fabricantes.FindAsync(id);
        if (f is null) return NotFound(new { mensagem = "Fabricante não encontrado." });

        f.Nome = input.Nome;
        f.PaisOrigem = input.PaisOrigem;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Remove um fabricante (bloqueado se houver veículos vinculados).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remover(int id)
    {
        var f = await _db.Fabricantes.Include(x => x.Veiculos).FirstOrDefaultAsync(x => x.Id == id);
        if (f is null) return NotFound(new { mensagem = "Fabricante não encontrado." });
        if (f.Veiculos.Any())
            return Conflict(new { mensagem = "Não é possível excluir: há veículos vinculados a este fabricante." });

        _db.Fabricantes.Remove(f);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
