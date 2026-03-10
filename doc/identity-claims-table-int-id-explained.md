# Why AspNetUserClaims & AspNetRoleClaims Have `int` Id Despite Using `Guid` as TKey

## The Question

After customizing ASP.NET Core Identity to use `Guid` as the primary key type:

```csharp
public class ApplicationUser : IdentityUser<Guid> { }
public class ApplicationRole : IdentityRole<Guid> { }
```

And specifying all generic parameters in the `IdentityDbContext`:

```csharp
public class ApplicationDbContext : IdentityDbContext<
    ApplicationUser,
    ApplicationRole,
    Guid,                       // TKey
    IdentityUserClaim<Guid>,
    IdentityUserRole<Guid>,
    IdentityUserLogin<Guid>,
    IdentityRoleClaim<Guid>,
    IdentityUserToken<Guid>>
```

The generated migration shows that `AspNetUserClaims.Id` and `AspNetRoleClaims.Id` are still `int` (integer), not `Guid`. Why?

---

## Short Answer

**This is by design.** The `TKey` generic parameter only controls the **user/role primary keys** and their **foreign key references**. The `Id` property on claims entities is hardcoded as `int` in the Identity source code — it is not affected by `TKey`.

---

## What `TKey` Actually Controls

The third generic parameter (`Guid` in our case) flows through the Identity entity hierarchy like this:

```
TKey = Guid
  │
  ├── AspNetUsers.Id           → Guid ✓  (IdentityUser<TKey>.Id)
  ├── AspNetRoles.Id           → Guid ✓  (IdentityRole<TKey>.Id)
  │
  ├── AspNetUserClaims.UserId  → Guid ✓  (FK to AspNetUsers)
  ├── AspNetUserClaims.Id      → int  ✗  (hardcoded, not affected by TKey)
  │
  ├── AspNetRoleClaims.RoleId  → Guid ✓  (FK to AspNetRoles)
  ├── AspNetRoleClaims.Id      → int  ✗  (hardcoded, not affected by TKey)
  │
  ├── AspNetUserRoles.UserId   → Guid ✓  (composite PK + FK)
  ├── AspNetUserRoles.RoleId   → Guid ✓  (composite PK + FK)
  │
  ├── AspNetUserLogins.UserId  → Guid ✓  (FK to AspNetUsers)
  │
  └── AspNetUserTokens.UserId  → Guid ✓  (FK to AspNetUsers)
```

---

## Proof From Identity Source Code

### `IdentityUserClaim<TKey>`

```csharp
// Source: Microsoft.AspNetCore.Identity.EntityFrameworkCore
public class IdentityUserClaim<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the identifier for this user claim.
    /// </summary>
    public virtual int Id { get; set; }          // ← Always int

    /// <summary>
    /// Gets or sets the primary key of the user associated with this claim.
    /// </summary>
    public virtual TKey UserId { get; set; }      // ← Uses TKey (Guid)

    public virtual string? ClaimType { get; set; }
    public virtual string? ClaimValue { get; set; }
}
```

### `IdentityRoleClaim<TKey>`

```csharp
public class IdentityRoleClaim<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the identifier for this role claim.
    /// </summary>
    public virtual int Id { get; set; }          // ← Always int

    /// <summary>
    /// Gets or sets the of the primary key of the role.
    /// </summary>
    public virtual TKey RoleId { get; set; }      // ← Uses TKey (Guid)

    public virtual string? ClaimType { get; set; }
    public virtual string? ClaimValue { get; set; }
}
```

Notice that `Id` is declared as `int` directly — it does **not** use the `TKey` type parameter.

---

## Why Microsoft Designed It This Way

### 1. Claims Are Dependent Entities

Claims never exist independently. They always belong to a user or role. The relationship is:

```
AspNetUsers (1) ──── (*) AspNetUserClaims
AspNetRoles (1) ──── (*) AspNetRoleClaims
```

No other table references a claim by its `Id`. The `Id` is only a surrogate key for EF Core to perform row-level operations (update, delete a specific claim).

### 2. No External References

Unlike `AspNetUsers.Id` which is referenced by multiple tables (`AspNetUserClaims`, `AspNetUserRoles`, `AspNetUserLogins`, `AspNetUserTokens`), the claims `Id` is referenced by **nothing**. It's a terminal leaf in the schema — no FK points to it.

### 3. Performance

| Type | Size | Index Performance |
|------|------|-------------------|
| `int` | 4 bytes | Compact, sequential, cache-friendly |
| `Guid` | 16 bytes | 4x larger, random, causes page splits |

For a table that could contain thousands of rows per user (especially in claims-heavy authorization systems), the `int` PK is significantly more efficient.

### 4. Auto-Increment Is Sufficient

Claims are typically:
- Created and deleted within a single application context
- Never shared across distributed systems
- Never used as correlation identifiers

An auto-incrementing integer is perfectly suited for this use case.

---

## Complete Table Schema Summary

| Table | PK Column(s) | PK Type | FK Column | FK Type | FK Target |
|-------|-------------|---------|-----------|---------|-----------|
| **AspNetUsers** | `Id` | `Guid` | — | — | — |
| **AspNetRoles** | `Id` | `Guid` | — | — | — |
| **AspNetUserClaims** | `Id` | `int` (auto) | `UserId` | `Guid` | `AspNetUsers.Id` |
| **AspNetRoleClaims** | `Id` | `int` (auto) | `RoleId` | `Guid` | `AspNetRoles.Id` |
| **AspNetUserRoles** | `UserId + RoleId` | `Guid + Guid` | `UserId`, `RoleId` | `Guid` | `AspNetUsers.Id`, `AspNetRoles.Id` |
| **AspNetUserLogins** | `LoginProvider + ProviderKey` | `string + string` | `UserId` | `Guid` | `AspNetUsers.Id` |
| **AspNetUserTokens** | `UserId + LoginProvider + Name` | `Guid + string + string` | `UserId` | `Guid` | `AspNetUsers.Id` |

---

## Entity Relationship Diagram

```
┌─────────────────────┐         ┌─────────────────────┐
│    AspNetUsers       │         │    AspNetRoles       │
├─────────────────────┤         ├─────────────────────┤
│ Id          (Guid)  │◄──┐  ┌─►│ Id          (Guid)  │
│ UserName             │   │  │  │ Name                │
│ Email                │   │  │  │ NormalizedName      │
│ PasswordHash         │   │  │  │ ConcurrencyStamp    │
│ SecurityStamp        │   │  │  └─────────────────────┘
│ ...                  │   │  │           │
└─────────────────────┘   │  │           │ 1
          │                │  │           │
          │ 1              │  │  ┌────────┴────────────┐
          │                │  │  │  AspNetRoleClaims    │
 ┌────────┴────────────┐  │  │  ├─────────────────────┤
 │  AspNetUserClaims    │  │  │  │ Id       (int, auto)│
 ├─────────────────────┤  │  │  │ RoleId   (Guid) FK──┘
 │ Id       (int, auto)│  │  │  │ ClaimType           │
 │ UserId   (Guid) FK──┘  │  │  │ ClaimValue          │
 │ ClaimType             │  │  └─────────────────────┘
 │ ClaimValue            │  │
 └───────────────────────┘  │
                            │
 ┌───────────────────────┐  │
 │  AspNetUserRoles       │  │
 ├───────────────────────┤  │
 │ UserId   (Guid) FK────┘  │
 │ RoleId   (Guid) FK───────┘
 └───────────────────────┘
```

---

## Can You Override It?

Yes, but it's **not recommended**. If you absolutely need `Guid` on claims:

```csharp
public class ApplicationUserClaim : IdentityUserClaim<Guid>
{
    public new Guid Id { get; set; }
}

public class ApplicationRoleClaim : IdentityRoleClaim<Guid>
{
    public new Guid Id { get; set; }
}
```

Then update `ApplicationDbContext`:

```csharp
public class ApplicationDbContext : IdentityDbContext<
    ApplicationUser,
    ApplicationRole,
    Guid,
    ApplicationUserClaim,       // ← custom claim entity
    IdentityUserRole<Guid>,
    IdentityUserLogin<Guid>,
    ApplicationRoleClaim,       // ← custom claim entity
    IdentityUserToken<Guid>>
```

**Why you shouldn't do this:**
- Adds complexity for zero architectural benefit
- Breaks the framework's expected behavior
- `Guid` PKs on high-volume child tables cause index fragmentation
- No consumer ever looks up a claim by its `Id` — claims are always queried by `UserId`/`RoleId`

---

## References

- [IdentityUserClaim<TKey> Source Code](https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Stores/src/IdentityUserClaim.cs)
- [IdentityRoleClaim<TKey> Source Code](https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Stores/src/IdentityRoleClaim.cs)
- [Customize Identity Model — Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model)
