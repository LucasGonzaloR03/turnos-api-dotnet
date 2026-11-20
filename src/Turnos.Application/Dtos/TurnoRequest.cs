namespace Turnos.Application.Dtos;

public class TurnoRequest
{
    public int ProfesionalId { get; set; }
    public int ServicioId { get; set; }
    public DateTime Inicio { get; set; }
}
