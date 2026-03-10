# Raksha API - Postman Testing Guide

## Start the Application

```bash
docker compose -f docker-compose.app.yml up --build -d
```

Verify all services are running:

```bash
docker compose -f docker-compose.app.yml ps
```

All 4 services (`raksha-postgres`, `raksha-mongodb`, `raksha-redis`, `raksha-api`) should show as **running/healthy**.

**Base URL:** `http://localhost:5000`

## Import Swagger into Postman

1. Open Postman
2. Click **Import**
3. Select **Link** and paste: `http://localhost:5000/swagger/v1/swagger.json`
4. Click **Import** — all endpoints will be added to a collection

## Seeded Admin Credentials

| Field    | Value              |
|----------|--------------------|
| Email    | `admin@raksha.com` |
| Password | `Admin12#`         |
| Role     | `Admin`            |

---

## 1. Auth Endpoints (`/api/auth`)

### 1.1 Login (Admin)

`POST /api/auth/login` — **Anonymous**

```json
{
  "email": "admin@raksha.com",
  "password": "Admin12#"
}
```

**Expected:** `200 OK` with `accessToken`, `refreshToken`, `userId`, `roles: ["Admin"]`

> Save the `accessToken` and `refreshToken` from the response. Set `accessToken` as the Bearer token in Postman's **Authorization** tab for subsequent requests.

### 1.2 Register New User

`POST /api/auth/register` — **Anonymous**

```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@example.com",
  "userName": "johndoe",
  "password": "Test123#"
}
```

**Expected:** `200 OK` with `accessToken`, `refreshToken`, `roles: ["User"]`

### 1.3 Refresh Token

`POST /api/auth/refresh-token` — **Anonymous**

```json
{
  "accessToken": "<expired-or-current-access-token>",
  "refreshToken": "<refresh-token-from-login>"
}
```

**Expected:** `200 OK` with new `accessToken` and `refreshToken`

### 1.4 Get Current User

`GET /api/auth/me` — **Authenticated**

No body. Set Bearer token in Authorization header.

**Expected:** `200 OK` with user profile (userId, email, userName, roles)

### 1.5 Change Password

`POST /api/auth/change-password` — **Authenticated**

```json
{
  "currentPassword": "Test123#",
  "newPassword": "NewPass123#"
}
```

**Expected:** `200 OK` with success message

### 1.6 Revoke Token (Logout)

`POST /api/auth/revoke-token` — **Authenticated**

```json
{
  "refreshToken": "<refresh-token-from-login>"
}
```

**Expected:** `200 OK` — token is invalidated. Subsequent requests with the old access token will return 401.

---

## 2. User Management Endpoints (`/api/users`)

> All admin-only endpoints require the **Admin** user's access token.

### 2.1 Create User

`POST /api/users` — **Admin Only**

```json
{
  "firstName": "Jane",
  "lastName": "Smith",
  "email": "jane@example.com",
  "userName": "janesmith",
  "password": "Test123#",
  "role": "User"
}
```

**Expected:** `200 OK` with created user details

### 2.2 Get User by ID

`GET /api/users/{id}` — **Authenticated**

Replace `{id}` with a GUID from a previous response.

**Expected:** `200 OK` with user details

### 2.3 Get All Users (Paginated + Filtered)

`GET /api/users` — **Admin Only**

Query parameters (all optional):

| Parameter    | Example Value | Description              |
|--------------|---------------|--------------------------|
| `searchTerm` | `john`        | Search by name/email     |
| `status`     | `Active`      | Filter by status (Active, Inactive, Deleted) |
| `role`       | `User`        | Filter by role           |
| `page`       | `1`           | Page number              |
| `pageSize`   | `10`          | Items per page           |

**Example:** `GET /api/users?status=Active&role=User&page=1&pageSize=10`

**Expected:** `200 OK` with paginated list of users

### 2.4 Update User Profile

`PUT /api/users/{id}/profile` — **Authenticated**

```json
{
  "firstName": "Jane",
  "lastName": "Updated"
}
```

**Expected:** `200 OK` with updated user details

### 2.5 Activate User

`PUT /api/users/{id}/activate` — **Admin Only**

No body.

**Expected:** `200 OK`

### 2.6 Deactivate User

`PUT /api/users/{id}/deactivate` — **Admin Only**

No body.

**Expected:** `200 OK`

### 2.7 Delete User (Soft Delete)

`DELETE /api/users/{id}` — **Admin Only**

No body.

**Expected:** `200 OK`

### 2.8 Assign Role

`POST /api/users/{id}/roles/{role}` — **Admin Only**

Replace `{role}` with `Admin` or `User`.

**Expected:** `200 OK`

### 2.9 Remove Role

`DELETE /api/users/{id}/roles/{role}` — **Admin Only**

**Expected:** `200 OK`

### 2.10 Force Logout

`POST /api/users/{id}/force-logout` — **Admin Only**

No body. Blacklists all active tokens for the user.

**Expected:** `200 OK`

---

## 3. Profile Endpoints (`/api/profile`)

> All endpoints require authentication (any role).

### 3.1 Get My Profile

`GET /api/profile` — **Authenticated**

**Expected:** `200 OK` with current user's profile

### 3.2 Update My Profile

`PUT /api/profile` — **Authenticated**

```json
{
  "firstName": "UpdatedFirst",
  "lastName": "UpdatedLast"
}
```

**Expected:** `200 OK` with updated profile

### 3.3 Upload Profile Picture

`POST /api/profile/picture` — **Authenticated**

In Postman: Set body to **form-data**, add key `file` of type **File**, select an image (`.jpg`, `.jpeg`, or `.png`, max 2MB).

**Expected:** `200 OK` with image key

### 3.4 Download Profile Picture

`GET /api/profile/picture` — **Authenticated**

**Expected:** `200 OK` with image file stream (save response to file in Postman)

---

## 4. Audit Endpoints (`/api/audit`)

> All endpoints are **Admin Only**.

### 4.1 Password Change History

`GET /api/audit/password-changes` — **Admin Only**

Query parameters (all optional):

| Parameter  | Example Value | Description         |
|------------|---------------|---------------------|
| `userId`   | `<guid>`      | Filter by user      |
| `page`     | `1`           | Page number         |
| `pageSize` | `10`          | Items per page      |

**Expected:** `200 OK` with paginated list of password change audit logs

### 4.2 Profile Update History

`GET /api/audit/profile-updates` — **Admin Only**

Same query parameters as above.

**Expected:** `200 OK` with paginated list of profile update audit logs

---

## 5. Expected Error Responses

All errors return a consistent JSON format:

```json
{
  "isSuccess": false,
  "message": "Error description",
  "data": null
}
```

| Scenario                                | Status | Message                                                       |
|-----------------------------------------|--------|---------------------------------------------------------------|
| No token on protected endpoint          | `401`  | "You are not authenticated. Please provide a valid token."    |
| Valid token, insufficient role           | `403`  | "You do not have permission to access this resource."         |
| Blacklisted token (after revoke/logout) | `401`  | "Your session has been invalidated. Please login again."      |
| Deactivated user tries to login         | `400`  | "Your account has been deactivated. Please contact support."  |
| Deleted user tries to login             | `400`  | "Invalid email or password."                                  |
| Invalid credentials                     | `400`  | "Invalid email or password."                                  |

---

## 6. Authorization Test Scenarios

### Scenario A: Unauthenticated Access

1. Remove the Bearer token from Postman
2. Call `GET /api/profile`
3. **Expected:** `401` with JSON error

### Scenario B: Insufficient Role

1. Login as a regular User (e.g., `john@example.com`)
2. Set their access token as Bearer
3. Call `GET /api/users` (Admin-only)
4. **Expected:** `403` with JSON error

### Scenario C: Deactivated User

1. Login as Admin, deactivate a user: `PUT /api/users/{id}/deactivate`
2. Try to login as the deactivated user
3. **Expected:** `400` — account deactivated message

### Scenario D: Token Revocation

1. Login as any user, save `accessToken` and `refreshToken`
2. Revoke: `POST /api/auth/revoke-token` with the `refreshToken`
3. Use the old `accessToken` to call `GET /api/profile`
4. **Expected:** `401` — session invalidated

### Scenario E: Force Logout

1. Login as User, save their access token
2. Login as Admin, call `POST /api/users/{userId}/force-logout`
3. Use the User's old access token to call `GET /api/profile`
4. **Expected:** `401` — session invalidated
