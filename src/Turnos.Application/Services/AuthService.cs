using Turnos.Application.Dtos;
using Turnos.Application.Exceptions;
using Turnos.Application.Interfaces;
using Turnos.Domain.Entities;

namespace Turnos.Application.Services;

public class AuthService(
    IUsuarioRepository usuarioRepository,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator)
{
    public async Task<AuthResponse> RegistrarAsync(RegistroRequest request, CancellationToken cancellationToken = default)
    {
        ValidarRegistro(request);

        var email = request.Email.Trim().ToLowerInvariant();

        if (await usuarioRepository.ExisteEmailAsync(email, cancellationToken))
            throw new InvalidOperationException("Ya existe un usuario con ese email.");

        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(),
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Rol = RolUsuario.Cliente
        };

        await usuarioRepository.AgregarAsync(usuario, cancellationToken);

        return ArmarRespuesta(usuario);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new CredencialesInvalidasException();

        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await usuarioRepository.ObtenerPorEmailAsync(email, cancellationToken);

        if (usuario is null || !passwordHasher.Verificar(request.Password, usuario.PasswordHash))
            throw new CredencialesInvalidasException();

        return ArmarRespuesta(usuario);
    }

    private AuthResponse ArmarRespuesta(Usuario usuario) => new()
    {
        Token = tokenGenerator.Generar(usuario),
        Id = usuario.Id,
        Nombre = usuario.Nombre,
        Email = usuario.Email,
        Rol = usuario.Rol
    };

    private static void ValidarRegistro(RegistroRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new ArgumentException("El nombre es obligatorio.");

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            throw new ArgumentException("El email no es válido.");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");
    }
}
