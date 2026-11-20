namespace Turnos.Application.Exceptions;

public class CredencialesInvalidasException(string mensaje = "Credenciales inválidas.") : Exception(mensaje);
