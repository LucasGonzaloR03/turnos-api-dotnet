using Microsoft.EntityFrameworkCore;
using Turnos.Application.Interfaces;
using Turnos.Domain.Entities;
using Turnos.Infrastructure.Persistence;

namespace Turnos.Infrastructure.Repositories;

public class ServicioRepository(TurnosDbContext context) : IServicioRepository
{
    public async Task<IReadOnlyList<Servicio>> ListarAsync(CancellationToken ct = default)
        => await context.Servicios.AsNoTracking().OrderBy(s => s.Id).ToListAsync(ct);

    public Task<Servicio?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        => context.Servicios.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> ExisteAsync(int id, CancellationToken ct = default)
        => context.Servicios.AnyAsync(s => s.Id == id, ct);

    public async Task AgregarAsync(Servicio servicio, CancellationToken ct = default)
    {
        context.Servicios.Add(servicio);
        await context.SaveChangesAsync(ct);
    }

    public async Task ActualizarAsync(Servicio servicio, CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }

    public async Task EliminarAsync(Servicio servicio, CancellationToken ct = default)
    {
        context.Servicios.Remove(servicio);
        await context.SaveChangesAsync(ct);
    }
}
