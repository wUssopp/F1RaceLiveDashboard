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


        [HttpPost]
        public async Task<IActionResult> Start()
        {
            await _raceSimulationService.StartRaceInBackground();
            return Ok(new { message = "Race started" });
        }

        [HttpPost]
        public async Task<IActionResult> Pause()
        {
            await _raceSimulationService.PauseRaceAsync();
            return Ok(new { message = "Race paused" });
        }

        [HttpPost]
        public async Task<IActionResult> Resume()
        {
            await _raceSimulationService.ResumeRaceAsync();
            return Ok(new { message = "Race resumed" });
        }

        [HttpPost]
        public async Task<IActionResult> Reset()
        {
            await _raceSimulationService.ResetRaceAsync();
            return Ok(new { message = "Race reset" });
        }

        [HttpPost]
        public IActionResult SetSpeed(double multiplier)
        {
            _raceSimulationService.SetSimulationSpeed(multiplier);
            return Ok(new { message = $"Simulation speed set to x{multiplier}" });
        }
    }
}