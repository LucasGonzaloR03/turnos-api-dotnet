namespace Turnos.Domain.Entities;

public class Profesional
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public ICollection<Servicio> Servicios { get; set; } = new List<Servicio>();
    public ICollection<HorarioAtencion> Horarios { get; set; } = new List<HorarioAtencion>();
    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
}
