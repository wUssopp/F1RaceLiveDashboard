namespace F1RaceLiveDashboard.Models
{
  // ten model przechowuje pelny stan aktualnej symulacji
  public class RaceState
  {
    public string RaceName { get; set; } = "F1 Grand Prix Simulation";

    // numer aktualnego okrazenia wyscigu
    public int CurrentLap { get; set; } = 0;

    public int TotalLaps { get; set; } = 20;

    // status wyscigu, np. ready, running, paused albo finished
    public string Status { get; set; } = "";

    // czas symulacji liczony w sekundach
    public int ElapsedSeconds { get; set; } = 0;

    // lista wszystkich kierowcow z ich biezacym stanem
    public List<Driver> Drivers { get; set; } = new();

    // log zdarzen wyscigu wyswietlany na dashboardzie
    public List<RaceEvent> Events { get; set; } = new();
  }
}