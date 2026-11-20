using Turnos.Domain.Entities;

namespace Turnos.Application.Dtos;

public class HorarioAtencionRequest
{
    public int ProfesionalId { get; set; }
    public DiaSemana DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
}
