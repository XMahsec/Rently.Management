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
4) Development keys (optional but recommended for payments/auth testing):
   - `WebApi/appsettings.Development.json`
     - Set Paymob values: `ApiKey`, `IntegrationIdCard`, `IntegrationIdWallet`, `IframeId`, `HmacSecret`, `RedirectionUrl`, `PartnerWebhookUrl`
     - Set Admin credentials for dev login: `Admin:Email`, `Admin:Password`
   - JWT settings: `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key`, `Jwt:ExpiresMinutes` (or via environment variables)
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
- `POST /api/account/request-reset`: Issue reset token (valid 30m) for an email
- `POST /api/account/reset-password`: Verify token + email then set new password
- `POST /api/account/add-admin` (JWT, role=Admin): Create a new Admin user
- Email immutability:
  - User email cannot be changed via update; requests attempting to change email return 400

## Payments (Paymob)
- Checkout (card/wallet):
  - `GET /api/payment/paymob/checkout`
    - Query: `bookingId`, `userId`, `amount`, `currency=EGP`, `email`, `name`, `phone`, `method=card|wallet`
    - Behavior:
      - `card`: returns HTML page that embeds Paymob iframe (direct render)
      - `wallet`: redirects to Paymob hosted payment page
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

## API Reference

### AuthController (`/api/auth`)
- `POST /login` → Admin development login, returns JWT

### AccountController (`/api/account`)
- `POST /change-name` (JWT)
- `POST /change-password` (JWT)
- `POST /request-reset`
- `POST /reset-password`
- `POST /add-admin` (JWT, requires Admin role)

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

## Error and Validation
- DataAnnotations are used across DTOs to enforce required fields and formats (email, ranges, enums).
- Standard HTTP responses:
  - 200 OK, 201 Created, 204 NoContent
  - 400 BadRequest (validation or immutable email)
  - 401 Unauthorized / 403 Forbidden (authz)
  - 404 NotFound

## Security Notes
- Do not commit production secrets/keys to appsettings; use environment variables or Secret Manager.
- JWT keys must be strong and rotated periodically.
- HMAC validation is enforced for Paymob callbacks/webhooks.

## CORS
- Default policy allows `http://localhost:5173` and `http://localhost:3000` with credentials; adjust as needed for your Flutter/web clients.

## Contributing
- Open issues and PRs on GitHub.
- Keep endpoint contracts and validation consistent; update docs/ARCHITECTURE.md if workflows change.
