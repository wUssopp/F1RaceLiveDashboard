using F1RaceLiveDashboard.Entities;
using Microsoft.EntityFrameworkCore;

namespace F1RaceLiveDashboard.Data
{
  // dbcontext opisuje tabele i relacje w bazie danych
  public class AppDbContext : DbContext
  {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // tabela zespolow w bazie
    public DbSet<TeamEntity> Teams => Set<TeamEntity>();

    // tabela kierowcow w bazie
    public DbSet<DriverEntity> Drivers => Set<DriverEntity>();

    // konfiguracja relacji ef core miedzy encjami
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      modelBuilder.Entity<TeamEntity>()
          .HasMany(t => t.Drivers) // jeden zespol ma wielu kierowcow
          .WithOne(d => d.Team) // jeden kierowca nalezy do jednego zespolu
          .HasForeignKey(d => d.TeamEntityId); // klucz obcy w tabeli drivers
    }
  }
}