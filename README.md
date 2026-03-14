# Rently.Management

Backend for the “Management” part of the mobile app (Flutter). It provides JWT authentication, Paymob payments (card iframe and wallet redirect), account management, and core admin dashboards and resources (Users, Cars, Bookings, Requests, Payments).

## Quick Start
- Run locally: `dotnet run --project .\Rently.Management.csproj`
- Swagger: http://localhost:5000/swagger
- Development settings: WebApi/appsettings.Development.json

## Documentation
- Architecture and diagrams: docs/ARCHITECTURE.md (ER diagram, class diagram, endpoint workflows)

## Tech Stack
- .NET 9, ASP.NET Core Web API
- Entity Framework Core (SQL Server)
- JWT Bearer Authentication
- Swashbuckle/Swagger
- Paymob payment gateway integration

## Project Structure
- Domain/Entities: Core domain models (User, Car, Booking, Payment, etc.)
- Infrastructure: EF Core DbContext, repositories, configurations, migrations
- WebApi/Controllers: API endpoints
- WebApi/Services: PaymobService, PasswordService
- WebApi/appsettings.json/.Development.json: configuration
- docs/: architecture documentation

## Requirements
- .NET SDK 9.0
- SQL Server (LocalDB or any accessible instance)
- Paymob test/development credentials (ApiKey, IntegrationIdCard, IntegrationIdWallet, IframeId, HmacSecret)

## 🔐 Security & Configuration
This project follows professional security standards for handling sensitive data. **Never commit real credentials to the repository.**

### 1. Sensitive Data (User Secrets)
Use `dotnet user-secrets` to store your private keys locally during development:
```bash
# Admin Credentials for Dev Login
dotnet user-secrets set "Admin:Email" "admin@example.com"
dotnet user-secrets set "Admin:Password" "YourStrongPassword123!"

# SMTP Configuration (Gmail recommended)
dotnet user-secrets set "Smtp:Email" "your-app-email@gmail.com"
dotnet user-secrets set "Smtp:Password" "your-16-char-app-password"

# Paymob Integration
dotnet user-secrets set "Paymob:ApiKey" "zx_prd_..."
dotnet user-secrets set "Paymob:HmacSecret" "..."
```

### 2. SMTP Settings (`appsettings.json`)
The infrastructure is ready for Google SMTP. Ensure these settings are in your `appsettings.json`:
```json
"Smtp": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "EnableSsl": true,
  "Email": "__USE_USER_SECRETS__",
  "Password": "__USE_USER_SECRETS__"
}
```

---

## 📚 API Reference

### 🔐 Authentication (`/api/auth`)
| Endpoint | Method | Auth | Success | Error Codes | Description |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `/login` | `POST` | Public | `200 OK` | `401`, `400` | Admin/User login, returns JWT and user details. |

> **Error Response Example:**
> ```json
> { "statusCode": 401, "message": "Invalid email or password", "details": null }
> ```

### 👤 Account Management (`/api/account`)
| Endpoint | Method | Auth | Success | Error Codes | Description |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `/change-name` | `POST` | JWT | `204` | `401`, `404` | Update current user's display name. |
| `/change-password` | `POST` | JWT | `204` | `401`, `400` | Update password after verifying the current one. |
| `/request-reset` | `POST` | Public | `200` | `204` | Send 6-digit OTP to email for password recovery. |
| `/reset-password` | `POST` | Public | `204` | `401`, `404` | Reset password using email, OTP, and new password. |
| `/request-admin-otp` | `POST` | Admin | `200` | `403`, `409` | Send verification OTP to a new admin's email. |
| `/add-admin` | `POST` | Admin | `201` | `403`, `400`, `409` | Create a new Admin account using the verified OTP. |

> **Error Response Example (Invalid OTP):**
> ```json
> { "statusCode": 400, "message": "Invalid or expired OTP.", "details": null }
> ```

### 🚗 Car Management (`/api/car`)
| Endpoint | Method | Auth | Success | Error Codes | Description |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `/statistics` | `GET` | JWT | `200` | `401` | Get car counts (Available, On Trip, Pending, Offline). |
| `/` | `GET` | JWT | `200` | `401` | List cars with paging, search, and status filters. |
| `/{id}` | `GET` | JWT | `200` | `404`, `401` | Get detailed information for a specific car. |
| `/` | `POST` | JWT | `201` | `400`, `401` | Create a new car listing (Status: Pending). |
| `/{id}` | `PUT` | JWT | `204` | `404`, `401` | Update car details (Brand, Model, Price, etc.). |
| `/{id}` | `DELETE` | JWT | `204` | `404`, `401` | Remove a car listing. |
| `/{id}/status` | `PATCH` | JWT | `204` | `404`, `401` | Update car status (e.g., Approve/Reject listing). |

### 📅 Booking Management (`/api/booking`)
| Endpoint | Method | Auth | Success | Error Codes | Description |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `/statistics` | `GET` | JWT | `200` | `401` | Get booking stats (Active, Pick-up today, Canceled). |
| `/` | `GET` | JWT | `200` | `401` | List all bookings with paging, search, and status. |
| `/{id}` | `GET` | JWT | `200` | `404`, `401` | Get details of a specific booking. |
| `/{id}/status` | `PATCH` | JWT | `204` | `404`, `401` | Update booking status (e.g., Cancel, Complete). |
| `/refund-all` | `POST` | JWT | `200` | `401` | Batch mark bookings for refund. |

### 💳 Payment & Paymob (`/api/payment`)
| Endpoint | Method | Auth | Success | Error Codes | Description |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `/statistics` | `GET` | JWT | `200` | `401` | Get revenue, profit, and pending payout stats. |
| `/paymob/init` | `POST` | JWT | `200` | `400`, `401` | Initialize Paymob payment (Returns `payment_token`). |
| `/paymob/checkout` | `GET` | JWT | `200` | `400`, `401` | Direct checkout (Returns Iframe HTML or Redirect). |
| `/paymob/callback` | `GET` | Public | `200` | `400` | Paymob success/failure redirection handler. |
| `/paymob/webhook` | `POST` | Public | `200` | `400` | Paymob server-to-server status notification. |

> **Error Response Example (FK Missing):**
> ```json
> { "statusCode": 400, "message": "Booking with ID 999 does not exist.", "details": null }
> ```

### 📊 Dashboard (`/api/dashboard`)
| Endpoint | Method | Auth | Success | Error Codes | Description |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `/stats` | `GET` | JWT | `200` | `401` | High-level totals for Users, Cars, Bookings, Profit. |
| `/weekly-revenue` | `GET` | JWT | `200` | `401` | Daily revenue data for the last 7 days. |
| `/bookings-by-month` | `GET` | JWT | `200` | `401` | Monthly booking counts for chart representation. |

### 📝 Verification Requests (`/api/request`)
| Endpoint | Method | Auth | Success | Error Codes | Description |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `/` | `GET` | JWT | `200` | `401` | List pending requests (User verification, Car listing). |
| `/{id}` | `GET` | JWT | `200` | `404`, `400`, `401` | Get details and documents for a specific request. |
| `/{id}/status` | `PATCH` | JWT | `204` | `404`, `401` | Approve or Reject a verification request. |

### 👥 User Management (`/api/user`)
| Endpoint | Method | Auth | Success | Error Codes | Description |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `/` | `GET` | JWT | `200` | `401` | List all users with paging and search. |
| `/{id}` | `GET` | JWT | `200` | `404`, `401` | Get profile details for a specific user. |
| `/` | `POST` | JWT | `201` | `400`, `401` | Create a new user manually. |
| `/{id}` | `PUT` | JWT | `204` | `404`, `401` | Update user information (Role, Approval Status, etc.). |
| `/{id}` | `DELETE` | JWT | `204` | `404`, `401` | Delete a user account. |

---

## 🛡️ Global Error Handling
The API includes a **Global Exception Middleware** that captures all unhandled errors and returns a consistent, clean JSON structure:
```json
{
  "statusCode": 400,
  "message": "Friendly error message",
  "details": "Stack trace (only visible in Development mode)"
}
```
*Note: This prevents database-level errors (like Foreign Key violations) from crashing the client or exposing sensitive DB info.*

---

## 🛠️ Installation & Run
1. **Clone & Restore**:
   ```bash
   git clone https://github.com/XMahsec/Rently.Management.git
   dotnet restore
   ```
2. **Database**: Update the connection string in `appsettings.json`.
3. **Secrets**: Set your secrets as described in the Security section above.
4. **Run**:
   ```bash
   dotnet run --project WebApi/Rently.Management.csproj
   ```
5. **Swagger**: Access documentation at `http://localhost:5000/swagger`

## Payments (Paymob)
- Checkout (card/wallet):
  - `GET /api/payment/paymob/checkout`
    - Query: `bookingId`, `userId`, `amount`, `currency=EGP`, `email`, `name`, `phone`, `method=card|wallet`
    - Behavior:
      - `card`: returns HTML page that embeds Paymob iframe (direct render)
      - `wallet`: redirects to Paymob hosted payment page
- Initialization (API style):
  - `POST /api/payment/paymob/init` (JWT)
    - Body (snake_case):
      ```
      {
        "booking_id": 1,
        "user_id": 6,
        "amount": 100.00,
        "currency": "EGP",
        "email": "user@example.com",
        "name": "Test User",
        "phone": "01000000000",
        "method": "card"
      }
      ```
    - Returns: `{ payment_id, order_id, payment_token, url, method }`
- Callback:
  - `GET /api/payment/paymob/callback` (AllowAnonymous)
    - Validates HMAC, updates `Payment` status, notifies partner via webhook
- Webhook:
  - `POST /api/payment/paymob/webhook` (AllowAnonymous)
    - Validates HMAC, updates `Payment`, and sends a partner webhook (`PartnerWebhookUrl`)
- Utility:
  - `GET /api/payment/test/iframe?url=...` (AllowAnonymous): simple test page to render any iframe URL
- Notes:
  - Use ngrok (or similar) to expose local callback/webhook URLs for Paymob during development

### Outgoing Partner Webhooks
- Service: `WebhookService` posts signed events to an external listener (e.g., Flask)
- Config keys (Development via User Secrets preferred):
  - `Webhooks:Flask:Url` → e.g., `http://localhost:8000/webhook/rently`
  - `Webhooks:Flask:Secret` → HMAC secret for signing payload
  - `Webhooks:Flask:Enabled` → `true|false`
- HTTP headers:
  - `X-Rently-Event`: event name (e.g., `payment.created`, `payment.updated`, `user.created`, `car.updated`)
  - `X-Rently-Signature`: `HMACSHA256(body, Webhooks:Flask:Secret)` as lowercase hex
- Payload envelope (snake_case):
  ```
  {
    "id": "<event-id>",
    "event": "payment.updated",
    "created_at": "2026-03-01T12:34:56Z",
    "data": {
      "payment_id": 123,
      "booking_id": 1,
      "status": "Succeeded",
      "amount": 100.00,
      "currency": "EGP",
      "provider_payment_id": "...."
    }
  }
  ```
- Emitted events:
  - Payments: `payment.created`, `payment.updated`, `payment.refund_requested`
  - Users: `user.created`, `user.updated`, `user.password_changed`
  - Cars: `car.created`, `car.updated`, `car.status_changed`

## API Reference

### AuthController (`/api/auth`)
- `POST /login` → Admin development login, returns JWT

### AccountController (`/api/account`)
- `POST /change-name` (JWT)
- `POST /change-password` (JWT)
- `POST /request-reset` → Sends OTP for password reset
- `POST /reset-password` → Verifies OTP and sets new password
- `POST /request-admin-otp` (JWT, Admin) → Sends OTP to the email of the new admin to be added
- `POST /add-admin` (JWT, Admin) → Verifies OTP and adds the new admin user

### DashboardController (`/api/dashboard`)
- `GET /stats` → totals and trends for users, cars, bookings, profit

### UserController (`/api/user`) [JWT]
- `GET /` → list with paging and filters (`search`, `status`, `page`, `pageSize`)
- `GET /{id}`
- `POST /` → create user (dev/admin use)
- `PUT /{id}` → update user (email cannot be changed)
- `DELETE /{id}`
- `PATCH /{id}/status` → update approval status via UI status mapping

### CarController (`/api/car`)
- `GET /statistics`
- `GET /` → list with paging/filters
- `GET /{id}`
- `POST /` → create car
- `PUT /{id}` → update car
- `DELETE /{id}`
- `PATCH /{id}/status` → update status

### BookingController (`/api/booking`)
- `GET /statistics`
- `GET /` → list with paging/filters

### RequestController (`/api/request`) [JWT]
- `GET /` → list requests (`search`, `type`, `status`, `sort`, paging)
- `GET /{id}?type=Owner%20verification|Car%20listing` → details merged from User/Car
- `PATCH /{id}/status` → update request status

### PaymentController (`/api/payment`)
- `GET /statistics`
- `GET /transactions` → list payments with filters (`search`, `type`, `status`, paging)
- `GET /owner-payouts` → aggregated payouts per owner
- `GET /{id}`
- `POST /` → create payment
- `PUT /{id}` → update payment (status, provider fields)
- `POST /process-payout` → process an owner payout (stubbed, business-specific)
- `POST /refund-all` → mark succeeded payments as refunding (batch)
- Paymob integration:
  - `GET /paymob/checkout` (card/html iframe or wallet/redirect)
  - `GET /paymob/callback` (AllowAnonymous)
  - `POST /paymob/webhook` (AllowAnonymous)
  - `GET /test/iframe?url=...` (AllowAnonymous)

## Error Handling
- Global Exception Middleware: All unhandled exceptions are caught and returned as a structured JSON response.
- Structured Responses (snake_case):
  ```json
  {
    "status_code": 400,
    "message": "Error message details",
    "details": "Stack trace (only in Development)"
  }
  ```
- Validation: DataAnnotations are used across DTOs to enforce required fields and formats.
- Existence Checks: The API verifies that related records (e.g., `BookingId`, `UserId`, `OwnerId`) exist before performing operations, preventing Foreign Key constraint violations (500 errors converted to 400 Bad Request).

## Security Notes
- Do not commit production secrets/keys to appsettings; use environment variables or Secret Manager.
- JWT keys must be strong and rotated periodically.
- HMAC validation is enforced for Paymob callbacks/webhooks.

## CORS
- Default policy allows `http://localhost:5173` and `http://localhost:3000` with credentials; adjust as needed for your Flutter/web clients.

## Contributing
- Open issues and PRs on GitHub.
- Keep endpoint contracts and validation consistent; update docs/ARCHITECTURE.md if workflows change.
