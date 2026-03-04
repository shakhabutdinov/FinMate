namespace aspnet.DTOs;

public record SubscriptionStatusDto(
    bool IsActive,
    bool IsTrial,
    int DaysRemaining,
    DateTime TrialEndDate,
    DateTime? SubscriptionEndDate,
    string PlanName
);

public record ActivateSubscriptionDto(string PaymentToken, string PaymentMethod);
