using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Turnos.Application.Dtos;
using Turnos.Application.Services;

namespace Turnos.API.Controllers;

[ApiController]
[Route("profesionales")]
public class ProfesionalesController(ProfesionalService service, TurnoService turnoService) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Listar(CancellationToken ct)
        => Ok(await service.ListarAsync(ct));

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
        => Ok(await service.ObtenerAsync(id, ct));

    [HttpGet("{id:int}/disponibilidad")]
    [Authorize]
    public async Task<IActionResult> Disponibilidad(
        int id, [FromQuery] DateOnly fecha, [FromQuery] int? servicioId, CancellationToken ct)
        => Ok(await turnoService.ObtenerDisponibilidadAsync(id, fecha, servicioId, ct));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Crear([FromBody] ProfesionalRequest request, CancellationToken ct)
        => Ok(await service.CrearAsync(request, ct));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ProfesionalRequest request, CancellationToken ct)
        => Ok(await service.ActualizarAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await service.EliminarAsync(id, ct);
        return NoContent();
    }
}
