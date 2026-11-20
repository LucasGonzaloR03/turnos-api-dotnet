namespace Turnos.Domain.Entities;

public class Servicio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int DuracionMinutos { get; set; }
    public decimal Precio { get; set; }
    public int HorasAnticipoCancelacion { get; set; } = 2;

    public ICollection<Profesional> Profesionales { get; set; } = new List<Profesional>();
    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
}
