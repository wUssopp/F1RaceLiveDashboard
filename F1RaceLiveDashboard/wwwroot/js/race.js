let connection = null;
let currentTotalLaps = 0;

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

        connection.on("RaceStateUpdated", renderRaceState);

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
        renderRaceState(data);
    } catch (error) {
        console.error("Error while loading race data:", error);
        renderLoadErrorState();
    }
}

function renderRaceState(data) {
    renderRaceSummary(data);
    renderDriversTable(data.drivers);
    renderEvents(data.events);
}

function renderLoadErrorState() {
    const driversTableBody = document.getElementById("driversTableBody");
    const eventsBox = document.getElementById("eventsBox");

    if (driversTableBody) {
        driversTableBody.innerHTML = `<tr><td colspan="9">Failed to load drivers data.</td></tr>`;
    }

    if (eventsBox) {
        eventsBox.innerHTML = `<div class="event-item event-danger">Failed to load events.</div>`;
    }
}

function renderRaceSummary(data) {
    currentTotalLaps = data.totalLaps ?? 0;

    setText("raceStatus", data.status ?? "-");
    setText("raceTime", formatRaceTime(data.elapsedSeconds ?? 0));
    setText("totalLapsValue", currentTotalLaps);

    const canEditLaps = data.status === "Ready";
    setButtonDisabled("increaseLapsButton", !canEditLaps);
    setButtonDisabled("decreaseLapsButton", !canEditLaps);

    if (data.drivers && data.drivers.length > 0) {
        setText("leaderName", data.drivers[0].name);
        setText("activeDriversCount", data.drivers.filter(driver => !driver.isOut).length);
        return;
    }

    setText("leaderName", "-");
    setText("activeDriversCount", "-");
}

function renderDriversTable(drivers) {
    const tableBody = document.getElementById("driversTableBody");

    if (!tableBody) {
        return;
    }

    if (!drivers || drivers.length === 0) {
        tableBody.innerHTML = `<tr><td colspan="9">No drivers found.</td></tr>`;
        return;
    }

    tableBody.innerHTML = drivers.map(driver => {
        const progress = Math.floor(driver.lapProgressPercent ?? 0);

        return `
            <tr class="driver-row">
                <td>${driver.position}</td>
                <td class="${getPositionChangeClass(driver.positionChange)}">${formatPositionChange(driver.positionChange)}</td>
                <td>${driver.name}</td>
                <td>${driver.team}</td>
                <td>${driver.currentLap}/${currentTotalLaps}</td>
                <td>
                    <div class="lap-progress-cell">
                        <div class="lap-progress-bar">
                            <div class="lap-progress-fill" style="width: ${progress}%"></div>
                        </div>
                        <span class="lap-progress-text">${progress}%</span>
                    </div>
                </td>
                <td>${formatLapTime(driver.bestLapTime)}</td>
                <td>${formatLapTime(driver.lastLapTime)}</td>
                <td>${driver.status}</td>
            </tr>
        `;
    }).join("");
}

function renderEvents(events) {
    const eventsBox = document.getElementById("eventsBox");

    if (!eventsBox) {
        return;
    }

    if (!events || events.length === 0) {
        eventsBox.innerHTML = `<div class="events-list__item event-info">No events yet.</div>`;
        return;
    }

    eventsBox.innerHTML = [...events]
        .reverse()
        .map(raceEvent => `
            <div class="events-list__item ${getEventClass(raceEvent)}">
                <span class="events-list__time">${formatEventSimulationTime(raceEvent)}</span>
                <span class="events-list__message">${raceEvent.message}</span>
            </div>
        `)
        .join("");
}

function setupButtons() {
    const buttonActions = [
        ["startRaceButton", "/Race/Start"],
        ["pauseRaceButton", "/Race/Pause"],
        ["resumeRaceButton", "/Race/Resume"],
        ["resetRaceButton", "/Race/Reset"],
        ["speed1xButton", "/Race/SetSpeed?multiplier=1"],
        ["speed2xButton", "/Race/SetSpeed?multiplier=2"],
        ["speed4xButton", "/Race/SetSpeed?multiplier=4"],
        ["speed8xButton", "/Race/SetSpeed?multiplier=8"],
        ["increaseLapsButton", "/Race/IncreaseLaps"],
        ["decreaseLapsButton", "/Race/DecreaseLaps"]
    ];

    buttonActions.forEach(([elementId, url]) => {
        const button = document.getElementById(elementId);

        if (!button) {
            return;
        }

        button.addEventListener("click", async () => {
            await postAction(url);
        });
    });
}

async function postAction(url) {
    try {
        const response = await fetch(url, { method: "POST" });

        console.log(url, response.status);

        if (!response.ok) {
            throw new Error(`Request failed for ${url}`);
        }
    } catch (error) {
        console.error("Action error:", error);
    }
}

function getEventClass(raceEvent) {
    const eventType = (raceEvent.type || "").toLowerCase();
    const message = (raceEvent.message || "").toLowerCase();

    if (eventType === "pit" || message.includes("pit")) {
        return "event-pit";
    }

    if (eventType === "finish" || message.includes("finished")) {
        return "event-finish";
    }

    if (eventType === "danger" || message.includes("out")) {
        return "event-danger";
    }

    return "event-info";
}

function formatEventSimulationTime(raceEvent) {
    if (raceEvent.simulationSecond !== undefined && raceEvent.simulationSecond !== null) {
        return formatRaceTime(raceEvent.simulationSecond);
    }

    return "--:--";
}

function formatLapTime(value) {
    if (!value || value === 0) {
        return "-";
    }

    return `${value.toFixed(3)} s`;
}

function formatRaceTime(totalSeconds) {
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;

    return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
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

function setText(elementId, value) {
    const element = document.getElementById(elementId);

    if (element) {
        element.textContent = value;
    }
}

function setButtonDisabled(elementId, disabled) {
    const button = document.getElementById(elementId);

    if (button) {
        button.disabled = disabled;
    }
}