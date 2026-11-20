using Turnos.Domain.Entities;

namespace Turnos.Application.Interfaces;

public interface IHorarioAtencionRepository
{
    Task<IReadOnlyList<HorarioAtencion>> ListarPorProfesionalAsync(int profesionalId, CancellationToken ct = default);
    Task<IReadOnlyList<HorarioAtencion>> ListarPorProfesionalYDiaAsync(int profesionalId, DiaSemana dia, CancellationToken ct = default);
    Task<HorarioAtencion?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExisteDuplicadoAsync(int profesionalId, DiaSemana dia, TimeOnly inicio, TimeOnly fin, int? idExcluido = null, CancellationToken ct = default);
    Task AgregarAsync(HorarioAtencion horario, CancellationToken ct = default);
    Task ActualizarAsync(HorarioAtencion horario, CancellationToken ct = default);
    Task EliminarAsync(HorarioAtencion horario, CancellationToken ct = default);
}
