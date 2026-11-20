using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Turnos.Application.Interfaces;
using Turnos.Domain.Entities;

namespace Turnos.Infrastructure.Security;

public class JwtTokenGenerator(JwtOptions options) : ITokenGenerator
{
    public string Generar(Usuario usuario)
    {
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol.ToString())
        ];

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(options.ExpiracionMinutos),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
