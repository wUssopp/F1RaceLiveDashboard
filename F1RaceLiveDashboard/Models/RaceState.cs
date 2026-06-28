namespace F1RaceLiveDashboard.Models
{
    public class RaceState
    {
        public string RaceName { get; set; } = "F1 Grand Prix Simulation";
        public int CurrentLap { get; set; } = 0;
        public int TotalLaps { get; set; } = 20;
        public string Status { get; set; } = "Not started";
        public int ElapsedSeconds { get; set; } = 0;
        public List<Driver> Drivers { get; set; } = new();
        public List<RaceEvent> Events { get; set; } = new();
    }
}