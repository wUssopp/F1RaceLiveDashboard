namespace F1RaceLiveDashboard.Models
{
    public class Driver
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string Team { get; set; } = "";

        public int Position { get; set; }

        public int StartPosition { get; set; }

        public int PositionChange { get; set; }

        public int CurrentLap { get; set; }

        public double LastLapTime { get; set; }

        public double BestLapTime { get; set; }

        public double TotalTime { get; set; }

        public double CurrentLapTargetTime { get; set; }

        public double CurrentLapProgressSeconds { get; set; }

        public double LapProgressPercent { get; set; }

        public string Status { get; set; } = "Ready";

        public bool IsOut { get; set; }
        public int PitStopCount { get; set; }
        public int NextPitLap { get; set; }
    }
}