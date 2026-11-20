namespace Turnos.Domain.Entities;

public class Turno
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public int ProfesionalId { get; set; }
    public Profesional Profesional { get; set; } = null!;

    public int ServicioId { get; set; }
    public Servicio Servicio { get; set; } = null!;

    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
    public TurnoEstado Estado { get; set; } = TurnoEstado.Pendiente;
}
