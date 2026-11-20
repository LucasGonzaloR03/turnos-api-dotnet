using Turnos.Domain.Entities;

namespace Turnos.Application.Dtos;

public class TurnoResponse
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public int ProfesionalId { get; set; }
    public string ProfesionalNombre { get; set; } = string.Empty;
    public int ServicioId { get; set; }
    public string ServicioNombre { get; set; } = string.Empty;
    public int DuracionMinutos { get; set; }
    public decimal Precio { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
    public TurnoEstado Estado { get; set; }
}
