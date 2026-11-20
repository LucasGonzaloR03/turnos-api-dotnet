namespace Turnos.Application.Dtos;

public class ProfesionalRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public List<int> ServicioIds { get; set; } = [];
}
