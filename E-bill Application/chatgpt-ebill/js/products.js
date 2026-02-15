let products = [];
let categories = [];

window.onload = async () => {
  if (!checkAuth()) {
    window.location.href = 'login.html';
    return;
  }

  await loadProducts();
  await loadCategories();
  initProfile();

  // Search functionality
  const searchInput = document.getElementById("searchProducts");
  if (searchInput) {
    searchInput.addEventListener("input", (e) => {
      filterProducts(e.target.value);
    });
  }

  // Form submission
  const form = document.getElementById("productForm");
  if (form) {
    form.addEventListener("submit", handleFormSubmit);
  }
};

/**
 * Load products
 */
async function loadProducts() {
  try {
    products = await getProducts();
    renderProducts(products);
  } catch (error) {
    console.error('Error loading products:', error);
  }
}

/**
 * Render products list grouped by category
 */
function renderProducts(productsList) {
  const container = document.getElementById("productsList");
  if (!container) return;

  container.innerHTML = "";

  if (productsList.length === 0) {
    container.innerHTML = '<p style="color: #666; text-align: center; padding: 40px;">No products found. Add your first product!</p>';
    return;
  }

  // Group products by category
  const groupedProducts = productsList.reduce((acc, product) => {
    const category = product.category || 'Uncategorized';
    if (!acc[category]) {
      acc[category] = [];
    }
    acc[category].push(product);
    return acc;
  }, {});

  // Render each category group
  Object.keys(groupedProducts).sort().forEach(category => {
    const groupDiv = document.createElement("div");
    groupDiv.className = "category-group";

    const header = document.createElement("h3");
    header.className = "category-group-header";
    header.innerText = category;
    groupDiv.appendChild(header);

    const grid = document.createElement("div");
    grid.className = "products-grid"; // Using a grid for the items inside the group
    grid.style.display = "grid";
    grid.style.gridTemplateColumns = "repeat(auto-fill, minmax(300px, 1fr))";
    grid.style.gap = "1.5rem";

    groupedProducts[category].forEach(product => {
      const card = document.createElement("div");
      card.className = "product-card";
      card.innerHTML = `
        <div class="product-info">
          <h4>${product.name}</h4>
          <p>₹${product.price.toFixed(2)}</p>
          <span class="category">${product.category || 'Uncategorized'}</span>
        </div>
        <div class="product-actions">
          <button class="edit-btn" onclick="editProduct(${product.id})">Edit</button>
          <button class="delete-btn" onclick="deleteProductHandler(${product.id})">Delete</button>
        </div>
      `;
      grid.appendChild(card);
    });

    groupDiv.appendChild(grid);
    container.appendChild(groupDiv);
  });
}

/**
 * Filter products
 */
function filterProducts(searchTerm) {
  const filtered = products.filter(p =>
    p.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    (p.category && p.category.toLowerCase().includes(searchTerm.toLowerCase()))
  );
  renderProducts(filtered);
}

/**
 * Show add product modal
 */
function showAddProductModal() {
  const modal = document.getElementById("productModal");
  const form = document.getElementById("productForm");
  const title = document.getElementById("modalTitle");

  if (modal && form && title) {
    populateCategoryDropdown();
    form.reset();
    document.getElementById("productId").value = "";
    title.innerText = "Add Product";
    modal.classList.add("show");
  }
}

/**
 * Load categories
 */
async function loadCategories() {
  try {
    categories = await getProductCategories();
    populateCategoryDropdown();
  } catch (error) {
    console.error('Error loading categories:', error);
  }
}

/**
 * Populate category dropdown
 */
function populateCategoryDropdown() {
  const select = document.getElementById("productCategory");
  if (!select) return;

  const currentValue = select.value;
  select.innerHTML = '<option value="">Select Category</option>';
  categories.forEach(cat => {
    const option = document.createElement("option");
    option.value = cat.name;
    option.innerText = cat.name;
    select.appendChild(option);
  });

  if (currentValue) select.value = currentValue;
}

/**
 * Show Manage Categories Modal
 */
async function showManageCategoriesModal() {
  const modal = document.getElementById("categoryModal");
  if (modal) {
    await loadCategories();
    renderCategoryList();
    modal.classList.add("show");
  }
}

/**
 * Close Manage Categories Modal
 */
function closeManageCategoriesModal() {
  const modal = document.getElementById("categoryModal");
  if (modal) {
    modal.classList.remove("show");
  }
}

/**
 * Render category list in manager
 */
function renderCategoryList() {
  const container = document.getElementById("categoryListContainer");
  if (!container) return;

  if (categories.length === 0) {
    container.innerHTML = '<p style="text-align: center; color: var(--text-muted); padding: 10px;">No categories found.</p>';
    return;
  }

  container.innerHTML = categories.map(cat => `
    <div class="category-item" style="display: flex; justify-content: space-between; align-items: center; padding: 10px; border-bottom: 1px solid rgba(255,255,255,0.05);">
      <span id="cat-name-${cat.id}">${cat.name}</span>
      <div style="display: flex; gap: 0.5rem;">
        <button onclick="handleEditCategory(${cat.id}, '${cat.name.replace(/'/g, "\\'")}')" class="edit-btn" style="padding: 4px 8px; font-size: 0.8rem; background: var(--primary);">Edit</button>
        <button onclick="handleDeleteCategory(${cat.id})" class="delete-btn" style="padding: 4px 8px; font-size: 0.8rem;">Delete</button>
      </div>
    </div>
  `).join('');
}

/**
 * Handle Edit Category
 */
async function handleEditCategory(id, currentName) {
  const newName = prompt('Enter new category name:', currentName);
  if (!newName || newName === currentName) return;

  try {
    await updateProductCategory(id, { name: newName });
    await loadCategories();
    await loadProducts(); // Refresh products to show new category name in cards/groups
    renderCategoryList();
    populateCategoryDropdown();
  } catch (error) {
    console.error('Error editing category:', error);
    alert('Failed to edit category');
  }
}

/**
 * Handle Add Category
 */
async function handleAddCategory() {
  const input = document.getElementById("newCategoryName");
  const name = input.value.trim();
  if (!name) return;

  try {
    await createProductCategory({ name });
    input.value = "";
    await loadCategories();
    renderCategoryList();
    populateCategoryDropdown();
  } catch (error) {
    console.error('Error adding category:', error);
    alert('Failed to add category');
  }
}

/**
 * Handle Delete Category
 */
async function handleDeleteCategory(id) {
  if (!confirm('Are you sure you want to delete this category?')) return;

  try {
    await deleteProductCategory(id);
    await loadCategories();
    renderCategoryList();
    populateCategoryDropdown();
  } catch (error) {
    console.error('Error deleting category:', error);
    alert('Failed to delete category');
  }
}

/**
 * Close product modal
 */
function closeProductModal() {
  const modal = document.getElementById("productModal");
  if (modal) {
    modal.classList.remove("show");
    const form = document.getElementById("productForm");
    if (form) form.reset();
  }
}

/**
 * Edit product
 */
function editProduct(id) {
  const product = products.find(p => p.id === id);
  if (!product) return;

  const modal = document.getElementById("productModal");
  const form = document.getElementById("productForm");
  const title = document.getElementById("modalTitle");

  if (modal && form && title) {
    populateCategoryDropdown();
    document.getElementById("productId").value = product.id;
    document.getElementById("productName").value = product.name;
    document.getElementById("productPrice").value = product.price;
    document.getElementById("productCategory").value = product.category || "";
    document.getElementById("productIsFavorite").value = product.isFavorite || false;
    title.innerText = "Edit Product";
    modal.classList.add("show");
  }
}

/**
 * Handle form submission
 */
async function handleFormSubmit(e) {
  e.preventDefault();

  const id = document.getElementById("productId").value;
  const name = document.getElementById("productName").value;
  const price = parseFloat(document.getElementById("productPrice").value);
  const category = document.getElementById("productCategory").value;
  const isFavorite = document.getElementById("productIsFavorite").value === "true";

  if (!name || !price || !category) {
    alert("Please fill in all fields");
    return;
  }

  const product = {
    name: name,
    price: price,
    category: category,
    isFavorite: isFavorite
  };

  try {
    if (id) {
      // Update existing product
      await updateProduct(id, product);
    } else {
      // Create new product
      await createProduct(product);
    }

    closeProductModal();
    await loadProducts(); // Reload products
  } catch (error) {
    console.error('Error saving product:', error);
    alert('Failed to save product. Please try again.');
  }
}

/**
 * Delete product
 */
async function deleteProductHandler(id) {
  const product = products.find(p => p.id === id);
  if (!product) return;

  if (!confirm(`Are you sure you want to delete "${product.name}"?`)) {
    return;
  }

  try {
    await deleteProduct(id);
    await loadProducts(); // Reload products
  } catch (error) {
    console.error('Error deleting product:', error);
    alert('Failed to delete product. Please try again.');
  }
}
// Profile initialization is now in common.js

