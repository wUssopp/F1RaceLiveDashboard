using F1RaceLiveDashboard.Data; // appdbcontext do bazy sqlite
using F1RaceLiveDashboard.Hubs; // hub signalr do realtime
using F1RaceLiveDashboard.Services; // serwis z logika wyscigu
using Microsoft.EntityFrameworkCore; // ef core i migracje

var builder = WebApplication.CreateBuilder(args); // tworzy builder aplikacji

// services
builder.Services.AddDbContextFactory<AppDbContext>(options => // rejestruje fabryke dbcontext
    options.UseSqlite("Data Source=f1race.db")); // ustawia polaczenie z plikiem sqlite

builder.Services.AddSingleton<RaceSimulationService>(); // jeden wspolny stan wyscigu dla wszystkich
builder.Services.AddSignalR(); // wlacza signalr, wymaganie realtime
builder.Services.AddControllersWithViews(); // wlacza mvc: kontrolery + widoki

var app = builder.Build(); // buduje aplikacje z tej konfiguracji

// database initialization
using (var scope = app.Services.CreateScope()) // tworzy scope do pobrania serwisow z di
{
  var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>(); // pobiera fabryke dbcontext
  using var dbContext = dbContextFactory.CreateDbContext(); // tworzy instancje dbcontext

  dbContext.Database.Migrate(); // wykonuje migracje bazy przy starcie
  DbSeeder.Seed(dbContext); // dodaje dane startowe do bazy
}

// http pipeline
if (!app.Environment.IsDevelopment()) // sprawdza czy aplikacja nie dziala w dev
{
  app.UseHsts(); // zwieksza bezpieczenstwo https poza dev
}

app.UseHttpsRedirection(); // przekierowuje z http na https
app.UseRouting(); // wlacza routing adresow
app.UseAuthorization(); // wlacza middleware autoryzacji

app.MapStaticAssets(); // udostepnia pliki statyczne jak css i js

app.MapControllerRoute( // ustawia domyslna trase mvc
    name: "default",
    pattern: "{controller=Race}/{action=Index}/{id?}") // start aplikacji otwiera race/index
    .WithStaticAssets(); // laczy trase z obsluga statycznych plikow

app.MapHub<RaceHub>("/raceHub"); // endpoint signalr, frontend laczy sie tutaj

app.Run(); // uruchamia aplikacje