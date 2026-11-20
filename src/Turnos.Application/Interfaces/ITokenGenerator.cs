using Turnos.Domain.Entities;

namespace Turnos.Application.Interfaces;

public interface ITokenGenerator
{
    string Generar(Usuario usuario);
}
