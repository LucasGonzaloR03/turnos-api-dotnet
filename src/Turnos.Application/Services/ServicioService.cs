using Turnos.Application.Dtos;
using Turnos.Application.Interfaces;
using Turnos.Domain.Entities;

namespace Turnos.Application.Services;

public class ServicioService(IServicioRepository servicioRepository)
{
    public async Task<IReadOnlyList<ServicioResponse>> ListarAsync(CancellationToken ct = default)
    {
        var servicios = await servicioRepository.ListarAsync(ct);
        return servicios.Select(Map).ToList();
    }

    public async Task<ServicioResponse> ObtenerAsync(int id, CancellationToken ct = default)
        => Map(await ObtenerServicioAsync(id, ct));

    public async Task<ServicioResponse> CrearAsync(ServicioRequest request, CancellationToken ct = default)
    {
        Validar(request);

        var servicio = new Servicio
        {
            Nombre = request.Nombre.Trim(),
            DuracionMinutos = request.DuracionMinutos,
            Precio = request.Precio,
            HorasAnticipoCancelacion = request.HorasAnticipoCancelacion
        };

        await servicioRepository.AgregarAsync(servicio, ct);
        return Map(servicio);
    }

    public async Task<ServicioResponse> ActualizarAsync(int id, ServicioRequest request, CancellationToken ct = default)
    {
        Validar(request);

        var servicio = await ObtenerServicioAsync(id, ct);
        servicio.Nombre = request.Nombre.Trim();
        servicio.DuracionMinutos = request.DuracionMinutos;
        servicio.Precio = request.Precio;
        servicio.HorasAnticipoCancelacion = request.HorasAnticipoCancelacion;

        await servicioRepository.ActualizarAsync(servicio, ct);
        return Map(servicio);
    }

    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        var servicio = await ObtenerServicioAsync(id, ct);
        await servicioRepository.EliminarAsync(servicio, ct);
    }

    private async Task<Servicio> ObtenerServicioAsync(int id, CancellationToken ct)
        => await servicioRepository.ObtenerPorIdAsync(id, ct)
           ?? throw new KeyNotFoundException("No existe un servicio con ese id.");

    private static ServicioResponse Map(Servicio servicio) => new()
    {
        Id = servicio.Id,
        Nombre = servicio.Nombre,
        DuracionMinutos = servicio.DuracionMinutos,
        Precio = servicio.Precio,
        HorasAnticipoCancelacion = servicio.HorasAnticipoCancelacion
    };

    private static void Validar(ServicioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new ArgumentException("El nombre del servicio es obligatorio.");

        if (request.DuracionMinutos <= 0)
            throw new ArgumentException("La duración debe ser mayor a cero.");

        if (request.Precio < 0)
            throw new ArgumentException("El precio no puede ser negativo.");

        if (request.HorasAnticipoCancelacion < 0)
            throw new ArgumentException("El anticipo de cancelación no puede ser negativo.");
    }
}
