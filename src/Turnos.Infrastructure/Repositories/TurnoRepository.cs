using Microsoft.EntityFrameworkCore;
using Turnos.Application.Interfaces;
using Turnos.Domain.Entities;
using Turnos.Infrastructure.Persistence;

namespace Turnos.Infrastructure.Repositories;

public class TurnoRepository(TurnosDbContext context) : ITurnoRepository
{
    private IQueryable<Turno> ConNavegaciones()
        => context.Turnos
            .Include(t => t.Usuario)
            .Include(t => t.Profesional)
            .Include(t => t.Servicio);

    public Task<Turno?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        => ConNavegaciones().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Turno>> ObtenerActivosDelProfesionalEnRangoAsync(
        int profesionalId, DateTime desde, DateTime hasta, CancellationToken ct = default)
        => await ConNavegaciones()
            .AsNoTracking()
            .Where(t => t.ProfesionalId == profesionalId
                     && t.Estado != TurnoEstado.Cancelado
                     && t.Inicio < hasta
                     && t.Fin > desde)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Turno>> ListarMiosAsync(int usuarioId, CancellationToken ct = default)
        => await ConNavegaciones()
            .AsNoTracking()
            .Where(t => t.UsuarioId == usuarioId)
            .OrderByDescending(t => t.Inicio)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Turno>> ListarConFiltrosAsync(
        int? profesionalId, DateOnly? fecha, TurnoEstado? estado, CancellationToken ct = default)
    {
        IQueryable<Turno> query = ConNavegaciones().AsNoTracking();

        if (profesionalId.HasValue)
            query = query.Where(t => t.ProfesionalId == profesionalId.Value);

        if (estado.HasValue)
            query = query.Where(t => t.Estado == estado.Value);

        if (fecha.HasValue)
        {
            var desde = DateTime.SpecifyKind(fecha.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var hasta = desde.AddDays(1);
            query = query.Where(t => t.Inicio >= desde && t.Inicio < hasta);
        }

        return await query.OrderBy(t => t.Inicio).ToListAsync(ct);
    }

    public Task<bool> ExisteSuperposicionAsync(
        int profesionalId, DateTime inicio, DateTime fin, int? idExcluido = null, CancellationToken ct = default)
        => context.Turnos.AnyAsync(t =>
            t.ProfesionalId == profesionalId
            && t.Estado != TurnoEstado.Cancelado
            && t.Inicio < fin
            && t.Fin > inicio
            && (!idExcluido.HasValue || t.Id != idExcluido.Value), ct);

    public async Task AgregarAsync(Turno turno, CancellationToken ct = default)
    {
        context.Turnos.Add(turno);
        await context.SaveChangesAsync(ct);
    }

    public async Task ActualizarAsync(Turno turno, CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }

    public async Task EjecutarConBloqueoAsync(
        int profesionalId, Func<CancellationToken, Task> operacion, CancellationToken ct = default)
    {
        await using var transaccion = await context.Database.BeginTransactionAsync(ct);
        await context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({(long)profesionalId});", ct);
        await operacion(ct);
        await transaccion.CommitAsync(ct);
    }
}
