# Rently.Management Architecture

## Database ER Diagram
```mermaid
erDiagram
    USER ||--o{ CAR : owns
    USER ||--o{ BOOKING : rents
    USER ||--o{ PAYMENT : pays
    USER ||--o{ REVIEW : writes
    USER ||--o{ NOTIFICATION : receives
    USER ||--o{ FAVORITE : marks
    USER ||--o{ MESSAGE : sends
    USER ||--o{ MESSAGE : receives
    USER ||--o{ OTP : has

    CAR ||--o{ BOOKING : has
    CAR ||--o{ REVIEW : gets
    CAR ||--o{ CARIMAGE : has
    CAR ||--o{ CARUNAVAILABLEDATE : blocks
    CAR ||--o{ FAVORITE : featuredIn

    BOOKING ||--o{ PAYMENT : generates

    USER {
      int Id PK
      string Name
      string Email UNIQUE
      string Phone UNIQUE
      string Role
      string ApprovalStatus
      string PasswordHash
      string PasswordSalt
      string PasswordResetToken
      datetime PasswordResetTokenExpires
    }
    CAR {
      int Id PK
      int OwnerId FK
      string Brand
      string Model
      int Year
      decimal PricePerDay
      string Status
      string LicensePlate
    }
    BOOKING {
      int Id PK
      int CarId FK
      int RenterId FK
      datetime StartDate
      datetime EndDate
      string Status
      string TransactionId
      decimal TotalPrice
      decimal PaidAmount
    }
    PAYMENT {
      int Id PK
      int BookingId FK
      int UserId FK
      decimal Amount
      string Currency
      string Status
      string Provider
      string ProviderPaymentId
      string ProviderReceiptUrl
    }
    REVIEW {
      int Id PK
      int RenterId FK
      int CarId FK
      int Rating
      string Comment
    }
    NOTIFICATION {
      int Id PK
      int UserId FK
      string Title
      string Message
      string Type
      bool IsRead
    }
    MESSAGE {
      int Id PK
      int SenderId FK
      int ReceiverId FK
      string Content
      bool IsRead
    }
    OTP {
      int Id PK
      int UserId FK
      string OtpHash
    }
    CARIMAGE {
      int Id PK
      int CarId FK
      string ImagePath
    }
    CARUNAVAILABLEDATE {
      int Id PK
      int CarId FK
      datetime StartDate
      datetime EndDate
      string Reason
    }
```

## Class Diagram (Core)
```mermaid
classDiagram
    class User {
      +int Id
      +string Name
      +string Email
      +string Phone
      +string Role
      +string ApprovalStatus
      +string PasswordHash
      +string PasswordSalt
      +string PasswordResetToken
      +DateTime? PasswordResetTokenExpires
      +List~Car~ OwnedCars
      +List~Booking~ BookingsAsRenter
      +List~Payment~ Payments
    }
    class Car {
      +int Id
      +int OwnerId
      +string Brand
      +string Model
      +int Year
      +decimal PricePerDay
      +string Status
      +string LicensePlate
      +List~Booking~ Bookings
    }
    class Booking {
      +int Id
      +int CarId
      +int RenterId
      +DateTime StartDate
      +DateTime EndDate
      +string Status
      +string TransactionId
      +decimal TotalPrice
      +decimal PaidAmount
      +List~Payment~ Payments
    }
    class Payment {
      +int Id
      +int BookingId
      +int? UserId
      +decimal Amount
      +string Currency
      +string Status
      +string Provider
      +string ProviderPaymentId
      +string ProviderReceiptUrl
    }
    User "1" --> "many" Car : owns
    User "1" --> "many" Booking : rents
    Booking "1" --> "many" Payment : generates
    Car "1" --> "many" Booking : has
```

## Workflows

### Authentication (Login)
```mermaid
sequenceDiagram
    actor Client
    participant API as AuthController
    participant Repo as UserRepository
    Client->>API: POST /api/auth/login (email, password)
    API->>Repo: GetUsers(email)
    Repo-->>API: User
    API->>API: Validate admin email/password from settings
    API->>API: Issue JWT (sub, email, name, role)
    API-->>Client: 200 { token, user }
```

### Payments via Paymob (Card/Wallet)
```mermaid
sequenceDiagram
    actor Client
    participant API as PaymentController
    participant Svc as PaymobService
    participant Paymob
    Client->>API: GET /api/payment/paymob/checkout?method=card|wallet&...
    API->>Svc: Initiate(auth, order, payment_key)
    Svc->>Paymob: /auth/tokens, /ecommerce/orders, /acceptance/payment_keys
    Paymob-->>Svc: tokens + ids
    Svc-->>API: (orderId, token, url)
    API->>API: Save Payment Pending
    alt method=card
      API-->>Client: HTML iframe(url)
    else method=wallet
      API-->>Client: Redirect url
    end
    Paymob-->>API: GET /callback or POST /webhook
    API->>API: Validate HMAC, Update Payment (Succeeded/Failed)
    API-->>Partner(Flask): POST /webhook/payment (status)
```

### Account Management
```mermaid
sequenceDiagram
    actor User
    participant API as AccountController
    User->>API: POST /api/account/change-name (JWT)
    API->>API: Update Name
    API-->>User: 204
    User->>API: POST /api/account/change-password (JWT)
    API->>API: Verify current, hash new
    API-->>User: 204
    User->>API: POST /api/account/request-reset (email)
    API->>API: Create reset token (30m)
    API-->>User: 200
    User->>API: POST /api/account/reset-password (email, token, new)
    API->>API: Verify token, hash new
    API-->>User: 204
```

## Key Files
- Program: [Program.cs](file:///c:/Users/Administrator/source/repos/Rently.Management/WebApi/Program.cs)
- Auth: [AuthController.cs](file:///c:/Users/Administrator/source/repos/Rently.Management/WebApi/Controllers/AuthController.cs)
- Payments: [PaymentController.cs](file:///c:/Users/Administrator/source/repos/Rently.Management/WebApi/Controllers/PaymentController.cs), [PaymobService.cs](file:///c:/Users/Administrator/source/repos/Rently.Management/WebApi/Services/PaymobService.cs)
- Account: [AccountController.cs](file:///c:/Users/Administrator/source/repos/Rently.Management/WebApi/Controllers/AccountController.cs), [PasswordService.cs](file:///c:/Users/Administrator/source/repos/Rently.Management/WebApi/Services/PasswordService.cs)
- Entities: [User.cs](file:///c:/Users/Administrator/source/repos/Rently.Management/Domain/Entities/User.cs), [Car.cs](file:///c:/Users/Administrator/source/repos/Rently.Management/Domain/Entities/Car.cs), [Booking.cs](file:///c:/Users/Administrator/source/repos/Rently.Management/Domain/Entities/Booking.cs), [Payment.cs](file:///c:/Users/Administrator/source/repos/Rently.Management/Domain/Entities/Payment.cs)
