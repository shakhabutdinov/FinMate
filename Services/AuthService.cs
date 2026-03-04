using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Google.Apis.Auth;
using aspnet.DTOs;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Services;

public class AuthService(IUserRepository userRepository, IConfiguration configuration)
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var existing = await userRepository.GetByEmailAsync(dto.Email);
        if (existing != null)
            throw new InvalidOperationException("User with this email already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Initials = $"{dto.FirstName[0]}{dto.LastName[0]}".ToUpper(),
            CreatedAt = DateTime.UtcNow,
            TrialEndDate = DateTime.UtcNow.AddDays(30)
        };

        await userRepository.CreateAsync(user);
        var token = GenerateJwtToken(user);
        return BuildAuthResponse(token, user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await userRepository.GetByEmailAsync(dto.Email)
            ?? throw new InvalidOperationException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new InvalidOperationException("Invalid email or password.");

        var token = GenerateJwtToken(user);
        return BuildAuthResponse(token, user);
    }

    public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto dto)
    {
        var clientId = configuration["Google:ClientId"]
            ?? throw new InvalidOperationException("Google Client ID not configured.");

        var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [clientId]
        });

        var user = await userRepository.GetByGoogleIdAsync(payload.Subject);
        if (user == null)
        {
            user = await userRepository.GetByEmailAsync(payload.Email);
            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = payload.Email,
                    FirstName = payload.GivenName ?? "",
                    LastName = payload.FamilyName ?? "",
                    Initials = $"{(payload.GivenName ?? "U")[0]}{(payload.FamilyName ?? "N")[0]}".ToUpper(),
                    GoogleId = payload.Subject,
                    CreatedAt = DateTime.UtcNow,
                    TrialEndDate = DateTime.UtcNow.AddDays(30)
                };
                await userRepository.CreateAsync(user);
            }
            else
            {
                user.GoogleId = payload.Subject;
                await userRepository.UpdateAsync(user);
            }
        }

        var token = GenerateJwtToken(user);
        return BuildAuthResponse(token, user);
    }

    private static AuthResponseDto BuildAuthResponse(string token, User user)
    {
        var now = DateTime.UtcNow;
        var endDate = user.SubscriptionEndDate ?? user.TrialEndDate;
        var daysRemaining = Math.Max(0, (int)(endDate - now).TotalDays);
        var isActive = endDate > now;

        return new AuthResponseDto(
            token, user.Email, user.FirstName, user.LastName, user.Initials,
            user.TrialEndDate, user.SubscriptionEndDate, isActive, daysRemaining
        );
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured.")));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName)
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
