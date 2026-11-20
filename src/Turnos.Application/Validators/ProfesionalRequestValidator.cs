using FluentValidation;
using Turnos.Application.Dtos;

namespace Turnos.Application.Validators;

public class ProfesionalRequestValidator : AbstractValidator<ProfesionalRequest>
{
    public ProfesionalRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Especialidad).NotEmpty().MaximumLength(200);
        RuleForEach(x => x.ServicioIds).GreaterThan(0);
    }
}
