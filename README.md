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

## Configuration
1) Clone repository:
   - `git clone https://github.com/XMahsec/Rently.Management.git`
   - `cd Rently.Management`
2) Restore packages:
   - `dotnet restore`
3) Database connection:
   - Edit `WebApi/appsettings.json` → `ConnectionStrings:DefaultConnection` to match your SQL instance.
4) Development keys and Secrets (MANDATORY for payments/auth/email testing):
   - Use `dotnet user-secrets` to store sensitive data locally:
     - `dotnet user-secrets set "Admin:Email" "your-admin-email@example.com"`
     - `dotnet user-secrets set "Admin:Password" "YourStrongPassword123!"`
     - `dotnet user-secrets set "Smtp:Email" "your-email@gmail.com"`
     - `dotnet user-secrets set "Smtp:Password" "<Your-Gmail-App-Password>"`
     - `dotnet user-secrets set "Paymob:ApiKey" "your-paymob-api-key"`
   - `WebApi/appsettings.Development.json`
     - Set remaining Paymob values: `IntegrationIdCard`, `IntegrationIdWallet`, `IframeId`, `HmacSecret`, `RedirectionUrl`
   - JWT settings: `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key`, `Jwt:ExpiresMinutes` (or via environment variables)
   - SMTP configuration in `appsettings.json`:
     - `Smtp:Host`: `smtp.gmail.com`
     - `Smtp:Port`: `587`
     - `Smtp:EnableSsl`: `true`
5) Database migrations:
   - If your local DB is empty or missing latest schema:
     - `dotnet ef migrations add InitRun`
     - `dotnet ef database update`

## Run
- `dotnet run --project .\Rently.Management.csproj`
- Visit Swagger at http://localhost:5000/swagger
- Use “Authorize” to set `Bearer <token>` after login

## Authentication Flow
- Admin dev login:
  - `POST /api/auth/login`
    - Body: `{ "email": "<Admin:Email>", "password": "<Admin:Password>" }`
  - Returns a JWT containing claims: sub (userId), email, name, role
- Global authorization:
  - Fallback policy requires authentication for all endpoints unless explicitly marked `[AllowAnonymous]`

## Account Management
- `POST /api/account/change-name` (JWT): Change display name
- `POST /api/account/change-password` (JWT): Verify current and set new password (PBKDF2)
- `POST /api/account/request-reset`: Generates a 6-digit OTP sent via email (valid 10m)
- `POST /api/account/reset-password`: Verify OTP + email then set new password
- `POST /api/account/request-admin-otp` (JWT, role=Admin): Sends a verification OTP to the new admin's email
- `POST /api/account/add-admin` (JWT, role=Admin): Create a new Admin user (requires valid OTP from the new admin)
- Email immutability:
  - User email cannot be changed via update; requests attempting to change email return 400

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
