using Turnos.Application.Dtos;
using Turnos.Application.Validators;
using Turnos.Domain.Entities;

namespace Turnos.Tests;

public class RequestValidatorTests
{
    [Fact]
    public void Registro_RechazaEmailInvalidoYContrasenaCorta()
    {
        var resultado = new RegistroRequestValidator().Validate(new RegistroRequest
        {
            Nombre = "Ana", Email = "no-es-un-email", Password = "123"
        });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Email");
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Password");
    }

    [Fact]
    public void Registro_AceptaDatosValidos()
    {
        var resultado = new RegistroRequestValidator().Validate(new RegistroRequest
        {
            Nombre = "Ana", Email = "ana@example.com", Password = "Password123!"
        });

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Servicio_RechazaDuracionCeroYPrecioNegativo()
    {
        var resultado = new ServicioRequestValidator().Validate(new ServicioRequest
        {
            Nombre = "Corte", DuracionMinutos = 0, Precio = -1, HorasAnticipoCancelacion = -5
        });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "DuracionMinutos");
        Assert.Contains(resultado.Errors, e => e.PropertyName == "Precio");
        Assert.Contains(resultado.Errors, e => e.PropertyName == "HorasAnticipoCancelacion");
    }

    [Fact]
    public void Horario_RechazaRangoInvertidoYDiaInvalido()
    {
        var resultado = new HorarioAtencionRequestValidator().Validate(new HorarioAtencionRequest
        {
            ProfesionalId = 0,
            DiaSemana = (DiaSemana)99,
            HoraInicio = new TimeOnly(17, 0),
            HoraFin = new TimeOnly(9, 0)
        });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == "ProfesionalId");
        Assert.Contains(resultado.Errors, e => e.PropertyName == "DiaSemana");
        Assert.Contains(resultado.Errors, e => e.PropertyName == "HoraFin");
    }

    [Fact]
    public void Turno_RechazaIdsCeroEInicioVacio()
    {
        var resultado = new TurnoRequestValidator().Validate(new TurnoRequest
        {
            ProfesionalId = 0, ServicioId = 0, Inicio = default
        });

        Assert.False(resultado.IsValid);
        Assert.Equal(3, resultado.Errors.Count);
    }

    [Fact]
    public void Profesional_RechazaServicioConIdCero()
    {
        var resultado = new ProfesionalRequestValidator().Validate(new ProfesionalRequest
        {
            Nombre = "Ana", Especialidad = "Peluqueria", ServicioIds = [1, 0]
        });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName.Contains("ServicioIds"));
    }
}
