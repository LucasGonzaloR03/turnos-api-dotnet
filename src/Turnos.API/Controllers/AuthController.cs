using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Turnos.Application.Dtos;
using Turnos.Application.Services;

namespace Turnos.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("registro")]
    public async Task<IActionResult> Registro([FromBody] RegistroRequest request, CancellationToken cancellationToken)
        => Ok(await authService.RegistrarAsync(request, cancellationToken));

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        => Ok(await authService.LoginAsync(request, cancellationToken));

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me() => Ok(new
    {
        Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        Nombre = User.Identity?.Name,
        Email = User.FindFirst(ClaimTypes.Email)?.Value,
        Rol = User.FindFirst(ClaimTypes.Role)?.Value
    });
}
