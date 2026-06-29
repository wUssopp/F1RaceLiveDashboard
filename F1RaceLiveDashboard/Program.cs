using F1RaceLiveDashboard.Data;
using F1RaceLiveDashboard.Hubs;
using F1RaceLiveDashboard.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite("Data Source=f1race.db"));

builder.Services.AddSingleton<RaceSimulationService>();
builder.Services.AddSignalR();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var dbContext = dbContextFactory.CreateDbContext();

    dbContext.Database.Migrate();
    DbSeeder.Seed(dbContext);
}

// HTTP pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Race}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<RaceHub>("/raceHub");

app.Run();