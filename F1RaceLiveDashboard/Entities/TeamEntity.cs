namespace F1RaceLiveDashboard.Entities
{
  // encja zespolu zapisywana w bazie danych
  public class TeamEntity
  {
    public int Id { get; set; }

    public string Name { get; set; } = "";

    // okresla czy zespol ma lepsze tempo w symulacji
    public bool IsTopTeam { get; set; }

    // relacja ef core: jeden zespol ma wielu kierowcow
    public List<DriverEntity> Drivers { get; set; } = new();
  }
}