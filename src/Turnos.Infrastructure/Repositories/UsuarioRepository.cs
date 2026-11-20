using Microsoft.EntityFrameworkCore;
using Turnos.Application.Interfaces;
using Turnos.Domain.Entities;
using Turnos.Infrastructure.Persistence;

namespace Turnos.Infrastructure.Repositories;

public class UsuarioRepository(TurnosDbContext context) : IUsuarioRepository
{
    public Task<Usuario?> ObtenerPorEmailAsync(string email, CancellationToken cancellationToken = default)
        => context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken = default)
        => context.Usuarios.AnyAsync(u => u.Email == email, cancellationToken);

    public async Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync(cancellationToken);
    }
}
