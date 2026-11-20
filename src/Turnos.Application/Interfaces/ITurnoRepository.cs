using Turnos.Domain.Entities;

namespace Turnos.Application.Interfaces;

public interface ITurnoRepository
{
    Task<Turno?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Turno>> ObtenerActivosDelProfesionalEnRangoAsync(int profesionalId, DateTime desde, DateTime hasta, CancellationToken ct = default);
    Task<IReadOnlyList<Turno>> ListarMiosAsync(int usuarioId, CancellationToken ct = default);
    Task<IReadOnlyList<Turno>> ListarConFiltrosAsync(int? profesionalId, DateOnly? fecha, TurnoEstado? estado, CancellationToken ct = default);
    Task<bool> ExisteSuperposicionAsync(int profesionalId, DateTime inicio, DateTime fin, int? idExcluido = null, CancellationToken ct = default);
    Task AgregarAsync(Turno turno, CancellationToken ct = default);
    Task ActualizarAsync(Turno turno, CancellationToken ct = default);
    Task EjecutarConBloqueoAsync(int profesionalId, Func<CancellationToken, Task> operacion, CancellationToken ct = default);
}
