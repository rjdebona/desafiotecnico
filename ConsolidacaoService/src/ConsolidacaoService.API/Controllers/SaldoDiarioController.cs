using Microsoft.AspNetCore.Mvc;
using ConsolidacaoService.Infrastructure;
using ConsolidacaoService.Domain;

namespace ConsolidacaoService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize]
public class SaldoDiarioController : ControllerBase
{
    private readonly ConsolidacaoDbContext _db;
    private readonly ISaldoDiarioService _service;

    public SaldoDiarioController(ConsolidacaoDbContext db, ISaldoDiarioService service)
    {
        _db = db; _service = service;
    }

    [HttpGet]
    public IActionResult Get([FromQuery] DateTime data)
    {
    // Normaliza para UTC se vier sem Kind (query string "2025-08-18" gera Unspecified)
    var reqDay = data.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(data, DateTimeKind.Utc) : data.ToUniversalTime();
    var start = reqDay.Date;
    var end = start.AddDays(1);
    // Comparação por faixa evita necessidade de funções (date_trunc) no lado do banco
    var lancs = _db.Lancamentos.Where(l => l.Data >= start && l.Data < end).ToList();
    var saldo = _service.CalcularSaldo(lancs, start);
    return Ok(new { data = saldo.Data, saldoTotal = saldo.SaldoTotal, lancamentos = lancs });
    }
}
