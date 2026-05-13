using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using aspnet.DTOs;
using aspnet.Models;
using aspnet.Repositories.Interfaces;

namespace aspnet.Services;

public class AiService(
    IChatMessageRepository chatRepo,
    IAssetRepository assetRepo,
    ITransactionRepository transactionRepo,
    IFinancialGoalRepository goalRepo,
    IStockHoldingRepository stockHoldingRepo,
    ICryptoHoldingRepository cryptoHoldingRepo,
    AlpacaService alpacaService,
    PfmService pfmService,
    IConfiguration configuration,
    HttpClient httpClient)
{
    private static readonly string[] QuickQuestions =
    [
        "How can I improve my savings in Uzbekistan?",
        "What are the best investment options in Uzbekistan?",
        "Analyze my spending habits this month"
    ];

    // Tool definitions sent to OpenAI
    private static readonly object[] AgentTools =
    [
        new {
            type = "function",
            function = new {
                name = "create_transaction",
                description = "Record a new income or expense transaction in the user's personal finance tracker. Use this when the user says things like 'log my expense', 'I spent X on Y', 'I received salary', etc.",
                parameters  = new {
                    type       = "object",
                    properties = new {
                        type        = new { type = "string", @enum = new[] { "Income", "Expense" }, description = "Income or Expense" },
                        category    = new { type = "string", description = "Category: Salary, Food, Housing, Transport, Entertainment, Healthcare, Education, Utilities, Shopping, Other" },
                        amount      = new { type = "number", description = "Amount in USD" },
                        description = new { type = "string", description = "Short description of the transaction" }
                    },
                    required = new[] { "type", "category", "amount", "description" }
                }
            }
        },
        new {
            type = "function",
            function = new {
                name        = "create_goal",
                description = "Create a new savings goal for the user. Use when user says 'set a goal', 'I want to save for X', 'help me save Y amount', etc.",
                parameters  = new {
                    type       = "object",
                    properties = new {
                        name          = new { type = "string", description = "Goal name, e.g. 'Emergency Fund', 'New Laptop', 'Vacation'" },
                        targetAmount  = new { type = "number", description = "Target amount to save in USD" },
                        currentAmount = new { type = "number", description = "Amount already saved (default 0)" },
                        deadline      = new { type = "string", description = "Target date in YYYY-MM-DD format (optional)" }
                    },
                    required = new[] { "name", "targetAmount" }
                }
            }
        },
        new {
            type = "function",
            function = new {
                name        = "get_live_stock_price",
                description = "Fetch the current real-time market price for a stock. Use when user asks 'what is the price of X', 'how much is AAPL', 'current TSLA price', etc.",
                parameters  = new {
                    type       = "object",
                    properties = new {
                        symbol = new { type = "string", description = "Stock ticker, e.g. AAPL, TSLA, NVDA, MSFT, AMZN" }
                    },
                    required = new[] { "symbol" }
                }
            }
        },
        new {
            type = "function",
            function = new {
                name        = "place_stock_order",
                description = "Place a buy or sell order via the user's connected Alpaca brokerage. Only use if the user explicitly asks to buy or sell a stock AND confirms. Always confirm before executing.",
                parameters  = new {
                    type       = "object",
                    properties = new {
                        symbol     = new { type = "string", description = "Stock ticker symbol" },
                        qty        = new { type = "number", description = "Number of shares" },
                        side       = new { type = "string", @enum = new[] { "buy", "sell" } },
                        orderType  = new { type = "string", @enum = new[] { "market", "limit" }, description = "Order type" },
                        limitPrice = new { type = "number", description = "Limit price (required for limit orders)" }
                    },
                    required = new[] { "symbol", "qty", "side", "orderType" }
                }
            }
        },
        new {
            type = "function",
            function = new {
                name        = "get_portfolio_summary",
                description = "Retrieve a fresh, detailed breakdown of the user's entire financial portfolio. Use when the user asks for a portfolio overview, balance summary, or net worth.",
                parameters  = new {
                    type       = "object",
                    properties = new { },
                    required   = new string[] { }
                }
            }
        }
    ];

    // System prompt 
    private static string BuildSystemPrompt(
        List<Asset> assets, List<Transaction> transactions,
        List<FinancialGoal> goals, List<StockHolding> stockHoldings,
        List<CryptoHolding> cryptoHoldings, AlpacaAccountDataDto? alpaca)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var assetLines = FormatList(assets, a => $"{a.Name} ({a.Type}): ${a.Balance:N2}");
        var stockLines = FormatList(stockHoldings, s => $"{s.Symbol} ({s.CompanyName}): {s.Quantity} shares = ${s.TotalValue:N2}");
        var cryptoLines = FormatList(cryptoHoldings, c => $"{c.Symbol}: {c.Amount} @ ${c.PricePerUnit:N4} = ${c.TotalValue:N2}");
        var goalLines = FormatList(goals, g =>
        {
            var pct = g.TargetAmount > 0 ? Math.Round(g.CurrentAmount / g.TargetAmount * 100, 1) : 0;
            return $"{g.Name}: ${g.CurrentAmount:N2} / ${g.TargetAmount:N2} ({pct}%)";
        });

        var recentTxLines = string.Join("\n", transactions
            .OrderByDescending(t => t.Date).Take(10)
            .Select(t => $"  [{t.Date:MM-dd}] {t.Type} {t.Category} ${t.Amount:N2}: {t.Description}"));

        var monthTx = transactions.Where(t => t.Date >= startOfMonth).ToList();
        var income = monthTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expenses = monthTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        string alpacaLines;
        if (alpaca is { IsConnected: true } && alpaca.Positions.Count > 0)
        {
            var posLines = string.Join("\n", alpaca.Positions.Select(p =>
                $"  {p.Symbol}: {p.Qty} shares, ${p.MarketValue:N2}, P&L ${p.UnrealizedPl:+0.00;-0.00}"));
            alpacaLines = $"  Equity: ${alpaca.Equity:N2} | Cash: ${alpaca.Cash:N2} | Paper: {alpaca.IsPaper}\n{posLines}";
        }
        else alpacaLines = "  Not connected.";

        var grandTotal = assets.Sum(a => a.Balance) + stockHoldings.Sum(s => s.TotalValue)
                       + (alpaca?.Equity ?? 0) + cryptoHoldings.Sum(c => c.TotalValue);

        return $"""
You are FinMate AI — an intelligent personal finance assistant and agent for the Uzbekistan market.

## Capabilities
You are an AI AGENT. You have tools to:
- **Record transactions** (create_transaction): when user logs income or spending
- **Create savings goals** (create_goal): when user wants to save for something
- **Get live stock prices** (get_live_stock_price): real-time market data
- **Place stock orders** (place_stock_order): buy/sell via Alpaca (always confirm first)
- **Portfolio summary** (get_portfolio_summary): fresh financial overview

Use tools proactively when the user's intent is clear. After calling a tool, confirm what you did in a friendly message.

## Uzbekistan context
- Local banks: Kapitalbank, Hamkorbank, Xalq Banki, Ipoteka Bank, TBC Uzbekistan
- Currency: UZS (USD ≈ 12,700 UZS as of 2025)
- Investment options: OFZ-UZ government bonds, Toshkent Fond Birjasi, licensed bank deposits
- Respond in English or Uzbek based on which language the user writes in

## User's current financial snapshot
**Total portfolio: ${grandTotal:N2}**
This month — Income: ${income:N2} | Expenses: ${expenses:N2} | Surplus: ${income - expenses:N2}

### Assets: {assetLines}
### Stocks: {stockLines}
### Alpaca: {alpacaLines}
### Crypto: {cryptoLines}
### Goals: {goalLines}
### Recent transactions:
{recentTxLines}

## Rules
- Use **bold** for key numbers and actions
- Use numbered lists for steps
- Keep replies under 250 words unless more detail is requested
- Never invent data — only reference what's provided or fetched via tools
""";
    }

    // Public API 

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

        // Fetch all relevant user data for system prompt
        var assets = await assetRepo.GetByUserIdAsync(userId);
        var transactions = await transactionRepo.GetByUserIdAsync(userId);
        var goals = await goalRepo.GetByUserIdAsync(userId);
        var stockHoldings = await stockHoldingRepo.GetByUserIdAsync(userId);
        var cryptoHoldings = await cryptoHoldingRepo.GetByUserIdAsync(userId);
        var alpacaAccount = await alpacaService.GetAccountDataAsync(userId);
        var history = (await chatRepo.GetByUserIdAsync(userId)).TakeLast(20).ToList();

        var systemPrompt = BuildSystemPrompt(assets, transactions, goals,
                                             stockHoldings, cryptoHoldings, alpacaAccount);

        var aiText = await RunAgentAsync(systemPrompt, history, dto.Content, userId);

        var aiMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Content = aiText,
            IsFromAI = true
        };
        await chatRepo.CreateAsync(aiMessage);

        return new ChatMessageDto(aiMessage.Id, aiMessage.Content, aiMessage.IsFromAI, aiMessage.CreatedAt);
    }

    public static string[] GetQuickQuestions() => QuickQuestions;

    public async Task ClearHistoryAsync(Guid userId) =>
        await chatRepo.DeleteAllByUserIdAsync(userId);

    //  Agent loop 

    private async Task<string> RunAgentAsync(
        string systemPrompt, List<ChatMessage> history,
        string userMessage, Guid userId)
    {
        var apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI API key is not configured.");

        var model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";

        // Build initial message list
        var messages = new List<JsonElement>();
        var initMessages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };
        foreach (var h in history)
            initMessages.Add(new { role = h.IsFromAI ? "assistant" : "user", content = h.Content });
        initMessages.Add(new { role = "user", content = userMessage });

        // Serialize to JsonElement list so we can mix tool result shapes
        var rawMessages = JsonSerializer.SerializeToElement(initMessages).EnumerateArray().ToList();

        // Agent loop — max 5 iterations to prevent runaway loops
        for (int iteration = 0; iteration < 5; iteration++)
        {
            var requestBody = new
            {
                model,
                messages = rawMessages.Cast<object>().ToList(),
                tools = AgentTools,
                tool_choice = "auto",
                max_completion_tokens = 800,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await httpClient.PostAsync(
                "https://api.openai.com/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
                return "I'm having trouble connecting to the AI service. Please try again.";

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var choice = doc.RootElement.GetProperty("choices")[0];
            var finishReason = choice.GetProperty("finish_reason").GetString();
            var assistantMsg = choice.GetProperty("message");

            // No tool calls → final response
            if (finishReason == "stop")
            {
                return assistantMsg.TryGetProperty("content", out var textEl)
                    ? textEl.GetString() ?? "Done."
                    : "Done.";
            }

            // Tool calls execute each and loop
            if (finishReason == "tool_calls" &&
                assistantMsg.TryGetProperty("tool_calls", out var toolCallsEl))
            {
                // Add assistant message (with tool_calls) to history
                rawMessages.Add(JsonSerializer.SerializeToElement(new
                {
                    role = "assistant",
                    content = assistantMsg.TryGetProperty("content", out var c) ? c.GetString() : null,
                    tool_calls = toolCallsEl
                }));

                // Execute each tool call and append result
                foreach (var toolCall in toolCallsEl.EnumerateArray())
                {
                    var toolCallId = toolCall.GetProperty("id").GetString()!;
                    var functionName = toolCall.GetProperty("function").GetProperty("name").GetString()!;
                    var argsJson = toolCall.GetProperty("function").GetProperty("arguments").GetString()!;

                    var result = await ExecuteToolAsync(functionName, argsJson, userId);

                    rawMessages.Add(JsonSerializer.SerializeToElement(new
                    {
                        role = "tool",
                        tool_call_id = toolCallId,
                        content = result
                    }));
                }

                // Continue loop with updated messages
                continue;
            }

            // Unexpected finish reason
            break;
        }

        return "I wasn't able to complete that task. Please try rephrasing your request.";
    }

    // Tool execution 

    private async Task<string> ExecuteToolAsync(string name, string argsJson, Guid userId)
    {
        try
        {
            using var doc = JsonDocument.Parse(argsJson);
            var args = doc.RootElement;

            return name switch
            {
                "create_transaction" => await CreateTransactionTool(args, userId),
                "create_goal" => await CreateGoalTool(args, userId),
                "get_live_stock_price" => await GetLiveStockPriceTool(args, userId),
                "place_stock_order" => await PlaceStockOrderTool(args, userId),
                "get_portfolio_summary" => await GetPortfolioSummaryTool(userId),
                _ => $"Unknown tool: {name}"
            };
        }
        catch (Exception ex)
        {
            return $"Tool error: {ex.Message}";
        }
    }

    private async Task<string> CreateTransactionTool(JsonElement args, Guid userId)
    {
        var type = args.GetProperty("type").GetString()!;
        var category = args.GetProperty("category").GetString()!;
        var amount = args.GetProperty("amount").GetDecimal();
        var description = args.GetProperty("description").GetString()!;

        var dto = new CreateTransactionDto(type, category, amount, description, DateTime.UtcNow);
        var result = await pfmService.CreateTransactionAsync(userId, dto);

        return $"SUCCESS: Recorded {type} transaction — {category} ${amount:N2} ({description}). ID: {result.Id}";
    }

    private async Task<string> CreateGoalTool(JsonElement args, Guid userId)
    {
        var name = args.GetProperty("name").GetString()!;
        var targetAmount = args.GetProperty("targetAmount").GetDecimal();
        var currentAmount = args.TryGetProperty("currentAmount", out var ca) ? ca.GetDecimal() : 0m;

        DateTime? deadline = null;
        if (args.TryGetProperty("deadline", out var dl) && !string.IsNullOrWhiteSpace(dl.GetString()))
            deadline = DateTime.SpecifyKind(
                DateTime.Parse(dl.GetString()!, CultureInfo.InvariantCulture), DateTimeKind.Utc);

        var dto = new CreateGoalDto(name, targetAmount, currentAmount, deadline);
        var result = await pfmService.CreateGoalAsync(userId, dto);

        return $"SUCCESS: Goal '{name}' created — target ${targetAmount:N2}, " +
               $"saved ${currentAmount:N2} so far ({result.ProgressPercent}% complete).";
    }

    private async Task<string> GetLiveStockPriceTool(JsonElement args, Guid userId)
    {
        var symbol = args.GetProperty("symbol").GetString()!.ToUpper();

        // Try to fetch from Alpaca data API
        var alpacaAccount = await alpacaService.GetAccountDataAsync(userId);
        if (alpacaAccount.IsConnected)
        {
            var bars = await alpacaService.GetBarsForUserAsync(userId, symbol, "1Day", 2);
            if (bars.Count > 0)
                return $"{symbol} latest close price: ${bars[^1].Close:N2} (from Alpaca market data)";
        }

        // Fallback: Alpaca IEX free feed 
        try
        {
            var url = $"https://data.alpaca.markets/v2/stocks/quotes/latest?symbols={symbol}&feed=iex";
            var response = await httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("quotes", out var quotes) &&
                quotes.TryGetProperty(symbol, out var quote) &&
                quote.TryGetProperty("ap", out var ap))
            {
                var price = ap.GetDecimal();
                return $"{symbol} current ask price: ${price:N2}";
            }
        }
        catch { /* fall through */ }

        return $"Could not fetch live price for {symbol}. " +
               $"Connect your Alpaca account in Stocks for real-time data.";
    }

    private async Task<string> PlaceStockOrderTool(JsonElement args, Guid userId)
    {
        var symbol = args.GetProperty("symbol").GetString()!;
        var qty = args.GetProperty("qty").GetDecimal();
        var side = args.GetProperty("side").GetString()!;
        var orderType = args.GetProperty("orderType").GetString()!;
        decimal? limitPrice = args.TryGetProperty("limitPrice", out var lp) ? lp.GetDecimal() : null;

        var dto = new PlaceOrderDto(symbol, qty, side, orderType, limitPrice, "day");
        var result = await alpacaService.PlaceOrderAsync(userId, dto);

        return $"SUCCESS: {side.ToUpper()} order placed — {qty} {symbol.ToUpper()} ({orderType}). " +
               $"Order ID: {result.OrderId}, Status: {result.Status}";
    }

    private async Task<string> GetPortfolioSummaryTool(Guid userId)
    {
        var assets = await assetRepo.GetByUserIdAsync(userId);
        var stocks = await stockHoldingRepo.GetByUserIdAsync(userId);
        var crypto = await cryptoHoldingRepo.GetByUserIdAsync(userId);
        var alpaca = await alpacaService.GetAccountDataAsync(userId);
        var goals = await goalRepo.GetByUserIdAsync(userId);
        var transactions = await transactionRepo.GetByUserIdAsync(userId);

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthTx = transactions.Where(t => t.Date >= startOfMonth).ToList();
        var income = monthTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expenses = monthTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        var dashTotal = assets.Sum(a => a.Balance);
        var stockTotal = stocks.Sum(s => s.TotalValue);
        var alpacaTotal = alpaca.IsConnected ? alpaca.Equity : 0;
        var cryptoTotal = crypto.Sum(c => c.TotalValue);
        var grandTotal = dashTotal + stockTotal + alpacaTotal + cryptoTotal;

        var posLines = alpaca.IsConnected && alpaca.Positions.Count > 0
            ? string.Join(", ", alpaca.Positions.Select(p => $"{p.Symbol} ${p.MarketValue:N2}"))
            : "none";

        return $"""
PORTFOLIO SUMMARY (live data):
Total net worth: ${grandTotal:N2}
- Bank/savings assets: ${dashTotal:N2}
- Stock holdings: ${stockTotal:N2}
- Alpaca brokerage ({(alpaca.IsPaper ? "paper" : "live")}): ${alpacaTotal:N2} | positions: {posLines}
- Crypto: ${cryptoTotal:N2}

This month: Income ${income:N2} | Expenses ${expenses:N2} | Net ${income - expenses:N2}
Active goals: {goals.Count} ({string.Join(", ", goals.Select(g => g.Name))})
""";
    }

    // Helpers 

    private static string FormatList<T>(List<T> items, Func<T, string> formatter) =>
        items.Count > 0
            ? "\n" + string.Join("\n", items.Select(i => $"  - {formatter(i)}"))
            : " None.";
}