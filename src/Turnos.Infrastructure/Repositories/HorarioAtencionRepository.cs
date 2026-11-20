using Microsoft.EntityFrameworkCore;
using Turnos.Application.Interfaces;
using Turnos.Domain.Entities;
using Turnos.Infrastructure.Persistence;

namespace Turnos.Infrastructure.Repositories;

public class HorarioAtencionRepository(TurnosDbContext context) : IHorarioAtencionRepository
{
    public async Task<IReadOnlyList<HorarioAtencion>> ListarPorProfesionalAsync(int profesionalId, CancellationToken ct = default)
    {
        var horarios = await context.HorariosAtencion
            .AsNoTracking()
            .Include(h => h.Profesional)
            .Where(h => h.ProfesionalId == profesionalId)
            .ToListAsync(ct);

        return horarios.OrderBy(h => h.DiaSemana).ThenBy(h => h.HoraInicio).ToList();
    }

    public async Task<IReadOnlyList<HorarioAtencion>> ListarPorProfesionalYDiaAsync(
        int profesionalId, DiaSemana dia, CancellationToken ct = default)
        => await context.HorariosAtencion
            .AsNoTracking()
            .Where(h => h.ProfesionalId == profesionalId && h.DiaSemana == dia)
            .ToListAsync(ct);

    public Task<HorarioAtencion?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        => context.HorariosAtencion.FirstOrDefaultAsync(h => h.Id == id, ct);

    public Task<bool> ExisteDuplicadoAsync(
        int profesionalId,
        DiaSemana dia,
        TimeOnly inicio,
        TimeOnly fin,
        int? idExcluido = null,
        CancellationToken ct = default)
        => context.HorariosAtencion.AnyAsync(h =>
            h.ProfesionalId == profesionalId
            && h.DiaSemana == dia
            && h.HoraInicio == inicio
            && h.HoraFin == fin
            && (!idExcluido.HasValue || h.Id != idExcluido.Value), ct);

    public async Task AgregarAsync(HorarioAtencion horario, CancellationToken ct = default)
    {
        context.HorariosAtencion.Add(horario);
        await context.SaveChangesAsync(ct);
    }

    public async Task ActualizarAsync(HorarioAtencion horario, CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }

    public async Task EliminarAsync(HorarioAtencion horario, CancellationToken ct = default)
    {
        context.HorariosAtencion.Remove(horario);
        await context.SaveChangesAsync(ct);
    }
}
