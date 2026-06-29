namespace F1RaceLiveDashboard.Models
{
  // pojedynczy event wyscigu wyswietlany na dashboardzie
  public class RaceEvent
  {
    public DateTime Timestamp { get; set; }

    public string Message { get; set; } = "";

    // typ eventu pozwala np. rozroznic info, pit, finish albo danger
    public string Type { get; set; } = "";

    // sekunda symulacji, w ktorej event zostal dodany
    public int SimulationSecond { get; set; }
  }
}