namespace aspnet.DTOs;

public record RegisterDto(string Email, string Password, string FirstName, string LastName);
public record LoginDto(string Email, string Password);
public record GoogleLoginDto(string IdToken);

public record AuthResponseDto(
    string Token,
    string Email,
    string FirstName,
    string LastName,
    string Initials,
    DateTime TrialEndDate,
    DateTime? SubscriptionEndDate,
    bool IsActive,
    int DaysRemaining
);
