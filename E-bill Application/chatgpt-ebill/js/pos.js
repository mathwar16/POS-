let cart = [];
let globalSettings = {
  gstEnabled: true,
  gstPercentage: 5,
  serviceChargeEnabled: true,
  serviceChargePercentage: 5,
  restaurantName: "My Restaurant",
  restaurantAddress: "123, Main Street",
  restaurantPhone: "+91-00000 00000"
};
let selectedCategory = "All";
let allProducts = [];
let posCategories = [];

window.onload = async () => {
  if (!checkAuth()) {
    window.location.href = 'login.html';
    return;
  }

  await loadGeneralSettings();
  allProducts = await getProducts();
  try {
    const managedCategories = await getProductCategories();
    const productCategories = [...new Set(allProducts.map(p => p.category || "Uncategorized"))];

    // Merge both lists, keeping managed ones first for order
    const merged = ["All", ...managedCategories.map(c => c.name)];
    productCategories.forEach(cat => {
      if (!merged.includes(cat)) merged.push(cat);
    });

    posCategories = merged;
  } catch (error) {
    console.error("Error fetching categories, falling back to derived list:", error);
    posCategories = ["All", ...new Set(allProducts.map(p => p.category || "Uncategorized"))];
  }
  renderCategoryNav();
  applyFilters();
  initProfile();

  // Search functionality
  const searchInput = document.getElementById("searchMenu");
  if (searchInput) {
    searchInput.addEventListener("input", (e) => {
      filterProducts(e.target.value);
    });
  }
};

async function loadGeneralSettings() {
  try {
    const settings = await getGeneralSettings();
    globalSettings = settings;

    // Update labels in UI
    const gstLabel = document.getElementById("gstPercentLabel");
    const serviceLabel = document.getElementById("servicePercentLabel");
    if (gstLabel) gstLabel.innerText = globalSettings.gstPercentage;
    if (serviceLabel) serviceLabel.innerText = globalSettings.serviceChargePercentage;

    // Toggle visibility
    const gstRow = document.getElementById("gstRow");
    const serviceRow = document.getElementById("serviceRow");
    if (gstRow) gstRow.style.display = globalSettings.gstEnabled ? "block" : "none";
    if (serviceRow) serviceRow.style.display = globalSettings.serviceChargeEnabled ? "block" : "none";

  } catch (error) {
    console.error("Error loading settings:", error);
  }
}

// Profile initialization is now in common.js

let selectedPlatform = 'Direct';

function setPlatform(platform) {
  selectedPlatform = platform;
  const buttons = ['Direct', 'Zomato', 'Swiggy'];
  buttons.forEach(p => {
    const btn = document.getElementById(`platform-${p}`);
    if (btn) btn.classList.toggle('active', p === platform);
  });
}

/**
 * Render category navigation buttons
 */
function renderCategoryNav() {
  const container = document.getElementById("categoryNav");
  if (!container) return;

  const categoriesToRender = posCategories.length > 0 ? posCategories : ["All"];

  container.innerHTML = "";
  categoriesToRender.forEach(cat => {
    const btn = document.createElement("button");
    btn.className = `category-btn ${selectedCategory === cat ? 'active' : ''}`;
    btn.innerText = cat;
    btn.onclick = () => {
      selectedCategory = cat;
      renderCategoryNav();
      applyFilters();
    };
    container.appendChild(btn);
  });
}

/**
 * Apply both category and search filters
 */
function applyFilters() {
  const searchTerm = document.getElementById("searchMenu")?.value.toLowerCase() || "";

  const filtered = allProducts.filter(p => {
    const matchesCategory = selectedCategory === "All" || (p.category || "Uncategorized") === selectedCategory;
    const matchesSearch = p.name.toLowerCase().includes(searchTerm) ||
      (p.category && p.category.toLowerCase().includes(searchTerm));
    return matchesCategory && matchesSearch;
  });

  displayProducts(filtered);
  renderFavorites();
}

/**
 * Render favorites section
 */
function renderFavorites() {
  const container = document.getElementById("favoritesSection");
  const grid = document.getElementById("favoritesGrid");
  if (!container || !grid) return;

  const favorites = allProducts.filter(p => p.isFavorite || p.IsFavorite);

  if (favorites.length === 0) {
    container.classList.add("hidden");
    return;
  }

  container.classList.remove("hidden");
  grid.innerHTML = favorites.map(p => `
    <div class="card" style="padding: 1rem; min-height: auto;">
      <span class="favorite-toggle active" onclick="event.stopPropagation(); toggleFavorite(${p.id})">★</span>
      <h4 style="font-size: 0.95rem; margin-bottom: 0.25rem;">${p.name}</h4>
      <p class="price" style="font-size: 1.1rem; margin-bottom: 0.75rem;">₹${p.price.toFixed(2)}</p>
      <button class="small" onclick='addToCart(${JSON.stringify(p).replace(/"/g, '&quot;')})' style="padding: 0.5rem; font-size: 0.8rem;">+ Add</button>
    </div>
  `).join('');
}

async function toggleFavorite(productId) {
  const product = allProducts.find(p => p.id === productId);
  if (!product) return;

  const originalValue = product.isFavorite || product.IsFavorite || false;
  product.isFavorite = !originalValue;
  product.IsFavorite = !originalValue;

  try {
    const updated = await updateProduct(productId, {
      name: product.name,
      price: product.price,
      category: product.category,
      isFavorite: product.isFavorite
    });

    // Update local state
    const index = allProducts.findIndex(p => p.id === productId);
    if (index !== -1) allProducts[index] = updated;

    applyFilters();
  } catch (error) {
    console.error("Error toggling favorite:", error);
    product.isFavorite = originalValue; // Rollback
    applyFilters();
    showToast("Failed to update favorite status", "error");
  }
}

/**
 * Display filtered products
 */
function displayProducts(productsList) {
  const grid = document.getElementById("productGrid");
  if (!grid) return;

  grid.innerHTML = "";

  if (productsList.length === 0) {
    grid.innerHTML = "<div style='grid-column: 1/-1; text-align: center; padding: 40px; color: var(--text-muted);'>No items found matching your filter.</div>";
    return;
  }

  productsList.forEach(p => {
    const card = document.createElement("div");
    card.className = "card";
    const isFav = (p.isFavorite || p.IsFavorite) ? "active" : "";
    card.innerHTML = `
      <span class="favorite-toggle ${isFav}" onclick="event.stopPropagation(); toggleFavorite(${p.id})">★</span>
      <h4>${p.name}</h4>
      <p class="price">₹${p.price.toFixed(2)}</p>
      <span class="category">${p.category || 'Uncategorized'}</span>
      <button onclick='addToCart(${JSON.stringify(p).replace(/"/g, '&quot;')})'>+ Add to Cart</button>
    `;
    grid.appendChild(card);
  });
}

/**
 * Load products and display them (kept for legacy/reloads)
 */
async function loadProducts() {
  try {
    allProducts = await getProducts();
    renderCategoryNav();
    applyFilters();
  } catch (error) {
    console.error('Error loading products:', error);
  }
}

/**
 * Filter products based on search (updated to use applyFilters)
 */
async function filterProducts(searchTerm) {
  applyFilters();
}

/**
 * Add product to cart
 */
function addToCart(product) {
  const existingItem = cart.find(item => item.id === product.id);

  if (existingItem) {
    existingItem.quantity = (existingItem.quantity || 1) + 1;
  } else {
    cart.push({
      ...product,
      quantity: 1
    });
  }

  renderBill();
}

/**
 * Remove item from cart
 */
function removeFromCart(index) {
  cart.splice(index, 1);
  renderBill();
}

/**
 * Update item quantity
 */
function updateQuantity(index, change) {
  const item = cart[index];
  item.quantity = Math.max(1, (item.quantity || 1) + change);
  renderBill();
}

/**
 * Render the bill
 */
function renderBill() {
  const list = document.getElementById("billItems");
  if (!list) return;

  list.innerHTML = "";

  if (cart.length === 0) {
    list.innerHTML = "<p style='color: var(--text-muted); text-align: center; padding: 40px; font-size: 1.1rem;'>🛒 Your cart is empty<br><span style='font-size: 0.9rem; opacity: 0.7;'>Add items to get started</span></p>";
    updateSummary(0);
    return;
  }

  let subtotal = 0;

  cart.forEach((item, index) => {
    const quantity = item.quantity || 1;
    const itemTotal = item.price * quantity;
    subtotal += itemTotal;

    const billItem = document.createElement("div");
    billItem.className = "bill-item";
    billItem.innerHTML = `
      <div class="bill-item-name">
        <strong>${item.name}</strong>
        <div class="bill-item-controls">
          <button onclick="updateQuantity(${index}, -1)">-</button>
          <span>${quantity}</span>
          <button onclick="updateQuantity(${index}, 1)">+</button>
        </div>
      </div>
      <div class="bill-item-price">₹${itemTotal.toFixed(2)}</div>
      <button class="bill-item-remove" onclick="removeFromCart(${index})">×</button>
    `;
    list.appendChild(billItem);
  });

  updateSummary(subtotal);
}

/**
 * Update summary totals
 */
function updateSummary(subtotal) {
  const gst = globalSettings.gstEnabled ? subtotal * (globalSettings.gstPercentage / 100) : 0;
  const service = globalSettings.serviceChargeEnabled ? subtotal * (globalSettings.serviceChargePercentage / 100) : 0;
  const total = subtotal + gst + service;

  const subTotalEl = document.getElementById("subTotal");
  const gstEl = document.getElementById("gst");
  const serviceEl = document.getElementById("service");
  const totalEl = document.getElementById("total");

  if (subTotalEl) subTotalEl.innerText = `₹${subtotal.toFixed(2)}`;
  if (gstEl) gstEl.innerText = `₹${gst.toFixed(2)}`;
  if (serviceEl) serviceEl.innerText = `₹${service.toFixed(2)}`;
  if (totalEl) totalEl.innerText = `₹${total.toFixed(2)}`;

  // Update visibility again in case it changes
  const gstRow = document.getElementById("gstRow");
  const serviceRow = document.getElementById("serviceRow");
  if (gstRow) gstRow.style.display = globalSettings.gstEnabled ? "block" : "none";
  if (serviceRow) serviceRow.style.display = globalSettings.serviceChargeEnabled ? "block" : "none";
}

/**
 * Clear cart
 */
function clearCart() {
  if (confirm("Are you sure you want to clear the cart?")) {
    cart = [];
    renderBill();
  }
}

/**
 * Generate bill
 */
async function generateBill(paymentMethod) {
  if (cart.length === 0) {
    alert("Cart is empty!");
    return;
  }

  const subtotal = cart.reduce((sum, item) => sum + (item.price * (item.quantity || 1)), 0);
  const gst = globalSettings.gstEnabled ? subtotal * (globalSettings.gstPercentage / 100) : 0;
  const service = globalSettings.serviceChargeEnabled ? subtotal * (globalSettings.serviceChargePercentage / 100) : 0;
  const total = subtotal + gst + service;

  const customerName = document.getElementById("customerName")?.value || "";
  const customerPhone = document.getElementById("customerPhone")?.value || "";

  const bill = {
    items: cart.map(item => ({
      id: item.id,
      name: item.name,
      price: item.price,
      quantity: item.quantity || 1,
      total: item.price * (item.quantity || 1)
    })),
    subtotal: subtotal,
    gst: gst,
    service: service,
    total: total,
    paymentMethod: paymentMethod,
    platform: selectedPlatform,
    customerName: customerName,
    customerPhone: customerPhone,
    date: new Date().toISOString()
  };

  try {
    const savedBill = await saveBill(bill);
    // Use server response for token/bill numbers while keeping local items
    showBillPreview(bill, savedBill);
    cart = [];
    if (document.getElementById("customerName")) document.getElementById("customerName").value = "";
    if (document.getElementById("customerPhone")) document.getElementById("customerPhone").value = "";
    renderBill();
  } catch (error) {
    console.error('Error generating bill:', error);
    alert('Failed to generate bill. Please try again.');
  }
}

/**
 * Show bill preview modal
 */
function showBillPreview(bill, savedBill) {
  const modal = document.getElementById("billModal");
  const billText = document.getElementById("billText");

  if (!modal || !billText) return;

  modal.classList.add("show");

  const billDateSource = (savedBill && savedBill.createdAt) || bill.date;
  const date = formatToIST(billDateSource);

  const restaurantName = (globalSettings.restaurantName || "My Restaurant").toUpperCase();
  const restaurantAddress = globalSettings.restaurantAddress || "";
  const restaurantPhone = globalSettings.restaurantPhone || "";
  const tokenNumber = savedBill && typeof savedBill.tokenNumber === "number" ? savedBill.tokenNumber : null;
  const billNumber = savedBill && savedBill.billNumber ? savedBill.billNumber : null;

  let text = "";

  // Header
  text += `${restaurantName}\n`;
  if (restaurantAddress) text += `${restaurantAddress}\n`;
  if (restaurantPhone) text += `Ph: ${restaurantPhone}\n`;
  text += "============================\n";

  // Identifiers & metadata
  if (tokenNumber || billNumber) {
    if (tokenNumber) {
      text += `Token: ${String(tokenNumber).padStart(3, "0")}  `;
    }
    if (billNumber) {
      text += `Bill: ${billNumber}\n`;
    } else if (tokenNumber) {
      text += "\n";
    }
  }
  text += `Date : ${date}\n`;
  text += `Paymt: ${bill.paymentMethod}   Platform: ${bill.platform}\n`;
  // if (bill.customerName || bill.customerPhone) {
  //   if (bill.customerName) text += `Cust : ${bill.customerName}\n`;
  //   if (bill.customerPhone) text += `Ph   : ${bill.customerPhone}\n`;
  // }
  text += "============================\n";

  // Itemized list with columns
  text += "Item               Qty  Price   Total\n";
  text += "----------------------------\n";

  bill.items.forEach(item => {
    const name = (item.name || "").toString();
    const qty = item.quantity || 1;
    const lineTotal = item.price * qty;

    // Handle long item names - wrap within 16 char width
    const maxNameWidth = 16;
    let nameLines = [];
    if (name.length <= maxNameWidth) {
      nameLines.push(name);
    } else {
      // Split long names into multiple lines
      for (let i = 0; i < name.length; i += maxNameWidth) {
        nameLines.push(name.slice(i, i + maxNameWidth));
      }
    }

    // First line with all columns
    const firstLineName = nameLines[0].padEnd(maxNameWidth, " ");
    const qtyCol = String(qty).padStart(3, " ");
    const priceCol = item.price.toFixed(2).padStart(7, " ");
    const totalCol = lineTotal.toFixed(2).padStart(7, " ");
    text += `${firstLineName}${qtyCol}${priceCol}${totalCol}\n`;

    // Additional lines for wrapped name (name only, no columns)
    for (let i = 1; i < nameLines.length; i++) {
      text += `${nameLines[i]}\n`;
    }
  });

  text += "----------------------------\n";
  text += `Subtotal         ₹${bill.subtotal.toFixed(2).padStart(8, " ")}\n`;

  if (globalSettings.gstEnabled) {
    text += `GST ${globalSettings.gstPercentage}%       ₹${bill.gst.toFixed(2).padStart(8, " ")}\n`;
  }
  if (globalSettings.serviceChargeEnabled) {
    text += `Service ${globalSettings.serviceChargePercentage}%   ₹${bill.service.toFixed(2).padStart(8, " ")}\n`;
  }

  text += "============================\n";
  text += `GRAND TOTAL      ₹${bill.total.toFixed(2).padStart(8, " ")}\n`;
  text += "============================\n\n";
  text += "   Thank you for your visit!\n\n";

  billText.innerText = text;
}

/**
 * Close modal
 */
function closeModal() {
  const modal = document.getElementById("billModal");
  if (modal) {
    modal.classList.remove("show");
  }
}

/**
 * Print bill
 */
function printBill() {
  window.print();
}
