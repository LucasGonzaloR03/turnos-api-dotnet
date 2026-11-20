namespace Turnos.Application.Dtos;

public class ServicioResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int DuracionMinutos { get; set; }
    public decimal Precio { get; set; }
    public int HorasAnticipoCancelacion { get; set; } = 2;
}
