using System.Text;
using CapShop.AuthService.Application.Interfaces;
using CapShop.AuthService.Application.Services;
using CapShop.AuthService.Data;
using CapShop.AuthService.Infrastructure.Repositories;
using CapShop.AuthService.Middleware;
using CapShop.AuthService.Services;
using CapShop.AuthService.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(); // add controllers to the service collection so that we can use attribute routing and model binding in our controllers
builder.Services.AddEndpointsApiExplorer(); //api documentation for swagger

//configuring swagger to include security definitions for JWT authentication. This allows us to test our secured endpoints directly from the Swagger UI by providing a valid JWT token in the Authorization header. We define a security scheme named "Bearer" that specifies how the token should be included in requests, and we also add a security requirement to ensure that this scheme is applied to all API operations.
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

//configuring db context with ms sql server and connection string from appsettings.json
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



//dependency injection for services and repositories -> add scoped for per-request lifetime (we can also use transient but it is fight and forget while singleton is shared across entire app so user b can see what user a is doing  )
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthAppService, AuthAppService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IAuthenticatorService, AuthenticatorService>();


//configuring jwt authentication -> we need to get the secret key from appsettings.json and validate it before configuring the authentication middleware, if the secret key is missing or empty we throw an exception to prevent the app from starting with invalid configuration. We also set up token validation parameters to ensure that incoming JWTs are properly validated against our expected issuer, audience, signing key, and lifetime. Additionally, we set a short clock skew to minimize the window for token expiration issues.


var secret = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrWhiteSpace(secret))
{
    throw new InvalidOperationException("JwtSettings:SecretKey is missing.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

//we setup if the app is in development environment to use swagger for api documentation and testing.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//we add our custom global exception handling middleware to the pipeline before any other middleware to ensure that all exceptions are caught and handled consistently.This middleware will catch specific exceptions like unauthorzed access and invalid operations and return appropriate status codes and messages, while also logging unhandled exceptions with a trace identifier for easier debugging.
app.UseMiddleware<GlobalExceptionMiddleware>();
//we enforce HTTPS redirection to ensure that all communication with the API is secure. We also add authentication and authorization middleware to protect our endpoints and ensure that only authenticated users can access them.
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

//we map our controllers to the request pipeline so that incoming http requests can be routed to the appropriate controller actions based on the defined routes. This allows our API to handle requests and return responses according to the logic implemented in our controllers.
app.MapControllers();


//before running the app we create a scope to access our db context and logger. We then apply any pending migrations to ensure that our database schema is up to date with our entity models. After that, we execute raw SQL commands to add new columns to the Users table if they do not already exist, allowing us to extend our user model without losing existing data. Finally, we call a seeding method to populate the database with initial data if necessary, and log that the startup checks have been completed successfully.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    await db.Database.MigrateAsync();

    await db.Database.ExecuteSqlRawAsync(
            @"
            IF COL_LENGTH('Users', 'AvatarUrl') IS NULL
            BEGIN
                ALTER TABLE [Users] ADD [AvatarUrl] nvarchar(max) NULL;
            END"
    );

    await db.Database.ExecuteSqlRawAsync(
            @"
            IF COL_LENGTH('Users', 'IsGoogleAccount') IS NULL
            BEGIN
                ALTER TABLE [Users] ADD [IsGoogleAccount] bit NOT NULL CONSTRAINT [DF_Users_IsGoogleAccount] DEFAULT(0);
            END"
    );

    await CapShop.AuthService.Data.AuthDbSeeder.SeedAsync(db);
    logger.LogInformation("Auth database startup checks completed.");
}

app.Run();