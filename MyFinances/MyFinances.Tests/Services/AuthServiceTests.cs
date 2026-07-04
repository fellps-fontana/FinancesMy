using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MyFinances.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IPasswordHasherService> _passwordHasherMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly AppDbContext _context;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _passwordHasherMock = new Mock<IPasswordHasherService>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _authService = new AuthService(_context, _passwordHasherMock.Object, _jwtTokenServiceMock.Object);
    }

    [Fact]
    public async Task RegistrarAsync_WithValidData_CreatesUserWithHashedPassword()
    {
        var request = new RegistrarUsuarioRequest
        {
            Username = "usuario_valido",
            Email = "usuario@example.com",
            Senha = "SenhaForte123!@#"
        };

        var senhaHashEsperado = "mockedHashPassword";
        _passwordHasherMock.Setup(x => x.HashPassword(request.Senha))
            .Returns(senhaHashEsperado);

        var response = await _authService.RegistrarAsync(request);

        Assert.NotNull(response);
        Assert.Equal(request.Username, response.Username);
        Assert.Equal(request.Email, response.Email);

        var usuarioNoDB = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        Assert.NotNull(usuarioNoDB);
        Assert.Equal(senhaHashEsperado, usuarioNoDB.SenhaHash);
        Assert.NotEqual(request.Senha, usuarioNoDB.SenhaHash);
    }

    [Fact]
    public async Task RegistrarAsync_WithValidData_ReturnsResponseWithoutSenhaHash()
    {
        var request = new RegistrarUsuarioRequest
        {
            Username = "usuario_teste",
            Email = "teste@example.com",
            Senha = "SenhaForte123!@#"
        };

        _passwordHasherMock.Setup(x => x.HashPassword(request.Senha))
            .Returns("mockedHashPassword");

        var response = await _authService.RegistrarAsync(request);

        Assert.NotNull(response);
        Assert.NotEmpty(response.Id.ToString());
        Assert.Equal(request.Username, response.Username);
        Assert.Equal(request.Email, response.Email);
        Assert.True(response.CriadoEm > DateTime.MinValue);
    }

    [Fact]
    public async Task RegistrarAsync_WithDuplicateUsername_ThrowsInvalidOperationException()
    {
        var usuarioExistente = new Usuario
        {
            Id = Guid.NewGuid(),
            Username = "usuario_duplicado",
            Email = "original@example.com",
            SenhaHash = "hash_existente",
            CriadoEm = DateTime.UtcNow
        };

        await _context.Usuarios.AddAsync(usuarioExistente);
        await _context.SaveChangesAsync();

        var request = new RegistrarUsuarioRequest
        {
            Username = "usuario_duplicado",
            Email = "novo@example.com",
            Senha = "SenhaForte123!@#"
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegistrarAsync(request));

        Assert.Contains("ja esta em uso", exception.Message);
    }
    [Fact]
    public async Task RegistrarAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        var usuarioExistente = new Usuario
        {
            Id = Guid.NewGuid(),
            Username = "usuario_original",
            Email = "email_duplicado@example.com",
            SenhaHash = "hash_existente",
            CriadoEm = DateTime.UtcNow
        };

        await _context.Usuarios.AddAsync(usuarioExistente);
        await _context.SaveChangesAsync();

        var request = new RegistrarUsuarioRequest
        {
            Username = "novo_usuario",
            Email = "email_duplicado@example.com",
            Senha = "SenhaForte123!@#"
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _authService.RegistrarAsync(request));

        Assert.Contains("ja esta em uso", exception.Message);
    }

    [Fact]
    public async Task RegistrarAsync_WithEmptyUsername_ThrowsArgumentException()
    {
        var request = new RegistrarUsuarioRequest
        {
            Username = "",
            Email = "valido@example.com",
            Senha = "SenhaForte123!@#"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.RegistrarAsync(request));

        Assert.Contains("nao pode ser vazio", exception.Message);
    }

    [Fact]
    public async Task RegistrarAsync_WithUsernameShortThanMinimum_ThrowsArgumentException()
    {
        var request = new RegistrarUsuarioRequest
        {
            Username = "ab",
            Email = "valido@example.com",
            Senha = "SenhaForte123!@#"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.RegistrarAsync(request));

        Assert.Contains("pelo menos 3", exception.Message);
    }
    [Fact]
    public async Task RegistrarAsync_WithEmptyEmail_ThrowsArgumentException()
    {
        var request = new RegistrarUsuarioRequest
        {
            Username = "usuario_valido",
            Email = "",
            Senha = "SenhaForte123!@#"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.RegistrarAsync(request));

        Assert.Contains("nao pode ser vazio", exception.Message);
    }

    [Fact]
    public async Task RegistrarAsync_WithInvalidEmail_ThrowsArgumentException()
    {
        var request = new RegistrarUsuarioRequest
        {
            Username = "usuario_valido",
            Email = "email_invalido_sem_arroba",
            Senha = "SenhaForte123!@#"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.RegistrarAsync(request));

        Assert.Contains("invalido", exception.Message);
    }

    [Fact]
    public async Task RegistrarAsync_WithEmptySenha_ThrowsArgumentException()
    {
        var request = new RegistrarUsuarioRequest
        {
            Username = "usuario_valido",
            Email = "valido@example.com",
            Senha = ""
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.RegistrarAsync(request));

        Assert.Contains("nao pode ser vazia", exception.Message);
    }

    [Fact]
    public async Task RegistrarAsync_WithSenhaShortThanMinimum_ThrowsArgumentException()
    {
        var request = new RegistrarUsuarioRequest
        {
            Username = "usuario_valido",
            Email = "valido@example.com",
            Senha = "Curta1!"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.RegistrarAsync(request));

        Assert.Contains("pelo menos 8", exception.Message);
    }
    [Fact]
    public async Task RegistrarAsync_WithWhitespaceUsername_ThrowsArgumentException()
    {
        var request = new RegistrarUsuarioRequest
        {
            Username = "   ",
            Email = "valido@example.com",
            Senha = "SenhaForte123!@#"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.RegistrarAsync(request));

        Assert.Contains("nao pode ser vazio", exception.Message);
    }

    [Fact]
    public async Task RegistrarAsync_WithWhitespaceEmail_ThrowsArgumentException()
    {
        var request = new RegistrarUsuarioRequest
        {
            Username = "usuario_valido",
            Email = "   ",
            Senha = "SenhaForte123!@#"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.RegistrarAsync(request));

        Assert.Contains("nao pode ser vazio", exception.Message);
    }

    [Fact]
    public async Task RegistrarAsync_WithWhitespaceSenha_ThrowsArgumentException()
    {
        var request = new RegistrarUsuarioRequest
        {
            Username = "usuario_valido",
            Email = "valido@example.com",
            Senha = "        "
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.RegistrarAsync(request));

        Assert.Contains("nao pode ser vazia", exception.Message);
    }

    [Fact]
    public async Task RegistrarAsync_WithValidDataAndHasherCalled_PasswordHasherIsInvoked()
    {
        var request = new RegistrarUsuarioRequest
        {
            Username = "usuario_teste_hash",
            Email = "teste_hash@example.com",
            Senha = "SenhaForte123!@#"
        };

        _passwordHasherMock.Setup(x => x.HashPassword(request.Senha))
            .Returns("mockedHashPassword");

        await _authService.RegistrarAsync(request);

        _passwordHasherMock.Verify(x => x.HashPassword(request.Senha), Times.Once);
    }

    /* LoginAsync tests */

    [Fact]
    public async Task LoginAsync_WithValidUsernameAndPassword_ReturnsLoginResponseWithToken()
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Username = "usuario_login",
            Email = "login@example.com",
            SenhaHash = "hash_da_senha",
            CriadoEm = DateTime.UtcNow
        };

        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            UsernameOrEmail = "usuario_login",
            Senha = "SenhaCorreta123"
        };

        var tokenEsperado = "mocked_jwt_token_123";
        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Senha, usuario.SenhaHash))
            .Returns(true);
        _jwtTokenServiceMock.Setup(x => x.GerarToken(usuario))
            .Returns(tokenEsperado);

        var response = await _authService.LoginAsync(request);

        Assert.NotNull(response);
        Assert.Equal(tokenEsperado, response.Token);
        Assert.NotEmpty(response.Token);
        Assert.NotNull(response.Usuario);
        Assert.Equal(usuario.Username, response.Usuario.Username);
        Assert.Equal(usuario.Email, response.Usuario.Email);
    }

    [Fact]
    public async Task LoginAsync_WithValidEmailAndPassword_ReturnsLoginResponse()
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Username = "usuario_por_email",
            Email = "busca_por_email@example.com",
            SenhaHash = "hash_da_senha",
            CriadoEm = DateTime.UtcNow
        };

        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            UsernameOrEmail = "busca_por_email@example.com",
            Senha = "SenhaCorreta123"
        };

        var tokenEsperado = "mocked_jwt_token_email";
        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Senha, usuario.SenhaHash))
            .Returns(true);
        _jwtTokenServiceMock.Setup(x => x.GerarToken(usuario))
            .Returns(tokenEsperado);

        var response = await _authService.LoginAsync(request);

        Assert.NotNull(response);
        Assert.Equal(tokenEsperado, response.Token);
        Assert.Equal(usuario.Email, response.Usuario.Email);
    }

    [Fact]
    public async Task LoginAsync_WithNonexistentUser_ThrowsUnauthorizedAccessException()
    {
        var request = new LoginRequest
        {
            UsernameOrEmail = "usuario_inexistente",
            Senha = "QualquerSenha123"
        };

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(request));

        Assert.Equal("Credenciais invalidas.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedAccessException()
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Username = "usuario_senha_errada",
            Email = "senha_errada@example.com",
            SenhaHash = "hash_correto",
            CriadoEm = DateTime.UtcNow
        };

        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            UsernameOrEmail = "usuario_senha_errada",
            Senha = "SenhaErrada123"
        };

        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Senha, usuario.SenhaHash))
            .Returns(false);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(request));

        Assert.Equal("Credenciais invalidas.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPasswordAndNonexistentUser_BothThrowSameMessage()
    {
        var exceptionUserNotFound = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(new LoginRequest
            {
                UsernameOrEmail = "usuario_inexistente",
                Senha = "QualquerSenha"
            }));

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Username = "usuario_teste",
            Email = "teste@example.com",
            SenhaHash = "hash_correto",
            CriadoEm = DateTime.UtcNow
        };

        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();

        _passwordHasherMock.Setup(x => x.VerifyPassword("SenhaErrada123", usuario.SenhaHash))
            .Returns(false);

        var exceptionWrongPassword = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(new LoginRequest
            {
                UsernameOrEmail = "usuario_teste",
                Senha = "SenhaErrada123"
            }));

        Assert.Equal(exceptionUserNotFound.Message, exceptionWrongPassword.Message);
        Assert.Equal("Credenciais invalidas.", exceptionUserNotFound.Message);
    }

    [Fact]
    public async Task LoginAsync_WithEmptyUsernameOrEmail_ThrowsArgumentException()
    {
        var request = new LoginRequest
        {
            UsernameOrEmail = "",
            Senha = "SenhaValida123"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.LoginAsync(request));

        Assert.Contains("nao pode ser vazio", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithWhitespaceUsernameOrEmail_ThrowsArgumentException()
    {
        var request = new LoginRequest
        {
            UsernameOrEmail = "   ",
            Senha = "SenhaValida123"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.LoginAsync(request));

        Assert.Contains("nao pode ser vazio", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithEmptySenha_ThrowsArgumentException()
    {
        var request = new LoginRequest
        {
            UsernameOrEmail = "usuario@example.com",
            Senha = ""
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.LoginAsync(request));

        Assert.Contains("nao pode ser vazia", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithWhitespaceSenha_ThrowsArgumentException()
    {
        var request = new LoginRequest
        {
            UsernameOrEmail = "usuario@example.com",
            Senha = "        "
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _authService.LoginAsync(request));

        Assert.Contains("nao pode ser vazia", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithValidPassword_JwtTokenServiceIsInvoked()
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Username = "usuario_jwt_test",
            Email = "jwt@example.com",
            SenhaHash = "hash_correto",
            CriadoEm = DateTime.UtcNow
        };

        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            UsernameOrEmail = "usuario_jwt_test",
            Senha = "SenhaCorreta123"
        };

        _passwordHasherMock.Setup(x => x.VerifyPassword(request.Senha, usuario.SenhaHash))
            .Returns(true);
        _jwtTokenServiceMock.Setup(x => x.GerarToken(usuario))
            .Returns("mocked_token");

        await _authService.LoginAsync(request);

        _jwtTokenServiceMock.Verify(x => x.GerarToken(usuario), Times.Once);
    }
}
