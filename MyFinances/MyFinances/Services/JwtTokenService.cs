using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MyFinances.Domain;

namespace MyFinances.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiracaoHoras;

    public JwtTokenService(IConfiguration configuration)
    {
        var jwtConfig = configuration.GetSection("Jwt");
        _secretKey = jwtConfig["Key"] ?? throw new InvalidOperationException("JWT Key nao configurada em appsettings.json");

        var secretKeyBytes = Encoding.UTF8.GetByteCount(_secretKey);
        if (secretKeyBytes < 32)
        {
            throw new InvalidOperationException($"JWT Key deve ter no minimo 32 bytes (256 bits) em UTF8. Configurada com {secretKeyBytes} bytes.");
        }

        _issuer = jwtConfig["Issuer"] ?? throw new InvalidOperationException("JWT Issuer nao configurado em appsettings.json");
        _audience = jwtConfig["Audience"] ?? throw new InvalidOperationException("JWT Audience nao configurado em appsettings.json");

        _expiracaoHoras = jwtConfig.GetValue<int>("ExpiracaoHoras", 8);
    }

    public string GerarToken(Usuario usuario)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, usuario.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(_expiracaoHoras),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
