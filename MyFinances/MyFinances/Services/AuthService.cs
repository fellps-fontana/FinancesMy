using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MyFinances.Services;

public interface IAuthService
{
    Task<UsuarioResponse> RegistrarAsync(RegistrarUsuarioRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
}

public class AuthService : IAuthService
{
    private readonly MyFinancesDbContext _context;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    private const int UsernameMinLength = 3;
    private const int SenhaMinLength = 8;

    public AuthService(MyFinancesDbContext context, IPasswordHasherService passwordHasher, IJwtTokenService jwtTokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<UsuarioResponse> RegistrarAsync(RegistrarUsuarioRequest request)
    {
        ValidarEntrada(request);
        await ValidarDuplicatasAsync(request.Username, request.Email);

        var usuario = CriarUsuario(request);
        await SalvarUsuarioAsync(usuario);

        return MapearParaResponse(usuario);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        ValidarEntradaLogin(request);

        var usuario = await BuscarUsuarioPorUsernameOuEmailAsync(request.UsernameOrEmail);

        if (usuario == null)
        {
            throw new UnauthorizedAccessException("Credenciais invalidas.");
        }

        if (!_passwordHasher.VerifyPassword(request.Senha, usuario.SenhaHash))
        {
            throw new UnauthorizedAccessException("Credenciais invalidas.");
        }

        var token = _jwtTokenService.GerarToken(usuario);

        return new LoginResponse
        {
            Token = token,
            Usuario = MapearParaResponse(usuario)
        };
    }

    private void ValidarEntrada(RegistrarUsuarioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ArgumentException("Username nao pode ser vazio.", nameof(request.Username));
        }

        if (request.Username.Length < UsernameMinLength)
        {
            throw new ArgumentException($"Username deve ter pelo menos {UsernameMinLength} caracteres.", nameof(request.Username));
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Email nao pode ser vazio.", nameof(request.Email));
        }

        if (!ValidarFormatoEmail(request.Email))
        {
            throw new ArgumentException("Email em formato invalido.", nameof(request.Email));
        }

        if (string.IsNullOrWhiteSpace(request.Senha))
        {
            throw new ArgumentException("Senha nao pode ser vazia.", nameof(request.Senha));
        }

        if (request.Senha.Length < SenhaMinLength)
        {
            throw new ArgumentException($"Senha deve ter pelo menos {SenhaMinLength} caracteres.", nameof(request.Senha));
        }
    }

    private void ValidarEntradaLogin(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UsernameOrEmail))
        {
            throw new ArgumentException("Username ou email nao pode ser vazio.", nameof(request.UsernameOrEmail));
        }

        if (string.IsNullOrWhiteSpace(request.Senha))
        {
            throw new ArgumentException("Senha nao pode ser vazia.", nameof(request.Senha));
        }
    }

    private bool ValidarFormatoEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private async Task ValidarDuplicatasAsync(string username, string email)
    {
        var usernameExiste = await _context.Usuarios
            .AnyAsync(u => u.Username == username);

        if (usernameExiste)
        {
            throw new InvalidOperationException($"Username '{username}' ja esta em uso.");
        }

        var emailExiste = await _context.Usuarios
            .AnyAsync(u => u.Email == email);

        if (emailExiste)
        {
            throw new InvalidOperationException($"Email '{email}' ja esta em uso.");
        }
    }

    private async Task<Usuario?> BuscarUsuarioPorUsernameOuEmailAsync(string usernameOrEmail)
    {
        return await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);
    }

    private Usuario CriarUsuario(RegistrarUsuarioRequest request)
    {
        var senhaHash = _passwordHasher.HashPassword(request.Senha);

        return new Usuario
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            SenhaHash = senhaHash,
            CriadoEm = DateTime.UtcNow
        };
    }

    private async Task SalvarUsuarioAsync(Usuario usuario)
    {
        _context.Usuarios.Add(usuario);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Captura violacao de unique constraint (race condition entre ValidarDuplicatasAsync e INSERT)
            if (ex.InnerException is PostgresException pgEx && pgEx.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw new InvalidOperationException(
                    $"Username '{usuario.Username}' ou Email '{usuario.Email}' ja estao em uso.", ex);
            }
            throw;
        }
    }

    private UsuarioResponse MapearParaResponse(Usuario usuario)
    {
        return new UsuarioResponse
        {
            Id = usuario.Id,
            Username = usuario.Username,
            Email = usuario.Email,
            CriadoEm = usuario.CriadoEm
        };
    }
}
