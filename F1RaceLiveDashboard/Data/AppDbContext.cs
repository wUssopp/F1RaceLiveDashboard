using F1RaceLiveDashboard.Entities;
using Microsoft.EntityFrameworkCore;

namespace F1RaceLiveDashboard.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<TeamEntity> Teams => Set<TeamEntity>();
        public DbSet<DriverEntity> Drivers => Set<DriverEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TeamEntity>()
                .HasMany(t => t.Drivers)
                .WithOne(d => d.Team)
                .HasForeignKey(d => d.TeamEntityId);
        }
    }
}