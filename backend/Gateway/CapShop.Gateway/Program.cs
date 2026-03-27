using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
.SetBasePath(builder.Environment.ContentRootPath)
.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
.AddEnvironmentVariables();

builder.Services.AddCors(options =>
{
        options.AddPolicy("FrontendPolicy", policy =>
        {
                policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod();
        });
});

builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.UseCors("FrontendPolicy");

if (!app.Environment.IsDevelopment())
{
        app.UseHttpsRedirection();
}

await app.UseOcelot();

app.Run();