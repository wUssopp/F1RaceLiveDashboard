namespace F1RaceLiveDashboard.Models
{
    public class RaceEvent
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = "";
        public string Type { get; set; } = "";
        public int SimulationSecond { get; set; }
    }
}