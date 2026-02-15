/**
 * Common utility functions used across all pages
 */

/**
 * Initialize profile dropdown functionality
 * This function is reused across all pages
 */
function initProfile() {
  const profileIcon = document.getElementById('profileIcon');
  const dropdown = document.getElementById('profileDropdown');
  const nameDisplay = document.getElementById('userNameDisplay');

  if (profileIcon && dropdown) {
    profileIcon.onclick = (e) => {
      e.stopPropagation();
      dropdown.classList.toggle('show');
    };

    document.onclick = () => dropdown.classList.remove('show');
  }

  const user = JSON.parse(localStorage.getItem('user') || '{}');
  if (nameDisplay && user.name) {
    nameDisplay.innerText = user.name;
    if (profileIcon) {
      profileIcon.innerText = user.name.charAt(0).toUpperCase();
    }
  }
}

/**
 * Initialize header navigation on page load
 * Call this after DOM is ready to inject header if using data-header attribute
 */
function initHeader() {
  const headerContainer = document.querySelector('[data-header]');
  if (headerContainer) {
    const activePage = headerContainer.getAttribute('data-header') || '';
    renderHeader(activePage);
  }
}

/**
 * Show toast notification
 * @param {string} message - The message to display
 * @param {string} type - 'success' or 'error' (default: 'success')
 */
function showToast(message, type = "success") {
  let toast = document.getElementById("toast");

  // Create toast element if it doesn't exist
  if (!toast) {
    toast = document.createElement("div");
    toast.id = "toast";
    toast.className = "toast hidden";
    document.body.appendChild(toast);
  }

  toast.innerText = message;
  toast.className = `toast ${type}`;
  toast.classList.remove("hidden");

  setTimeout(() => {
    toast.classList.add("hidden");
  }, 3000);
}

/**
 * Render navigation header with active page highlighting
 * @param {string} activePage - The current active page name (e.g., 'POS', 'Dashboard', etc.)
 */
function renderHeader(activePage = '') {
  const header = document.getElementById('app-header');
  if (!header) return;

  const pages = [
    { name: 'POS', href: 'index.html', key: 'POS' },
    { name: 'Dashboard', href: 'dashboard.html', key: 'Dashboard' },
    { name: 'Expenses', href: 'expenses.html', key: 'Expenses' },
    { name: 'Products', href: 'products.html', key: 'Products' },
    { name: 'Reports', href: 'reports.html', key: 'Reports' },
    { name: 'Transactions', href: 'transactions.html', key: 'Transactions' }
  ];

  const navLinks = pages.map(page => {
    const isActive = page.key === activePage ? 'class="active"' : '';
    return `<a href="${page.href}" ${isActive}>${page.name}</a>`;
  }).join('');

  header.innerHTML = `
    <h2 class="logo">M-POS</h2>
    <nav>
      ${navLinks}
      <div class="profile-container">
        <div class="profile-icon" id="profileIcon">👤</div>
        <div class="profile-dropdown" id="profileDropdown">
          <div class="dropdown-item" id="userNameDisplay">Name</div>
          <div class="dropdown-divider"></div>
          <div class="dropdown-item" onclick="logout()">Logout</div>
        </div>
      </div>
    </nav>
  `;

  // Initialize profile after header is rendered
  initProfile();
}

/**
 * Format a date string or object to IST localized string
 * @param {string|Date} dateSource - The date to format
 * @param {boolean} includeTime - Whether to include time in result (default: true)
 * @returns {string} Formatted IST date string
 */
function formatToIST(dateSource, includeTime = true) {
  if (!dateSource) return 'N/A';

  const date = new Date(dateSource);
  if (isNaN(date.getTime())) return 'N/A';

  const options = {
    timeZone: 'Asia/Kolkata',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  };

  if (includeTime) {
    options.hour = '2-digit';
    options.minute = '2-digit';
    options.hour12 = true;
  }

  return date.toLocaleString('en-IN', options);
}
