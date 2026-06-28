using F1RaceLiveDashboard.Entities;

namespace F1RaceLiveDashboard.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext context)
        {
            if (!context.Teams.Any())
            {
                var teams = new List<TeamEntity>
                {
                    new TeamEntity { Name = "Ferrari", IsTopTeam = true },
                    new TeamEntity { Name = "Mercedes", IsTopTeam = true },
                    new TeamEntity { Name = "Red Bull Racing", IsTopTeam = true },
                    new TeamEntity { Name = "McLaren", IsTopTeam = true },

                    new TeamEntity { Name = "Alpine", IsTopTeam = false },
                    new TeamEntity { Name = "Racing Bulls", IsTopTeam = false },
                    new TeamEntity { Name = "Haas F1 Team", IsTopTeam = false },
                    new TeamEntity { Name = "Williams", IsTopTeam = false },
                    new TeamEntity { Name = "Audi", IsTopTeam = false },
                    new TeamEntity { Name = "Aston Martin", IsTopTeam = false },
                    new TeamEntity { Name = "Cadillac", IsTopTeam = false }
                };

                context.Teams.AddRange(teams);
                context.SaveChanges();
            }

            if (!context.Drivers.Any())
            {
                var mercedes = context.Teams.First(t => t.Name == "Mercedes");
                var ferrari = context.Teams.First(t => t.Name == "Ferrari");
                var mclaren = context.Teams.First(t => t.Name == "McLaren");
                var redBull = context.Teams.First(t => t.Name == "Red Bull Racing");
                var alpine = context.Teams.First(t => t.Name == "Alpine");
                var racingBulls = context.Teams.First(t => t.Name == "Racing Bulls");
                var haas = context.Teams.First(t => t.Name == "Haas F1 Team");
                var williams = context.Teams.First(t => t.Name == "Williams");
                var audi = context.Teams.First(t => t.Name == "Audi");
                var astonMartin = context.Teams.First(t => t.Name == "Aston Martin");
                var cadillac = context.Teams.First(t => t.Name == "Cadillac");

                var drivers = new List<DriverEntity>
                {
                    new DriverEntity { Name = "George Russell", TeamEntityId = mercedes.Id, StartPosition = 1 },
                    new DriverEntity { Name = "Kimi Antonelli", TeamEntityId = mercedes.Id, StartPosition = 2 },

                    new DriverEntity { Name = "Charles Leclerc", TeamEntityId = ferrari.Id, StartPosition = 3 },
                    new DriverEntity { Name = "Lewis Hamilton", TeamEntityId = ferrari.Id, StartPosition = 4 },

                    new DriverEntity { Name = "Lando Norris", TeamEntityId = mclaren.Id, StartPosition = 5 },
                    new DriverEntity { Name = "Oscar Piastri", TeamEntityId = mclaren.Id, StartPosition = 6 },

                    new DriverEntity { Name = "Max Verstappen", TeamEntityId = redBull.Id, StartPosition = 7 },
                    new DriverEntity { Name = "Isack Hadjar", TeamEntityId = redBull.Id, StartPosition = 8 },

                    new DriverEntity { Name = "Pierre Gasly", TeamEntityId = alpine.Id, StartPosition = 9 },
                    new DriverEntity { Name = "Franco Colapinto", TeamEntityId = alpine.Id, StartPosition = 10 },

                    new DriverEntity { Name = "Liam Lawson", TeamEntityId = racingBulls.Id, StartPosition = 11 },
                    new DriverEntity { Name = "Arvid Lindblad", TeamEntityId = racingBulls.Id, StartPosition = 12 },

                    new DriverEntity { Name = "Esteban Ocon", TeamEntityId = haas.Id, StartPosition = 13 },
                    new DriverEntity { Name = "Oliver Bearman", TeamEntityId = haas.Id, StartPosition = 14 },

                    new DriverEntity { Name = "Carlos Sainz", TeamEntityId = williams.Id, StartPosition = 15 },
                    new DriverEntity { Name = "Alexander Albon", TeamEntityId = williams.Id, StartPosition = 16 },

                    new DriverEntity { Name = "Nico Hulkenberg", TeamEntityId = audi.Id, StartPosition = 17 },
                    new DriverEntity { Name = "Gabriel Bortoleto", TeamEntityId = audi.Id, StartPosition = 18 },

                    new DriverEntity { Name = "Fernando Alonso", TeamEntityId = astonMartin.Id, StartPosition = 19 },
                    new DriverEntity { Name = "Lance Stroll", TeamEntityId = astonMartin.Id, StartPosition = 20 },

                    new DriverEntity { Name = "Sergio Perez", TeamEntityId = cadillac.Id, StartPosition = 21 },
                    new DriverEntity { Name = "Valtteri Bottas", TeamEntityId = cadillac.Id, StartPosition = 22 }
                };

                context.Drivers.AddRange(drivers);
                context.SaveChanges();
            }
        }
    }
}