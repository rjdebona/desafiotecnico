using System;
using System.Linq;
using LancamentoService.Domain;
using LancamentoService.Infrastructure;
using LancamentoService.API.Controllers.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LancamentoService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FluxoDeCaixaController : ControllerBase
    {
        private readonly FluxoDeCaixaRepository _repository;
        private readonly ILogger<FluxoDeCaixaController> _logger;

        public FluxoDeCaixaController(FluxoDeCaixaRepository repository, ILogger<FluxoDeCaixaController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var fluxos = _repository.GetAll().ToList();
            _logger.LogInformation("GetAll returned {Count} items", fluxos.Count);
            return Ok(fluxos);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(Guid id)
        {
            var fluxo = _repository.GetById(id);
            if (fluxo == null) return NotFound();
            return Ok(fluxo);
        }

        [HttpPost]
        public IActionResult Add([FromBody] Controllers.Dto.CreateFluxoDto dto)
        {
            _logger.LogInformation("Add called with payload: {@Fluxo}", dto);
            var fluxo = new FluxoDeCaixa(Guid.NewGuid(), dto.Nome);
            _repository.Add(fluxo);
            _logger.LogInformation("Added fluxo with id {Id}", fluxo.Id);
            return CreatedAtAction(nameof(GetById), new { id = fluxo.Id }, fluxo);
        }

        [HttpPut("{id}")]
        public IActionResult Update(Guid id, [FromBody] FluxoDeCaixa fluxo)
        {
            FluxoDeCaixa _fluxo = new FluxoDeCaixa(id, fluxo.Nome, fluxo.Lancamentos);
            _repository.Update(_fluxo);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id)
        {
            _repository.Delete(id);
            return NoContent();
        }

        [HttpPost("{fluxoId}/lancamentos")]
        public IActionResult AddLancamento(Guid fluxoId, [FromBody] CreateLancamentoDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var fluxo = _repository.GetById(fluxoId);
            if (fluxo == null) return NotFound(new { error = "Fluxo não encontrado" });

            var lId = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id;
            var _lancamento = new Lancamento(lId, dto.Tipo, dto.Valor, dto.Data, dto.Descricao, fluxoId);

            // Use the tracked entity returned by repository to avoid EF Core tracking conflicts
            fluxo.AddLancamento(_lancamento);
            _repository.Update(fluxo); // salva o agregado completo e publica evento FluxoFull

            return Created($"api/FluxoDeCaixa/{fluxoId}/lancamentos/{_lancamento.Id}", _lancamento);
        }

        [HttpPut("{fluxoId}/lancamentos/{lancamentoId}")]
        public IActionResult UpdateLancamento(Guid fluxoId, Guid lancamentoId, [FromBody] Lancamento lancamento)
        {
            _repository.UpdateLancamento(fluxoId, lancamentoId, lancamento);
            return NoContent();
        }

        [HttpDelete("{fluxoId}/lancamentos/{lancamentoId}")]
        public IActionResult DeleteLancamento(Guid fluxoId, Guid lancamentoId)
        {
            _repository.DeleteLancamento(fluxoId, lancamentoId);
            return NoContent();
        }
    }
}
