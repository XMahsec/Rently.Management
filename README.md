# Rently.Management

إدارة الباك إند لجزء الـ Management لتطبيق الموبايل (Flutter) مع مصادقة JWT وتكامل Paymob، ودعم إدارة الحساب.

## البدء السريع
- تشغيل محلي: `dotnet run --project .\Rently.Management.csproj`
- Swagger: http://localhost:5000/swagger
- إعدادات التطوير: WebApi/appsettings.Development.json

## المستندات
- المعمارية والرسومات: docs/ARCHITECTURE.md

## نقاط رئيسية
- المصادقة: JWT مع سياسة تفويض افتراضية
- الدفع: Paymob (Card/Wallet) مع HMAC وWebhook للشريك (Flask)
- الحساب: تغيير الاسم/كلمة المرور، طلب ريسيت، إعادة تعيين

## ملفات مرجعية
- Program: WebApi/Program.cs
- Controllers: WebApi/Controllers
- Services: WebApi/Services
- Entities: Domain/Entities

## المتطلبات
- .NET SDK 9.0
- SQL Server (LocalDB أو مثيل متاح)
- حساب Paymob (مفاتيح تطوير: ApiKey, IntegrationIdCard, IntegrationIdWallet, IframeId, HmacSecret)

## الإعداد
1) انسخ الريبو:
   - `git clone https://github.com/XMahsec/Rently.Management.git`
   - `cd Rently.Management`
2) تثبيت الحزم:
   - `dotnet restore`
3) إعداد الاتصال بقاعدة البيانات:
   - عدّل WebApi/appsettings.json بقيمة ConnectionStrings:DefaultConnection بما يناسب مثيل SQL لديك.
4) إعداد مفاتيح التطوير (اختياري للتجربة الكاملة):
   - WebApi/appsettings.Development.json
   - ضع قيم Paymob المناسبة (ApiKey, IntegrationIdCard/Wallet, IframeId, HmacSecret)
   - اضبط Jwt:Issuer/Audience/Key في WebApi/appsettings.json أو عبر Environment Variables
5) ترحيل قاعدة البيانات:
   - `dotnet ef migrations add InitRun` (لو لا توجد ترحيلات على بيئتك)
   - `dotnet ef database update`

## التشغيل
- `dotnet run --project .\Rently.Management.csproj`
- المتصفح: http://localhost:5000/swagger

## تسجيل الدخول
- POST api/auth/login
  - body:
    - email, password
- احصل على التوكن وضعه في Authorize داخل Swagger كـ Bearer <token>

## الدفع (Paymob)
- بطاقة عبر iframe أو محفظة عبر redirect:
  - GET api/payment/paymob/checkout
    - query:
      - bookingId, userId, amount, currency=EGP, email, name, phone, method=card|wallet
  - بعد الدفع، النظام يستقبل الكولباك/الويب هوك، يحدّث Payment، ويرسل ويب هوك للشريك (PartnerWebhookUrl)
- لتجربة الويب هوك محليًا استخدم ngrok وحدث RedirectionUrl/PartnerWebhookUrl بقيم عامة.

## إدارة الحساب
- تغيير الاسم: POST api/account/change-name (JWT)
- تغيير كلمة المرور: POST api/account/change-password (JWT)
- طلب ريسيت: POST api/account/request-reset
- إعادة تعيين: POST api/account/reset-password
- إضافة أدمن: POST api/account/add-admin (JWT دور Admin)

## ملاحظات إنتاج
- لا تضع مفاتيح/أسرار في ملفات الإعدادات العامة؛ استخدم Secret Manager أو Environment Variables.
- راجع سياسات CORS وفقًا لتطبيق الـ Flutter لديك.
