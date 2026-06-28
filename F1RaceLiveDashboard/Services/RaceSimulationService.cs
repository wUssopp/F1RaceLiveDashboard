using F1RaceLiveDashboard.Hubs;
using F1RaceLiveDashboard.Models;
using Microsoft.AspNetCore.SignalR;

namespace F1RaceLiveDashboard.Services
{
    public class RaceSimulationService
    {
        private readonly object _raceLock = new object();
        private RaceState _raceState;
        private readonly IHubContext<RaceHub> _hubContext;
        private readonly Random _random = new Random();

        private bool _isSimulationRunning = false;
        private bool _isPaused = false;
        private double _simulationSpeedMultiplier = 1.0;

        private const double TickSeconds = 0.2;
        private const double MinLapTime = 38.000;
        private const double MaxLapTime = 42.000;

        public RaceSimulationService(IHubContext<RaceHub> hubContext)
        {
            _hubContext = hubContext;
            _raceState = CreateInitialRaceState();
        }

        public RaceState GetRaceState()
        {
            lock (_raceLock)
            {
                return _raceState;
            }
        }

        public List<Driver> GetDrivers()
        {
            lock (_raceLock)
            {
                return _raceState.Drivers.ToList();
            }
        }

        public Driver? GetDriverById(int id)
        {
            lock (_raceLock)
            {
                return _raceState.Drivers.FirstOrDefault(d => d.Id == id);
            }
        }

        public async Task StartRaceInBackground()
        {
            bool shouldStartLoop = false;

            lock (_raceLock)
            {
                if (_raceState.Status != "Ready")
                {
                    return;
                }

                _isSimulationRunning = true;
                _isPaused = false;
                _raceState.Status = "Running";
                _raceState.Events.Add(new RaceEvent
                {
                    Timestamp = DateTime.Now,
                    Message = "Race started.",
                    Type = "info"
                });

                foreach (var driver in _raceState.Drivers.Where(d => !d.IsOut))
                {
                    PrepareNextLap(driver);
                    driver.Status = "Running";
                }

                shouldStartLoop = true;
            }

            await BroadcastRaceStateAsync();

            if (shouldStartLoop)
            {
                StartRaceLoop();
            }
        }

        public async Task PauseRaceAsync()
        {
            lock (_raceLock)
            {
                if (!_isSimulationRunning || _isPaused || _raceState.Status == "Finished")
                {
                    return;
                }

                _isPaused = true;
                _raceState.Status = "Paused";
                _raceState.Events.Add(new RaceEvent
                {
                    Timestamp = DateTime.Now,
                    Message = "Race paused.",
                    Type = "info"
                });
            }

            await BroadcastRaceStateAsync();
        }

        public async Task ResumeRaceAsync()
        {
            bool shouldStartLoop = false;

            lock (_raceLock)
            {
                if (!_isSimulationRunning || !_isPaused || _raceState.Status == "Finished")
                {
                    return;
                }

                _isPaused = false;
                _raceState.Status = "Running";
                _raceState.Events.Add(new RaceEvent
                {
                    Timestamp = DateTime.Now,
                    Message = "Race resumed.",
                    Type = "info"
                });

                shouldStartLoop = true;
            }

            await BroadcastRaceStateAsync();

            if (shouldStartLoop)
            {
                StartRaceLoop();
            }
        }

        public void SetSimulationSpeed(double multiplier)
        {
            lock (_raceLock)
            {
                if (multiplier < 0.25)
                {
                    multiplier = 0.25;
                }

                if (multiplier > 8.0)
                {
                    multiplier = 8.0;
                }

                _simulationSpeedMultiplier = multiplier;

                _raceState.Events.Add(new RaceEvent
                {
                    Timestamp = DateTime.Now,
                    Message = $"Simulation speed set to x{_simulationSpeedMultiplier:0.##}.",
                    Type = "info"
                });
            }
        }

        private void StartRaceLoop()
        {
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    bool shouldContinue;

                    lock (_raceLock)
                    {
                        shouldContinue = _isSimulationRunning && !_isPaused && _raceState.Status != "Finished";

                        if (shouldContinue)
                        {
                            AdvanceRaceByTick();
                        }
                    }

                    if (!shouldContinue)
                    {
                        break;
                    }

                    await BroadcastRaceStateAsync();
                    await Task.Delay(TimeSpan.FromMilliseconds((TickSeconds * 1000) / _simulationSpeedMultiplier));
                }
            });
        }

        private void AdvanceRaceByTick()
        {
            _raceState.ElapsedSeconds = (int)Math.Floor(
                _raceState.Drivers
                    .Where(d => !d.IsOut)
                    .Select(d => d.TotalTime + d.CurrentLapProgressSeconds)
                    .DefaultIfEmpty(0)
                    .Min()
            );

            foreach (var driver in _raceState.Drivers)
            {
                if (driver.IsOut || driver.Status == "Finished")
                {
                    continue;
                }

                driver.CurrentLapProgressSeconds += TickSeconds;
                driver.LapProgressPercent = Math.Min(
                    100,
                    (driver.CurrentLapProgressSeconds / driver.CurrentLapTargetTime) * 100
                );

                if (driver.CurrentLapProgressSeconds >= driver.CurrentLapTargetTime)
                {
                    FinishDriverLap(driver);
                }
            }

            UpdatePositions();
            KeepOnlyLatestEvents(30);
        }

        private void FinishDriverLap(Driver driver)
        {
            double completedLapTime = driver.CurrentLapTargetTime;

            driver.CurrentLap++;
            driver.LastLapTime = completedLapTime;
            driver.TotalTime += completedLapTime;
            driver.CurrentLapProgressSeconds = 0;
            driver.LapProgressPercent = 0;

            if (driver.BestLapTime == 0 || completedLapTime < driver.BestLapTime)
            {
                driver.BestLapTime = completedLapTime;
            }

            bool shouldDnfNow = _random.Next(1, 101) <= 3;

            if (shouldDnfNow)
            {
                driver.Status = "Out";
                driver.IsOut = true;
                driver.LapProgressPercent = 0;

                _raceState.Events.Add(new RaceEvent
                {
                    Timestamp = DateTime.Now,
                    Message = $"{driver.Name} is out of the race.",
                    Type = "danger"
                });

                return;
            }

            if (driver.CurrentLap >= _raceState.TotalLaps)
            {
                driver.Status = "Finished";
                driver.LapProgressPercent = 100;

                _raceState.Events.Add(new RaceEvent
                {
                    Timestamp = DateTime.Now,
                    Message = $"{driver.Name} finished the race.",
                    Type = "finish"
                });
            }
            else
            {
                driver.Status = "Running";
                PrepareNextLap(driver);

                if (driver.NextPitLap > 0 && driver.CurrentLap == driver.NextPitLap)
                {
                    driver.Status = "Pit stop";
                    driver.PitStopCount++;

                    double pitStopDelay = 8.5 + (_random.NextDouble() * 3.0);
                    driver.TotalTime += pitStopDelay;
                    driver.CurrentLapTargetTime += pitStopDelay;

                    driver.NextPitLap = driver.CurrentLap + _random.Next(11, 14);

                    _raceState.Events.Add(new RaceEvent
                    {
                        Timestamp = DateTime.Now,
                        Message = $"{driver.Name} entered the pit lane.",
                        Type = "pit"
                    });
                }
                else
                {
                    _raceState.Events.Add(new RaceEvent
                    {
                        Timestamp = DateTime.Now,
                        Message = $"{driver.Name} completed lap {driver.CurrentLap}.",
                        Type = "info"
                    });
                }
            }

            if (_raceState.Drivers.Where(d => !d.IsOut && d.Status == "Finished").Count() == _raceState.Drivers.Where(d => !d.IsOut).Count())
            {
                _raceState.Status = "Finished";
                _isSimulationRunning = false;
                _isPaused = false;

                _raceState.Events.Add(new RaceEvent
                {
                    Timestamp = DateTime.Now,
                    Message = "Race finished.",
                    Type = "finish"
                });
            }
        }

        private void PrepareNextLap(Driver driver)
        {
            driver.CurrentLapTargetTime = GenerateRandomLapTime();
            driver.CurrentLapProgressSeconds = 0;
            driver.LapProgressPercent = 0;

            if (driver.PitStopCount == 0 && driver.NextPitLap == 0)
            {
                driver.NextPitLap = _random.Next(11, 14);
            }
        }

        private double GenerateRandomLapTime()
        {
            return Math.Round(MinLapTime + (_random.NextDouble() * (MaxLapTime - MinLapTime)), 3);
        }

        public async Task ResetRaceAsync()
        {
            lock (_raceLock)
            {
                _isSimulationRunning = false;
                _isPaused = false;
                _raceState = CreateInitialRaceState();
            }

            await BroadcastRaceStateAsync();
        }

        private void UpdatePositions()
        {
            var activeDrivers = _raceState.Drivers
                .Where(d => !d.IsOut)
                .OrderByDescending(d => d.CurrentLap)
                .ThenBy(d => d.TotalTime + d.CurrentLapProgressSeconds)
                .ToList();

            var outDrivers = _raceState.Drivers
                .Where(d => d.IsOut)
                .OrderByDescending(d => d.CurrentLap)
                .ThenBy(d => d.TotalTime)
                .ToList();

            var finalOrder = activeDrivers.Concat(outDrivers).ToList();

            for (int i = 0; i < finalOrder.Count; i++)
            {
                finalOrder[i].Position = i + 1;
                finalOrder[i].PositionChange = finalOrder[i].StartPosition - finalOrder[i].Position;
            }

            _raceState.Drivers = finalOrder;
        }

        private void KeepOnlyLatestEvents(int maxCount)
        {
            if (_raceState.Events.Count > maxCount)
            {
                _raceState.Events = _raceState.Events
                    .OrderByDescending(e => e.Timestamp)
                    .Take(maxCount)
                    .OrderBy(e => e.Timestamp)
                    .ToList();
            }
        }

        public async Task BroadcastRaceStateAsync()
        {
            RaceState stateCopy;

            lock (_raceLock)
            {
                stateCopy = _raceState;
            }

            await _hubContext.Clients.All.SendAsync("RaceStateUpdated", new
            {
                raceName = stateCopy.RaceName,
                currentLap = stateCopy.CurrentLap,
                totalLaps = stateCopy.TotalLaps,
                status = stateCopy.Status,
                elapsedSeconds = stateCopy.ElapsedSeconds,
                drivers = stateCopy.Drivers,
                events = stateCopy.Events
            });
        }

        private RaceState CreateInitialRaceState()
        {
            return new RaceState
            {
                RaceName = "F1 Grand Prix Simulation",
                CurrentLap = 0,
                TotalLaps = 20,
                Status = "Ready",
                ElapsedSeconds = 0,
                Drivers = new List<Driver>
                {
                    new Driver { Id = 1, Name = "Max Verstappen", Team = "Red Bull", Position = 1, StartPosition = 1, PositionChange = 0, Status = "Ready", IsOut = false },
                    new Driver { Id = 2, Name = "Charles Leclerc", Team = "Ferrari", Position = 2, StartPosition = 2, PositionChange = 0, Status = "Ready", IsOut = false },
                    new Driver { Id = 3, Name = "Lando Norris", Team = "McLaren", Position = 3, StartPosition = 3, PositionChange = 0, Status = "Ready", IsOut = false },
                    new Driver { Id = 4, Name = "Lewis Hamilton", Team = "Mercedes", Position = 4, StartPosition = 4, PositionChange = 0, Status = "Ready", IsOut = false }
                },
                Events = new List<RaceEvent>
                {
                    new RaceEvent
                    {
                        Timestamp = DateTime.Now,
                        Message = "Race state initialized.",
                        Type = "info"
                    },
                    new RaceEvent
                    {
                        Timestamp = DateTime.Now,
                        Message = "Drivers loaded into memory.",
                        Type = "info"
                    }
                }
            };
        }
    }
}