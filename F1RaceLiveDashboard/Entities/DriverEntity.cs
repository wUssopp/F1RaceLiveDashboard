namespace F1RaceLiveDashboard.Entities
{
    public class DriverEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        public int TeamEntityId { get; set; }
        public TeamEntity? Team { get; set; }

        public int StartPosition { get; set; }
    }
}