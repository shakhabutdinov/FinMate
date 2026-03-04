using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using aspnet.Data;
using aspnet.Models;
using aspnet.Repositories;
using aspnet.Repositories.Interfaces;
using aspnet.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FinMate API",
        Version = "v1",
        Description = "Financial management API with JWT authentication"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<FinMateDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IStockHoldingRepository, StockHoldingRepository>();
builder.Services.AddScoped<ICryptoHoldingRepository, CryptoHoldingRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IFinancialGoalRepository, FinancialGoalRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
builder.Services.AddScoped<ITrendingItemRepository, TrendingItemRepository>();
builder.Services.AddScoped<IBinanceAccountRepository, BinanceAccountRepository>();
builder.Services.AddScoped<IAlpacaAccountRepository, AlpacaAccountRepository>();
builder.Services.AddScoped<IAssetSnapshotRepository, AssetSnapshotRepository>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<CryptoService>();
builder.Services.AddScoped<PfmService>();
builder.Services.AddScoped<AiService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<BinanceService>();
builder.Services.AddScoped<AlpacaService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FinMate API v1");
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
}

app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinMateDbContext>();
    db.Database.Migrate();

    if (!db.Users.Any())
    {
        var userId = Guid.NewGuid();

        db.Users.Add(new User
        {
            Id = userId,
            Email = "john.doe@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
            FirstName = "John",
            LastName = "Doe",
            Initials = "JD",
            CreatedAt = DateTime.UtcNow
        });

        var savingsId = Guid.NewGuid();
        var stocksId = Guid.NewGuid();
        var cryptoId = Guid.NewGuid();

        db.Assets.AddRange(
            new Asset { Id = savingsId, UserId = userId, Name = "Savings Account", Type = AssetType.Savings, Balance = 25000m, ChangePercent = 2.5m, Icon = "piggy-bank" },
            new Asset { Id = stocksId, UserId = userId, Name = "Stock Portfolio", Type = AssetType.Stock, Balance = 48500m, ChangePercent = -1.3m, Icon = "trending-up" },
            new Asset { Id = cryptoId, UserId = userId, Name = "Crypto Wallet", Type = AssetType.Crypto, Balance = 15200m, ChangePercent = 5.7m, Icon = "bitcoin" }
        );

        var today = DateTime.UtcNow.Date;
        decimal[] savingsHist = [24380, 24420, 24510, 24480, 24600, 24750, 25000];
        decimal[] stocksHist  = [49200, 49450, 49100, 48800, 49050, 48870, 48500];
        decimal[] cryptoHist  = [14350, 14200, 14500, 14680, 14900, 15050, 15200];

        for (var i = 0; i < 7; i++)
        {
            var date = today.AddDays(i - 6);
            db.AssetSnapshots.Add(new AssetSnapshot { Id = Guid.NewGuid(), AssetId = savingsId, Date = date, Balance = savingsHist[i] });
            db.AssetSnapshots.Add(new AssetSnapshot { Id = Guid.NewGuid(), AssetId = stocksId, Date = date, Balance = stocksHist[i] });
            db.AssetSnapshots.Add(new AssetSnapshot { Id = Guid.NewGuid(), AssetId = cryptoId, Date = date, Balance = cryptoHist[i] });
        }

        db.StockHoldings.AddRange(
            new StockHolding { Id = Guid.NewGuid(), UserId = userId, Symbol = "AAPL", CompanyName = "Apple Inc.", PricePerShare = 185.5m, Quantity = 50, Color = "rgba(0,122,255,0.125)" },
            new StockHolding { Id = Guid.NewGuid(), UserId = userId, Symbol = "TSLA", CompanyName = "Tesla, Inc.", PricePerShare = 245.2m, Quantity = 25, Color = "rgba(227,25,55,0.125)" },
            new StockHolding { Id = Guid.NewGuid(), UserId = userId, Symbol = "NVDA", CompanyName = "NVIDIA Corp", PricePerShare = 945.8m, Quantity = 10, Color = "rgba(118,185,0,0.125)" }
        );

        db.CryptoHoldings.AddRange(
            new CryptoHolding { Id = Guid.NewGuid(), UserId = userId, Symbol = "BTC", Name = "Bitcoin", PricePerUnit = 64230.5m, Amount = 0.45m, Color = "rgba(247,147,26,0.125)" },
            new CryptoHolding { Id = Guid.NewGuid(), UserId = userId, Symbol = "ETH", Name = "Ethereum", PricePerUnit = 3450.2m, Amount = 4.2m, Color = "rgba(98,126,234,0.125)" },
            new CryptoHolding { Id = Guid.NewGuid(), UserId = userId, Symbol = "SOL", Name = "Solana", PricePerUnit = 145.8m, Amount = 150m, Color = "rgba(20,241,149,0.125)" }
        );

        db.Transactions.AddRange(
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Income, Category = "Salary", Amount = 5000m, Description = "Monthly salary", Date = new DateTime(2025, 1, 5, 0, 0, 0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Housing", Amount = 1200m, Description = "Rent payment", Date = new DateTime(2025, 1, 3, 0, 0, 0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Food", Amount = 450m, Description = "Groceries", Date = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Transport", Amount = 300m, Description = "Gas and parking", Date = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Utilities", Amount = 200m, Description = "Electric and water", Date = new DateTime(2025, 1, 8, 0, 0, 0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Entertainment", Amount = 350m, Description = "Streaming and dining", Date = new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Income, Category = "Freelance", Amount = 1240.5m, Description = "Design project", Date = new DateTime(2025, 1, 25, 0, 0, 0, DateTimeKind.Utc) }
        );

        db.TrendingItems.AddRange(
            new TrendingItem { Id = Guid.NewGuid(), Symbol = "AMZN", Price = 175.35m, ChangePercent = 2.5m, Category = "stock", ChartData = [10, 12, 11, 14, 13, 15, 16] },
            new TrendingItem { Id = Guid.NewGuid(), Symbol = "GOOGL", Price = 155.2m, ChangePercent = -1.2m, Category = "stock", ChartData = [15, 14, 13, 14, 12, 11, 11] },
            new TrendingItem { Id = Guid.NewGuid(), Symbol = "MSFT", Price = 420.45m, ChangePercent = 1.4m, Category = "stock", ChartData = [20, 21, 22, 21, 23, 24, 25] },
            new TrendingItem { Id = Guid.NewGuid(), Symbol = "META", Price = 485.6m, ChangePercent = 3.3m, Category = "stock", ChartData = [30, 32, 31, 34, 35, 37, 38] },
            new TrendingItem { Id = Guid.NewGuid(), Symbol = "PEPE", Price = 0.0000045m, ChangePercent = 12.5m, Category = "crypto", ChartData = [5, 6, 5.5m, 7, 6.5m, 8, 9] },
            new TrendingItem { Id = Guid.NewGuid(), Symbol = "ARB", Price = 1.2m, ChangePercent = -5.2m, Category = "crypto", ChartData = [10, 9.5m, 9, 8.5m, 8, 7.5m, 8] },
            new TrendingItem { Id = Guid.NewGuid(), Symbol = "TIA", Price = 12.4m, ChangePercent = 8.4m, Category = "crypto", ChartData = [8, 9, 10, 9.5m, 11, 12, 13] },
            new TrendingItem { Id = Guid.NewGuid(), Symbol = "SEI", Price = 0.85m, ChangePercent = 15.3m, Category = "crypto", ChartData = [3, 4, 3.5m, 5, 5, 6, 7] }
        );

        db.SaveChanges();
    }
}

app.Run();
