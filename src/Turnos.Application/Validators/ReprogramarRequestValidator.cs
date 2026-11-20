using FluentValidation;
using Turnos.Application.Dtos;

namespace Turnos.Application.Validators;

public class ReprogramarRequestValidator : AbstractValidator<ReprogramarRequest>
{
    public ReprogramarRequestValidator()
    {
        RuleFor(x => x.Inicio).Must(x => x != default).WithMessage("El inicio es obligatorio.");
    }
}
