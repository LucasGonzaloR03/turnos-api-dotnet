using Turnos.Application.Dtos;
using Turnos.Application.Interfaces;
using Turnos.Domain.Entities;

namespace Turnos.Application.Services;

public class ProfesionalService(IProfesionalRepository profesionalRepository)
{
    public async Task<IReadOnlyList<ProfesionalResponse>> ListarAsync(CancellationToken ct = default)
    {
        var profesionales = await profesionalRepository.ListarAsync(incluirServicios: true, ct);
        return profesionales.Select(Map).ToList();
    }

    public async Task<ProfesionalResponse> ObtenerAsync(int id, CancellationToken ct = default)
        => Map(await ObtenerProfesionalAsync(id, ct));

    public async Task<ProfesionalResponse> CrearAsync(ProfesionalRequest request, CancellationToken ct = default)
    {
        Validar(request);

        var profesional = new Profesional
        {
            Nombre = request.Nombre.Trim(),
            Especialidad = request.Especialidad.Trim(),
            Activo = request.Activo
        };

        await profesionalRepository.AgregarAsync(profesional, (request.ServicioIds ?? []).Distinct(), ct);
        return Map(profesional);
    }

    public async Task<ProfesionalResponse> ActualizarAsync(int id, ProfesionalRequest request, CancellationToken ct = default)
    {
        Validar(request);

        var profesional = await ObtenerProfesionalAsync(id, ct);
        profesional.Nombre = request.Nombre.Trim();
        profesional.Especialidad = request.Especialidad.Trim();
        profesional.Activo = request.Activo;

        await profesionalRepository.ActualizarAsync(profesional, (request.ServicioIds ?? []).Distinct(), ct);
        return Map(profesional);
    }

    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        var profesional = await ObtenerProfesionalAsync(id, ct);
        await profesionalRepository.EliminarAsync(profesional, ct);
    }

    private async Task<Profesional> ObtenerProfesionalAsync(int id, CancellationToken ct)
        => await profesionalRepository.ObtenerPorIdAsync(id, incluirServicios: true, ct)
           ?? throw new KeyNotFoundException("No existe un profesional con ese id.");

    private static ProfesionalResponse Map(Profesional profesional) => new()
    {
        Id = profesional.Id,
        Nombre = profesional.Nombre,
        Especialidad = profesional.Especialidad,
        Activo = profesional.Activo,
        Servicios = profesional.Servicios.Select(s => new ServicioResponse
        {
            Id = s.Id,
            Nombre = s.Nombre,
            DuracionMinutos = s.DuracionMinutos,
            Precio = s.Precio,
            HorasAnticipoCancelacion = s.HorasAnticipoCancelacion
        }).ToList()
    };

    private static void Validar(ProfesionalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new ArgumentException("El nombre del profesional es obligatorio.");

        if (string.IsNullOrWhiteSpace(request.Especialidad))
            throw new ArgumentException("La especialidad es obligatoria.");
    }
}
