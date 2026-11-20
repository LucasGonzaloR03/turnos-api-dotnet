using Turnos.Domain.Entities;

namespace Turnos.Application.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default);
}
