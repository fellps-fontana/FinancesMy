namespace MyFinances.DTOs;

public class LoginRequest
{
    public string UsernameOrEmail { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}
