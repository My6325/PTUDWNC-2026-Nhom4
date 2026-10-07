using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CulinaryBlog.Infrastructure.Services;
using CulinaryBlog.Domain.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace CulinaryBlog.Infrastructure.IntegrationTests;

public class JwtServiceTests
{
    private readonly JwtSettings _jwtSettings;
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            Secret = "SuperSecretKeyForTestingPurpose1234567890",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };
        _jwtService = new JwtService(Options.Create(_jwtSettings));
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ValidToken_ReturnsPrincipal()
    {
        // Arrange
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "123")
            }),
            NotBefore = DateTime.UtcNow.AddMinutes(-10),
            Expires = DateTime.UtcNow.AddMinutes(-5), // Expired token
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Act
        var principal = _jwtService.GetPrincipalFromExpiredToken(tokenString);

        // Assert
        Assert.NotNull(principal);
        Assert.Equal("123", principal.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_InvalidSignature_ReturnsNull()
    {
        // Arrange
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("WrongSecretKeyForTestingPurpose1234567890");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "123")
            }),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Act
        var principal = _jwtService.GetPrincipalFromExpiredToken(tokenString);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_NoneAlgorithm_ReturnsNull()
    {
        // Arrange
        var tokenString = "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJzdWIiOiIxMjMifQ."; // alg=none

        // Act
        var principal = _jwtService.GetPrincipalFromExpiredToken(tokenString);

        // Assert
        Assert.Null(principal);
    }
}
