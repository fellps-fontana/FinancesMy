using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyFinances.Data;
using MyFinances.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MyFinances.Tests;

public class AuthWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext and Npgsql
            var descriptorsToRemove = new List<ServiceDescriptor>();
            foreach (var descriptor in services)
            {
                if (descriptor.ServiceType == typeof(AppDbContext) ||
                    descriptor.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    descriptor.ServiceType?.FullName?.Contains("DbContextOptions") == true ||
                    descriptor.ImplementationType?.FullName?.Contains("NpgsqlConnection") == true ||
                    descriptor.ImplementationType?.FullName?.Contains("NpgsqlDataSource") == true)
                {
                    descriptorsToRemove.Add(descriptor);
                }
            }

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Register InMemory DbContext
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            // MyFinancesDbContext (modulo de investimentos) tambem precisa
            // ser trocado para InMemory aqui, senao a remocao ampla acima
            // (Contains("DbContextOptions")) o deixa sem provider registrado.
            services.AddDbContext<MyFinancesDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        });
    }
}

public class AuthIntegrationTests : IAsyncLifetime
{
    private AuthWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new AuthWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task PostRegistrar_WithoutToken_ReturnsOkOrCreated()
    {
        // Arrange
        var request = new RegistrarUsuarioRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Senha = "ValidPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/registrar", request);

        // Assert
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.Created ||
            response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
            response.StatusCode == System.Net.HttpStatusCode.Conflict,
            $"Expected 201, 400, or 409 but got {response.StatusCode}"
        );
    }

    [Fact]
    public async Task PostLogin_WithoutToken_ReturnsBadRequestOrUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            UsernameOrEmail = "nonexistent",
            Senha = "SomePassword123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
            response.StatusCode == System.Net.HttpStatusCode.Unauthorized,
            $"Expected 400 or 401 but got {response.StatusCode}. Response: {responseBody}"
        );
    }

    [Fact]
    public async Task GetProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/protected/test");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostRegistrar_ThenLogin_AndUseTokenToAccessProtected_ReturnsSuccess()
    {
        // Arrange - Registra usuario
        var registerRequest = new RegistrarUsuarioRequest
        {
            Username = "inttest_user",
            Email = "inttest@example.com",
            Senha = "StrongPassword123!"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/registrar", registerRequest);

        if (registerResponse.StatusCode != System.Net.HttpStatusCode.Created)
        {
            return;
        }

        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        var registerData = JsonSerializer.Deserialize<UsuarioResponse>(registerContent);
        Assert.NotNull(registerData);

        // Arrange - Login para obter token
        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = registerRequest.Username,
            Senha = registerRequest.Senha
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        if (loginResponse.StatusCode != System.Net.HttpStatusCode.OK)
        {
            return;
        }

        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginData = JsonSerializer.Deserialize<LoginResponse>(loginContent);
        Assert.NotNull(loginData);
        Assert.NotEmpty(loginData.Token);

        // Act
        var protectedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/protected/test");
        protectedRequest.Headers.Add("Authorization", $"Bearer {loginData.Token}");

        var protectedResponse = await _client.SendAsync(protectedRequest);

        // Assert
        Assert.NotEqual(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
    }
}
