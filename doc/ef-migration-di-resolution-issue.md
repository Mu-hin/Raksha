# EF Core Migration — DI Service Resolution Failure

## Problem Statement

When generating EF Core migrations using the following command:

```bash
dotnet ef migrations add <migration-name> \
  --project src/Raksha.Infrastructure \
  --startup-project src/Raksha.Api \
  --context ApplicationDbContext
```

The console outputs a warning:

> **An error occurred while accessing the Microsoft.Extensions.Hosting services. Continuing without the application service provider.**

The migration **still succeeds** because EF Core falls back to the `IDesignTimeDbContextFactory<ApplicationDbContext>` implementation (`ApplicationDbContextFactory`), but the warning indicates an unhealthy DI container.

---

## What Happens During `dotnet ef migrations add`

Understanding the EF Core design-time pipeline is key to diagnosing this issue.

```
dotnet ef migrations add
        │
        ▼
┌──────────────────────────────┐
│ 1. Build the project         │
└──────────────┬───────────────┘
               │
               ▼
┌──────────────────────────────┐
│ 2. Execute Program.Main()    │
│    to build the DI container │
│    (WebApplicationBuilder)   │
└──────────────┬───────────────┘
               │
        ┌──────┴──────┐
        │             │
    SUCCESS        FAILURE
        │             │
        ▼             ▼
┌──────────────┐ ┌──────────────────────────┐
│ Resolve      │ │ Fallback to              │
│ DbContext    │ │ IDesignTimeDbContextFactory│
│ from DI      │ │ (ApplicationDbContextFactory)│
└──────────────┘ └──────────────────────────┘
        │             │
        ▼             ▼
┌──────────────────────────────┐
│ 3. Compare model snapshot    │
│    with current DbContext    │
│    model to generate         │
│    migration diff            │
└──────────────────────────────┘
```

In our case, **Step 2 fails** during DI container validation. The container detects that some registered services have unresolvable dependencies. EF Core then falls back to `ApplicationDbContextFactory` which creates the `DbContext` manually with a hardcoded connection string — bypassing DI entirely.

---

## The Three Unresolved Services

### Error 1: `System.TimeProvider`

**Affected services:**
- `ISecurityStampValidator` → `SecurityStampValidator<ApplicationUser>`
- `ITwoFactorSecurityStampValidator` → `TwoFactorSecurityStampValidator<ApplicationUser>`
- `IPostConfigureOptions<SecurityStampValidatorOptions>` → `PostConfigureSecurityStampValidatorOptions`

**Root cause:**

The call to `.AddSignInManager()` in `DependencyInjection.cs` registers `SecurityStampValidator`, which internally depends on `PostConfigureSecurityStampValidatorOptions`. This class was updated in .NET 8 to accept `TimeProvider` for testability:

```csharp
// Inside ASP.NET Core Identity source code
internal sealed class PostConfigureSecurityStampValidatorOptions
    : IPostConfigureOptions<SecurityStampValidatorOptions>
{
    private readonly TimeProvider _timeProvider;

    public PostConfigureSecurityStampValidatorOptions(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }
}
```

`TimeProvider` is a .NET 8 abstraction (`System.TimeProvider`) but it is **not auto-registered** in the DI container. The host does not add it by default.

**Resolution:**

```csharp
builder.Services.AddSingleton(TimeProvider.System);
```

---

### Error 2: `IAuthenticationSchemeProvider`

**Affected service:**
- `SignInManager<ApplicationUser>`

**Root cause:**

`SignInManager` needs to know which authentication schemes are available (e.g., cookies, JWT, external providers). It depends on `IAuthenticationSchemeProvider`, which is registered by `AddAuthentication()`.

```csharp
// Inside SignInManager<TUser> constructor
public SignInManager(
    UserManager<TUser> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<TUser> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<TUser>> logger,
    IAuthenticationSchemeProvider schemes,     // ← this is missing
    IUserConfirmation<TUser> confirmation)
```

Our DI setup calls `.AddSignInManager()` but never calls `AddAuthentication()`, so `IAuthenticationSchemeProvider` is never registered.

**Resolution:**

```csharp
builder.Services.AddAuthentication();
```

---

### Error 3: `IDataProtectionProvider`

**Affected service:**
- `DataProtectorTokenProvider<ApplicationUser>`

**Root cause:**

The call to `.AddDefaultTokenProviders()` registers token providers for:
- Email confirmation tokens
- Password reset tokens
- Two-factor authentication tokens
- Authenticator key tokens

The `DataProtectorTokenProvider` encrypts these tokens using ASP.NET Core's Data Protection system. It depends on `IDataProtectionProvider`:

```csharp
// Inside DataProtectorTokenProvider<TUser> constructor
public DataProtectorTokenProvider(
    IDataProtectionProvider dataProtectionProvider,  // ← this is missing
    IOptions<DataProtectionTokenProviderOptions> options,
    ILogger<DataProtectorTokenProvider<TUser>> logger)
```

`IDataProtectionProvider` is normally registered automatically by the ASP.NET Core host during `WebApplicationBuilder.Build()`. However, when EF Core tooling validates the DI container, it performs eager validation **before** the host fully initializes — catching this gap.

Calling `AddAuthentication()` triggers the registration of Data Protection services as a side effect, which resolves this dependency.

**Resolution:**

```csharp
builder.Services.AddAuthentication();
// This internally calls AddDataProtection() as a dependency
```

---

## Summary Table

| # | Missing Service | Required By | Registered By | Why It Was Missing |
|---|----------------|-------------|---------------|-------------------|
| 1 | `System.TimeProvider` | `SecurityStampValidator`, `TwoFactorSecurityStampValidator` | Manual registration | .NET 8 added this dependency but does not auto-register it |
| 2 | `IAuthenticationSchemeProvider` | `SignInManager<ApplicationUser>` | `AddAuthentication()` | We called `AddSignInManager()` without calling `AddAuthentication()` first |
| 3 | `IDataProtectionProvider` | `DataProtectorTokenProvider<ApplicationUser>` | `AddAuthentication()` / `AddDataProtection()` | Design-time validation runs before the host fully initializes Data Protection |

---

## DI Registration Chain (Visual)

```
AddIdentityCore<ApplicationUser>()
  ├── UserManager<ApplicationUser>
  ├── UserValidator
  ├── PasswordValidator
  ├── PasswordHasher
  └── UserClaimsPrincipalFactory

.AddRoles<ApplicationRole>()
  └── RoleManager<ApplicationRole>

.AddEntityFrameworkStores<ApplicationDbContext>()
  └── UserStore, RoleStore (EF Core backed)

.AddSignInManager()                          ← PROBLEM SOURCE
  ├── SignInManager<ApplicationUser>
  │     └── needs IAuthenticationSchemeProvider  ← ✗ NOT REGISTERED
  ├── SecurityStampValidator
  │     └── needs TimeProvider                   ← ✗ NOT REGISTERED
  └── TwoFactorSecurityStampValidator
        └── needs TimeProvider                   ← ✗ NOT REGISTERED

.AddDefaultTokenProviders()                  ← PROBLEM SOURCE
  └── DataProtectorTokenProvider
        └── needs IDataProtectionProvider        ← ✗ NOT REGISTERED
```

---

## Solution Applied

In `Program.cs`, the following two registrations were added:

```csharp
// Register authentication services (provides IAuthenticationSchemeProvider
// and IDataProtectionProvider)
builder.Services.AddAuthentication();

// Register TimeProvider (required by SecurityStampValidator in .NET 8+)
builder.Services.AddSingleton(TimeProvider.System);
```

### Final `Program.cs` Service Registration Order

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication();                    // ← FIX: auth scheme + data protection
builder.Services.AddInfrastructure(builder.Configuration); // Identity + EF Core + other infra
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(TimeProvider.System);      // ← FIX: TimeProvider for .NET 8

var app = builder.Build();
```

---

## Why the Migration Still Succeeded Without the Fix

EF Core has a two-tier DbContext resolution strategy:

1. **Primary:** Resolve `DbContext` from the application's DI container
2. **Fallback:** Use `IDesignTimeDbContextFactory<TContext>` if the DI container fails

Our project has `ApplicationDbContextFactory` which implements `IDesignTimeDbContextFactory<ApplicationDbContext>`. When the DI container validation failed, EF Core used this factory to create the `DbContext` with a hardcoded connection string — bypassing all DI services.

### Why You Should Still Fix It

Even though migrations work via the factory fallback, a broken DI container means:

- **EF interceptors won't run** at design-time (e.g., `AuditableEntityInterceptor` once activated)
- **Seed data logic** that depends on DI services won't execute
- **`OnModelCreating`** code that resolves services from `DbContext` won't work
- **CI/CD pipelines** may treat these warnings as errors depending on configuration
- It masks real DI issues that could surface at runtime

---

## EF Tools Version Warning

The console also showed:

> The Entity Framework tools version '8.0.0' is older than that of the runtime '8.0.24'.

This is a separate issue. The globally installed `dotnet-ef` tool is version `8.0.0` while the project references EF Core `8.0.24`. Update the tool:

```bash
dotnet tool update --global dotnet-ef
```

---

## References

- [ASP.NET Core Identity Source Code — SecurityStampValidator](https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Core/src/SecurityStampValidator.cs)
- [ASP.NET Core Identity Source Code — SignInManager](https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Core/src/SignInManager.cs)
- [EF Core Design-Time DbContext Creation](https://learn.microsoft.com/en-us/ef/core/cli/dbcontext-creation)
- [.NET 8 TimeProvider Abstraction](https://learn.microsoft.com/en-us/dotnet/api/system.timeprovider)
