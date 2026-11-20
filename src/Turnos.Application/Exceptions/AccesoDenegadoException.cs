namespace Turnos.Application.Exceptions;

public class AccesoDenegadoException(string mensaje = "No tenés permiso para esta operación.") : Exception(mensaje);
