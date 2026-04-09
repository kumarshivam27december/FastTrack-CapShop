using CapShop.OrderService.Data;
using CapShop.OrderService.Application.Interfaces;
using CapShop.OrderService.Application.Services;
using CapShop.OrderService.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using CapShop.OrderService.Middleware;
using CapShop.OrderService.Sagas;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer <your_token>"
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

builder.Services.AddDbContext<OrderDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderAppService, OrderAppService>();

builder.Services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("order", false));
    x.AddSagaStateMachine<OrderSagaStateMachine, OrderSagaState>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<OrderDbContext>();
            r.UseSqlServer();
        });

    x.AddConfigureEndpointsCallback((context, _, cfg) =>
    {
        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(2)));
        cfg.UseInMemoryOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});

var secret = builder.Configuration.GetSection("JwtSettings")["SecretKey"];
if (string.IsNullOrWhiteSpace(secret))
{
    throw new InvalidOperationException("JwtSettings:SecretKey is missing in configuration.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = "CapShop.AuthService",
            ValidAudience = "CapShop.Client",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();


app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await db.Database.MigrateAsync();
    await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[OrderSagaStates]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OrderSagaStates]
    (
        [CorrelationId] uniqueidentifier NOT NULL,
        [CurrentState] nvarchar(64) NULL,
        [OrderId] int NOT NULL,
        [UserId] int NOT NULL,
        [UserEmail] nvarchar(256) NOT NULL,
        [OrderNumber] nvarchar(50) NOT NULL,
        [TotalAmount] decimal(12,2) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_OrderSagaStates] PRIMARY KEY ([CorrelationId])
    );
END;

IF OBJECT_ID(N'[dbo].[OrderSagaStates]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'[dbo].[OrderSagaStates]')
          AND name = N'CurrentState'
          AND is_nullable = 0)
    BEGIN
        ALTER TABLE [dbo].[OrderSagaStates] ALTER COLUMN [CurrentState] nvarchar(64) NULL;
    END;

    DELETE FROM [dbo].[OrderSagaStates]
    WHERE [CurrentState] IS NOT NULL AND LTRIM(RTRIM([CurrentState])) = N'';
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderSagaStates_CurrentState' AND object_id = OBJECT_ID(N'[dbo].[OrderSagaStates]'))
BEGIN
    CREATE INDEX [IX_OrderSagaStates_CurrentState] ON [dbo].[OrderSagaStates] ([CurrentState]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderSagaStates_OrderId' AND object_id = OBJECT_ID(N'[dbo].[OrderSagaStates]'))
BEGIN
    CREATE INDEX [IX_OrderSagaStates_OrderId] ON [dbo].[OrderSagaStates] ([OrderId]);
END;
");
    await CapShop.OrderService.Data.OrderDbSeeder.SeedAsync(db);
}

app.Run();