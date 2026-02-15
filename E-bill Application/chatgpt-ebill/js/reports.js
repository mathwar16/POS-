window.onload = async () => {
    if (!checkAuth()) {
        window.location.href = 'login.html';
        return;
    }
    initReports();
};

function initReports() {
    initProfile();
    loadSchedules();
    loadGeneralSettings();
    loadEmailConfig();
}

// Profile initialization is now in common.js

async function loadSchedules() {
    try {
        const schedules = await getReportSchedules();
        renderSchedules(schedules);
    } catch (error) {
        showToast("Error loading schedules", "error");
    }
}

function renderSchedules(schedules) {
    const orderTable = document.getElementById("orderSchedulesTable");
    const expenseTable = document.getElementById("expenseSchedulesTable");

    if (!orderTable || !expenseTable) return;

    const orderRows = schedules.filter(s => s.reportType.includes("_Sales"));
    const expenseRows = schedules.filter(s => s.reportType.includes("_Expenses"));

    orderTable.innerHTML = renderScheduleRows(orderRows);
    expenseTable.innerHTML = renderScheduleRows(expenseRows);
}

function renderScheduleRows(rows) {
    if (!rows || rows.length === 0) {
        return '<tr><td colspan="5" style="text-align:center; padding: 2rem; color: var(--text-muted);">No schedules configured.</td></tr>';
    }

    return rows.map(s => {
        const typeParts = s.reportType.split('_');
        const frequency = typeParts[0];

        let frequencyHtml = "";
        if (frequency === "Weekly") {
            frequencyHtml = `
                <select id="freq-${s.id}" class="filter-select small">
                    <option value="1" ${s.dayOfWeek === 1 ? 'selected' : ''}>Monday</option>
                    <option value="2" ${s.dayOfWeek === 2 ? 'selected' : ''}>Tuesday</option>
                    <option value="3" ${s.dayOfWeek === 3 ? 'selected' : ''}>Wednesday</option>
                    <option value="4" ${s.dayOfWeek === 4 ? 'selected' : ''}>Thursday</option>
                    <option value="5" ${s.dayOfWeek === 5 ? 'selected' : ''}>Friday</option>
                    <option value="6" ${s.dayOfWeek === 6 ? 'selected' : ''}>Saturday</option>
                    <option value="0" ${s.dayOfWeek === 0 ? 'selected' : ''}>Sunday</option>
                </select>
            `;
        } else if (frequency === "Monthly") {
            frequencyHtml = `
                <input type="number" id="freq-${s.id}" value="${s.dayOfMonth || 1}" min="1" max="31" class="date-input small" style="width: 50px;">
                <span>(Day)</span>
            `;
        } else {
            frequencyHtml = "<span>Every Day</span>";
        }

        const runNowButton = `<span class="action-icon run-icon" title="Run Now" onclick="triggerManualReport('${s.reportType}')">▶</span>`;

        return `
            <tr>
                <td><strong>${frequency}</strong></td>
                <td>
                    <select id="status-${s.id}" class="filter-select small">
                        <option value="true" ${s.isActive ? 'selected' : ''}>Active</option>
                        <option value="false" ${!s.isActive ? 'selected' : ''}>Paused</option>
                    </select>
                </td>
                <td>${frequencyHtml}</td>
                <td>
                    <input type="time" id="time-${s.id}" value="${s.scheduledTime}" class="date-input small">
                </td>
                <td>
                    ${runNowButton}
                    <button onclick="saveSchedule(${s.id}, '${s.reportType}')" class="add-btn small">Save</button>
                </td>
            </tr>
        `;
    }).join('');
}

async function saveSchedule(id, type) {
    const status = document.getElementById(`status-${id}`).value === "true";
    const time = document.getElementById(`time-${id}`).value;
    const freqEl = document.getElementById(`freq-${id}`);

    const parts = type.split('_');
    const frequency = parts[0];

    const data = {
        isActive: status,
        scheduledTime: time,
        dayOfWeek: frequency === "Weekly" ? parseInt(freqEl.value) : null,
        dayOfMonth: frequency === "Monthly" ? parseInt(freqEl.value) : null
    };

    try {
        await updateReportSchedule(id, data);
        showToast(`${type.replace('_', ' ')} schedule updated`);
        loadSchedules();
    } catch (error) {
        showToast("Error updating schedule", "error");
    }
}

async function loadEmailConfig() {
    try {
        const response = await getReportEmails();
        const input = document.getElementById("reportEmails");
        if (input) input.value = response.emails || "";
    } catch (error) {
        console.error("Error loading email config:", error);
    }
}

async function saveEmails() {
    const emails = document.getElementById("reportEmails").value;
    try {
        await updateReportEmails(emails);
        showToast("Email recipients updated");
    } catch (error) {
        showToast("Error updating emails", "error");
    }
}

async function loadGeneralSettings() {
    try {
        const settings = await getGeneralSettings();
        document.getElementById("gstEnabled").checked = settings.gstEnabled;
        document.getElementById("gstPercentage").value = settings.gstPercentage;
        document.getElementById("serviceEnabled").checked = settings.serviceChargeEnabled;
        document.getElementById("servicePercentage").value = settings.serviceChargePercentage;
        
        // Load restaurant details
        if (document.getElementById("restaurantName")) {
            document.getElementById("restaurantName").value = settings.restaurantName || "";
        }
        if (document.getElementById("restaurantAddress")) {
            document.getElementById("restaurantAddress").value = settings.restaurantAddress || "";
        }
        if (document.getElementById("restaurantPhone")) {
            document.getElementById("restaurantPhone").value = settings.restaurantPhone || "";
        }
    } catch (error) {
        showToast("Error loading general settings", "error");
    }
}

async function saveGeneralSettings() {
    const settings = {
        gstEnabled: document.getElementById("gstEnabled").checked,
        gstPercentage: parseFloat(document.getElementById("gstPercentage").value),
        serviceChargeEnabled: document.getElementById("serviceEnabled").checked,
        serviceChargePercentage: parseFloat(document.getElementById("servicePercentage").value),
        restaurantName: document.getElementById("restaurantName")?.value || "",
        restaurantAddress: document.getElementById("restaurantAddress")?.value || "",
        restaurantPhone: document.getElementById("restaurantPhone")?.value || ""
    };

    try {
        await updateGeneralSettings(settings);
        showToast("Settings updated successfully");
    } catch (error) {
        showToast("Error saving settings", "error");
    }
}

async function triggerManualReport(type) {
    try {
        await runReport(type);
        showToast(`${type.replace('_', ' ')} report triggered successfully! Check your email.`);
    } catch (error) {
        showToast("Error triggering report", "error");
    }
}

// Toast function is now in common.js

