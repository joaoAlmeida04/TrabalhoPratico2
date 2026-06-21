using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LocadoraVeiculos.Data;
using LocadoraVeiculos.DTOs;
using LocadoraVeiculos.Models;

namespace LocadoraVeiculos.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriasController : ControllerBase
{
    private readonly AppDbContext _db;
    public CategoriasController(AppDbContext db) => _db = db;

    /// <summary>Lista todas as categorias.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoriaDTO>>> Listar([FromQuery] string? nome)
    {
        var query = _db.Categorias.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(nome))
            query = query.Where(c => c.Nome.Contains(nome));

        var lista = await query
            .Select(c => new CategoriaDTO(c.Id, c.Nome, c.Descricao, c.FatorPreco))
            .ToListAsync();
        return Ok(lista);
    }

    /// <summary>Busca uma categoria pelo Id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoriaDTO>> Obter(int id)
    {
        var c = await _db.Categorias.FindAsync(id);
        if (c is null) return NotFound(new { mensagem = "Categoria não encontrada." });
        return Ok(new CategoriaDTO(c.Id, c.Nome, c.Descricao, c.FatorPreco));
    }

    /// <summary>Cria uma nova categoria.</summary>
    [HttpPost]
    public async Task<ActionResult<CategoriaDTO>> Criar(CategoriaInputDTO input)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var c = new CategoriaVeiculo { Nome = input.Nome, Descricao = input.Descricao, FatorPreco = input.FatorPreco };
        _db.Categorias.Add(c);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obter), new { id = c.Id },
            new CategoriaDTO(c.Id, c.Nome, c.Descricao, c.FatorPreco));
    }

    /// <summary>Atualiza uma categoria existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, CategoriaInputDTO input)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var c = await _db.Categorias.FindAsync(id);
        if (c is null) return NotFound(new { mensagem = "Categoria não encontrada." });

        c.Nome = input.Nome;
        c.Descricao = input.Descricao;
        c.FatorPreco = input.FatorPreco;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Remove uma categoria (bloqueado se houver veículos vinculados).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id)
    {
        var c = await _db.Categorias.Include(x => x.Veiculos).FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound(new { mensagem = "Categoria não encontrada." });
        if (c.Veiculos.Any())
            return Conflict(new { mensagem = "Não é possível excluir: há veículos nesta categoria." });

        _db.Categorias.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
