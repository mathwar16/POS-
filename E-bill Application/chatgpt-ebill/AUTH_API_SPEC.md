# Backend Authentication API Specification

This document outlines the API contracts required by the frontend application for the Login and Signup flows. The backend must implement these endpoints exactly as described to ensure compatibility with the frontend (`login.html`, `signup.html`, and `js/api.js`).

**Base URL**: `https://localhost:5001/api`

---

## 1. User Login

Authenticate a user and return a JWT token.

- **Endpoint**: `POST /auth/login`
- **Content-Type**: `application/json`

### Request Body
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

### Success Response (200 OK)
The response **must** include `token`, `id`, `name`, and `email`.

```json
{
  "id": 1,
  "name": "User Name",
  "email": "user@example.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

### Error Response (401 Unauthorized)
```json
{
  "error": "Invalid email or password"
}
```

### Error Response (400 Bad Request)
If validation fails.
```json
{
  "error": "Validation failed message"
}
```

---

## 2. User Signup

Register a new user and automatically log them in (return the token).

- **Endpoint**: `POST /auth/signup`
- **Content-Type**: `application/json`

### Request Body
```json
{
  "name": "User Name",
  "email": "user@example.com",
  "password": "password123"
}
```

### Success Response (200 OK or 201 Created)
The response **must** include `token`, `id`, `name`, and `email` to allow auto-login after signup.

```json
{
  "id": 1,
  "name": "User Name",
  "email": "user@example.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

### Error Response (400 Bad Request)
If the email already exists or validation fails. The frontend expects either a simple `error` field or an `errors` object (ASP.NET default).

Format A (Simple):
```json
{
  "error": "Email already exists"
}
```

Format B (Validation Errors):
```json
{
  "errors": {
    "email": ["Email already exists"],
    "password": ["Password is too short"]
  }
}
```

---

## 3. Secured Endpoints (General Rule)

All other API endpoints (Products, Bills, Dashboard) must enforce authentication.

- **Header Required**:
  ```
  Authorization: Bearer <valid_jwt_token>
  ```

- **Unauthorized Response (401)**:
  If the token is missing, invalid, or expired, the API must return `401 Unauthorized`. The frontend is configured to automatically redirect the user to `login.html` upon receiving a 401 response.
