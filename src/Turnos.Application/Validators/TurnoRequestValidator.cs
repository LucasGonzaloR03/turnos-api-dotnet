using FluentValidation;
using Turnos.Application.Dtos;

namespace Turnos.Application.Validators;

public class TurnoRequestValidator : AbstractValidator<TurnoRequest>
{
    public TurnoRequestValidator()
    {
        RuleFor(x => x.ProfesionalId).GreaterThan(0);
        RuleFor(x => x.ServicioId).GreaterThan(0);
        RuleFor(x => x.Inicio).Must(x => x != default).WithMessage("El inicio es obligatorio.");
    }
}
