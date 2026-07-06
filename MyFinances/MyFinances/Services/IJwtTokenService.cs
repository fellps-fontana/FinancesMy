using MyFinances.Domain;

namespace MyFinances.Services;

public interface IJwtTokenService
{
    string GerarToken(Usuario usuario);
}
