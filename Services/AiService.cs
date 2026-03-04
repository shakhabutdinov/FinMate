using aspnet.DTOs;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Services;

public class AiService(IChatMessageRepository chatRepo)
{
    private static readonly string[] QuickQuestions =
    [
        "How much return over investment I may expect this year?",
        "What are the current trends in crypto?",
        "Analyze my spending habits for this month",
        "Should I invest in tech stocks now?",
        "How can I diversify my portfolio?"
    ];

    public async Task<List<ChatMessageDto>> GetChatHistoryAsync(Guid userId)
    {
        var messages = await chatRepo.GetByUserIdAsync(userId);
        return messages.Select(m => new ChatMessageDto(m.Id, m.Content, m.IsFromAI, m.CreatedAt)).ToList();
    }

    public async Task<ChatMessageDto> SendMessageAsync(Guid userId, SendMessageDto dto)
    {
        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Content = dto.Content,
            IsFromAI = false
        };
        await chatRepo.CreateAsync(userMessage);

        var aiResponse = GenerateAiResponse(dto.Content);
        var aiMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Content = aiResponse,
            IsFromAI = true
        };
        await chatRepo.CreateAsync(aiMessage);

        return new ChatMessageDto(aiMessage.Id, aiMessage.Content, aiMessage.IsFromAI, aiMessage.CreatedAt);
    }

    public static string[] GetQuickQuestions() => QuickQuestions;

    public async Task ClearHistoryAsync(Guid userId) =>
        await chatRepo.DeleteAllByUserIdAsync(userId);

    private static string GenerateAiResponse(string userMessage)
    {
        var lower = userMessage.ToLower();

        if (lower.Contains("return") || lower.Contains("investment") || lower.Contains("roi"))
            return "Based on your current portfolio performance, you can expect approximately 8-12% annual return. Your stock portfolio has been performing well with a 3.2% gain, while your crypto assets show higher volatility with a 5.1% increase. Consider maintaining your current diversification strategy.";

        if (lower.Contains("crypto") || lower.Contains("trend"))
            return "Current crypto trends show Bitcoin maintaining strong support above $60,000, while Ethereum continues to build momentum in the DeFi space. Solana has been gaining traction with its fast transaction speeds. PEPE and other meme coins are showing volatility - exercise caution with speculative assets.";

        if (lower.Contains("spending") || lower.Contains("habit") || lower.Contains("expense"))
            return "Your spending analysis shows Housing (48%) as your largest expense at $1,200/month. Food expenses are at $450 (18%), followed by Entertainment at $350 (14%). Consider reducing entertainment spending by 10% to increase your savings rate. Your overall spending is within healthy limits.";

        if (lower.Contains("tech") || lower.Contains("stock"))
            return "Tech stocks are showing mixed signals. Apple and NVIDIA continue to show strong fundamentals, while Tesla remains volatile. Microsoft shows steady growth. I'd recommend maintaining your current positions and considering dollar-cost averaging into NVIDIA given the AI sector growth.";

        if (lower.Contains("diversif") || lower.Contains("portfolio"))
            return "Your portfolio allocation is: Stocks 55%, Crypto 17%, Savings 28%. For better diversification, consider: 1) Adding bonds or ETFs for stability, 2) Increasing your emergency fund to 6 months of expenses, 3) Exploring international markets. Your current crypto allocation is slightly high for a balanced portfolio.";

        return "I've analyzed your financial data. Your total portfolio value is $88,700 with a healthy mix of savings, stocks, and crypto. Would you like me to provide specific advice on any area? You can ask about investment returns, market trends, spending habits, or portfolio diversification.";
    }
}
