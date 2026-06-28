let connection = null;

document.addEventListener("DOMContentLoaded", async () => {
    await startSignalRConnection();
    await loadRaceData();
    setupButtons();
});

async function startSignalRConnection() {
    try {
        connection = new signalR.HubConnectionBuilder()
            .withUrl("/raceHub")
            .withAutomaticReconnect()
            .build();

        connection.on("RaceStateUpdated", (data) => {
            renderRaceSummary(data);
            renderDriversTable(data.drivers);
            renderEvents(data.events);
        });

        await connection.start();
        console.log("SignalR connected");
    } catch (error) {
        console.error("SignalR connection error:", error);
    }
}

async function loadRaceData() {
    try {
        const response = await fetch("/Race/Drivers");

        if (!response.ok) {
            throw new Error("Failed to load race data");
        }

        const data = await response.json();

        renderRaceSummary(data);
        renderDriversTable(data.drivers);
        renderEvents(data.events);
    } catch (error) {
        console.error("Error while loading race data:", error);

        document.getElementById("driversTableBody").innerHTML =
            `<tr><td colspan="7">Failed to load drivers data.</td></tr>`;
    }
}

function renderRaceSummary(data) {
    document.getElementById("raceStatus").textContent = data.status;
    document.getElementById("raceTime").textContent = formatRaceTime(data.elapsedSeconds ?? 0);

    if (data.drivers && data.drivers.length > 0) {
        document.getElementById("leaderName").textContent = data.drivers[0].name;

        const activeDrivers = data.drivers.filter(driver => !driver.isOut).length;
        document.getElementById("activeDriversCount").textContent = activeDrivers;
    } else {
        document.getElementById("leaderName").textContent = "-";
        document.getElementById("activeDriversCount").textContent = "-";
    }
}

function renderDriversTable(drivers) {
    const tableBody = document.getElementById("driversTableBody");

    if (!drivers || drivers.length === 0) {
        tableBody.innerHTML = `<tr><td colspan="9">No drivers found.</td></tr>`;
        return;
    }

    tableBody.innerHTML = drivers.map(driver => `
        <tr data-driver-id="${driver.id}" class="driver-row">
            <td>${driver.position}</td>
            <td class="${getPositionChangeClass(driver.positionChange)}">${formatPositionChange(driver.positionChange)}</td>
            <td>${driver.name}</td>
            <td>${driver.team}</td>
            <td>${driver.currentLap}</td>
            <td>
                <div class="lap-progress-cell">
                    <div class="lap-progress-bar">
                        <div class="lap-progress-fill" style="width: ${Math.floor(driver.lapProgressPercent ?? 0)}%"></div>
                    </div>
                    <span class="lap-progress-text">${Math.floor(driver.lapProgressPercent ?? 0)}%</span>
                </div>
            </td>
            <td>${formatLapTime(driver.bestLapTime)}</td>
            <td>${formatLapTime(driver.lastLapTime)}</td>
            <td>${driver.status}</td>
        </tr>
    `).join("");

    addDriverRowClickEvents();
}

function renderEvents(events) {
    const eventsBox = document.getElementById("eventsBox");

    if (!events || events.length === 0) {
        eventsBox.innerHTML = `<div class="event-item">No events yet.</div>`;
        return;
    }

    const sortedEvents = [...events].reverse();

    eventsBox.innerHTML = sortedEvents.map(raceEvent => `
        <div class="event-item">${raceEvent.message}</div>
    `).join("");
}

function addDriverRowClickEvents() {
    const rows = document.querySelectorAll(".driver-row");

    rows.forEach(row => {
        row.addEventListener("click", async () => {
            const driverId = row.getAttribute("data-driver-id");
            await loadDriverDetails(driverId);
        });
    });
}

async function loadDriverDetails(driverId) {
    try {
        const response = await fetch(`/Race/DriverDetails?id=${driverId}`);

        if (!response.ok) {
            throw new Error("Failed to load driver details");
        }

        const driver = await response.json();

        renderDriverDetails(driver);
    } catch (error) {
        console.error("Error while loading driver details:", error);
    }
}

function renderDriverDetails(driver) {
    const detailsBox = document.getElementById("driverDetailsBox");

    detailsBox.innerHTML = `
        <p><strong>Name:</strong> ${driver.name}</p>
        <p><strong>Team:</strong> ${driver.team}</p>
        <p><strong>Position:</strong> ${driver.position}</p>
        <p><strong>Status:</strong> ${driver.status}</p>
        <p><strong>Best Lap:</strong> ${formatLapTime(driver.bestLapTime)}</p>
    `;
}

function formatLapTime(value) {
    if (!value || value === 0) {
        return "-";
    }

    return value.toFixed(3) + " s";
}

function setupButtons() {
    const startButton = document.getElementById("startRaceButton");
    const pauseButton = document.getElementById("pauseRaceButton");
    const resumeButton = document.getElementById("resumeRaceButton");
    const resetButton = document.getElementById("resetRaceButton");
    const speed1xButton = document.getElementById("speed1xButton");
    const speed2xButton = document.getElementById("speed2xButton");
    const speed4xButton = document.getElementById("speed4xButton");
    const speed8xButton = document.getElementById("speed8xButton");

    if (startButton) {
        startButton.addEventListener("click", async () => {
            await postAction("/Race/Start");
        });
    }

    if (pauseButton) {
        pauseButton.addEventListener("click", async () => {
            await postAction("/Race/Pause");
        });
    }

    if (resumeButton) {
        resumeButton.addEventListener("click", async () => {
            await postAction("/Race/Resume");
        });
    }

    if (resetButton) {
        resetButton.addEventListener("click", async () => {
            await postAction("/Race/Reset");
        });
    }

    if (speed1xButton) {
        speed1xButton.addEventListener("click", async () => {
            await postAction("/Race/SetSpeed?multiplier=1");
        });
    }

    if (speed2xButton) {
        speed2xButton.addEventListener("click", async () => {
            await postAction("/Race/SetSpeed?multiplier=2");
        });
    }

    if (speed4xButton) {
        speed4xButton.addEventListener("click", async () => {
            await postAction("/Race/SetSpeed?multiplier=4");
        });
    }

    if (speed8xButton) {
        speed8xButton.addEventListener("click", async () => {
            await postAction("/Race/SetSpeed?multiplier=8");
        });
    }
}

async function postAction(url) {
    try {
        const response = await fetch(url, {
            method: "POST"
        });

        if (!response.ok) {
            throw new Error(`Request failed for ${url}`);
        }
    } catch (error) {
        console.error("Action error:", error);
    }
}

function formatRaceTime(totalSeconds) {
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;

    const minutesText = String(minutes).padStart(2, "0");
    const secondsText = String(seconds).padStart(2, "0");

    return `${minutesText}:${secondsText}`;
}

function getDriverLapProgressPercent(driver) {
    const raceTimeElement = document.getElementById("raceTime");

    if (!raceTimeElement) {
        return 0;
    }

    const raceTimeText = raceTimeElement.textContent ?? "00:00";
    const totalSeconds = parseRaceTimeToSeconds(raceTimeText);

    if (driver.status === "Finished") {
        return 100;
    }

    if (driver.status === "Out") {
        return 0;
    }

    const lapDurationSeconds = 5;
    const progressInCurrentLap = totalSeconds % lapDurationSeconds;
    const progressPercent = Math.floor((progressInCurrentLap / lapDurationSeconds) * 100);

    return progressPercent;
}

function parseRaceTimeToSeconds(timeText) {
    const parts = timeText.split(":");

    if (parts.length !== 2) {
        return 0;
    }

    const minutes = parseInt(parts[0], 10);
    const seconds = parseInt(parts[1], 10);

    if (isNaN(minutes) || isNaN(seconds)) {
        return 0;
    }

    return (minutes * 60) + seconds;
}

function formatPositionChange(positionChange) {
    if (positionChange > 0) {
        return `↑${positionChange}`;
    }

    if (positionChange < 0) {
        return `↓${Math.abs(positionChange)}`;
    }

    return "-";
}

function getPositionChangeClass(positionChange) {
    if (positionChange > 0) {
        return "position-up";
    }

    if (positionChange < 0) {
        return "position-down";
    }

    return "position-flat";
}