using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LocadoraVeiculos.Data;
using LocadoraVeiculos.DTOs;
using LocadoraVeiculos.Models;

namespace LocadoraVeiculos.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ClientesController : ControllerBase
{
    private readonly AppDbContext _db;
    public ClientesController(AppDbContext db) => _db = db;

    private static ClienteDTO Projetar(Cliente c) => new(
        c.Id, c.Nome, c.Cpf, c.Email, c.Telefone, c.Cidade, c.Saldo, c.DataCadastro);

    /// <summary>
    /// Lista clientes com filtros personalizados (nome e cidade — pelo menos 2 campos).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClienteDTO>>> Listar(
        [FromQuery] string? nome,
        [FromQuery] string? cidade)
    {
        var query = _db.Clientes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(nome))
            query = query.Where(c => c.Nome.Contains(nome));
        if (!string.IsNullOrWhiteSpace(cidade))
            query = query.Where(c => c.Cidade != null && c.Cidade.Contains(cidade));

        var lista = await query.Select(c => Projetar(c)).ToListAsync();
        return Ok(lista);
    }

    /// <summary>Busca um cliente pelo Id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteDTO>> Obter(int id)
    {
        var c = await _db.Clientes.FindAsync(id);
        if (c is null) return NotFound(new { mensagem = "Cliente não encontrado." });
        return Ok(Projetar(c));
    }

    /// <summary>Consulta apenas o saldo atual do cliente (funcionalidade TP2).</summary>
    [HttpGet("{id:int}/saldo")]
    public async Task<ActionResult<object>> ConsultarSaldo(int id)
    {
        var c = await _db.Clientes.FindAsync(id);
        if (c is null) return NotFound(new { mensagem = "Cliente não encontrado." });
        return Ok(new { clienteId = c.Id, nome = c.Nome, saldo = c.Saldo });
    }

    /// <summary>
    /// FILTRO 3 (LEFT JOIN): lista clientes com a quantidade de aluguéis e total gasto.
    /// Usa LEFT JOIN para incluir também clientes que nunca alugaram.
    /// </summary>
    [HttpGet("com-resumo-alugueis")]
    public async Task<ActionResult<object>> ComResumoAlugueis()
    {
        var resultado = await _db.Clientes
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.Nome,
                c.Cidade,
                QtdAlugueis = c.Alugueis.Count(),                       // LEFT JOIN implícito
                TotalGasto = c.Alugueis.Sum(a => (decimal?)a.ValorTotal) ?? 0m
            })
            .OrderByDescending(x => x.TotalGasto)
            .ToListAsync();
        return Ok(resultado);
    }

    /// <summary>Cria um novo cliente. Valida unicidade de CPF e e-mail.</summary>
    [HttpPost]
    public async Task<ActionResult<ClienteDTO>> Criar(ClienteInputDTO input)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (await _db.Clientes.AnyAsync(c => c.Cpf == input.Cpf))
            return Conflict(new { mensagem = "Já existe um cliente com este CPF." });
        if (await _db.Clientes.AnyAsync(c => c.Email == input.Email))
            return Conflict(new { mensagem = "Já existe um cliente com este e-mail." });

        var c = new Cliente
        {
            Nome = input.Nome,
            Cpf = input.Cpf,
            Email = input.Email,
            Telefone = input.Telefone,
            Cidade = input.Cidade,
            Saldo = 0m,
            DataCadastro = DateTime.Now
        };
        _db.Clientes.Add(c);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obter), new { id = c.Id }, Projetar(c));
    }

    /// <summary>Atualiza um cliente existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, ClienteInputDTO input)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var c = await _db.Clientes.FindAsync(id);
        if (c is null) return NotFound(new { mensagem = "Cliente não encontrado." });

        // Verifica conflito de CPF/e-mail com OUTROS clientes
        if (await _db.Clientes.AnyAsync(x => x.Cpf == input.Cpf && x.Id != id))
            return Conflict(new { mensagem = "CPF já utilizado por outro cliente." });
        if (await _db.Clientes.AnyAsync(x => x.Email == input.Email && x.Id != id))
            return Conflict(new { mensagem = "E-mail já utilizado por outro cliente." });

        c.Nome = input.Nome;
        c.Cpf = input.Cpf;
        c.Email = input.Email;
        c.Telefone = input.Telefone;
        c.Cidade = input.Cidade;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Recarrega o saldo do cliente (funcionalidade TP2).</summary>
    [HttpPost("{id:int}/recarga")]
    public async Task<ActionResult<object>> Recarregar(int id, RecargaDTO input)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var c = await _db.Clientes.FindAsync(id);
        if (c is null) return NotFound(new { mensagem = "Cliente não encontrado." });

        c.Saldo += input.Valor;
        await _db.SaveChangesAsync();
        return Ok(new { clienteId = c.Id, nome = c.Nome, novoSaldo = c.Saldo });
    }

    /// <summary>Remove um cliente (bloqueado se houver aluguéis vinculados).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id)
    {
        var c = await _db.Clientes.Include(x => x.Alugueis).FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound(new { mensagem = "Cliente não encontrado." });
        if (c.Alugueis.Any())
            return Conflict(new { mensagem = "Não é possível excluir: há aluguéis vinculados a este cliente." });

        _db.Clientes.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
