using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LocadoraVeiculos.Data;
using LocadoraVeiculos.DTOs;
using LocadoraVeiculos.Models;

namespace LocadoraVeiculos.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AlugueisController : ControllerBase
{
    private readonly AppDbContext _db;
    public AlugueisController(AppDbContext db) => _db = db;

    private static AluguelDTO Projetar(Aluguel a) => new(
        a.Id, a.ClienteId, a.Cliente != null ? a.Cliente.Nome : null,
        a.VeiculoId, a.Veiculo != null ? a.Veiculo.Modelo : null,
        a.DataRetirada, a.DataDevolucaoPrevista, a.DataDevolucaoReal,
        a.QuilometragemInicial, a.QuilometragemFinal,
        a.ValorDiaria, a.ValorTotal, a.Devolvido);

    /// <summary>
    /// Lista aluguéis com filtros personalizados (cliente e status — pelo menos 2 campos).
    /// Faz INNER JOIN com Cliente e Veiculo.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AluguelDTO>>> Listar(
        [FromQuery] int? clienteId,
        [FromQuery] bool? devolvido)
    {
        var query = _db.Alugueis
            .Include(a => a.Cliente)
            .Include(a => a.Veiculo)
            .AsNoTracking()
            .AsQueryable();

        if (clienteId.HasValue)
            query = query.Where(a => a.ClienteId == clienteId.Value);
        if (devolvido.HasValue)
            query = query.Where(a => a.Devolvido == devolvido.Value);

        var lista = await query
            .OrderByDescending(a => a.DataRetirada)
            .Select(a => Projetar(a))
            .ToListAsync();
        return Ok(lista);
    }

    /// <summary>Busca um aluguel pelo Id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AluguelDTO>> Obter(int id)
    {
        var a = await _db.Alugueis
            .Include(x => x.Cliente)
            .Include(x => x.Veiculo)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
        if (a is null) return NotFound(new { mensagem = "Aluguel não encontrado." });
        return Ok(Projetar(a));
    }

    /// <summary>
    /// FILTRO 4 (MÚLTIPLOS INNER JOINs): relatório detalhado de aluguéis cruzando
    /// Aluguel → Cliente, Aluguel → Veiculo → Fabricante → Categoria.
    /// Filtros opcionais por período de retirada.
    /// </summary>
    [HttpGet("relatorio-detalhado")]
    public async Task<ActionResult<object>> RelatorioDetalhado(
        [FromQuery] DateTime? de,
        [FromQuery] DateTime? ate)
    {
        var query = _db.Alugueis
            .Include(a => a.Cliente)
            .Include(a => a.Veiculo).ThenInclude(v => v!.Fabricante)
            .Include(a => a.Veiculo).ThenInclude(v => v!.CategoriaVeiculo)
            .AsNoTracking()
            .AsQueryable();

        if (de.HasValue) query = query.Where(a => a.DataRetirada >= de.Value);
        if (ate.HasValue) query = query.Where(a => a.DataRetirada <= ate.Value);

        var resultado = await query
            .OrderByDescending(a => a.DataRetirada)
            .Select(a => new
            {
                a.Id,
                Cliente = a.Cliente!.Nome,
                Veiculo = a.Veiculo!.Modelo,
                Fabricante = a.Veiculo.Fabricante!.Nome,
                Categoria = a.Veiculo.CategoriaVeiculo!.Nome,
                a.DataRetirada,
                a.DataDevolucaoPrevista,
                a.DataDevolucaoReal,
                a.ValorTotal,
                Status = a.Devolvido ? "Devolvido" : "Em andamento"
            })
            .ToListAsync();
        return Ok(resultado);
    }

    /// <summary>
    /// FILTRO 5 (INNER JOIN + agrupamento): faturamento total por fabricante,
    /// cruzando Aluguel → Veiculo → Fabricante.
    /// </summary>
    [HttpGet("faturamento-por-fabricante")]
    public async Task<ActionResult<object>> FaturamentoPorFabricante()
    {
        var resultado = await _db.Alugueis
            .Include(a => a.Veiculo).ThenInclude(v => v!.Fabricante)
            .AsNoTracking()
            .GroupBy(a => a.Veiculo!.Fabricante!.Nome)
            .Select(g => new
            {
                Fabricante = g.Key,
                QtdAlugueis = g.Count(),
                Faturamento = g.Sum(a => a.ValorTotal)
            })
            .OrderByDescending(x => x.Faturamento)
            .ToListAsync();
        return Ok(resultado);
    }

    /// <summary>
    /// Cria um novo aluguel. Calcula o valor da diária (diária base * fator da categoria),
    /// o valor total pelo período previsto, marca o veículo como indisponível.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AluguelDTO>> Criar(AluguelInputDTO input)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (input.DataDevolucaoPrevista <= input.DataRetirada)
            return BadRequest(new { mensagem = "A devolução prevista deve ser posterior à retirada." });

        var cliente = await _db.Clientes.FindAsync(input.ClienteId);
        if (cliente is null) return BadRequest(new { mensagem = "Cliente não existe." });

        var veiculo = await _db.Veiculos
            .Include(v => v.CategoriaVeiculo)
            .FirstOrDefaultAsync(v => v.Id == input.VeiculoId);
        if (veiculo is null) return BadRequest(new { mensagem = "Veículo não existe." });
        if (!veiculo.Disponivel)
            return Conflict(new { mensagem = "Veículo indisponível para aluguel." });

        var fator = veiculo.CategoriaVeiculo?.FatorPreco ?? 1.0m;
        var valorDiaria = Math.Round(veiculo.ValorDiariaBase * fator, 2);
        var dias = (input.DataDevolucaoPrevista.Date - input.DataRetirada.Date).Days;
        if (dias < 1) dias = 1;
        var valorTotal = valorDiaria * dias;

        var aluguel = new Aluguel
        {
            ClienteId = input.ClienteId,
            VeiculoId = input.VeiculoId,
            DataRetirada = input.DataRetirada,
            DataDevolucaoPrevista = input.DataDevolucaoPrevista,
            QuilometragemInicial = input.QuilometragemInicial,
            ValorDiaria = valorDiaria,
            ValorTotal = valorTotal,
            Devolvido = false
        };

        veiculo.Disponivel = false;   // bloqueia o veículo durante o aluguel
        _db.Alugueis.Add(aluguel);
        await _db.SaveChangesAsync();

        await _db.Entry(aluguel).Reference(x => x.Cliente).LoadAsync();
        await _db.Entry(aluguel).Reference(x => x.Veiculo).LoadAsync();
        return CreatedAtAction(nameof(Obter), new { id = aluguel.Id }, Projetar(aluguel));
    }

    /// <summary>
    /// Registra a devolução do veículo: grava km final, data real, recalcula o valor
    /// total se houve atraso, libera o veículo e atualiza sua quilometragem.
    /// </summary>
    [HttpPut("{id:int}/devolucao")]
    public async Task<ActionResult<AluguelDTO>> RegistrarDevolucao(int id, DevolucaoDTO input)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var aluguel = await _db.Alugueis
            .Include(a => a.Veiculo)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (aluguel is null) return NotFound(new { mensagem = "Aluguel não encontrado." });
        if (aluguel.Devolvido) return Conflict(new { mensagem = "Este aluguel já foi devolvido." });
        if (input.QuilometragemFinal < aluguel.QuilometragemInicial)
            return BadRequest(new { mensagem = "Km final não pode ser menor que a inicial." });

        aluguel.DataDevolucaoReal = input.DataDevolucaoReal;
        aluguel.QuilometragemFinal = input.QuilometragemFinal;
        aluguel.Devolvido = true;

        // Recalcula valor total considerando dias efetivos (atraso gera diárias extras)
        var diasReais = (input.DataDevolucaoReal.Date - aluguel.DataRetirada.Date).Days;
        if (diasReais < 1) diasReais = 1;
        aluguel.ValorTotal = aluguel.ValorDiaria * diasReais;

        if (aluguel.Veiculo is not null)
        {
            aluguel.Veiculo.Disponivel = true;
            aluguel.Veiculo.Quilometragem = input.QuilometragemFinal;
        }

        await _db.SaveChangesAsync();
        await _db.Entry(aluguel).Reference(x => x.Cliente).LoadAsync();
        return Ok(Projetar(aluguel));
    }

    /// <summary>Remove um aluguel (e seus pagamentos por cascata). Libera o veículo.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id)
    {
        var a = await _db.Alugueis.Include(x => x.Veiculo).FirstOrDefaultAsync(x => x.Id == id);
        if (a is null) return NotFound(new { mensagem = "Aluguel não encontrado." });

        if (a.Veiculo is not null && !a.Devolvido)
            a.Veiculo.Disponivel = true;

        _db.Alugueis.Remove(a);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
