using Turnos.Domain.Entities;

namespace Turnos.Application.Interfaces;

public interface IServicioRepository
{
    Task<IReadOnlyList<Servicio>> ListarAsync(CancellationToken ct = default);
    Task<Servicio?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<bool> ExisteAsync(int id, CancellationToken ct = default);
    Task AgregarAsync(Servicio servicio, CancellationToken ct = default);
    Task ActualizarAsync(Servicio servicio, CancellationToken ct = default);
    Task EliminarAsync(Servicio servicio, CancellationToken ct = default);
}
