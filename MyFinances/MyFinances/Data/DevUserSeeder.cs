using MyFinances.Domain;
using MyFinances.Services;
using Microsoft.EntityFrameworkCore;

namespace MyFinances.Data;

public static class DevUserSeeder
{
    public const string Username = "teste";
    public const string Email = "teste@teste.com";
    public const string Senha = "Teste123!";

    public static async Task SeedAsync(AppDbContext context, IPasswordHasherService hasher)
    {
        var jaExiste = await context.Usuarios.AnyAsync(u => u.Username == Username);
        if (jaExiste)
        {
            return;
        }

        context.Usuarios.Add(new Usuario
        {
            Id = Guid.NewGuid(),
            Username = Username,
            Email = Email,
            SenhaHash = hasher.HashPassword(Senha),
            CriadoEm = DateTime.UtcNow,
        });

        await context.SaveChangesAsync();
    }
}
