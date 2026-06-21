using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LocadoraVeiculos.Data;
using LocadoraVeiculos.DTOs;
using LocadoraVeiculos.Models;

namespace LocadoraVeiculos.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PagamentosController : ControllerBase
{
    private readonly AppDbContext _db;
    public PagamentosController(AppDbContext db) => _db = db;

    private static PagamentoDTO Projetar(Pagamento p) => new(
        p.Id, p.AluguelId, p.Valor, p.Forma, p.DataPagamento, p.Confirmado);

    /// <summary>Lista pagamentos, com filtro opcional por aluguel.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PagamentoDTO>>> Listar([FromQuery] int? aluguelId)
    {
        var query = _db.Pagamentos.AsNoTracking().AsQueryable();
        if (aluguelId.HasValue)
            query = query.Where(p => p.AluguelId == aluguelId.Value);

        var lista = await query
            .OrderByDescending(p => p.DataPagamento)
            .Select(p => Projetar(p))
            .ToListAsync();
        return Ok(lista);
    }

    /// <summary>Busca um pagamento pelo Id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PagamentoDTO>> Obter(int id)
    {
        var p = await _db.Pagamentos.FindAsync(id);
        if (p is null) return NotFound(new { mensagem = "Pagamento não encontrado." });
        return Ok(Projetar(p));
    }

    /// <summary>
    /// Registra um pagamento. Se a forma for "Saldo", debita da carteira do cliente
    /// (valida saldo suficiente). Funcionalidade de pagamento do TP2.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PagamentoDTO>> Criar(PagamentoInputDTO input)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var aluguel = await _db.Alugueis
            .Include(a => a.Cliente)
            .FirstOrDefaultAsync(a => a.Id == input.AluguelId);
        if (aluguel is null) return BadRequest(new { mensagem = "Aluguel não existe." });

        if (input.Forma == FormaPagamento.Saldo)
        {
            if (aluguel.Cliente!.Saldo < input.Valor)
                return BadRequest(new { mensagem = "Saldo insuficiente para este pagamento." });
            aluguel.Cliente.Saldo -= input.Valor;
        }

        var pagamento = new Pagamento
        {
            AluguelId = input.AluguelId,
            Valor = input.Valor,
            Forma = input.Forma,
            DataPagamento = DateTime.Now,
            Confirmado = true
        };
        _db.Pagamentos.Add(pagamento);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obter), new { id = pagamento.Id }, Projetar(pagamento));
    }

    /// <summary>Remove um pagamento (estorna ao saldo se foi pago via saldo).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id)
    {
        var p = await _db.Pagamentos
            .Include(x => x.Aluguel).ThenInclude(a => a!.Cliente)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound(new { mensagem = "Pagamento não encontrado." });

        // Estorna ao saldo se tiver sido pago via saldo
        if (p.Forma == FormaPagamento.Saldo && p.Aluguel?.Cliente is not null)
            p.Aluguel.Cliente.Saldo += p.Valor;

        _db.Pagamentos.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
