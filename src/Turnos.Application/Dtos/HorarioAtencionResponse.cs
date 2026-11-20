using Turnos.Domain.Entities;

namespace Turnos.Application.Dtos;

public class HorarioAtencionResponse
{
    public int Id { get; set; }
    public int ProfesionalId { get; set; }
    public string ProfesionalNombre { get; set; } = string.Empty;
    public DiaSemana DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
}
