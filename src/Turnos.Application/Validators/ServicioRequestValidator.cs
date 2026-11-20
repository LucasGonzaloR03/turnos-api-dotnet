using FluentValidation;
using Turnos.Application.Dtos;

namespace Turnos.Application.Validators;

public class ServicioRequestValidator : AbstractValidator<ServicioRequest>
{
    public ServicioRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DuracionMinutos).GreaterThan(0)
            .WithMessage("La duración debe ser mayor a cero.");
        RuleFor(x => x.Precio).GreaterThanOrEqualTo(0)
            .WithMessage("El precio no puede ser negativo.");
        RuleFor(x => x.HorasAnticipoCancelacion).GreaterThanOrEqualTo(0)
            .WithMessage("El anticipo de cancelación no puede ser negativo.");
    }
}
