using Microsoft.EntityFrameworkCore;
using Turnos.Application.Interfaces;
using Turnos.Domain.Entities;
using Turnos.Infrastructure.Persistence;

namespace Turnos.Infrastructure.Repositories;

public class ProfesionalRepository(TurnosDbContext context) : IProfesionalRepository
{
    public async Task<IReadOnlyList<Profesional>> ListarAsync(bool incluirServicios = false, CancellationToken ct = default)
    {
        IQueryable<Profesional> query = context.Profesionales;

        if (incluirServicios)
            query = query.Include(p => p.Servicios);

        return await query.AsNoTracking().OrderBy(p => p.Id).ToListAsync(ct);
    }

    public Task<Profesional?> ObtenerPorIdAsync(int id, bool incluirServicios = false, CancellationToken ct = default)
    {
        IQueryable<Profesional> query = context.Profesionales;

        if (incluirServicios)
            query = query.Include(p => p.Servicios);

        return query.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public Task<bool> ExisteAsync(int id, CancellationToken ct = default)
        => context.Profesionales.AnyAsync(p => p.Id == id, ct);

    public async Task AgregarAsync(Profesional profesional, IEnumerable<int> servicioIds, CancellationToken ct = default)
    {
        profesional.Servicios = await ObtenerServiciosAsync(servicioIds, ct);
        context.Profesionales.Add(profesional);
        await context.SaveChangesAsync(ct);
    }

    public async Task ActualizarAsync(Profesional profesional, IEnumerable<int> servicioIds, CancellationToken ct = default)
    {
        var servicios = await ObtenerServiciosAsync(servicioIds, ct);

        profesional.Servicios.Clear();
        foreach (var servicio in servicios)
            profesional.Servicios.Add(servicio);

        await context.SaveChangesAsync(ct);
    }

    public async Task EliminarAsync(Profesional profesional, CancellationToken ct = default)
    {
        context.Profesionales.Remove(profesional);
        await context.SaveChangesAsync(ct);
    }

    private async Task<List<Servicio>> ObtenerServiciosAsync(IEnumerable<int> servicioIds, CancellationToken ct)
    {
        var ids = servicioIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var servicios = await context.Servicios.Where(s => ids.Contains(s.Id)).ToListAsync(ct);
        if (servicios.Count != ids.Count)
            throw new InvalidOperationException("Uno o más servicios no existen.");

        return servicios;
    }
}
