using System.ComponentModel.DataAnnotations;

namespace LancamentoService.API.Controllers.Dto
{
    public class CreateFluxoDto
    {
        [Required]
        public string Nome { get; set; } = string.Empty;
    }
}
