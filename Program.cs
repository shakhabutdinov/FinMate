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
builder.Services.AddHttpClient<StockService>();
builder.Services.AddHttpClient<CryptoService>();
builder.Services.AddScoped<PfmService>();
builder.Services.AddHttpClient<AiService>();
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

    //  Seed data — Uzbekistan market context 
    if (!db.Users.Any())
    {
        var userId = Guid.NewGuid();

        db.Users.Add(new User
        {
            Id           = userId,
            Email        = "shakhabutdinovabdulaziz@gmail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("1"),
            FirstName    = "Abdulaziz",
            LastName     = "Shakhabutdinov",
            Initials     = "AS",
            CreatedAt    = DateTime.UtcNow
        });

        //  Assets ─
        // Amounts reflect realistic Uzbekistan professional finances
        // Average IT salary in Tashkent: ~$800–1,200/month
        // 1 USD ≈ 12,700 UZS (2026)

        var kapitalId  = Guid.NewGuid();
        var hamkorId   = Guid.NewGuid();
        var cashId     = Guid.NewGuid();

        db.Assets.AddRange(
            new Asset
            {
                Id = kapitalId, UserId = userId,
                Name = "Kapitalbank — Jamg'arma", // Savings in Uzbek
                Type = AssetType.Savings, Balance = 3200m,
                ChangePercent = 1.8m, Icon = "piggy-bank"
            },
            new Asset
            {
                Id = hamkorId, UserId = userId,
                Name = "Hamkorbank — Joriy Hisob", // Current account
                Type = AssetType.Savings, Balance = 1450m,
                ChangePercent = 0.5m, Icon = "credit-card"
            },
            new Asset
            {
                Id = cashId, UserId = userId,
                Name = "Naqd pul (UZS)",           
                Type = AssetType.Savings, Balance = 380m,
                ChangePercent = 0m, Icon = "banknote"
            }
        );

        //  Asset snapshots (7 days history) 
        var today = DateTime.UtcNow.Date;
        decimal[] kapitalHist = [3050, 3080, 3100, 3120, 3150, 3180, 3200];
        decimal[] hamkorHist  = [1380, 1390, 1400, 1410, 1430, 1440, 1450];
        decimal[] cashHist    = [500, 480, 460, 440, 420, 400, 380];

        for (var i = 0; i < 7; i++)
        {
            var date = today.AddDays(i - 6);
            db.AssetSnapshots.Add(new AssetSnapshot { Id = Guid.NewGuid(), AssetId = kapitalId, Date = date, Balance = kapitalHist[i] });
            db.AssetSnapshots.Add(new AssetSnapshot { Id = Guid.NewGuid(), AssetId = hamkorId,  Date = date, Balance = hamkorHist[i] });
            db.AssetSnapshots.Add(new AssetSnapshot { Id = Guid.NewGuid(), AssetId = cashId,    Date = date, Balance = cashHist[i] });
        }

        //  Stock holdings 
        // Uzbek investors access global stocks via Alpaca
        db.StockHoldings.AddRange(
            new StockHolding { Id = Guid.NewGuid(), UserId = userId, Symbol = "AAPL",  CompanyName = "Apple Inc.",     PricePerShare = 185.5m,  Quantity = 5,  Color = "rgba(0,122,255,0.125)" },
            new StockHolding { Id = Guid.NewGuid(), UserId = userId, Symbol = "NVDA",  CompanyName = "NVIDIA Corp.",   PricePerShare = 875.3m,  Quantity = 2,  Color = "rgba(118,185,0,0.125)" },
            new StockHolding { Id = Guid.NewGuid(), UserId = userId, Symbol = "MSFT",  CompanyName = "Microsoft Corp.", PricePerShare = 415.2m, Quantity = 3,  Color = "rgba(0,120,215,0.125)" }
        );

        //  Crypto holdings 
        db.CryptoHoldings.AddRange(
            new CryptoHolding { Id = Guid.NewGuid(), UserId = userId, Symbol = "BTC", Name = "Bitcoin",  PricePerUnit = 64230.5m, Amount = 0.08m, Color = "rgba(247,147,26,0.125)" },
            new CryptoHolding { Id = Guid.NewGuid(), UserId = userId, Symbol = "ETH", Name = "Ethereum", PricePerUnit = 3450.2m,  Amount = 1.5m,  Color = "rgba(98,126,234,0.125)"  },
            new CryptoHolding { Id = Guid.NewGuid(), UserId = userId, Symbol = "SOL", Name = "Solana",   PricePerUnit = 145.8m,   Amount = 20m,   Color = "rgba(20,241,149,0.125)"  }
        );

        //  Transactions (6 months — Uzbekistan context)
        // Categories and descriptions reflect real Tashkent life
        var now = DateTime.UtcNow;
        db.Transactions.AddRange(

            //  May 2026 
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Income,  Category = "Salary",        Amount = 950m,   Description = "IT developer maoshi — Humans Lab",        Date = new DateTime(2026, 5,  5, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Housing",       Amount = 280m,   Description = "Uy ijarasi — Chilonzor tumani",           Date = new DateTime(2026, 5,  3, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Food",          Amount = 95m,    Description = "Korzinka supermarket — oziq-ovqat",       Date = new DateTime(2026, 5,  8, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Transport",     Amount = 35m,    Description = "Yandex Taxi va metro",                    Date = new DateTime(2026, 5, 12, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Utilities",     Amount = 28m,    Description = "Gaz, elektr va suv to'lovi",              Date = new DateTime(2026, 5,  7, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Mobile",        Amount = 8m,     Description = "Ucell internet va aloqa paketi",          Date = new DateTime(2026, 5,  1, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Income,  Category = "Freelance",     Amount = 320m,   Description = "Web loyiha — Toshkent startap uchun",     Date = new DateTime(2026, 5, 20, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Entertainment", Amount = 45m,    Description = "Do'stlar bilan restoran — Plov Centre",   Date = new DateTime(2026, 5, 18, 0,0,0, DateTimeKind.Utc) },

            //  April 2026 
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Income,  Category = "Salary",        Amount = 950m,   Description = "IT developer maoshi — Humans Lab",        Date = new DateTime(2026, 4,  5, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Housing",       Amount = 280m,   Description = "Uy ijarasi — Chilonzor tumani",           Date = new DateTime(2026, 4,  3, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Food",          Amount = 110m,   Description = "Makro do'kon — oylik xarid",              Date = new DateTime(2026, 4, 10, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Healthcare",    Amount = 55m,    Description = "Dr. Rakhimov — tibbiy ko'rik",            Date = new DateTime(2026, 4, 14, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Transport",     Amount = 42m,    Description = "Yandex taxi va avtobus",                  Date = new DateTime(2026, 4, 16, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Utilities",     Amount = 31m,    Description = "Kommunal xizmatlar",                      Date = new DateTime(2026, 4,  8, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Shopping",      Amount = 120m,   Description = "Kiyim — Next brendidan",                  Date = new DateTime(2026, 4, 22, 0,0,0, DateTimeKind.Utc) },

            //  March 2026 
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Income,  Category = "Salary",        Amount = 950m,   Description = "IT developer maoshi — Humans Lab",        Date = new DateTime(2026, 3,  5, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Income,  Category = "Bonus",         Amount = 200m,   Description = "Navruz bayrami mukofoti",                 Date = new DateTime(2026, 3, 21, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Housing",       Amount = 280m,   Description = "Uy ijarasi — Chilonzor tumani",           Date = new DateTime(2026, 3,  3, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Food",          Amount = 130m,   Description = "Navruz uchun maxsus xaridlar",            Date = new DateTime(2026, 3, 20, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Entertainment", Amount = 85m,    Description = "Navruz sayohati — Samarqand",             Date = new DateTime(2026, 3, 21, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Transport",     Amount = 60m,    Description = "Samarqandga poyezd chipta",               Date = new DateTime(2026, 3, 19, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Utilities",     Amount = 25m,    Description = "Kommunal xizmatlar",                      Date = new DateTime(2026, 3,  7, 0,0,0, DateTimeKind.Utc) },

            //  February 2026 
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Income,  Category = "Salary",        Amount = 950m,   Description = "IT developer maoshi — Humans Lab",        Date = new DateTime(2026, 2,  5, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Housing",       Amount = 280m,   Description = "Uy ijarasi — Chilonzor tumani",           Date = new DateTime(2026, 2,  3, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Food",          Amount = 98m,    Description = "Korzinka va Makro xaridlari",             Date = new DateTime(2026, 2, 10, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Education",     Amount = 150m,   Description = "Ingliz tili kursi — British Council",     Date = new DateTime(2026, 2, 15, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Utilities",     Amount = 35m,    Description = "Elektr va gaz to'lovi",                   Date = new DateTime(2026, 2,  8, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Income,  Category = "Freelance",     Amount = 180m,   Description = "Logo dizayn — Toshkent restoran",         Date = new DateTime(2026, 2, 22, 0,0,0, DateTimeKind.Utc) },

            //  January 2026 
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Income,  Category = "Salary",        Amount = 950m,   Description = "IT developer maoshi — Humans Lab",        Date = new DateTime(2026, 1,  5, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Housing",       Amount = 280m,   Description = "Uy ijarasi — Chilonzor tumani",           Date = new DateTime(2026, 1,  3, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Food",          Amount = 105m,   Description = "Yangi yil uchun mahsulotlar",             Date = new DateTime(2026, 1,  8, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Entertainment", Amount = 90m,    Description = "Yangi yil kechasi — Grand Mir hotel",     Date = new DateTime(2026, 1,  1, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Shopping",      Amount = 75m,    Description = "Yangi yil sovg'alari",                   Date = new DateTime(2026, 1, 28, 0,0,0, DateTimeKind.Utc) },
            new Transaction { Id = Guid.NewGuid(), UserId = userId, Type = TransactionType.Expense, Category = "Utilities",     Amount = 38m,    Description = "Qishki kommunal xizmatlar",               Date = new DateTime(2026, 1,  7, 0,0,0, DateTimeKind.Utc) }
        );

        //  Financial goals 
        db.FinancialGoals.AddRange(
            new FinancialGoal
            {
                Id = Guid.NewGuid(), UserId = userId,
                Name          = "Kvartira uchun boshlang'ich to'lov",
                TargetAmount  = 15000m,
                CurrentAmount = 3200m,
                Deadline      = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt     = DateTime.UtcNow
            },
            new FinancialGoal
            {
                Id = Guid.NewGuid(), UserId = userId,
                Name          = "Favqulodda jamg'arma fondi",         
                TargetAmount  = 2500m,
                CurrentAmount = 1450m,
                Deadline      = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt     = DateTime.UtcNow
            },
            new FinancialGoal
            {
                Id = Guid.NewGuid(), UserId = userId,
                Name          = "MacBook Pro M4",
                TargetAmount  = 2000m,
                CurrentAmount = 650m,
                Deadline      = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt     = DateTime.UtcNow
            },
            new FinancialGoal
            {
                Id = Guid.NewGuid(), UserId = userId,
                Name          = "Toshkent-Istanbul sayohati",          
                TargetAmount  = 1200m,
                CurrentAmount = 280m,
                Deadline      = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt     = DateTime.UtcNow
            }
        );

        //  Trending items 
        db.TrendingItems.AddRange(
            // Stocks popular among Uzbek investors
            new TrendingItem { Id = Guid.NewGuid(), Symbol = "AMZN", Price = 185.4m,  ChangePercent = 2.5m,  Category = "stock",  ChartData = [10, 12, 11, 14, 13, 15, 16] },
            new TrendingItem { Id = Guid.NewGuid(), Symbol = "GOOGL", Price = 175.2m, ChangePercent = -1.2m, Category = "stock",  ChartData = [15, 14, 13, 14, 12, 11, 11] },
            new TrendingItem { Id = Guid.NewGuid(), Symbol = "MSFT",  Price = 420.5m, ChangePercent = 1.4m,  Category = "stock",  ChartData = [20, 21, 22, 21, 23, 24, 25] },
            new TrendingItem { Id = Guid.NewGuid(), Symbol = "META",  Price = 512.3m, ChangePercent = 3.3m,  Category = "stock",  ChartData = [30, 32, 31, 34, 35, 37, 38] },
            // Crypto popular in Uzbekistan
            new TrendingItem { Id = Guid.NewGuid(), Symbol = "BTC",  Price = 64230m,  ChangePercent = 3.2m,  Category = "crypto", ChartData = [58, 60, 59, 62, 61, 63, 64] },
            new TrendingItem { Id = Guid.NewGuid(), Symbol = "ETH",  Price = 3450m,   ChangePercent = -2.1m, Category = "crypto", ChartData = [36, 35, 34, 33, 34, 35, 34] },
            new TrendingItem { Id = Guid.NewGuid(), Symbol = "SOL",  Price = 145.8m,  ChangePercent = 8.4m,  Category = "crypto", ChartData = [120, 125, 128, 132, 138, 142, 145] },
            new TrendingItem { Id = Guid.NewGuid(), Symbol = "TON",  Price = 5.85m,   ChangePercent = 15.3m, Category = "crypto", ChartData = [3, 4, 3.5m, 5, 5.2m, 5.6m, 5.85m] }
        );

        db.SaveChanges();
    }
}

app.Run();
