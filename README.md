# Rently.Management

إدارة الباك إند لجزء الـ Management لتطبيق الموبايل (Flutter) مع مصادقة JWT وتكامل Paymob، ودعم إدارة الحساب.

## البدء السريع
- تشغيل محلي: `dotnet run --project .\Rently.Management.csproj`
- Swagger: http://localhost:5000/swagger
- إعدادات التطوير: [appsettings.Development.json](file:///c:/Users/Administrator/source/repos/Rently.Management/WebApi/appsettings.Development.json)

## المستندات
- المعمارية والرسومات: [docs/ARCHITECTURE.md](file:///c:/Users/Administrator/source/repos/Rently.Management/docs/ARCHITECTURE.md)

## نقاط رئيسية
- المصادقة: JWT مع سياسة تفويض افتراضية
- الدفع: Paymob (Card/Wallet) مع HMAC وWebhook للشريك (Flask)
- الحساب: تغيير الاسم/كلمة المرور، طلب ريسيت، إعادة تعيين

## ملفات مرجعية
- Program: [WebApi/Program.cs](file:///c:/Users/Administrator/source/repos/Rently.Management/WebApi/Program.cs)
- Controllers: [WebApi/Controllers](file:///c:/Users/Administrator/source/repos/Rently.Management/WebApi/Controllers)
- Services: [WebApi/Services](file:///c:/Users/Administrator/source/repos/Rently.Management/WebApi/Services)
- Entities: [Domain/Entities](file:///c:/Users/Administrator/source/repos/Rently.Management/Domain/Entities)
