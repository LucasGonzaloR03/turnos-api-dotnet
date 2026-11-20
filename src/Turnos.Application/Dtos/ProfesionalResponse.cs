namespace Turnos.Application.Dtos;

public class ProfesionalResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public IReadOnlyList<ServicioResponse> Servicios { get; set; } = [];
}
