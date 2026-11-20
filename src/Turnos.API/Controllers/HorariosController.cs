using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Turnos.Application.Dtos;
using Turnos.Application.Services;

namespace Turnos.API.Controllers;

[ApiController]
[Route("horarios")]
public class HorariosController(HorarioAtencionService service) : ControllerBase
{
    [HttpGet("profesional/{profesionalId:int}")]
    [Authorize]
    public async Task<IActionResult> ListarPorProfesional(int profesionalId, CancellationToken ct)
        => Ok(await service.ListarPorProfesionalAsync(profesionalId, ct));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Crear([FromBody] HorarioAtencionRequest request, CancellationToken ct)
        => Ok(await service.CrearAsync(request, ct));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] HorarioAtencionRequest request, CancellationToken ct)
        => Ok(await service.ActualizarAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await service.EliminarAsync(id, ct);
        return NoContent();
    }
}
