namespace F1RaceLiveDashboard.Entities
{
  // encja kierowcy zapisywana w bazie danych
  public class DriverEntity
  {
    public int Id { get; set; }

    public string Name { get; set; } = "";

    // klucz obcy do zespolu w tabeli teams
    public int TeamEntityId { get; set; }

    // nawigacja ef core do powiazanego zespolu
    public TeamEntity? Team { get; set; }

    // pozycja startowa uzywana potem w symulacji
    public int StartPosition { get; set; }
  }
}