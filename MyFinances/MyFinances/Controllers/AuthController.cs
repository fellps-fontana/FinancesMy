using MyFinances.DTOs;
using MyFinances.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("registrar")]
    public async Task<ActionResult<UsuarioResponse>> Registrar([FromBody] RegistrarUsuarioRequest request)
    {
        try
        {
            var usuarioResponse = await _authService.RegistrarAsync(request);
            return CreatedAtAction(nameof(Registrar), usuarioResponse);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validacao de entrada falhou no registro: {Message}", ex.Message);
            return BadRequest(new { erro = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Conflito no registro: {Message}", ex.Message);
            return Conflict(new { erro = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao registrar usuario");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { erro = "Erro ao processar registro. Tente novamente mais tarde." });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var loginResponse = await _authService.LoginAsync(request);
            return Ok(loginResponse);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validacao de entrada falhou no login: {Message}", ex.Message);
            return BadRequest(new { erro = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Falha de autenticacao no login: {Message}", ex.Message);
            return Unauthorized(new { erro = "Credenciais invalidas." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao fazer login");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { erro = "Erro ao processar login. Tente novamente mais tarde." });
        }
    }
}
