namespace Turnos.Domain.Entities;

public class HorarioAtencion
{
    public int Id { get; set; }
    public int ProfesionalId { get; set; }
    public Profesional Profesional { get; set; } = null!;
    public DiaSemana DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
}
