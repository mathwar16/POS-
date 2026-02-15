# RestoPOS API Documentation

## Overview

This document provides comprehensive API documentation for the RestoPOS backend built with **ASP.NET Core Web API**, **Entity Framework Core**, and **SQL Server**.

**Base URL**: `https://localhost:5001/api`

---

## Table of Contents

1. [Database Schema](#database-schema)
2. [Entity Models](#entity-models)
3. [API Endpoints](#api-endpoints)
   - [Authentication API](#authentication-api)
   - [Products API](#products-api)
   - [Bills API](#bills-api)
   - [Dashboard API](#dashboard-api)
4. [Data Transfer Objects (DTOs)](#data-transfer-objects-dtos)
5. [Authentication & Authorization](#authentication--authorization)
6. [Implementation Guide](#implementation-guide)
7. [Database Configuration](#database-configuration)

---

## Database Schema

### Users Table

```sql
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(200) NOT NULL,
    Email NVARCHAR(255) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1
);

CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_IsActive ON Users(IsActive);
```

### Products Table

```sql
CREATE TABLE Products (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    Category NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1,
    
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_Products_UserId ON Products(UserId);
CREATE INDEX IX_Products_Category ON Products(Category);
CREATE INDEX IX_Products_IsActive ON Products(IsActive);
```

### Bills Table

```sql
CREATE TABLE Bills (
    Id INT PRIMARY KEY IDENTITY(1,1),
    BillNumber NVARCHAR(50) UNIQUE NOT NULL,
    Subtotal DECIMAL(18,2) NOT NULL,
    Gst DECIMAL(18,2) NOT NULL,
    ServiceCharge DECIMAL(18,2) NOT NULL,
    Total DECIMAL(18,2) NOT NULL,
    PaymentMethod NVARCHAR(20) NOT NULL, -- CASH, UPI, CARD
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

CREATE INDEX IX_Bills_CreatedAt ON Bills(CreatedAt);
CREATE INDEX IX_Bills_PaymentMethod ON Bills(PaymentMethod);
```

### BillItems Table

```sql
CREATE TABLE BillItems (
    Id INT PRIMARY KEY IDENTITY(1,1),
    BillId INT NOT NULL,
    ProductId INT NOT NULL,
    ProductName NVARCHAR(200) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    Total DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    
    FOREIGN KEY (BillId) REFERENCES Bills(Id) ON DELETE CASCADE,
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

CREATE INDEX IX_BillItems_BillId ON BillItems(BillId);
CREATE INDEX IX_BillItems_ProductId ON BillItems(ProductId);
```

---

## Entity Models

### User Entity

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestoPOS.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();
    }
}
```

### Product Entity

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestoPOS.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty; // Veg, Non-Veg, Beverage, Dessert

        [Required]
        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        public virtual ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();
    }
}
```

### Bill Entity

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestoPOS.Models
{
    public class Bill
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string BillNumber { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Gst { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceCharge { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [Required]
        [MaxLength(20)]
        public string PaymentMethod { get; set; } = string.Empty; // CASH, UPI, CARD

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        public virtual ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();
    }
}
```

### BillItem Entity

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestoPOS.Models
{
    public class BillItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BillId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("BillId")]
        public virtual Bill Bill { get; set; } = null!;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }
}
```

---

## API Endpoints

### Authentication API

All endpoints require authentication except `/api/auth/signup` and `/api/auth/login`.

#### 1. User Signup

**POST** `/api/auth/signup`

**Description**: Register a new user account.

**Request Body**:
```json
{
  "name": "John Doe",
  "email": "john.doe@example.com",
  "password": "SecurePassword123!"
}
```

**Validation**:
- `name`: Required, MaxLength(200)
- `email`: Required, Valid email format, Unique
- `password`: Required, MinLength(6)

**Response**: `201 Created`

```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john.doe@example.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "createdAt": "2026-01-18T10:30:00Z"
}
```

**Error Response**: `400 Bad Request`

```json
{
  "errors": {
    "email": ["Email already exists"],
    "password": ["Password must be at least 6 characters"]
  }
}
```

**Implementation**:
```csharp
[HttpPost("signup")]
public async Task<ActionResult<AuthResponseDto>> Signup(SignupDto dto)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    // Check if email already exists
    if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
    {
        ModelState.AddModelError("email", "Email already exists");
        return BadRequest(ModelState);
    }

    // Hash password
    var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

    var user = new User
    {
        Name = dto.Name,
        Email = dto.Email.ToLower(),
        PasswordHash = passwordHash,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsActive = true
    };

    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    // Generate JWT token
    var token = GenerateJwtToken(user);

    return CreatedAtAction(nameof(GetUser), new { id = user.Id }, 
        new AuthResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Token = token,
            CreatedAt = user.CreatedAt
        });
}
```

---

#### 2. User Login

**POST** `/api/auth/login`

**Description**: Authenticate user and return JWT token.

**Request Body**:
```json
{
  "email": "john.doe@example.com",
  "password": "SecurePassword123!"
}
```

**Validation**:
- `email`: Required, Valid email format
- `password`: Required

**Response**: `200 OK`

```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john.doe@example.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "createdAt": "2026-01-18T10:30:00Z"
}
```

**Error Response**: `401 Unauthorized`

```json
{
  "error": "Invalid email or password"
}
```

**Implementation**:
```csharp
[HttpPost("login")]
public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    var user = await _context.Users
        .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower() && u.IsActive);

    if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
    {
        return Unauthorized(new { error = "Invalid email or password" });
    }

    // Generate JWT token
    var token = GenerateJwtToken(user);

    return Ok(new AuthResponseDto
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
        Token = token,
        CreatedAt = user.CreatedAt
    });
}
```

---

#### 3. Get Current User

**GET** `/api/auth/me`

**Description**: Get current authenticated user information.

**Headers**:
- `Authorization: Bearer {token}`

**Response**: `200 OK`

```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john.doe@example.com",
  "createdAt": "2026-01-18T10:30:00Z"
}
```

**Error Response**: `401 Unauthorized`

**Implementation**:
```csharp
[HttpGet("me")]
[Authorize]
public async Task<ActionResult<UserDto>> GetCurrentUser()
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    
    var user = await _context.Users.FindAsync(userId);
    
    if (user == null || !user.IsActive)
        return NotFound();

    return Ok(new UserDto
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
        CreatedAt = user.CreatedAt
    });
}
```

---

### Products API

**Note**: All Products API endpoints require authentication and return only products belonging to the authenticated user.

#### 1. Get All Products

**GET** `/api/products`

**Description**: Retrieves all active products for the authenticated user.

**Headers**:
- `Authorization: Bearer {token}`

**Response**: `200 OK`

```json
[
  {
    "id": 1,
    "name": "Paneer Butter Masala",
    "price": 280.00,
    "category": "Veg",
    "userId": 1,
    "createdAt": "2026-01-18T10:30:00Z",
    "updatedAt": "2026-01-18T10:30:00Z",
    "isActive": true
  },
  {
    "id": 2,
    "name": "Chicken Biryani",
    "price": 320.00,
    "category": "Non-Veg",
    "userId": 1,
    "createdAt": "2026-01-18T10:30:00Z",
    "updatedAt": "2026-01-18T10:30:00Z",
    "isActive": true
  }
]
```

**Implementation**:
```csharp
[HttpGet]
[Authorize]
public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    
    var products = await _context.Products
        .Where(p => p.UserId == userId && p.IsActive)
        .OrderBy(p => p.Name)
        .ToListAsync();
    
    return Ok(products.Select(p => new ProductDto
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price,
        Category = p.Category,
        UserId = p.UserId
    }));
}
```

---

#### 2. Get Product by ID

**GET** `/api/products/{id}`

**Headers**:
- `Authorization: Bearer {token}`

**Parameters**:
- `id` (int, path): Product ID

**Response**: `200 OK`

```json
{
  "id": 1,
  "name": "Paneer Butter Masala",
  "price": 280.00,
  "category": "Veg",
  "userId": 1
}
```

**Error Response**: `404 Not Found` or `403 Forbidden` (if product belongs to another user)

**Implementation**:
```csharp
[HttpGet("{id}")]
[Authorize]
public async Task<ActionResult<ProductDto>> GetProduct(int id)
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    
    var product = await _context.Products
        .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId && p.IsActive);
    
    if (product == null)
        return NotFound();
    
    return Ok(new ProductDto
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price,
        Category = product.Category,
        UserId = product.UserId
    });
}
```

---

#### 3. Create Product

**POST** `/api/products`

**Headers**:
- `Authorization: Bearer {token}`

**Request Body**:
```json
{
  "name": "Butter Naan",
  "price": 40.00,
  "category": "Veg"
}
```

**Response**: `201 Created`

```json
{
  "id": 5,
  "name": "Butter Naan",
  "price": 40.00,
  "category": "Veg",
  "userId": 1,
  "createdAt": "2026-01-18T11:00:00Z",
  "updatedAt": "2026-01-18T11:00:00Z",
  "isActive": true
}
```

**Validation**:
- `name`: Required, MaxLength(200)
- `price`: Required, Must be > 0
- `category`: Required, MaxLength(50)

**Error Response**: `400 Bad Request`

**Implementation**:
```csharp
[HttpPost]
[Authorize]
public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto dto)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    var product = new Product
    {
        Name = dto.Name,
        Price = dto.Price,
        Category = dto.Category,
        UserId = userId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsActive = true
    };

    _context.Products.Add(product);
    await _context.SaveChangesAsync();

    return CreatedAtAction(nameof(GetProduct), new { id = product.Id },
        new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Category = product.Category,
            UserId = product.UserId,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            IsActive = product.IsActive
        });
}
```

```json
{
  "errors": {
    "name": ["The Name field is required."],
    "price": ["Price must be greater than 0."]
  }
}
```

**Implementation**:
```csharp
[HttpPost]
public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto dto)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    var product = new Product
    {
        Name = dto.Name,
        Price = dto.Price,
        Category = dto.Category,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsActive = true
    };

    _context.Products.Add(product);
    await _context.SaveChangesAsync();

    return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, 
        new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Category = product.Category
        });
}
```

---

#### 4. Update Product

**PUT** `/api/products/{id}`

**Parameters**:
- `id` (int, path): Product ID

**Request Body**:
```json
{
  "name": "Paneer Butter Masala (Large)",
  "price": 320.00,
  "category": "Veg"
}
```

**Response**: `200 OK`

```json
{
  "id": 1,
  "name": "Paneer Butter Masala (Large)",
  "price": 320.00,
  "category": "Veg",
  "updatedAt": "2026-01-18T11:15:00Z"
}
```

**Error Response**: `404 Not Found` or `400 Bad Request`

**Implementation**:
```csharp
[HttpPut("{id}")]
public async Task<ActionResult<ProductDto>> UpdateProduct(int id, UpdateProductDto dto)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    var product = await _context.Products.FindAsync(id);
    if (product == null)
        return NotFound();

    product.Name = dto.Name;
    product.Price = dto.Price;
    product.Category = dto.Category;
    product.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    return Ok(new ProductDto
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price,
        Category = product.Category
    });
}
```

---

#### 5. Delete Product

**DELETE** `/api/products/{id}`

**Headers**:
- `Authorization: Bearer {token}`

**Parameters**:
- `id` (int, path): Product ID

**Response**: `204 No Content`

**Error Response**: `404 Not Found` or `403 Forbidden` (if product belongs to another user)

**Implementation**:
```csharp
[HttpDelete("{id}")]
[Authorize]
public async Task<IActionResult> DeleteProduct(int id)
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    
    var product = await _context.Products
        .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
    
    if (product == null)
        return NotFound();

    // Soft delete - set IsActive to false
    product.IsActive = false;
    product.UpdatedAt = DateTime.UtcNow;
    
    await _context.SaveChangesAsync();

    return NoContent();
}
```

---

### Bills API

**Note**: All Bills API endpoints require authentication and return only bills belonging to the authenticated user.

#### 1. Create Bill

**POST** `/api/bills`

**Headers**:
- `Authorization: Bearer {token}`

**Request Body**:
```json
{
  "items": [
    {
      "id": 1,
      "name": "Paneer Butter Masala",
      "price": 280.00,
      "quantity": 2,
      "total": 560.00
    },
    {
      "id": 3,
      "name": "Coca Cola",
      "price": 50.00,
      "quantity": 2,
      "total": 100.00
    }
  ],
  "subtotal": 660.00,
  "gst": 33.00,
  "service": 33.00,
  "total": 726.00,
  "paymentMethod": "CASH",
  "date": "2026-01-18T11:30:00Z"
}
```

**Response**: `201 Created`

```json
{
  "id": 1,
  "billNumber": "BILL-20260118-001",
  "subtotal": 660.00,
  "gst": 33.00,
  "serviceCharge": 33.00,
  "total": 726.00,
  "paymentMethod": "CASH",
  "createdAt": "2026-01-18T11:30:00Z",
  "items": [
    {
      "id": 1,
      "productId": 1,
      "productName": "Paneer Butter Masala",
      "price": 280.00,
      "quantity": 2,
      "total": 560.00
    },
    {
      "id": 2,
      "productId": 3,
      "productName": "Coca Cola",
      "price": 50.00,
      "quantity": 2,
      "total": 100.00
    }
  ]
}
```

**Implementation**:
```csharp
[HttpPost]
public async Task<ActionResult<BillDto>> CreateBill(CreateBillDto dto)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    // Generate bill number
    var billNumber = GenerateBillNumber();

    var bill = new Bill
    {
        BillNumber = billNumber,
        Subtotal = dto.Subtotal,
        Gst = dto.Gst,
        ServiceCharge = dto.Service,
        Total = dto.Total,
        PaymentMethod = dto.PaymentMethod,
        CreatedAt = dto.Date,
        UpdatedAt = DateTime.UtcNow
    };

    // Add bill items
    foreach (var itemDto in dto.Items)
    {
        var billItem = new BillItem
        {
            Bill = bill,
            ProductId = itemDto.Id,
            ProductName = itemDto.Name,
            Price = itemDto.Price,
            Quantity = itemDto.Quantity,
            Total = itemDto.Total,
            CreatedAt = DateTime.UtcNow
        };
        
        bill.BillItems.Add(billItem);
    }

    _context.Bills.Add(bill);
    await _context.SaveChangesAsync();

    // Load bill with items for response
    await _context.Entry(bill)
        .Collection(b => b.BillItems)
        .LoadAsync();

    return CreatedAtAction(nameof(GetBill), new { id = bill.Id }, 
        new BillDto
        {
            Id = bill.Id,
            BillNumber = bill.BillNumber,
            UserId = bill.UserId,
            Subtotal = bill.Subtotal,
            Gst = bill.Gst,
            ServiceCharge = bill.ServiceCharge,
            Total = bill.Total,
            PaymentMethod = bill.PaymentMethod,
            CreatedAt = bill.CreatedAt,
            Items = bill.BillItems.Select(i => new BillItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Price = i.Price,
                Quantity = i.Quantity,
                Total = i.Total
            }).ToList()
        });
}

private string GenerateBillNumber()
{
    var date = DateTime.UtcNow.ToString("yyyyMMdd");
    var count = _context.Bills
        .Count(b => b.CreatedAt.Date == DateTime.UtcNow.Date) + 1;
    return $"BILL-{date}-{count:D3}";
}
```

---

#### 2. Get Bill by ID

**GET** `/api/bills/{id}`

**Parameters**:
- `id` (int, path): Bill ID

**Response**: `200 OK`

```json
{
  "id": 1,
  "billNumber": "BILL-20260118-001",
  "subtotal": 660.00,
  "gst": 33.00,
  "serviceCharge": 33.00,
  "total": 726.00,
  "paymentMethod": "CASH",
  "createdAt": "2026-01-18T11:30:00Z",
  "items": [...]
}
```

---

#### 3. Get All Bills

**GET** `/api/bills`

**Query Parameters**:
- `page` (int, optional): Page number (default: 1)
- `pageSize` (int, optional): Items per page (default: 20)
- `startDate` (datetime, optional): Filter by start date
- `endDate` (datetime, optional): Filter by end date

**Response**: `200 OK`

```json
{
  "items": [...],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8
}
```

---

### Dashboard API

**Note**: All Dashboard API endpoints require authentication and return only data belonging to the authenticated user.

#### 1. Get Today's Dashboard Data

**GET** `/api/dashboard/today`

**Headers**:
- `Authorization: Bearer {token}`

**Description**: Retrieves today's sales statistics, payment method breakdown, and recent orders for the authenticated user.

**Response**: `200 OK`

```json
{
  "revenue": 1430.00,
  "orders": 3,
  "avgOrder": 476.67,
  "platforms": [
    {
      "method": "CASH",
      "amount": 800.00,
      "count": 2
    },
    {
      "method": "UPI",
      "amount": 430.00,
      "count": 1
    },
    {
      "method": "CARD",
      "amount": 200.00,
      "count": 0
    }
  ],
  "recentOrders": [
    {
      "id": 3,
      "billNumber": "BILL-20260118-003",
      "total": 726.00,
      "paymentMethod": "CASH",
      "date": "2026-01-18T14:30:00Z"
    },
    {
      "id": 2,
      "billNumber": "BILL-20260118-002",
      "total": 430.00,
      "paymentMethod": "UPI",
      "date": "2026-01-18T13:15:00Z"
    },
    {
      "id": 1,
      "billNumber": "BILL-20260118-001",
      "total": 274.00,
      "paymentMethod": "CASH",
      "date": "2026-01-18T11:30:00Z"
    }
  ]
}
```

**Implementation**:
```csharp
[HttpGet("today")]
public async Task<ActionResult<DashboardDto>> GetTodayDashboard()
{
    var today = DateTime.UtcNow.Date;
    var tomorrow = today.AddDays(1);

    var bills = await _context.Bills
        .Where(b => b.CreatedAt >= today && b.CreatedAt < tomorrow)
        .ToListAsync();

    var revenue = bills.Sum(b => b.Total);
    var orders = bills.Count;
    var avgOrder = orders > 0 ? revenue / orders : 0;

    // Payment method breakdown
    var platforms = bills
        .GroupBy(b => b.PaymentMethod)
        .Select(g => new PaymentMethodDto
        {
            Method = g.Key,
            Amount = g.Sum(b => b.Total),
            Count = g.Count()
        })
        .ToList();

    // Recent orders (last 10)
    var recentOrders = bills
        .OrderByDescending(b => b.CreatedAt)
        .Take(10)
        .Select(b => new RecentOrderDto
        {
            Id = b.Id,
            BillNumber = b.BillNumber,
            Total = b.Total,
            PaymentMethod = b.PaymentMethod,
            Date = b.CreatedAt
        })
        .ToList();

    return Ok(new DashboardDto
    {
        Revenue = revenue,
        Orders = orders,
        AvgOrder = avgOrder,
        Platforms = platforms,
        RecentOrders = recentOrders
    });
}
```

---

## Data Transfer Objects (DTOs)

### SignupDto

```csharp
namespace RestoPOS.DTOs
{
    public class SignupDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }
}
```

### LoginDto

```csharp
namespace RestoPOS.DTOs
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
```

### AuthResponseDto

```csharp
namespace RestoPOS.DTOs
{
    public class AuthResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
```

### UserDto

```csharp
namespace RestoPOS.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
```

### ProductDto

```csharp
namespace RestoPOS.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
```

### CreateProductDto

```csharp
namespace RestoPOS.DTOs
{
    public class CreateProductDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;
    }
}
```

### UpdateProductDto

```csharp
namespace RestoPOS.DTOs
{
    public class UpdateProductDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;
    }
}
```

### CreateBillDto

```csharp
namespace RestoPOS.DTOs
{
    public class CreateBillDto
    {
        [Required]
        public List<BillItemDto> Items { get; set; } = new();

        [Required]
        public decimal Subtotal { get; set; }

        [Required]
        public decimal Gst { get; set; }

        [Required]
        public decimal Service { get; set; }

        [Required]
        public decimal Total { get; set; }

        [Required]
        [MaxLength(20)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }
    }

    public class BillItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get; set; }
    }
}
```

### BillDto

```csharp
namespace RestoPOS.DTOs
{
    public class BillDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Gst { get; set; }
        public decimal ServiceCharge { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<BillItemResponseDto> Items { get; set; } = new();
    }

    public class BillItemResponseDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get; set; }
    }
}
```

### DashboardDto

```csharp
namespace RestoPOS.DTOs
{
    public class DashboardDto
    {
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
        public decimal AvgOrder { get; set; }
        public List<PaymentMethodDto> Platforms { get; set; } = new();
        public List<RecentOrderDto> RecentOrders { get; set; } = new();
    }

    public class PaymentMethodDto
    {
        public string Method { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class RecentOrderDto
    {
        public int Id { get; set; }
        public string BillNumber { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}
```

---

## Implementation Guide

### 1. Project Setup

```bash
# Create new Web API project
dotnet new webapi -n RestoPOS.API

# Add Entity Framework Core packages
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.Tools

# Add JWT Authentication packages
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package System.IdentityModel.Tokens.Jwt

# Add BCrypt for password hashing
dotnet add package BCrypt.Net-Next
```

### 2. DbContext Configuration

**ApplicationDbContext.cs**:

```csharp
using Microsoft.EntityFrameworkCore;
using RestoPOS.Models;

namespace RestoPOS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<BillItem> BillItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.IsActive);
            });

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasIndex(p => p.UserId);
                entity.HasIndex(p => p.Category);
                entity.HasIndex(p => p.IsActive);
                
                entity.HasOne(p => p.User)
                    .WithMany(u => u.Products)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Bill configuration
            modelBuilder.Entity<Bill>(entity =>
            {
                entity.HasIndex(b => b.UserId);
                entity.HasIndex(b => b.CreatedAt);
                entity.HasIndex(b => b.PaymentMethod);
                entity.HasIndex(b => new { b.UserId, b.BillNumber }).IsUnique();
                
                entity.HasOne(b => b.User)
                    .WithMany(u => u.Bills)
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // BillItem configuration
            modelBuilder.Entity<BillItem>(entity =>
            {
                entity.HasOne(bi => bi.Bill)
                    .WithMany(b => b.BillItems)
                    .HasForeignKey(bi => bi.BillId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(bi => bi.Product)
                    .WithMany(p => p.BillItems)
                    .HasForeignKey(bi => bi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(bi => bi.BillId);
                entity.HasIndex(bi => bi.ProductId);
            });
        }
    }
}
```

### 3. Program.cs Configuration

```csharp
using Microsoft.EntityFrameworkCore;
using RestoPOS.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:5500")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

// Run database migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();
```

### 4. appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=RestoPOSDb;Trusted_Connection=true;TrustServerCertificate=true;"
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "RestoPOS",
    "Audience": "RestoPOSUsers"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**Note**: In production, store the JWT Key in environment variables or Azure Key Vault. Never commit secrets to source control.

### 5. Controllers

**ProductsController.cs**:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RestoPOS.Data;
using RestoPOS.DTOs;
using RestoPOS.Models;

namespace RestoPOS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var products = await _context.Products
                .Where(p => p.UserId == userId && p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return Ok(products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Category = p.Category
            }));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null || !product.IsActive)
                return NotFound();

            return Ok(new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Category = product.Category
            });
        }

        [HttpPost]
        public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = new Product
            {
                Name = dto.Name,
                Price = dto.Price,
                Category = dto.Category,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id },
                new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Category = product.Category
                });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ProductDto>> UpdateProduct(int id, UpdateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            product.Name = dto.Name;
            product.Price = dto.Price;
            product.Category = dto.Category;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Category = product.Category
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
```

**BillsController.cs**:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestoPOS.Data;
using RestoPOS.DTOs;
using RestoPOS.Models;

namespace RestoPOS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BillsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BillsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<BillDto>> CreateBill(CreateBillDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var billNumber = GenerateBillNumber();

            var bill = new Bill
            {
                BillNumber = billNumber,
                Subtotal = dto.Subtotal,
                Gst = dto.Gst,
                ServiceCharge = dto.Service,
                Total = dto.Total,
                PaymentMethod = dto.PaymentMethod,
                CreatedAt = dto.Date,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var itemDto in dto.Items)
            {
                var billItem = new BillItem
                {
                    Bill = bill,
                    ProductId = itemDto.Id,
                    ProductName = itemDto.Name,
                    Price = itemDto.Price,
                    Quantity = itemDto.Quantity,
                    Total = itemDto.Total,
                    CreatedAt = DateTime.UtcNow
                };

                bill.BillItems.Add(billItem);
            }

            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();

            await _context.Entry(bill)
                .Collection(b => b.BillItems)
                .LoadAsync();

            return CreatedAtAction(nameof(GetBill), new { id = bill.Id },
                new BillDto
                {
                    Id = bill.Id,
                    BillNumber = bill.BillNumber,
                    Subtotal = bill.Subtotal,
                    Gst = bill.Gst,
                    ServiceCharge = bill.ServiceCharge,
                    Total = bill.Total,
                    PaymentMethod = bill.PaymentMethod,
                    CreatedAt = bill.CreatedAt,
                    Items = bill.BillItems.Select(i => new BillItemResponseDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Price = i.Price,
                        Quantity = i.Quantity,
                        Total = i.Total
                    }).ToList()
                });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BillDto>> GetBill(int id)
        {
            var bill = await _context.Bills
                .Include(b => b.BillItems)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bill == null)
                return NotFound();

            return Ok(new BillDto
            {
                Id = bill.Id,
                BillNumber = bill.BillNumber,
                Subtotal = bill.Subtotal,
                Gst = bill.Gst,
                ServiceCharge = bill.ServiceCharge,
                Total = bill.Total,
                PaymentMethod = bill.PaymentMethod,
                CreatedAt = bill.CreatedAt,
                Items = bill.BillItems.Select(i => new BillItemResponseDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Price = i.Price,
                    Quantity = i.Quantity,
                    Total = i.Total
                }).ToList()
            });
        }

        private string GenerateBillNumber()
        {
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var count = _context.Bills
                .Count(b => b.CreatedAt.Date == DateTime.UtcNow.Date) + 1;
            return $"BILL-{date}-{count:D3}";
        }
    }
}
```

**DashboardController.cs**:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestoPOS.Data;
using RestoPOS.DTOs;

namespace RestoPOS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("today")]
        public async Task<ActionResult<DashboardDto>> GetTodayDashboard()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var bills = await _context.Bills
                .Where(b => b.CreatedAt >= today && b.CreatedAt < tomorrow)
                .ToListAsync();

            var revenue = bills.Sum(b => b.Total);
            var orders = bills.Count;
            var avgOrder = orders > 0 ? revenue / orders : 0;

            var platforms = bills
                .GroupBy(b => b.PaymentMethod)
                .Select(g => new PaymentMethodDto
                {
                    Method = g.Key,
                    Amount = g.Sum(b => b.Total),
                    Count = g.Count()
                })
                .ToList();

            var recentOrders = bills
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .Select(b => new RecentOrderDto
                {
                    Id = b.Id,
                    BillNumber = b.BillNumber,
                    Total = b.Total,
                    PaymentMethod = b.PaymentMethod,
                    Date = b.CreatedAt
                })
                .ToList();

            return Ok(new DashboardDto
            {
                Revenue = revenue,
                Orders = orders,
                AvgOrder = avgOrder,
                Platforms = platforms,
                RecentOrders = recentOrders
            });
        }
    }
}
```

---

## Database Configuration

### Migration Commands

```bash
# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migrations to database
dotnet ef database update

# Remove last migration (if needed)
dotnet ef migrations remove
```

### Seed Data (Optional)

Create a `DbInitializer.cs`:

```csharp
using RestoPOS.Data;
using RestoPOS.Models;

namespace RestoPOS
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Products.Any())
                return; // DB has been seeded

            var products = new Product[]
            {
                new Product { Name = "Paneer Butter Masala", Price = 280, Category = "Veg" },
                new Product { Name = "Chicken Biryani", Price = 320, Category = "Non-Veg" },
                new Product { Name = "Coca Cola", Price = 50, Category = "Beverage" },
                new Product { Name = "Gulab Jamun", Price = 80, Category = "Dessert" }
            };

            context.Products.AddRange(products);
            context.SaveChanges();
        }
    }
}
```

---

## Error Handling

### Global Exception Handler

```csharp
// Add to Program.cs
app.UseExceptionHandler("/error");

// Create ErrorController
[ApiController]
[Route("error")]
public class ErrorController : ControllerBase
{
    [HttpGet]
    public IActionResult Error()
    {
        return Problem();
    }
}
```

---

## Testing

### Postman Collection

Import the following endpoints for testing:

1. `GET /api/products`
2. `POST /api/products`
3. `PUT /api/products/{id}`
4. `DELETE /api/products/{id}`
5. `POST /api/bills`
6. `GET /api/dashboard/today`

---

## Notes

- All monetary values use `decimal(18,2)` for precision
- Dates are stored in UTC
- Bill numbers are auto-generated with format: `BILL-YYYYMMDD-XXX`
- Products use soft delete (IsActive flag)
- CORS is configured for frontend integration
- All endpoints return JSON
- Validation is handled via Data Annotations

---

## Support

For issues or questions, refer to:
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Web API Documentation](https://docs.microsoft.com/en-us/aspnet/core/web-api/)
