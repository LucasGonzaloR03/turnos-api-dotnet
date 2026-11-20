using Moq;
using Turnos.Application.Dtos;
using Turnos.Application.Exceptions;
using Turnos.Application.Interfaces;
using Turnos.Application.Services;
using Turnos.Domain.Entities;

namespace Turnos.Tests;

public class TurnoServiceTests
{
    private static readonly DateOnly Dia = ProximoLunes();

    private readonly Mock<ITurnoRepository> _turnoRepo = new();
    private readonly Mock<IProfesionalRepository> _profesionalRepo = new();
    private readonly Mock<IHorarioAtencionRepository> _horarioRepo = new();
    private readonly TurnoService _service;

    public TurnoServiceTests()
    {
        var profesional = new Profesional
        {
            Id = 1,
            Nombre = "Ana Perez",
            Activo = true,
            Servicios = [Corte(), Consulta()]
        };

        _profesionalRepo.Setup(r => r.ObtenerPorIdAsync(1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profesional);

        _horarioRepo.Setup(r => r.ListarPorProfesionalYDiaAsync(
                It.IsAny<int>(), It.IsAny<DiaSemana>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _horarioRepo.Setup(r => r.ListarPorProfesionalYDiaAsync(1, DiaSemana.Lunes, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new HorarioAtencion
            {
                ProfesionalId = 1,
                DiaSemana = DiaSemana.Lunes,
                HoraInicio = new TimeOnly(9, 0),
                HoraFin = new TimeOnly(17, 0)
            }]);

        _turnoRepo.Setup(r => r.EjecutarConBloqueoAsync(
                1, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns((int _, Func<CancellationToken, Task> operacion, CancellationToken ct) => operacion(ct));

        _turnoRepo.Setup(r => r.AgregarAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()))
            .Callback<Turno, CancellationToken>((t, _) => t.Id = 100)
            .Returns(Task.CompletedTask);

        _turnoRepo.Setup(r => r.ActualizarAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _turnoRepo.Setup(r => r.ObtenerActivosDelProfesionalEnRangoAsync(
                1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _turnoRepo.Setup(r => r.ExisteSuperposicionAsync(
                1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _service = new TurnoService(_turnoRepo.Object, _profesionalRepo.Object, _horarioRepo.Object);
    }

    [Fact]
    public async Task Reservar_CreaTurnoPendienteConFinSegunLaDuracion()
    {
        var resultado = await _service.ReservarAsync(10, new TurnoRequest
        {
            ProfesionalId = 1,
            ServicioId = 1,
            Inicio = A(new TimeOnly(9, 0))
        });

        Assert.Equal(TurnoEstado.Pendiente, resultado.Estado);
        Assert.Equal(A(new TimeOnly(9, 30)), resultado.Fin);
        _turnoRepo.Verify(r => r.AgregarAsync(
            It.Is<Turno>(t => t.UsuarioId == 10 && t.ProfesionalId == 1 && t.ServicioId == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reservar_RechazaSuperposicionYSaveNoGraba()
    {
        _turnoRepo.Setup(r => r.ExisteSuperposicionAsync(
                1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ReservarAsync(10, new TurnoRequest
        {
            ProfesionalId = 1,
            ServicioId = 1,
            Inicio = A(new TimeOnly(9, 0))
        }));

        _turnoRepo.Verify(r => r.AgregarAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reservar_RechazaInicioEnElPasado()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ReservarAsync(10, new TurnoRequest
        {
            ProfesionalId = 1,
            ServicioId = 1,
            Inicio = DateTime.UtcNow.AddHours(-1)
        }));
    }

    [Fact]
    public async Task Reservar_RechazaInicioFueraDelHorarioDeAtencion()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ReservarAsync(10, new TurnoRequest
        {
            ProfesionalId = 1,
            ServicioId = 1,
            Inicio = A(new TimeOnly(18, 0))
        }));
    }

    [Fact]
    public async Task Reservar_RechazaServicioQueElProfesionalNoOfrece()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ReservarAsync(10, new TurnoRequest
        {
            ProfesionalId = 1,
            ServicioId = 999,
            Inicio = A(new TimeOnly(9, 0))
        }));
    }

    [Fact]
    public async Task Disponibilidad_GeneraFranjasCada30MinutosDentroDelHorario()
    {
        var franjas = await _service.ObtenerDisponibilidadAsync(1, Dia, null);

        Assert.Equal(16, franjas.Count);
        Assert.Equal(A(new TimeOnly(9, 0)), franjas[0].Inicio);
        Assert.Equal(A(new TimeOnly(16, 30)), franjas[^1].Inicio);
        Assert.All(franjas, f => Assert.Equal(TimeSpan.FromMinutes(30), f.Fin - f.Inicio));
    }

    [Fact]
    public async Task Disponibilidad_UsaLaDuracionDelServicioElegido()
    {
        var franjas = await _service.ObtenerDisponibilidadAsync(1, Dia, 2);

        Assert.Equal(8, franjas.Count);
        Assert.Equal(A(new TimeOnly(9, 0)), franjas[0].Inicio);
        Assert.Equal(A(new TimeOnly(10, 0)), franjas[0].Fin);
    }

    [Fact]
    public async Task Disponibilidad_DescartaSoloLaFranjaOcupada()
    {
        _turnoRepo.Setup(r => r.ObtenerActivosDelProfesionalEnRangoAsync(
                1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([TurnoExistente(inicio: A(new TimeOnly(9, 0)))]);

        var franjas = await _service.ObtenerDisponibilidadAsync(1, Dia, null);

        Assert.Equal(15, franjas.Count);
        Assert.DoesNotContain(franjas, f => f.Inicio == A(new TimeOnly(9, 0)));
        Assert.Equal(A(new TimeOnly(9, 30)), franjas[0].Inicio);
    }

    [Fact]
    public async Task Disponibilidad_SinHorariosEseDia_DevuelveListaVacia()
    {
        var domingo = Dia.AddDays(6);

        var franjas = await _service.ObtenerDisponibilidadAsync(1, domingo, null);

        Assert.Empty(franjas);
    }

    [Fact]
    public async Task Reprogramar_ExcluyeElPropioTurnoDeLaBusquedaDeSuperposicion()
    {
        _turnoRepo.Setup(r => r.ObtenerPorIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TurnoExistente());

        await _service.ReprogramarAsync(10, 5, new ReprogramarRequest
        {
            Inicio = A(new TimeOnly(10, 0))
        });

        _turnoRepo.Verify(r => r.ExisteSuperposicionAsync(
            1, A(new TimeOnly(10, 0)), A(new TimeOnly(10, 30)), 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reprogramar_RechazaUnHorarioOcupadoPorOtroTurno()
    {
        _turnoRepo.Setup(r => r.ObtenerPorIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TurnoExistente());
        _turnoRepo.Setup(r => r.ExisteSuperposicionAsync(
                1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ReprogramarAsync(10, 5,
            new ReprogramarRequest { Inicio = A(new TimeOnly(10, 0)) }));
    }

    [Fact]
    public async Task Cancelar_RespetaElAnticipoDeDosHorasDelServicioNormal()
    {
        var turno = TurnoExistente(5, inicio: DateTime.UtcNow.AddHours(1));
        _turnoRepo.Setup(r => r.ObtenerPorIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(turno);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CancelarAsync(10, 5));

        turno.Inicio = DateTime.UtcNow.AddHours(3);
        var cancelado = await _service.CancelarAsync(10, 5);
        Assert.Equal(TurnoEstado.Cancelado, cancelado.Estado);
    }

    [Fact]
    public async Task Cancelar_RespetaElAnticipoDe24HorasDelServicioMedico()
    {
        var turno = TurnoExistente(6, servicio: Consulta(), inicio: DateTime.UtcNow.AddHours(3));
        _turnoRepo.Setup(r => r.ObtenerPorIdAsync(6, It.IsAny<CancellationToken>())).ReturnsAsync(turno);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CancelarAsync(10, 6));

        turno.Inicio = DateTime.UtcNow.AddHours(25);
        var cancelado = await _service.CancelarAsync(10, 6);
        Assert.Equal(TurnoEstado.Cancelado, cancelado.Estado);
    }

    [Fact]
    public async Task Cancelar_RechazaTurnoDeOtroUsuario()
    {
        _turnoRepo.Setup(r => r.ObtenerPorIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TurnoExistente(usuarioId: 99));

        await Assert.ThrowsAsync<AccesoDenegadoException>(() => _service.CancelarAsync(10, 5));
    }

    [Fact]
    public async Task ConfirmarYCompletar_RespetanElCicloDeEstados()
    {
        var pendiente = TurnoExistente(5);
        _turnoRepo.Setup(r => r.ObtenerPorIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(pendiente);

        var confirmado = await _service.ConfirmarAsync(5);
        Assert.Equal(TurnoEstado.Confirmado, confirmado.Estado);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ConfirmarAsync(5));

        var completado = await _service.CompletarAsync(5);
        Assert.Equal(TurnoEstado.Completado, completado.Estado);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CompletarAsync(5));
    }

    private static Servicio Corte() => new()
    {
        Id = 1, Nombre = "Corte de pelo", DuracionMinutos = 30, Precio = 2500m, HorasAnticipoCancelacion = 2
    };

    private static Servicio Consulta() => new()
    {
        Id = 2, Nombre = "Consulta medica", DuracionMinutos = 60, Precio = 5000m, HorasAnticipoCancelacion = 24
    };

    private static Turno TurnoExistente(int id = 5, int usuarioId = 10, Servicio? servicio = null,
        DateTime? inicio = null, TurnoEstado estado = TurnoEstado.Pendiente)
    {
        var inicioTurno = inicio ?? A(new TimeOnly(9, 0));
        var servicioTurno = servicio ?? Corte();

        return new Turno
        {
            Id = id,
            UsuarioId = usuarioId,
            ProfesionalId = 1,
            ServicioId = servicioTurno.Id,
            Servicio = servicioTurno,
            Inicio = inicioTurno,
            Fin = inicioTurno.AddMinutes(servicioTurno.DuracionMinutos),
            Estado = estado,
            Usuario = new Usuario { Id = usuarioId, Nombre = "Cliente" },
            Profesional = new Profesional { Id = 1, Nombre = "Ana Perez" }
        };
    }

    private static DateTime A(TimeOnly hora) => DateTime.SpecifyKind(Dia.ToDateTime(hora), DateTimeKind.Utc);

    private static DateOnly ProximoLunes()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var dias = ((int)DayOfWeek.Monday - (int)hoy.DayOfWeek + 7) % 7;
        if (dias < 3)
            dias += 7;
        return hoy.AddDays(dias);
    }
}
