window.onload = async () => {
    if (!checkAuth()) {
        window.location.href = 'login.html';
        return;
    }
    initTransactions();
};

function initTransactions() {
    initProfile();
    loadTransactions();
}

async function loadTransactions(page = 1) {
    const start = document.getElementById('historyStartDate').value;
    const end = document.getElementById('historyEndDate').value;

    try {
        const response = await getBills(page, 15, start, end);
        renderTransactions(response);
    } catch (error) {
        showToast("Error loading transactions", "error");
    }
}

function renderTransactions(data) {
    const tbody = document.getElementById("transactionsTable");
    if (!tbody) return;

    if (!data.items || data.items.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" style="text-align:center; padding: 2rem; color: var(--text-muted);">No transactions found.</td></tr>';
        return;
    }

    tbody.innerHTML = data.items.map(b => `
        <tr style="border-bottom: 1px solid var(--border);">
            <td style="padding: 1rem;"><strong>#${b.billNumber}</strong></td>
            <td style="padding: 1rem;">${formatToIST(b.createdAt)}</td>
            <td style="padding: 1rem;"><span class="badge" style="background: rgba(99, 102, 241, 0.1); color: var(--primary-light); padding: 0.25rem 0.5rem; border-radius: 4px;">${b.platform}</span></td>
            <td style="padding: 1rem;">${b.paymentMethod}</td>
            <td style="padding: 1rem; font-size: 0.85rem; color: var(--text-secondary);">
                ${b.items && b.items.length > 0 ? b.items.map(i => `${i.productName} (x${i.quantity})`).join(', ') : 'No items'}
            </td>
            <td style="padding: 1rem;"><strong>₹${b.total.toFixed(2)}</strong></td>
        </tr>
    `).join('');

    renderHistoryPagination(data);
}

function renderHistoryPagination(data) {
    const container = document.getElementById("historyPagination");
    if (!container) return;

    if (data.totalPages <= 1) {
        container.innerHTML = "";
        return;
    }

    let html = `<button class="page-btn" ${data.page === 1 ? 'disabled' : ''} onclick="loadTransactions(${data.page - 1})">Prev</button>`;
    for (let i = 1; i <= data.totalPages; i++) {
        html += `<button class="page-btn ${data.page === i ? 'active' : ''}" onclick="loadTransactions(${i})">${i}</button>`;
    }
    html += `<button class="page-btn" ${data.page === data.totalPages ? 'disabled' : ''} onclick="loadTransactions(${data.page + 1})">Next</button>`;
    container.innerHTML = html;
}

// Profile and toast functions are now in common.js

async function exportTransactions() {
    const start = document.getElementById('historyStartDate').value;
    const end = document.getElementById('historyEndDate').value;

    try {
        // Fetch a larger page size to export more records at once
        const response = await getBills(1, 1000, start, end);
        const items = response.items || [];
        if (items.length === 0) {
            showToast("No transactions to export for the selected range", "error");
            return;
        }

        const header = [
            "TokenNumber",
            "BillNumber",
            "DateTime",
            "Platform",
            "PaymentMethod",
            "Subtotal",
            "GST",
            "ServiceCharge",
            "Total"
        ];

        const rows = items.map(b => [
            b.tokenNumber ?? "",
            b.billNumber,
            new Date(b.createdAt).toLocaleString(),
            b.platform,
            b.paymentMethod,
            b.subtotal.toFixed(2),
            b.gst.toFixed(2),
            b.serviceCharge.toFixed(2),
            b.total.toFixed(2)
        ]);

        const csvContent = [header, ...rows]
            .map(row => row.map(val => `"${String(val).replace(/"/g, '""')}"`).join(","))
            .join("\r\n");

        const blob = new Blob(["\uFEFF" + csvContent], { type: "text/csv;charset=utf-8;" });
        const url = URL.createObjectURL(blob);

        const link = document.createElement("a");
        link.href = url;
        const fileNameDate = start || end || new Date().toISOString().split("T")[0];
        link.download = `transactions_${fileNameDate}.csv`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);

        showToast("Transactions exported successfully");
    } catch (error) {
        console.error("Error exporting transactions:", error);
        showToast("Failed to export transactions", "error");
    }
}
