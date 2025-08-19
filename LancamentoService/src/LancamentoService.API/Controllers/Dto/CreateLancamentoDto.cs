using System;
using System.ComponentModel.DataAnnotations;

namespace LancamentoService.API.Controllers.Dto
{
    public class CreateLancamentoDto
    {
        public Guid Id { get; set; } = Guid.Empty;

        [Required]
        public string Descricao { get; set; } = string.Empty;

        public LancamentoService.Domain.TipoLancamento Tipo { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Valor { get; set; }

        public DateTime Data { get; set; } = DateTime.UtcNow;
    }
}
