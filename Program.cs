using Rapdle.Api.Controllers;
using Rapdle.Api.Services;
using Rapdle.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddApplicationPart(typeof(SongController).Assembly)
    .AddControllersAsServices();

builder.Services.AddHttpClient<DeezerService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=rapdle.db"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        p => p.WithOrigins("http://localhost:5173", "http://localhost:3000", "https://rapdle.online")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Auto-migrate database and seed test data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend"); 

app.MapControllers();

app.Run();
