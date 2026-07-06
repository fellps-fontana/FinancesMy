using MyFinances.Domain;
using MyFinances.Services;

namespace MyFinances.Tests.Services;

public class PasswordHasherServiceTests
{
    private readonly PasswordHasherService _service = new();

    [Fact]
    public void HashPassword_ValidPassword_ReturnsNonEmptyHash()
    {
        var password = "SenhaForte123!@#";

        var hash = _service.HashPassword(password);

        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void HashPassword_ValidPassword_ReturnsHashDifferentFromPassword()
    {
        var password = "SenhaForte123!@#";

        var hash = _service.HashPassword(password);

        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void HashPassword_SamePasswordTwice_GeneratesDifferentHashes()
    {
        var password = "SenhaForte123!@#";

        var hash1 = _service.HashPassword(password);
        var hash2 = _service.HashPassword(password);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        var password = "SenhaForte123!@#";
        var hash = _service.HashPassword(password);

        var result = _service.VerifyPassword(password, hash);

        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_IncorrectPassword_ReturnsFalse()
    {
        var password = "SenhaForte123!@#";
        var incorrectPassword = "SenhaErrada123!@#";
        var hash = _service.HashPassword(password);

        var result = _service.VerifyPassword(incorrectPassword, hash);

        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_EmptyPassword_ThrowsArgumentException()
    {
        var hash = _service.HashPassword("QualquerSenha123!@#");

        var exception = Assert.Throws<ArgumentException>(() => 
            _service.VerifyPassword("", hash));

        Assert.Equal("Password cannot be null or empty. (Parameter 'password')", exception.Message);
    }

    [Fact]
    public void VerifyPassword_NullPassword_ThrowsArgumentException()
    {
        var hash = _service.HashPassword("QualquerSenha123!@#");

        var exception = Assert.Throws<ArgumentException>(() => 
            _service.VerifyPassword(null!, hash));

        Assert.Equal("Password cannot be null or empty. (Parameter 'password')", exception.Message);
    }

    [Fact]
    public void VerifyPassword_WhitespacePassword_ThrowsArgumentException()
    {
        var hash = _service.HashPassword("QualquerSenha123!@#");

        var exception = Assert.Throws<ArgumentException>(() => 
            _service.VerifyPassword("   ", hash));

        Assert.Equal("Password cannot be null or empty. (Parameter 'password')", exception.Message);
    }

    [Fact]
    public void VerifyPassword_EmptyHash_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => 
            _service.VerifyPassword("SenhaQualquer123!@#", ""));

        Assert.Equal("Hash cannot be null or empty. (Parameter 'hash')", exception.Message);
    }

    [Fact]
    public void VerifyPassword_NullHash_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => 
            _service.VerifyPassword("SenhaQualquer123!@#", null!));

        Assert.Equal("Hash cannot be null or empty. (Parameter 'hash')", exception.Message);
    }

    [Fact]
    public void VerifyPassword_WhitespaceHash_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => 
            _service.VerifyPassword("SenhaQualquer123!@#", "   "));

        Assert.Equal("Hash cannot be null or empty. (Parameter 'hash')", exception.Message);
    }

    [Fact]
    public void HashPassword_NullPassword_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => 
            _service.HashPassword(null!));

        Assert.Equal("Password cannot be null or empty. (Parameter 'password')", exception.Message);
    }

    [Fact]
    public void HashPassword_EmptyPassword_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => 
            _service.HashPassword(""));

        Assert.Equal("Password cannot be null or empty. (Parameter 'password')", exception.Message);
    }

    [Fact]
    public void HashPassword_WhitespacePassword_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => 
            _service.HashPassword("   "));

        Assert.Equal("Password cannot be null or empty. (Parameter 'password')", exception.Message);
    }

    [Theory]
    [InlineData("SenhaForte123!@#")]
    [InlineData("outraSenha456")]
    [InlineData("Maisouta$%&789")]
    public void VerifyPassword_MultipleValidPasswords_RoundTripSucceeds(string password)
    {
        var hash = _service.HashPassword(password);

        var result = _service.VerifyPassword(password, hash);

        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_InvalidHash_ReturnsFalse()
    {
        var password = "SenhaCorreta123!@#";
        var invalidHash = "naoEumHashValido";

        var result = _service.VerifyPassword(password, invalidHash);

        Assert.False(result);
    }
}
