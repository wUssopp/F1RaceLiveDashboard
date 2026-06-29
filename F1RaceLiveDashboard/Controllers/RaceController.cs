using F1RaceLiveDashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1RaceLiveDashboard.Controllers
{
  public class RaceController : Controller
  {
    private readonly RaceSimulationService _raceSimulationService;

    public RaceController(RaceSimulationService raceSimulationService)
    {
      _raceSimulationService = raceSimulationService;
    }

    public IActionResult Index()
    {
      return View();
    }

    // zwraca pelny stan wyscigu do frontendu jako json
    // to jest glowny endpoint pod ajax/fetch
    [HttpGet]
    public IActionResult Drivers()
    {
      var raceState = _raceSimulationService.GetRaceState();

      return Json(new
      {
        raceName = raceState.RaceName,
        currentLap = raceState.CurrentLap,
        totalLaps = raceState.TotalLaps,
        status = raceState.Status,
        elapsedSeconds = raceState.ElapsedSeconds,
        drivers = raceState.Drivers,
        events = raceState.Events
      });
    }

    // zwraca dane jednego kierowcy po id
    // jesli kierowcy nie ma, zwraca 404
    [HttpGet]
    public IActionResult DriverDetails(int id)
    {
      var driver = _raceSimulationService.GetDriverById(id);

      if (driver == null)
      {
        return NotFound(new { message = "Driver not found" });
      }

      return Json(driver);
    }

    // uruchamia symulacje wyscigu w serwisie
    [HttpPost]
    public async Task<IActionResult> Start()
    {
      await _raceSimulationService.StartRaceInBackground();
      return Message("Race started");
    }

    // zatrzymuje symulacje
    [HttpPost]
    public async Task<IActionResult> Pause()
    {
      await _raceSimulationService.PauseRaceAsync();
      return Message("Race paused");
    }

    // wznawia zatrzymana symulacje
    [HttpPost]
    public async Task<IActionResult> Resume()
    {
      await _raceSimulationService.ResumeRaceAsync();
      return Message("Race resumed");
    }

    // przywraca stan poczatkowy wyscigu
    [HttpPost]
    public async Task<IActionResult> Reset()
    {
      await _raceSimulationService.ResetRaceAsync();
      return Message("Race reset");
    }

    // zmienia mnoznik szybkosci symulacji
    [HttpPost]
    public IActionResult SetSpeed(double multiplier)
    {
      _raceSimulationService.SetSimulationSpeed(multiplier);
      return Message($"Simulation speed set to x{multiplier}");
    }

    // zwieksza liczbe okrazen przed startem wyscigu
    [HttpPost]
    public async Task<IActionResult> IncreaseLaps()
    {
      await _raceSimulationService.ChangeTotalLapsAsync(1);
      return Message("Lap count increased");
    }

    // zmniejsza liczbe okrazen przed startem wyscigu
    [HttpPost]
    public async Task<IActionResult> DecreaseLaps()
    {
      await _raceSimulationService.ChangeTotalLapsAsync(-1);
      return Message("Lap count decreased");
    }

    // pomocnicza metoda do zwracania prostych komunikatow json
    private OkObjectResult Message(string message)
    {
      return Ok(new { message });
    }
  }
}