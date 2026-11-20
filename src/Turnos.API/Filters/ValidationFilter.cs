using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Turnos.API.Filters;

/// <summary>
/// Busca un IValidator&lt;T&gt; para cada argumento de la action y lo ejecuta antes del controller.
/// Así los controladores no repiten validaciones y los errores llegan al middleware global.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    private static readonly System.Reflection.MethodInfo ValidateAsyncMethod =
        typeof(IValidator).GetMethod(
            nameof(IValidator.ValidateAsync),
            [typeof(IValidationContext), typeof(CancellationToken)])!;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argumento in context.ActionArguments.Values)
        {
            if (argumento is null)
                continue;

            var tipo = argumento.GetType();
            var tipoValidador = typeof(IValidator<>).MakeGenericType(tipo);
            var validador = context.HttpContext.RequestServices.GetService(tipoValidador);

            if (validador is null)
                continue;

            var contextoValidacion = (IValidationContext)Activator.CreateInstance(
                typeof(ValidationContext<>).MakeGenericType(tipo), argumento)!;

            var tarea = (Task<ValidationResult>)ValidateAsyncMethod.Invoke(
                validador, [contextoValidacion, context.HttpContext.RequestAborted])!;

            var resultado = await tarea;

            if (!resultado.IsValid)
                throw new ValidationException(resultado.Errors);
        }

        await next();
    }
}
