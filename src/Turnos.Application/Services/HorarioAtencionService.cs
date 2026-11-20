using Turnos.Application.Dtos;
using Turnos.Application.Interfaces;
using Turnos.Domain.Entities;

namespace Turnos.Application.Services;

public class HorarioAtencionService(
    IHorarioAtencionRepository horarioRepository,
    IProfesionalRepository profesionalRepository)
{
    public async Task<IReadOnlyList<HorarioAtencionResponse>> ListarPorProfesionalAsync(int profesionalId, CancellationToken ct = default)
    {
        await ValidarProfesionalAsync(profesionalId, ct);

        var horarios = await horarioRepository.ListarPorProfesionalAsync(profesionalId, ct);
        return horarios.Select(Map).ToList();
    }

    public async Task<HorarioAtencionResponse> CrearAsync(HorarioAtencionRequest request, CancellationToken ct = default)
    {
        await ValidarAsync(request, idExcluido: null, ct);

        var horario = new HorarioAtencion
        {
            ProfesionalId = request.ProfesionalId,
            DiaSemana = request.DiaSemana,
            HoraInicio = request.HoraInicio,
            HoraFin = request.HoraFin
        };

        await horarioRepository.AgregarAsync(horario, ct);
        return Map(horario);
    }

    public async Task<HorarioAtencionResponse> ActualizarAsync(int id, HorarioAtencionRequest request, CancellationToken ct = default)
    {
        var horario = await horarioRepository.ObtenerPorIdAsync(id, ct)
            ?? throw new KeyNotFoundException("No existe un horario con ese id.");

        await ValidarAsync(request, idExcluido: id, ct);

        horario.ProfesionalId = request.ProfesionalId;
        horario.DiaSemana = request.DiaSemana;
        horario.HoraInicio = request.HoraInicio;
        horario.HoraFin = request.HoraFin;

        await horarioRepository.ActualizarAsync(horario, ct);
        return Map(horario);
    }

    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        var horario = await horarioRepository.ObtenerPorIdAsync(id, ct)
            ?? throw new KeyNotFoundException("No existe un horario con ese id.");

        await horarioRepository.EliminarAsync(horario, ct);
    }

    private async Task ValidarAsync(HorarioAtencionRequest request, int? idExcluido, CancellationToken ct)
    {
        if (request.ProfesionalId <= 0)
            throw new ArgumentException("El profesional es obligatorio.");

        if (!Enum.IsDefined(request.DiaSemana))
            throw new ArgumentException("El día de la semana no es válido.");

        if (request.HoraInicio >= request.HoraFin)
            throw new ArgumentException("La hora de inicio debe ser menor a la hora de fin.");

        await ValidarProfesionalAsync(request.ProfesionalId, ct);

        if (await horarioRepository.ExisteDuplicadoAsync(
                request.ProfesionalId, request.DiaSemana, request.HoraInicio, request.HoraFin, idExcluido, ct))
            throw new InvalidOperationException("Ya existe un horario idéntico para ese profesional y día.");
    }

    private async Task ValidarProfesionalAsync(int profesionalId, CancellationToken ct)
    {
        if (!await profesionalRepository.ExisteAsync(profesionalId, ct))
            throw new KeyNotFoundException("No existe un profesional con ese id.");
    }

    private static HorarioAtencionResponse Map(HorarioAtencion horario) => new()
    {
        Id = horario.Id,
        ProfesionalId = horario.ProfesionalId,
        ProfesionalNombre = horario.Profesional?.Nombre ?? string.Empty,
        DiaSemana = horario.DiaSemana,
        HoraInicio = horario.HoraInicio,
        HoraFin = horario.HoraFin
    };
}
