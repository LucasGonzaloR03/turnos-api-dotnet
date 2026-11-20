using Turnos.Application.Dtos;
using Turnos.Application.Exceptions;
using Turnos.Application.Interfaces;
using Turnos.Domain.Entities;

namespace Turnos.Application.Services;

public class TurnoService(
    ITurnoRepository turnoRepository,
    IProfesionalRepository profesionalRepository,
    IHorarioAtencionRepository horarioRepository)
{
    public async Task<IReadOnlyList<DisponibilidadResponse>> ObtenerDisponibilidadAsync(
        int profesionalId, DateOnly fecha, int? servicioId, CancellationToken ct = default)
    {
        var profesional = await ObtenerProfesionalAsync(profesionalId, ct);

        if (!profesional.Activo)
            throw new InvalidOperationException("El profesional no está activo.");

        var duracion = ObtenerDuracion(profesional, servicioId);
        var dia = MapDia(fecha.DayOfWeek);
        var horarios = await horarioRepository.ListarPorProfesionalYDiaAsync(profesionalId, dia, ct);

        if (horarios.Count == 0)
            return [];

        var desde = Combinar(fecha, TimeOnly.MinValue);
        var hasta = Combinar(fecha, new TimeOnly(23, 59, 59, 999));
        var ocupados = await turnoRepository.ObtenerActivosDelProfesionalEnRangoAsync(profesionalId, desde, hasta, ct);
        var ahora = DateTime.UtcNow;
        var respuesta = new List<DisponibilidadResponse>();

        foreach (var horario in horarios)
        {
            var inicio = Combinar(fecha, horario.HoraInicio);
            var finHorario = Combinar(fecha, horario.HoraFin);

            while (inicio.AddMinutes(duracion) <= finHorario)
            {
                var fin = inicio.AddMinutes(duracion);

                if (inicio >= ahora && !SeSuperpone(inicio, fin, ocupados))
                    respuesta.Add(new DisponibilidadResponse { Inicio = inicio, Fin = fin });

                inicio = fin;
            }
        }

        return respuesta;
    }

    public async Task<TurnoResponse> ReservarAsync(int usuarioId, TurnoRequest request, CancellationToken ct = default)
    {
        var profesional = await ObtenerProfesionalAsync(request.ProfesionalId, ct);

        if (!profesional.Activo)
            throw new InvalidOperationException("El profesional no está activo.");

        var servicio = profesional.Servicios.FirstOrDefault(s => s.Id == request.ServicioId)
            ?? throw new ArgumentException("El profesional no ofrece ese servicio.");

        var inicio = AUtc(request.Inicio);
        ValidarNoPasado(inicio);
        var fin = inicio.AddMinutes(servicio.DuracionMinutos);

        Turno? creado = null;

        await turnoRepository.EjecutarConBloqueoAsync(profesional.Id, async ct2 =>
        {
            await ValidarHorarioAsync(profesional.Id, inicio, fin, ct2);
            await ValidarSinSuperposicionAsync(profesional.Id, inicio, fin, null, ct2);

            creado = new Turno
            {
                UsuarioId = usuarioId,
                ProfesionalId = profesional.Id,
                ServicioId = servicio.Id,
                Inicio = inicio,
                Fin = fin,
                Estado = TurnoEstado.Pendiente
            };

            await turnoRepository.AgregarAsync(creado, ct2);
        }, ct);

        return Map(creado!);
    }

    public async Task<IReadOnlyList<TurnoResponse>> ListarMiosAsync(int usuarioId, CancellationToken ct = default)
    {
        var turnos = await turnoRepository.ListarMiosAsync(usuarioId, ct);
        return turnos.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<TurnoResponse>> ListarAsync(
        int? profesionalId, DateOnly? fecha, TurnoEstado? estado, CancellationToken ct = default)
    {
        var turnos = await turnoRepository.ListarConFiltrosAsync(profesionalId, fecha, estado, ct);
        return turnos.Select(Map).ToList();
    }

    public async Task<TurnoResponse> ReprogramarAsync(int usuarioId, int turnoId, ReprogramarRequest request, CancellationToken ct = default)
    {
        var turno = await ObtenerTurnoAsync(turnoId, ct);
        ValidarPropietario(turno, usuarioId);
        ValidarEstado(turno, TurnoEstado.Pendiente, TurnoEstado.Confirmado);

        var inicio = AUtc(request.Inicio);
        ValidarNoPasado(inicio);
        var fin = inicio.AddMinutes(turno.Servicio.DuracionMinutos);

        await turnoRepository.EjecutarConBloqueoAsync(turno.ProfesionalId, async ct2 =>
        {
            await ValidarHorarioAsync(turno.ProfesionalId, inicio, fin, ct2);
            await ValidarSinSuperposicionAsync(turno.ProfesionalId, inicio, fin, turno.Id, ct2);

            turno.Inicio = inicio;
            turno.Fin = fin;
            await turnoRepository.ActualizarAsync(turno, ct2);
        }, ct);

        return Map(turno);
    }

    public async Task<TurnoResponse> CancelarAsync(int usuarioId, int turnoId, CancellationToken ct = default)
    {
        var turno = await ObtenerTurnoAsync(turnoId, ct);
        ValidarPropietario(turno, usuarioId);
        ValidarEstado(turno, TurnoEstado.Pendiente, TurnoEstado.Confirmado);

        var limite = DateTime.UtcNow.AddHours(turno.Servicio.HorasAnticipoCancelacion);
        if (turno.Inicio < limite)
            throw new InvalidOperationException(
                $"Solo se puede cancelar hasta {turno.Servicio.HorasAnticipoCancelacion} horas antes del inicio.");

        turno.Estado = TurnoEstado.Cancelado;
        await turnoRepository.ActualizarAsync(turno, ct);
        return Map(turno);
    }

    public async Task<TurnoResponse> ConfirmarAsync(int turnoId, CancellationToken ct = default)
    {
        var turno = await ObtenerTurnoAsync(turnoId, ct);
        ValidarEstado(turno, TurnoEstado.Pendiente);

        turno.Estado = TurnoEstado.Confirmado;
        await turnoRepository.ActualizarAsync(turno, ct);
        return Map(turno);
    }

    public async Task<TurnoResponse> CompletarAsync(int turnoId, CancellationToken ct = default)
    {
        var turno = await ObtenerTurnoAsync(turnoId, ct);
        ValidarEstado(turno, TurnoEstado.Confirmado);

        turno.Estado = TurnoEstado.Completado;
        await turnoRepository.ActualizarAsync(turno, ct);
        return Map(turno);
    }

    private async Task<Turno> ObtenerTurnoAsync(int id, CancellationToken ct)
        => await turnoRepository.ObtenerPorIdAsync(id, ct)
           ?? throw new KeyNotFoundException("No existe un turno con ese id.");

    private static void ValidarPropietario(Turno turno, int usuarioId)
    {
        if (turno.UsuarioId != usuarioId)
            throw new AccesoDenegadoException("El turno no pertenece al usuario autenticado.");
    }

    private static void ValidarEstado(Turno turno, params TurnoEstado[] permitidos)
    {
        if (!permitidos.Contains(turno.Estado))
            throw new InvalidOperationException($"No se puede realizar la operación con un turno {turno.Estado}.");
    }

    private static void ValidarNoPasado(DateTime inicio)
    {
        if (inicio <= DateTime.UtcNow)
            throw new ArgumentException("No se puede reservar en el pasado.");
    }

    private async Task ValidarHorarioAsync(int profesionalId, DateTime inicio, DateTime fin, CancellationToken ct)
    {
        if (fin > inicio.Date.AddDays(1))
            throw new ArgumentException("El turno no puede cruzar al día siguiente.");

        var dia = MapDia(inicio.DayOfWeek);
        var horarios = await horarioRepository.ListarPorProfesionalYDiaAsync(profesionalId, dia, ct);
        var horaInicio = TimeOnly.FromDateTime(inicio);
        var horaFin = TimeOnly.FromDateTime(fin);

        if (!horarios.Any(h => horaInicio >= h.HoraInicio && horaFin <= h.HoraFin))
            throw new InvalidOperationException("El turno no cae dentro de un horario de atención del profesional.");
    }

    private async Task ValidarSinSuperposicionAsync(int profesionalId, DateTime inicio, DateTime fin, int? idExcluido, CancellationToken ct)
    {
        if (await turnoRepository.ExisteSuperposicionAsync(profesionalId, inicio, fin, idExcluido, ct))
            throw new InvalidOperationException("El profesional ya tiene un turno en ese horario.");
    }

    private static int ObtenerDuracion(Profesional profesional, int? servicioId)
    {
        if (servicioId.HasValue)
        {
            var servicio = profesional.Servicios.FirstOrDefault(s => s.Id == servicioId.Value)
                ?? throw new ArgumentException("El profesional no ofrece ese servicio.");
            return servicio.DuracionMinutos;
        }

        return profesional.Servicios.Count == 0 ? 30 : profesional.Servicios.Min(s => s.DuracionMinutos);
    }

    private async Task<Profesional> ObtenerProfesionalAsync(int id, CancellationToken ct)
        => await profesionalRepository.ObtenerPorIdAsync(id, incluirServicios: true, ct)
           ?? throw new KeyNotFoundException("No existe un profesional con ese id.");

    private static bool SeSuperpone(DateTime inicio, DateTime fin, IEnumerable<Turno> ocupados)
        => ocupados.Any(t => inicio < t.Fin && fin > t.Inicio);

    private static DiaSemana MapDia(DayOfWeek dia)
        => dia == DayOfWeek.Sunday ? DiaSemana.Domingo : (DiaSemana)((int)dia);

    private static DateTime Combinar(DateOnly fecha, TimeOnly hora)
        => DateTime.SpecifyKind(fecha.ToDateTime(hora), DateTimeKind.Utc);

    private static DateTime AUtc(DateTime valor) => valor.Kind switch
    {
        DateTimeKind.Utc => valor,
        DateTimeKind.Local => valor.ToUniversalTime(),
        _ => DateTime.SpecifyKind(valor, DateTimeKind.Utc)
    };

    private static TurnoResponse Map(Turno turno) => new()
    {
        Id = turno.Id,
        UsuarioId = turno.UsuarioId,
        UsuarioNombre = turno.Usuario?.Nombre ?? string.Empty,
        ProfesionalId = turno.ProfesionalId,
        ProfesionalNombre = turno.Profesional?.Nombre ?? string.Empty,
        ServicioId = turno.ServicioId,
        ServicioNombre = turno.Servicio?.Nombre ?? string.Empty,
        DuracionMinutos = turno.Servicio?.DuracionMinutos ?? 0,
        Precio = turno.Servicio?.Precio ?? 0,
        Inicio = turno.Inicio,
        Fin = turno.Fin,
        Estado = turno.Estado
    };
}
