namespace Turnos.Infrastructure.Security;

public class JwtOptions
{
    public const string Seccion = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiracionMinutos { get; set; } = 60;
}
