using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Turnos.Application.Dtos;
using Turnos.Application.Services;
using Turnos.Domain.Entities;

namespace Turnos.API.Controllers;

[ApiController]
[Route("turnos")]
public class TurnosController(TurnoService service) : ControllerBase
{
    private int UsuarioId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet("mios")]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> Mios(CancellationToken ct)
        => Ok(await service.ListarMiosAsync(UsuarioId, ct));

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Listar(
        [FromQuery] int? profesionalId,
        [FromQuery] DateOnly? fecha,
        [FromQuery] TurnoEstado? estado,
        CancellationToken ct)
        => Ok(await service.ListarAsync(profesionalId, fecha, estado, ct));

    [HttpPost]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> Reservar([FromBody] TurnoRequest request, CancellationToken ct)
        => Ok(await service.ReservarAsync(UsuarioId, request, ct));

    [HttpPut("{id:int}/reprogramar")]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> Reprogramar(int id, [FromBody] ReprogramarRequest request, CancellationToken ct)
        => Ok(await service.ReprogramarAsync(UsuarioId, id, request, ct));

    [HttpPost("{id:int}/cancelar")]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> Cancelar(int id, CancellationToken ct)
        => Ok(await service.CancelarAsync(UsuarioId, id, ct));

    [HttpPost("{id:int}/confirmar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Confirmar(int id, CancellationToken ct)
        => Ok(await service.ConfirmarAsync(id, ct));

    [HttpPost("{id:int}/completar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Completar(int id, CancellationToken ct)
        => Ok(await service.CompletarAsync(id, ct));
}
