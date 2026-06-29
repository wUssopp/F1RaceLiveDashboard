using F1RaceLiveDashboard.Data;
using F1RaceLiveDashboard.Hubs;
using F1RaceLiveDashboard.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace F1RaceLiveDashboard.Services
{
    public class RaceSimulationService
    {
        private const double TickSeconds = 0.2;

        private const string StatusReady = "Ready";
        private const string StatusRunning = "Running";
        private const string StatusPaused = "Paused";
        private const string StatusFinished = "Finished";
        private const string StatusOut = "Out";
        private const string StatusPitStop = "Pit stop";

        private const string EventInfo = "info";
        private const string EventDanger = "danger";
        private const string EventFinish = "finish";
        private const string EventPit = "pit";

        private readonly object _raceLock = new();
        private readonly IHubContext<RaceHub> _hubContext;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly Random _random = new();

        private RaceState _raceState;
        private bool _isSimulationRunning = false;
        private bool _isPaused = false;
        private double _simulationSpeedMultiplier = 1.0;

        public RaceSimulationService(
            IHubContext<RaceHub> hubContext,
            IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _hubContext = hubContext;
            _dbContextFactory = dbContextFactory;
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
            var shouldStartLoop = false;

            lock (_raceLock)
            {
                if (!CanStartRace())
                {
                    return;
                }

                _isSimulationRunning = true;
                _isPaused = false;
                _raceState.Status = StatusRunning;

                AddRaceEvent("Race started.", EventInfo);
                InitializeDriversForRace();

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
                if (!CanPauseRace())
                {
                    return;
                }

                _isPaused = true;
                _raceState.Status = StatusPaused;
                AddRaceEvent("Race paused.", EventInfo);
            }

            await BroadcastRaceStateAsync();
        }

        public async Task ResumeRaceAsync()
        {
            var shouldStartLoop = false;

            lock (_raceLock)
            {
                if (!CanResumeRace())
                {
                    return;
                }

                _isPaused = false;
                _raceState.Status = StatusRunning;
                AddRaceEvent("Race resumed.", EventInfo);

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
                _simulationSpeedMultiplier = ClampSpeed(multiplier);
                AddRaceEvent($"Simulation speed set to x{_simulationSpeedMultiplier:0.##}.", EventInfo);
            }
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

        public async Task ChangeTotalLapsAsync(int delta)
        {
            lock (_raceLock)
            {
                if (_raceState.Status != StatusReady)
                {
                    return;
                }

                _raceState.TotalLaps += delta;

                if (_raceState.TotalLaps < 1)
                {
                    _raceState.TotalLaps = 1;
                }
            }

            await BroadcastRaceStateAsync();
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

        private bool CanStartRace()
        {
            return _raceState.Status == StatusReady;
        }

        private bool CanPauseRace()
        {
            return _isSimulationRunning
                && !_isPaused
                && _raceState.Status != StatusFinished;
        }

        private bool CanResumeRace()
        {
            return _isSimulationRunning
                && _isPaused
                && _raceState.Status != StatusFinished;
        }

        private double ClampSpeed(double multiplier)
        {
            if (multiplier < 0.25)
            {
                return 0.25;
            }

            if (multiplier > 8.0)
            {
                return 8.0;
            }

            return multiplier;
        }

        private void InitializeDriversForRace()
        {
            foreach (var driver in _raceState.Drivers.Where(d => !d.IsOut))
            {
                PrepareNextLap(driver);
                driver.Status = StatusRunning;
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
                        shouldContinue = ShouldContinueSimulation();

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

        private bool ShouldContinueSimulation()
        {
            return _isSimulationRunning
                && !_isPaused
                && _raceState.Status != StatusFinished;
        }

        private void AdvanceRaceByTick()
        {
            _raceState.ElapsedSeconds = (int)Math.Floor(
                _raceState.Drivers
                    .Where(d => !d.IsOut)
                    .Select(d => d.TotalTime + d.CurrentLapProgressSeconds)
                    .DefaultIfEmpty(0)
                    .Min());

            foreach (var driver in _raceState.Drivers)
            {
                AdvanceDriver(driver);
            }

            UpdatePositions();
        }

        private void AdvanceDriver(Driver driver)
        {
            if (driver.IsOut || driver.Status == StatusFinished)
            {
                return;
            }

            driver.CurrentLapProgressSeconds += TickSeconds;
            driver.LapProgressPercent = Math.Min(
                100,
                (driver.CurrentLapProgressSeconds / driver.CurrentLapTargetTime) * 100);

            if (driver.CurrentLapProgressSeconds >= driver.CurrentLapTargetTime)
            {
                FinishDriverLap(driver);
            }
        }

        private void FinishDriverLap(Driver driver)
        {
            var completedLapTime = driver.CurrentLapTargetTime;

            driver.CurrentLap++;
            driver.LastLapTime = completedLapTime;
            driver.TotalTime += completedLapTime;
            driver.CurrentLapProgressSeconds = 0;
            driver.LapProgressPercent = 0;

            if (driver.BestLapTime == 0 || completedLapTime < driver.BestLapTime)
            {
                driver.BestLapTime = completedLapTime;
            }

            if (ShouldDriverRetire())
            {
                HandleDriverRetirement(driver);
                return;
            }

            if (driver.CurrentLap >= _raceState.TotalLaps)
            {
                HandleDriverFinish(driver);
            }
            else
            {
                HandleDriverContinue(driver);
            }

            TryFinishRace();
        }

        private bool ShouldDriverRetire()
        {
            return _random.Next(1, 101) <= 1;
        }

        private void HandleDriverRetirement(Driver driver)
        {
            driver.Status = StatusOut;
            driver.IsOut = true;
            driver.LapProgressPercent = 0;

            AddRaceEvent(
                $"{driver.Name} is out of the race. Total time: {FormatDriverTotalTime(driver.TotalTime)}",
                EventDanger,
                (int)Math.Floor(driver.TotalTime));
        }

        private void HandleDriverFinish(Driver driver)
        {
            driver.Status = StatusFinished;
            driver.LapProgressPercent = 100;

            AddRaceEvent(
                $"{driver.Name} finished the race. Total time: {FormatDriverTotalTime(driver.TotalTime)}",
                EventFinish,
                (int)Math.Floor(driver.TotalTime));
        }

        private void HandleDriverContinue(Driver driver)
        {
            driver.Status = StatusRunning;
            PrepareNextLap(driver);

            if (driver.NextPitLap > 0 && driver.CurrentLap == driver.NextPitLap)
            {
                ApplyPitStop(driver);
                return;
            }

            AddRaceEvent(
                $"{driver.Name} completed lap {driver.CurrentLap} with time: {FormatDriverTotalTime(driver.LastLapTime)}.",
                EventInfo);
        }

        private void ApplyPitStop(Driver driver)
        {
            driver.Status = StatusPitStop;
            driver.PitStopCount++;

            var pitStopDelay = 8.5 + (_random.NextDouble() * 3.0);
            driver.TotalTime += pitStopDelay;
            driver.CurrentLapTargetTime += pitStopDelay;

            driver.NextPitLap = driver.CurrentLap + _random.Next(11, 14);

            AddRaceEvent($"{driver.Name} entered the pit lane.", EventPit);
        }

        private void TryFinishRace()
        {
            var finishedDrivers = _raceState.Drivers.Count(d => !d.IsOut && d.Status == StatusFinished);
            var activeDrivers = _raceState.Drivers.Count(d => !d.IsOut);

            if (finishedDrivers == activeDrivers)
            {
                _raceState.Status = StatusFinished;
                _isSimulationRunning = false;
                _isPaused = false;

                AddRaceEvent("Race finished.", EventFinish);
            }
        }

        private void PrepareNextLap(Driver driver)
        {
            driver.CurrentLapTargetTime = GenerateRandomLapTime(driver);
            driver.CurrentLapProgressSeconds = 0;
            driver.LapProgressPercent = 0;

            if (driver.PitStopCount == 0 && driver.NextPitLap == 0)
            {
                driver.NextPitLap = _random.Next(11, 14);
            }
        }

        private double GenerateRandomLapTime(Driver driver)
        {
            double minLapTime;
            double maxLapTime;

            if (driver.IsTopTeam)
            {
                minLapTime = 36.000;
                maxLapTime = 38.000;
            }
            else
            {
                minLapTime = 37.000;
                maxLapTime = 40.000;
            }

            return Math.Round(minLapTime + (_random.NextDouble() * (maxLapTime - minLapTime)), 3);
        }

        private void UpdatePositions()
        {
            var orderedDrivers = _raceState.Drivers
                .OrderBy(d => d.IsOut)
                .ThenByDescending(d => d.CurrentLap)
                .ThenBy(d => d.TotalTime)
                .ToList();

            for (var i = 0; i < orderedDrivers.Count; i++)
            {
                orderedDrivers[i].Position = i + 1;
                orderedDrivers[i].PositionChange = orderedDrivers[i].StartPosition - orderedDrivers[i].Position;
            }

            _raceState.Drivers = orderedDrivers;
        }

        private void AddRaceEvent(string message, string type, int? simulationSecond = null)
        {
            _raceState.Events.Add(new RaceEvent
            {
                Timestamp = DateTime.Now,
                Message = message,
                Type = type,
                SimulationSecond = simulationSecond ?? _raceState.ElapsedSeconds
            });
        }

        private string FormatDriverTotalTime(double totalSeconds)
        {
            var minutes = (int)(totalSeconds / 60);
            var seconds = totalSeconds % 60;
            return $"{minutes:00}:{seconds:00.000}";
        }

        private RaceState CreateInitialRaceState()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            var driverEntities = dbContext.Drivers
                .Include(d => d.Team)
                .OrderBy(d => d.StartPosition)
                .ToList();

            var drivers = driverEntities.Select(d => new Driver
            {
                Id = d.Id,
                Name = d.Name,
                Team = d.Team?.Name ?? "Unknown",
                IsTopTeam = d.Team?.IsTopTeam ?? false,
                Position = d.StartPosition,
                StartPosition = d.StartPosition,
                PositionChange = 0,
                CurrentLap = 0,
                LastLapTime = 0,
                BestLapTime = 0,
                TotalTime = 0,
                CurrentLapTargetTime = 0,
                CurrentLapProgressSeconds = 0,
                LapProgressPercent = 0,
                Status = StatusReady,
                IsOut = false,
                PitStopCount = 0,
                NextPitLap = 0
            }).ToList();

            return new RaceState
            {
                RaceName = "F1 Grand Prix Simulation",
                CurrentLap = 0,
                TotalLaps = 20,
                Status = StatusReady,
                ElapsedSeconds = 0,
                Drivers = drivers,
                Events = new List<RaceEvent>
                {
                    new RaceEvent
                    {
                        Timestamp = DateTime.Now,
                        Message = "Race state initialized.",
                        Type = EventInfo,
                        SimulationSecond = 0
                    },
                    new RaceEvent
                    {
                        Timestamp = DateTime.Now,
                        Message = "Drivers loaded.",
                        Type = EventInfo,
                        SimulationSecond = 0
                    }
                }
            };
        }
    }
}