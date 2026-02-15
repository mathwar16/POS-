const API_BASE = "http://localhost:5108/api";

/**
 * Get headers with optional auth token
 */
const getHeaders = () => {
    const token = localStorage.getItem('token');
    const headers = {
        'Content-Type': 'application/json'
    };
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }
    return headers;
};

/**
 * Handle API responses
 * If 401 occurs, it tries to refresh the token.
 */
const handleResponse = async (response, originalRequestConfig = null, skipAuthRefresh = false) => {
    if (response.status === 401 && !skipAuthRefresh) {
        const refreshed = await refreshToken();
        if (refreshed && originalRequestConfig) {
            // Retry the original request with new token
            const { url, options } = originalRequestConfig;
            options.headers = getHeaders(); // Get fresh headers with new token
            const retryResponse = await fetch(url, options);
            return handleResponse(retryResponse, null, true); // Don't retry again
        } else if (!refreshed) {
            logout();
            throw new Error('Session expired');
        }
    }

    if (!response.ok) {
        let errorMessage = `Request failed with status ${response.status}`;
        try {
            const errorData = await response.json();
            errorMessage = errorData.error || errorData.message || errorMessage;
            if (errorData.detail) {
                errorMessage += ` (Detail: ${errorData.detail})`;
            }
        } catch (e) {
            // Not JSON
        }
        throw new Error(errorMessage);
    }

    try {
        const text = await response.text();
        return text ? JSON.parse(text) : null;
    } catch (e) {
        return null;
    }
};

/**
 * Centralized fetch with multi-try logic for 401
 */
async function apiCall(endpoint, options = {}, skipAuthRefresh = false) {
    const url = `${API_BASE}${endpoint}`;
    options.headers = { ...getHeaders(), ...options.headers };

    const response = await fetch(url, options);
    return handleResponse(response, { url, options }, skipAuthRefresh);
}

/**
 * Login User
 */
async function login(email, password) {
    const data = await apiCall('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password })
    }, true);

    if (data?.token) {
        setAuth(data);
    }
    return data;
}

/**
 * Signup User
 */
async function signup(name, email, password) {
    const data = await apiCall('/auth/signup', {
        method: 'POST',
        body: JSON.stringify({ name, email, password })
    }, true);

    if (data?.token) {
        setAuth(data);
    }
    return data;
}

/**
 * Refresh Token Logic
 */
let isRefreshing = false;
let refreshSubscribers = [];

async function refreshToken() {
    const currentRefreshToken = localStorage.getItem('refreshToken');
    if (!currentRefreshToken || isRefreshing) return false;

    isRefreshing = true;
    try {
        const response = await fetch(`${API_BASE}/auth/refresh-token`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ refreshToken: currentRefreshToken })
        });

        if (!response.ok) throw new Error('Refresh failed');

        const data = await response.json();
        setAuth(data);
        isRefreshing = false;
        return true;
    } catch (error) {
        isRefreshing = false;
        return false;
    }
}

function setAuth(data) {
    localStorage.setItem('token', data.token);
    localStorage.setItem('refreshToken', data.refreshToken);
    localStorage.setItem('user', JSON.stringify({
        id: data.id,
        name: data.name,
        email: data.email
    }));
}

/**
 * Logout User
 */
function logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    window.location.href = 'login.html';
}

/**
 * Check if user is authenticated
 */
function checkAuth() {
    return !!localStorage.getItem('token');
}

/**
 * Products API
 */
async function getProducts() {
    return apiCall('/products');
}

async function createProduct(product) {
    return apiCall('/products', {
        method: 'POST',
        body: JSON.stringify(product)
    });
}

async function updateProduct(id, product) {
    return apiCall(`/products/${id}`, {
        method: 'PUT',
        body: JSON.stringify(product)
    });
}

async function deleteProduct(id) {
    return apiCall(`/products/${id}`, {
        method: 'DELETE'
    });
}

/**
 * Product Categories API
 */
async function getProductCategories() {
    return apiCall('/productcategories');
}

async function createProductCategory(category) {
    return apiCall('/productcategories', {
        method: 'POST',
        body: JSON.stringify(category)
    });
}

async function updateProductCategory(id, category) {
    return apiCall(`/productcategories/${id}`, {
        method: 'PUT',
        body: JSON.stringify(category)
    });
}

async function deleteProductCategory(id) {
    return apiCall(`/productcategories/${id}`, {
        method: 'DELETE'
    });
}

/**
 * Bills API
 */
async function saveBill(bill) {
    return apiCall('/bills', {
        method: 'POST',
        body: JSON.stringify(bill)
    });
}

async function getBills(page = 1, pageSize = 20, startDate = null, endDate = null) {
    let query = `?page=${page}&pageSize=${pageSize}`;
    if (startDate) query += `&startDate=${startDate}`;
    if (endDate) query += `&endDate=${endDate}`;
    return apiCall(`/bills${query}`);
}

/**
 * Dashboard API
 */
async function getDashboardData() {
    return apiCall('/dashboard/today');
}

async function getDashboardStats(startDate = null, endDate = null, page = 1, pageSize = 10) {
    let query = `?page=${page}&pageSize=${pageSize}`;
    if (startDate) query += `&startDate=${startDate}`;
    if (endDate) query += `&endDate=${endDate}`;
    return apiCall(`/dashboard/stats${query}`);
}

async function getReportSchedules() {
    return await apiCall('/reportsettings/schedules');
}

async function updateReportSchedule(id, data) {
    return await apiCall(`/reportsettings/schedules/${id}`, {
        method: 'PUT',
        body: JSON.stringify(data)
    });
}

async function getReportEmails() {
    return await apiCall('/reportsettings/emails');
}

async function updateReportEmails(emails) {
    return await apiCall('/reportsettings/emails', {
        method: 'PUT',
        body: JSON.stringify({ emails })
    });
}

async function runReport(type) {
    return await apiCall(`/reportsettings/run/${type}`, {
        method: 'POST'
    });
}

async function getGeneralSettings() {
    return await apiCall('/reportsettings/general');
}

async function updateGeneralSettings(settings) {
    return await apiCall('/reportsettings/general', {
        method: 'PUT',
        body: JSON.stringify(settings)
    });
}

/**
 * Expense Management API
 */
async function getExpenseCategories() {
    return await apiCall('/expenses/categories');
}

async function createExpenseCategory(name) {
    return await apiCall('/expenses/categories', {
        method: 'POST',
        body: JSON.stringify({ name })
    });
}

async function updateExpenseCategory(id, name) {
    return await apiCall(`/expenses/categories/${id}`, {
        method: 'PUT',
        body: JSON.stringify({ name })
    });
}

async function getExpenses(page = 1, pageSize = 20, filters = {}) {
    let query = `?page=${page}&pageSize=${pageSize}`;
    if (filters.startDate) query += `&startDate=${filters.startDate}`;
    if (filters.endDate) query += `&endDate=${filters.endDate}`;
    if (filters.categoryId) query += `&categoryId=${filters.categoryId}`;

    return await apiCall(`/expenses${query}`);
}

async function getExpenseSummary(startDate = null, endDate = null) {
    let query = '';
    if (startDate || endDate) {
        query = '?';
        if (startDate) query += `startDate=${startDate}&`;
        if (endDate) query += `endDate=${endDate}`;
    }
    return await apiCall(`/expenses/summary${query}`);
}

async function createExpense(expenseData) {
    return await apiCall('/expenses', {
        method: 'POST',
        body: JSON.stringify(expenseData)
    });
}

async function deleteExpense(id) {
    return await apiCall(`/expenses/${id}`, {
        method: 'DELETE'
    });
}
