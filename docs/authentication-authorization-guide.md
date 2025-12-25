# 🔐 Microservice Authentication & Authorization Rehberi

> Bu doküman, Microsoft dokümantasyonlarına dayanarak microservice mimarisinde authentication ve authorization implementasyonu için hazırlanmıştır.

---

## 📋 İçindekiler

- [Genel Bakış](#-genel-bakış)
- [Microsoft Önerileri](#-microsoft-önerileri)
- [Önerilen Mimari](#-önerilen-mimari)
- [Implementasyon Senaryoları](#-implementasyon-senaryoları)
- [Teknik Detaylar](#-teknik-detaylar)
- [Kaynaklar](#-kaynaklar)

---

## 🎯 Genel Bakış

### Authentication vs Authorization

**Authentication (Kimlik Doğrulama):**
- Kullanıcının kim olduğunu doğrulama
- "Bu kullanıcı gerçekten X kullanıcısı mı?"
- Örnek: Login işlemi, JWT token doğrulama

**Authorization (Yetkilendirme):**
- Kullanıcının ne yapabileceğini belirleme
- "Bu kullanıcı bu işlemi yapabilir mi?"
- Örnek: Admin rolü kontrolü, Policy kontrolü

### Microservice'lerde Neden Önemli?

1. **Güvenlik:** Her servis bağımsız çalışır, güvenlik merkezi yönetilmeli
2. **Ölçeklenebilirlik:** Token tabanlı yaklaşım stateless'tır
3. **Yönetilebilirlik:** Merkezi authentication, dağıtık authorization

---

## 📚 Microsoft Önerileri

Microsoft'un resmi dokümantasyonuna göre, microservice mimarisinde authentication ve authorization için şu yaklaşımlar önerilir:

### 1. Merkezi Kimlik Doğrulama (Centralized Authentication)

**Microsoft'un Önerisi:**
> "In microservices scenarios, authentication is typically performed at a central point. If you're using an API Gateway, you can perform authentication at the gateway level."

**Avantajlar:**
- ✅ Tüm servislerin authentication yükünden kurtulması
- ✅ Güvenlik politikalarının merkezi yönetimi
- ✅ Token doğrulamasının tek noktada yapılması
- ✅ Daha kolay güncelleme ve bakım

**Kaynak:** [Microsoft Learn - Secure .NET Microservices](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/secure-net-microservices-web-applications/authorization-net-microservices-web-applications)

### 2. JWT (JSON Web Token) Kullanımı

**Microsoft'un Önerisi:**
> "JWT tokens are commonly used for authentication and authorization in microservices architectures because they are stateless and can be validated by any service."

**JWT Yapısı:**
```
Header.Payload.Signature

Header: Token tipi ve algoritma
Payload: Claims (user id, roles, permissions, expiration)
Signature: Güvenlik için imza
```

**Avantajlar:**
- ✅ Stateless (veritabanı sorgusu gerekmez)
- ✅ Ölçeklenebilir
- ✅ Servisler arası taşınabilir
- ✅ Self-contained (tüm bilgi token içinde)

**Kaynak:** [Microsoft Learn - JWT Authentication](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/secure-net-microservices-web-applications/authorization-net-microservices-web-applications)

### 3. OAuth 2.0 ve OpenID Connect (OIDC)

**Microsoft'un Önerisi:**
> "OAuth 2.0 and OpenID Connect are industry standards for secure authentication and authorization. Use Microsoft Entra ID or IdentityServer for identity management."

**OAuth 2.0 Akışı:**
```
1. Client → Authorization Server: Login request
2. Authorization Server → User: Login page
3. User → Authorization Server: Credentials
4. Authorization Server → Client: Access Token (JWT)
5. Client → Resource Server: Request with Token
6. Resource Server → Authorization Server: Validate Token
7. Resource Server → Client: Resource data
```

**Kaynak:** [Microsoft Learn - OAuth 2.0](https://learn.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-auth-code-flow)

### 4. API Gateway Üzerinde Authentication

**Microsoft'un Önerisi:**
> "API Gateway is the ideal place to perform authentication. All requests pass through the gateway, making it a central point for security."

**YARP (Yet Another Reverse Proxy) ile:**
- Authentication middleware eklenebilir
- Token doğrulama yapılabilir
- Claims downstream servislere iletilir

**Kaynak:** [Microsoft Learn - API Gateway with Ocelot](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/implement-api-gateways-with-ocelot)

### 5. Authorization Stratejileri

**Microsoft iki yaklaşım önerir:**

#### A) Rol Tabanlı Yetkilendirme (RBAC)
```csharp
[Authorize(Roles = "Admin")]
public class ProductsController : ControllerBase
{
    // Sadece Admin rolü erişebilir
}
```

#### B) Policy Tabanlı Yetkilendirme
```csharp
// Startup'ta policy tanımla
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanDeleteProduct", policy =>
        policy.RequireClaim("permission", "delete-product"));
});

// Controller'da kullan
[Authorize(Policy = "CanDeleteProduct")]
public IActionResult DeleteProduct(int id) { }
```

**Kaynak:** [Microsoft Learn - Authorization](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/secure-net-microservices-web-applications/authorization-net-microservices-web-applications)

---

## 🏗️ Önerilen Mimari

### Mimari Diyagramı

```
┌─────────────────────────────────────────────────────────────┐
│                         CLIENT                               │
│                    (Web/Mobile App)                          │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        │ 1. Login Request
                        │    POST /api/auth/login
                        │    { username, password }
                        ▼
┌─────────────────────────────────────────────────────────────┐
│                    IDENTITY SERVICE                          │
│                  (Identity.API)                             │
│                                                              │
│  - User Registration                                       │
│  - Login/Logout                                              │
│  - JWT Token Generation                                      │
│  - Token Refresh                                             │
│  - User Management                                           │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        │ 2. JWT Token Response
                        │    { access_token, refresh_token, expires_in }
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│                      API GATEWAY                             │
│                    (Gateway.API - YARP)                      │
│                                                              │
│  ┌────────────────────────────────────────────┐             │
│  │  Authentication Middleware                 │             │
│  │  - JWT Token Validation                    │             │
│  │  - Claims Extraction                       │             │
│  │  - Add Claims to Headers                   │             │
│  └────────────────────────────────────────────┘             │
│                                                              │
│  ┌────────────────────────────────────────────┐             │
│  │  Authorization Middleware (Optional)        │             │
│  │  - Role-based checks                        │             │
│  │  - Policy-based checks                      │             │
│  └────────────────────────────────────────────┘             │
└───────────┬───────────────┬───────────────┬─────────────────┘
            │               │               │
            │ 3. Authenticated Request      │
            │    + X-User-Id Header         │
            │    + X-User-Roles Header      │
            │                               │
            ▼               ▼               ▼
    ┌──────────┐   ┌──────────┐   ┌──────────┐
    │ Catalog  │   │  Basket  │   │ Ordering │
    │   API    │   │   API    │   │   API    │
    │          │   │          │   │          │
    │ [Authorize] │ │ [Authorize] │ │ [Authorize] │
    │ Attribute   │ │ Attribute   │ │ Attribute   │
    └──────────┘   └──────────┘   └──────────┘
```

### Mimari Kararlar

| Karar | Seçim | Neden |
|-------|-------|-------|
| **Authentication Yeri** | API Gateway | Merkezi yönetim, servislerin yükünü azaltır |
| **Token Tipi** | JWT | Stateless, ölçeklenebilir, self-contained |
| **Identity Provider** | Identity Service (Kendi) | Öğrenme amaçlı, tam kontrol |
| **Authorization** | Her Serviste | Servis bazlı yetkilendirme, esneklik |
| **Token Doğrulama** | Gateway'de | Tek noktada doğrulama, performans |

---

## 🚀 Implementasyon Senaryoları

### Senaryo 1: Basit JWT (Öğrenme Amaçlı - Önerilen)

**Amaç:** Temel authentication ve authorization öğrenmek için basit bir implementasyon.

#### Adım 1: Identity Service Oluşturma

**Yeni Servis:** `Identity.API`

**Özellikler:**
- User registration
- Login/Logout
- JWT token generation
- Token refresh

**Teknolojiler:**
- ASP.NET Core 9.0
- ASP.NET Core Identity (opsiyonel)
- JWT Bearer Authentication
- PostgreSQL (User database)

**Endpoint'ler:**
```
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
GET  /api/auth/me (current user info)
```

#### Adım 2: Gateway'de Authentication Middleware

**Gateway.API'ye Eklenecekler:**
- JWT Bearer Authentication
- Token validation
- Claims extraction
- Claims'i downstream servislere header olarak ekleme

**YARP Transform:**
```json
{
  "Transforms": [
    { "PathRemovePrefix": "/catalog-service" },
    { 
      "RequestHeader": "X-User-Id",
      "Set": "{user-id-from-claim}"
    },
    {
      "RequestHeader": "X-User-Roles",
      "Set": "{roles-from-claim}"
    }
  ]
}
```

#### Adım 3: Downstream Servislerde Authorization

**Her serviste:**
- `[Authorize]` attribute kullanımı
- Rol tabanlı veya policy tabanlı yetkilendirme

**Örnek:**
```csharp
[Authorize(Roles = "Admin")]
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteProduct(Guid id)
{
    // Sadece Admin rolü erişebilir
}
```

**Avantajlar:**
- ✅ Basit ve anlaşılır
- ✅ Hızlı implementasyon
- ✅ Öğrenme için ideal

**Dezavantajlar:**
- ❌ Token revocation zor (JWT stateless)
- ❌ Production için ek güvenlik önlemleri gerekir

---

### Senaryo 2: IdentityServer (Production-Ready)

**Amaç:** Production ortamı için enterprise-grade authentication.

#### IdentityServer Nedir?

IdentityServer, OpenID Connect ve OAuth 2.0 protokollerini destekleyen bir Identity Provider'dır.

**Özellikler:**
- OAuth 2.0 / OpenID Connect
- Multiple client support
- Token revocation
- Refresh tokens
- User management
- External providers (Google, Facebook, Microsoft)

#### Implementasyon

**1. IdentityServer Service:**
- `IdentityServer.API` servisi
- IdentityServer4/IdentityServer6 kurulumu
- User store (PostgreSQL + ASP.NET Core Identity)

**2. Gateway Entegrasyonu:**
- IdentityServer authentication
- Token validation
- Claims transformation

**3. Client Configuration:**
- Web client
- Mobile client
- Service-to-service clients

**Avantajlar:**
- ✅ Production-ready
- ✅ Industry standards (OAuth 2.0, OIDC)
- ✅ Token revocation
- ✅ Multiple client support

**Dezavantajlar:**
- ❌ Daha karmaşık
- ❌ Daha fazla konfigürasyon

**Kaynak:** [IdentityServer Documentation](https://identityserver4.readthedocs.io/)

---

### Senaryo 3: Microsoft Entra ID (Azure)

**Amaç:** Azure kullanıyorsanız, cloud-native authentication.

#### Microsoft Entra ID Nedir?

Microsoft Entra ID (eski Azure Active Directory), Microsoft'un cloud-based identity ve access management servisidir.

**Özellikler:**
- OAuth 2.0 / OpenID Connect
- Single Sign-On (SSO)
- Multi-Factor Authentication (MFA)
- Conditional Access
- Azure Key Vault entegrasyonu

#### Implementasyon

**1. Azure Setup:**
- Entra ID tenant oluştur
- App registration
- Client ID ve Secret al

**2. Gateway Entegrasyonu:**
- Microsoft Entra ID authentication
- Token validation
- Claims mapping

**3. Secret Management:**
- Azure Key Vault kullan
- Connection strings ve secrets güvenli sakla

**Avantajlar:**
- ✅ Cloud-native
- ✅ Enterprise features (MFA, SSO)
- ✅ Azure entegrasyonu
- ✅ Managed service

**Dezavantajlar:**
- ❌ Azure dependency
- ❌ Cost (ücretsiz tier var ama sınırlı)

**Kaynak:** [Microsoft Entra ID Documentation](https://learn.microsoft.com/en-us/azure/active-directory/)

---

## 🔧 Teknik Detaylar

### JWT Token Yapısı

```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "user-id-123",
    "email": "user@example.com",
    "roles": ["Admin", "User"],
    "permissions": ["read:products", "write:products"],
    "exp": 1735689600,
    "iat": 1735686000
  },
  "signature": "HMACSHA256(...)"
}
```

### Gateway Middleware Örneği

```csharp
// Gateway.API/Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://identity.api:8080";
        options.Audience = "gateway-api";
        options.RequireHttpsMetadata = false; // Docker içinde HTTP kullanıyoruz
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// YARP middleware'den önce authentication olmalı
app.MapReverseProxy();
```

### Downstream Service Authorization

```csharp
// Catalog.API/Controllers/ProductsController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize] // Tüm endpoint'ler için authentication gerekli
public class ProductsController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous] // Bu endpoint herkese açık
    public async Task<IActionResult> GetProducts()
    {
        // ...
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")] // Sadece Admin veya Manager
    public async Task<IActionResult> CreateProduct(CreateProductDto dto)
    {
        // ...
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanDeleteProduct")] // Policy tabanlı
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        // ...
    }
}
```

### Policy Tanımlama

```csharp
// Catalog.API/Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanDeleteProduct", policy =>
        policy.RequireClaim("permission", "delete-product"));
    
    options.AddPolicy("CanManageProducts", policy =>
        policy.RequireRole("Admin", "Manager"));
});
```

### Servisler Arası İletişim (Service-to-Service)

**Sorun:** Basket.API, Discount.Grpc'ye istek atarken authentication gerekir mi?

**Çözüm 1: Internal Network (Önerilen)**
- Docker container network içinde servisler birbirine güvenir
- Internal istekler için authentication gerekmez
- Sadece external istekler (client → gateway) için authentication

**Çözüm 2: Service Token**
- Her servis kendi service token'ını alır
- Servisler arası isteklerde service token kullanılır
- Daha güvenli ama daha karmaşık

---

## 📦 Gerekli NuGet Paketleri

### Identity Service

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.0" />
```

### Gateway

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />
<PackageReference Include="Yarp.ReverseProxy" Version="2.2.0" />
```

### Downstream Services

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />
```

---

## 🔒 Güvenlik Best Practices

### 1. Token Güvenliği

- ✅ Token'ları HTTPS üzerinden gönder (production'da)
- ✅ Token expiration süresini kısa tut (15-30 dakika)
- ✅ Refresh token kullan
- ✅ Token'ları localStorage yerine httpOnly cookie'de sakla (XSS koruması)

### 2. Secret Management

- ✅ JWT secret'ı environment variable'da sakla
- ✅ Production'da Azure Key Vault veya benzeri kullan
- ✅ Secret'ları kod içinde hardcode etme

### 3. Token Validation

- ✅ Issuer validation
- ✅ Audience validation
- ✅ Expiration validation
- ✅ Signature validation

### 4. Authorization

- ✅ Principle of least privilege (en az yetki)
- ✅ Role-based + Policy-based kombinasyonu
- ✅ Resource-level authorization (kullanıcı sadece kendi kaynaklarına erişebilir)

---

## 📚 Kaynaklar

### Microsoft Resmi Dokümantasyonları

1. **Secure .NET Microservices**
   - URL: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/secure-net-microservices-web-applications/
   - İçerik: Microservice güvenliği genel bakış

2. **Authorization in Microservices**
   - URL: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/secure-net-microservices-web-applications/authorization-net-microservices-web-applications
   - İçerik: Authorization stratejileri ve implementasyon

3. **API Gateway with Ocelot**
   - URL: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/implement-api-gateways-with-ocelot
   - İçerik: API Gateway'de authentication (Ocelot örneği, YARP için de geçerli)

4. **ASP.NET Core Authentication**
   - URL: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/
   - İçerik: ASP.NET Core authentication temelleri

5. **ASP.NET Core Authorization**
   - URL: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/
   - İçerik: ASP.NET Core authorization (RBAC, Policies)

6. **JWT Bearer Authentication**
   - URL: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn
   - İçerik: JWT authentication implementasyonu

7. **OAuth 2.0 ve OpenID Connect**
   - URL: https://learn.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-auth-code-flow
   - İçerik: OAuth 2.0 akışı ve implementasyon

8. **Microsoft Entra ID**
   - URL: https://learn.microsoft.com/en-us/azure/active-directory/
   - İçerik: Microsoft Entra ID (Azure AD) kullanımı

9. **Azure Key Vault**
   - URL: https://learn.microsoft.com/en-us/azure/key-vault/
   - İçerik: Secret management

### Diğer Kaynaklar

10. **IdentityServer Documentation**
    - URL: https://identityserver4.readthedocs.io/
    - İçerik: IdentityServer4/6 dokümantasyonu

11. **YARP Documentation**
    - URL: https://microsoft.github.io/reverse-proxy/
    - İçerik: YARP (Yet Another Reverse Proxy) dokümantasyonu

12. **JWT.io**
    - URL: https://jwt.io/
    - İçerik: JWT token decode/encode ve test

### GitHub Örnekleri

13. **eShopOnContainers**
    - URL: https://github.com/dotnet-architecture/eShopOnContainers
    - İçerik: Microsoft'un resmi microservice örneği (IdentityService içerir)

14. **Microservices Architecture**
    - URL: https://github.com/aelassas/microservices
    - İçerik: JWT authentication örnekleri

---

## 🎯 Sonuç

Bu doküman, Microsoft'un resmi dokümantasyonlarına dayanarak microservice mimarisinde authentication ve authorization için önerilen yaklaşımları açıklamaktadır.

**Önerilen Başlangıç:**
1. Senaryo 1 (Basit JWT) ile başla
2. Temel authentication/authorization öğren
3. İhtiyaç duyuldukça IdentityServer veya Entra ID'ye geç

**Önemli Notlar:**
- Authentication merkezi (Gateway'de) yapılmalı
- Authorization her serviste yapılabilir
- JWT stateless ve ölçeklenebilir
- Production'da mutlaka HTTPS kullan
- Secret'ları güvenli sakla

---

**Son Güncelleme:** Aralık 2024  
**Kaynak:** Microsoft Learn Documentation

