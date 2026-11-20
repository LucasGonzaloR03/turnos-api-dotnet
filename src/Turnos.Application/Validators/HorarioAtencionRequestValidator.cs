using FluentValidation;
using Turnos.Application.Dtos;

namespace Turnos.Application.Validators;

public class HorarioAtencionRequestValidator : AbstractValidator<HorarioAtencionRequest>
{
    public HorarioAtencionRequestValidator()
    {
        RuleFor(x => x.ProfesionalId).GreaterThan(0);
        RuleFor(x => x.DiaSemana).IsInEnum();
        RuleFor(x => x.HoraFin).GreaterThan(x => x.HoraInicio)
            .WithMessage("La hora de inicio debe ser menor a la hora de fin.");
    }
}
