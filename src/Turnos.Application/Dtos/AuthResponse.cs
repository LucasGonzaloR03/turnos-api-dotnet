using Turnos.Domain.Entities;

namespace Turnos.Application.Dtos;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; }
}
