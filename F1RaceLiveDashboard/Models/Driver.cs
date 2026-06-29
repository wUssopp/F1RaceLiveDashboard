namespace F1RaceLiveDashboard.Models
{
  // ten model trzyma stan kierowcy podczas dzialania symulacji
  public class Driver
  {
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string Team { get; set; } = "";

    // kopia informacji z bazy, uzywana do lepszego lub slabszego tempa
    public bool IsTopTeam { get; set; }

    public int Position { get; set; }

    public int StartPosition { get; set; }

    // roznica miedzy pozycja startowa a obecna
    public int PositionChange { get; set; }

    public int CurrentLap { get; set; }

    public double LastLapTime { get; set; }

    public double BestLapTime { get; set; }

    // laczny czas kierowcy, uzywany do klasyfikacji
    public double TotalTime { get; set; }

    // docelowy czas trwania aktualnego okrazenia
    public double CurrentLapTargetTime { get; set; }

    // ile sekund kierowca przejechal juz w aktualnym okrazeniu
    public double CurrentLapProgressSeconds { get; set; }

    // procent ukonczenia biezacego okrazenia do paska postepu
    public double LapProgressPercent { get; set; }

    public string Status { get; set; } = "Ready";

    public bool IsOut { get; set; }

    // liczba pit stopow wykonanych przez kierowce
    public int PitStopCount { get; set; }

    // zaplanowane okrazenie kolejnego pit stopu
    public int NextPitLap { get; set; }
  }
}