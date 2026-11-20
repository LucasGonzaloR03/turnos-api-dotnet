using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Turnos.Application.Exceptions;

namespace Turnos.API.Middlewares;

/// <summary>
/// Único lugar donde se deciden los códigos HTTP y el formato de error de la API.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
                throw;

            await Responder(context, ex);
        }
    }

    private async Task Responder(HttpContext context, Exception ex)
    {
        (int status, string title, string detail, IDictionary<string, string[]>? errores) = ex switch
        {
            ValidationException vex => (
                400, "Validación", "Uno o más campos son inválidos.",
                vex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),

            KeyNotFoundException => (404, "No encontrado", ex.Message, null),
            CredencialesInvalidasException => (401, "No autorizado", ex.Message, null),
            AccesoDenegadoException => (403, "Acceso denegado", ex.Message, null),
            ArgumentException => (400, "Solicitud inválida", ex.Message, null),
            InvalidOperationException => (400, "Regla de negocio", ex.Message, null),

            DbUpdateException { InnerException: PostgresException { SqlState: "23P01" } } => (
                409, "Conflicto", "El profesional ya tiene un turno en ese horario.", null),

            _ => (500, "Error interno", "Ocurrió un error inesperado.", null)
        };

        if (status >= 500)
            logger.LogError(ex, "Error no controlado al procesar {Metodo} {Ruta}", context.Request.Method, context.Request.Path);
        else
            logger.LogWarning(ex, "Error {Status} al procesar {Metodo} {Ruta}", status, context.Request.Method, context.Request.Path);

        var extensiones = errores is null
            ? null
            : new Dictionary<string, object?> { ["errors"] = errores };

        await Results.Problem(
            title: title,
            detail: detail,
            statusCode: status,
            extensions: extensiones).ExecuteAsync(context);
    }
}
