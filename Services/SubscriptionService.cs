using aspnet.DTOs;
using aspnet.Repositories.Interfaces;

namespace aspnet.Services;

public class SubscriptionService(IUserRepository userRepo)
{
    public async Task<SubscriptionStatusDto> GetStatusAsync(Guid userId)
    {
        var user = await userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var now = DateTime.UtcNow;
        var isTrial = user.SubscriptionEndDate == null;
        var endDate = user.SubscriptionEndDate ?? user.TrialEndDate;
        var daysRemaining = Math.Max(0, (int)(endDate - now).TotalDays);
        var isActive = endDate > now;

        var planName = isTrial ? "Free Trial" : "Premium";

        return new SubscriptionStatusDto(isActive, isTrial, daysRemaining, user.TrialEndDate, user.SubscriptionEndDate, planName);
    }

    public async Task<SubscriptionStatusDto> ActivateAsync(Guid userId, ActivateSubscriptionDto dto)
    {
        var user = await userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var now = DateTime.UtcNow;
        var currentEnd = user.SubscriptionEndDate ?? now;
        var newEnd = currentEnd > now ? currentEnd.AddDays(30) : now.AddDays(30);

        user.SubscriptionEndDate = newEnd;
        await userRepo.UpdateAsync(user);

        return await GetStatusAsync(userId);
    }
}
