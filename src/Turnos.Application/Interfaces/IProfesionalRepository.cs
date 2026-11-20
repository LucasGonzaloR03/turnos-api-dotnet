using Turnos.Domain.Entities;

namespace Turnos.Application.Interfaces;

public interface IProfesionalRepository
{
    Task<IReadOnlyList<Profesional>> ListarAsync(bool incluirServicios = false, CancellationToken ct = default);
    Task<Profesional?> ObtenerPorIdAsync(int id, bool incluirServicios = false, CancellationToken ct = default);
    Task<bool> ExisteAsync(int id, CancellationToken ct = default);
    Task AgregarAsync(Profesional profesional, IEnumerable<int> servicioIds, CancellationToken ct = default);
    Task ActualizarAsync(Profesional profesional, IEnumerable<int> servicioIds, CancellationToken ct = default);
    Task EliminarAsync(Profesional profesional, CancellationToken ct = default);
}
