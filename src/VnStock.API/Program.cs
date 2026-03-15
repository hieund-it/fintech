using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using VnStock.API.Services;
using VnStock.Infrastructure;
using VnStock.Infrastructure.Data;

// Load .env file for local development (NoClobber: existing env vars take precedence)
DotNetEnv.Env.NoClobber().TraversePath().Load();

// Serilog bootstrap logger — captures startup errors before host is built
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog: read full config from appsettings.json [Serilog] section
    builder.Host.UseSerilog((ctx, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration));

    builder.Services.AddControllers();
    builder.Services.AddResponseCaching();
    builder.Services.AddProblemDetails(); // RFC 7807 ProblemDetails factory
    builder.Services.AddInfrastructure(builder.Configuration);

    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret not configured");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // preserve "sub" claim name; prevents NullRef in UserId getter
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

    builder.Services.AddAuthorization();

    var corsOrigins = (builder.Configuration["Cors:Origins"] ?? "http://localhost:5173")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ReactDev", policy =>
            policy.WithOrigins(corsOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()); // required for SignalR WebSocket
    });

    // SignalR with Redis backplane for horizontal scale
    var redisConn = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
    builder.Services.AddSignalR().AddStackExchangeRedis(redisConn);
    builder.Services.AddHostedService<RedisMarketDataSubscriber>();
    builder.Services.AddHostedService<AlertEngineService>();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "VnStock API", Version = "v1" });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                []
            }
        });
    });

    var app = builder.Build();

    // Apply EF Core migrations and seed reference data on startup (async to avoid thread pool starvation)
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<VnStock.Infrastructure.Data.AppDbContext>();
        await db.Database.MigrateAsync();

        // Seed stock reference data if table is empty; catch duplicate-key from concurrent startup race
        if (!await db.Stocks.AnyAsync())
        {
            db.Stocks.AddRange(
                new VnStock.Domain.Entities.Stock { Symbol = "VCB", Name = "Vietcombank", Exchange = "HOSE", Sector = "Banking" },
                new VnStock.Domain.Entities.Stock { Symbol = "VIC", Name = "Vingroup", Exchange = "HOSE", Sector = "Real Estate" },
                new VnStock.Domain.Entities.Stock { Symbol = "VHM", Name = "Vinhomes", Exchange = "HOSE", Sector = "Real Estate" },
                new VnStock.Domain.Entities.Stock { Symbol = "HPG", Name = "Hoa Phat Group", Exchange = "HOSE", Sector = "Steel" },
                new VnStock.Domain.Entities.Stock { Symbol = "BID", Name = "BIDV", Exchange = "HOSE", Sector = "Banking" },
                new VnStock.Domain.Entities.Stock { Symbol = "CTG", Name = "VietinBank", Exchange = "HOSE", Sector = "Banking" },
                new VnStock.Domain.Entities.Stock { Symbol = "MBB", Name = "MB Bank", Exchange = "HOSE", Sector = "Banking" },
                new VnStock.Domain.Entities.Stock { Symbol = "TCB", Name = "Techcombank", Exchange = "HOSE", Sector = "Banking" },
                new VnStock.Domain.Entities.Stock { Symbol = "ACB", Name = "ACB Bank", Exchange = "HOSE", Sector = "Banking" },
                new VnStock.Domain.Entities.Stock { Symbol = "VPB", Name = "VPBank", Exchange = "HOSE", Sector = "Banking" },
                new VnStock.Domain.Entities.Stock { Symbol = "FPT", Name = "FPT Corporation", Exchange = "HOSE", Sector = "Technology" },
                new VnStock.Domain.Entities.Stock { Symbol = "MWG", Name = "Mobile World", Exchange = "HOSE", Sector = "Retail" },
                new VnStock.Domain.Entities.Stock { Symbol = "GAS", Name = "PV Gas", Exchange = "HOSE", Sector = "Oil & Gas" },
                new VnStock.Domain.Entities.Stock { Symbol = "SAB", Name = "Sabeco", Exchange = "HOSE", Sector = "Consumer Goods" },
                new VnStock.Domain.Entities.Stock { Symbol = "PLX", Name = "Petrolimex", Exchange = "HOSE", Sector = "Oil & Gas" }
            );
            try { await db.SaveChangesAsync(); }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                // Another instance seeded concurrently — duplicate key is expected and safe to ignore
            }
        }
    }

    // Global exception handler: returns RFC 7807 ProblemDetails on unhandled exceptions
    app.UseExceptionHandler(errApp =>
    {
        errApp.Run(async ctx =>
        {
            var feature = ctx.Features.Get<IExceptionHandlerFeature>();
            var ex = feature?.Error;

            var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                ctx.Request.Method, ctx.Request.Path);

            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            ctx.Response.ContentType = "application/problem+json";

            var pd = new ProblemDetails
            {
                Status = 500,
                Title = "An unexpected error occurred.",
                Detail = app.Environment.IsDevelopment() ? ex?.Message : null,
                Instance = ctx.Request.Path
            };

            await ctx.Response.WriteAsJsonAsync(pd);
        });
    });

    // Restrict Swagger to development — prevents leaking full API surface in production
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    app.UseSerilogRequestLogging(); // structured HTTP request logs
    app.UseCors("ReactDev");
    app.UseResponseCaching();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<VnStock.API.Hubs.MarketHub>("/hubs/market");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed.");
}
finally
{
    Log.CloseAndFlush();
}
