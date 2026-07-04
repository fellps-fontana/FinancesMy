#nullable disable

using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Configuration;
using MyFinances.Domain;
using MyFinances.Services;

namespace MyFinances.Tests.Services;

public class JwtTokenServiceTests
{
    private IConfiguration CreateConfiguration(string key = "minha-chave-super-segura-com-mais-de-32-bytes-utf8",
        string issuer = "MyFinances",
        string audience = "MyFinancesApp",
        int expiracaoHoras = 8)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Jwt:Key", key },
                { "Jwt:Issuer", issuer },
                { "Jwt:Audience", audience },
                { "Jwt:ExpiracaoHoras", expiracaoHoras.ToString() }
            })
            .Build();
    }

    [Fact]
    public void Constructor_ValidConfigurationWithValidKey_InitializesSuccessfully()
    {
        var config = CreateConfiguration();
        var service = new JwtTokenService(config);
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_KeyWithLessThan32Bytes_ThrowsInvalidOperationException()
    {
        var config = CreateConfiguration(key: "chave-curta");
        var exception = Assert.Throws<InvalidOperationException>(() => new JwtTokenService(config));
        Assert.Contains("minimo 32 bytes", exception.Message);
    }

    [Fact]
    public void Constructor_KeyWithExactly32Bytes_InitializesSuccessfully()
    {
        var key = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        Assert.Equal(32, Encoding.UTF8.GetByteCount(key));
        var config = CreateConfiguration(key: key);
        var service = new JwtTokenService(config);
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_KeyWithMoreThan32Bytes_InitializesSuccessfully()
    {
        var key = "esta-chave-tem-muito-mais-que-32-bytes-de-tamanho-para-garantir-seguranca";
        Assert.True(Encoding.UTF8.GetByteCount(key) > 32);
        var config = CreateConfiguration(key: key);
        var service = new JwtTokenService(config);
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_MissingJwtKey_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Jwt:Issuer", "MyFinances" },
                { "Jwt:Audience", "MyFinancesApp" },
                { "Jwt:ExpiracaoHoras", "8" }
            })
            .Build();
        var exception = Assert.Throws<InvalidOperationException>(() => new JwtTokenService(config));
        Assert.Contains("JWT Key", exception.Message);
    }

    [Fact]
    public void Constructor_MissingJwtIssuer_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Jwt:Key", "minha-chave-super-segura-com-mais-de-32-bytes-utf8" },
                { "Jwt:Audience", "MyFinancesApp" },
                { "Jwt:ExpiracaoHoras", "8" }
            })
            .Build();
        var exception = Assert.Throws<InvalidOperationException>(() => new JwtTokenService(config));
        Assert.Contains("JWT Issuer", exception.Message);
    }

    [Fact]
    public void Constructor_MissingJwtAudience_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Jwt:Key", "minha-chave-super-segura-com-mais-de-32-bytes-utf8" },
                { "Jwt:Issuer", "MyFinances" },
                { "Jwt:ExpiracaoHoras", "8" }
            })
            .Build();
        var exception = Assert.Throws<InvalidOperationException>(() => new JwtTokenService(config));
        Assert.Contains("JWT Audience", exception.Message);
    }

    [Fact]
    public void GerarToken_ValidUsuario_ReturnsNonEmptyString()
    {
        var config = CreateConfiguration();
        var service = new JwtTokenService(config);
        var usuario = new Usuario { Id = Guid.NewGuid(), Username = "testuser" };
        var token = service.GerarToken(usuario);
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GerarToken_ValidUsuario_ReturnsValidJwtFormat()
    {
        var config = CreateConfiguration();
        var service = new JwtTokenService(config);
        var usuario = new Usuario { Id = Guid.NewGuid(), Username = "testuser" };
        var token = service.GerarToken(usuario);
        var parts = token.Split(".");
        Assert.Equal(3, parts.Length);
        Assert.All(parts, part => Assert.NotEmpty(part));
    }

    [Fact]
    public void GerarToken_ValidUsuario_ContainsCorrectSubClaim()
    {
        var config = CreateConfiguration();
        var service = new JwtTokenService(config);
        var usuarioId = Guid.NewGuid();
        var usuario = new Usuario { Id = usuarioId, Username = "testuser" };
        var token = service.GerarToken(usuario);
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        var subClaim = jwtToken.Claims.First(c => c.Type == "sub");
        Assert.Equal(usuarioId.ToString(), subClaim.Value);
    }

    [Fact]
    public void GerarToken_ValidUsuario_ContainsCorrectUniqueNameClaim()
    {
        var config = CreateConfiguration();
        var service = new JwtTokenService(config);
        var username = "testuser123";
        var usuario = new Usuario { Id = Guid.NewGuid(), Username = username };
        var token = service.GerarToken(usuario);
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        var uniqueNameClaim = jwtToken.Claims.First(c => c.Type == "unique_name");
        Assert.Equal(username, uniqueNameClaim.Value);
    }

    [Fact]
    public void GerarToken_DifferentUsuarios_GenerateDifferentTokens()
    {
        var config = CreateConfiguration();
        var service = new JwtTokenService(config);
        var usuario1 = new Usuario { Id = Guid.NewGuid(), Username = "user1" };
        var usuario2 = new Usuario { Id = Guid.NewGuid(), Username = "user2" };
        var token1 = service.GerarToken(usuario1);
        var token2 = service.GerarToken(usuario2);
        Assert.NotEqual(token1, token2);
    }


    [Fact]
    public void GerarToken_ValidUsuario_TokenContainsIssuer()
    {
        var issuer = "MyFinances";
        var config = CreateConfiguration(issuer: issuer);
        var service = new JwtTokenService(config);
        var usuario = new Usuario { Id = Guid.NewGuid(), Username = "testuser" };
        var token = service.GerarToken(usuario);
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        Assert.Equal(issuer, jwtToken.Issuer);
    }

    [Fact]
    public void GerarToken_ValidUsuario_TokenContainsAudience()
    {
        var audience = "MyFinancesApp";
        var config = CreateConfiguration(audience: audience);
        var service = new JwtTokenService(config);
        var usuario = new Usuario { Id = Guid.NewGuid(), Username = "testuser" };
        var token = service.GerarToken(usuario);
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        Assert.Contains(audience, jwtToken.Audiences);
    }

    [Fact]
    public void GerarToken_ValidUsuario_TokenHasExpirationTime()
    {
        var config = CreateConfiguration();
        var service = new JwtTokenService(config);
        var usuario = new Usuario { Id = Guid.NewGuid(), Username = "testuser" };
        var token = service.GerarToken(usuario);
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        Assert.True(jwtToken.ValidTo > DateTime.UtcNow);
    }

    [Fact]
    public void GerarToken_ExpiracaoHoras_RespectsConfiguredExpiration()
    {
        var expiracaoHoras = 2;
        var config = CreateConfiguration(expiracaoHoras: expiracaoHoras);
        var service = new JwtTokenService(config);
        var usuario = new Usuario { Id = Guid.NewGuid(), Username = "testuser" };
        var beforeGeneration = DateTime.UtcNow;
        var token = service.GerarToken(usuario);
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        var expectedExpiration = beforeGeneration.AddHours(expiracaoHoras);
        var timeDifference = Math.Abs((jwtToken.ValidTo - expectedExpiration).TotalSeconds);
        Assert.True(timeDifference < 5, $"Token expiration differs by {timeDifference} seconds");
    }

    [Theory]
    [InlineData("usuario-123", "user@example.com")]
    [InlineData("admin", "admin@example.com")]
    [InlineData("teste-user-89", "teste@example.com")]
    public void GerarToken_MultipleUsuarios_AllGenerateValidTokens(string username, string email)
    {
        var config = CreateConfiguration();
        var service = new JwtTokenService(config);
        var usuario = new Usuario { Id = Guid.NewGuid(), Username = username, Email = email };
        var token = service.GerarToken(usuario);
        Assert.NotEmpty(token);
        var parts = token.Split(".");
        Assert.Equal(3, parts.Length);
    }
}
