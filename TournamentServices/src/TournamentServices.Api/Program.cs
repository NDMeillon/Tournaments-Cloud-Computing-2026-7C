using System.Reflection;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore;
using TournamentServices.Api;
using TournamentServices.Api.Delegates;
using TournamentServices.Api.Routes;
using TournamentServices.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var sqliteConnection = builder.Configuration["SqliteConnection"];
builder.Services.AddDbContext<TournamentDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(sqliteConnection))
        options.UseSqlite(sqliteConnection);
    else
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDelegateLayer();

var app = builder.Build();

var runningUnderEfTools = Assembly.GetEntryAssembly()?.GetName().Name == "ef";
if (string.IsNullOrWhiteSpace(sqliteConnection) && !runningUnderEfTools)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TournamentDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Tournament Services");
});

app.MapGet("/", (IWebHostEnvironment env) =>
{
    var path = Path.Combine(env.WebRootPath, "index.html");
    return Results.Content(File.ReadAllText(path), "text/html; charset=utf-8");
});
app.MapGet("/health", () => Results.Text("Services running"));
app.MapTournamentRoutes();
app.MapTeamRoutes();
app.MapGroupRoutes();
app.MapMatchRoutes();

app.Run();

public partial class Program { }
