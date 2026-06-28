namespace F1RaceLiveDashboard.Entities
{
    public class TeamEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsTopTeam { get; set; }

        public List<DriverEntity> Drivers { get; set; } = new();
    }
}