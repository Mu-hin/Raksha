using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Raksha.Infrastructure.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Use a hardcoded connection string for design-time only
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=authentication_authorization_demo;Username=myuser;Password=mypassword");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}

/*
this file was not present when throwing this error during migration:

PM>dotnet ef migrations add InitialCreate --project src/Raksha.Infrastructure --startup-project src/Raksha.Api --context ApplicationDbContext
Build started...
Build succeeded.
An error occurred while accessing the Microsoft.Extensions.Hosting services. Continuing without the application service provider. Error: Some services are not able to be constructed (Error while validating the service descriptor 'ServiceType: Microsoft.AspNetCore.Identity.ISecurityStampValidator Lifetime: Scoped ImplementationType: Microsoft.AspNetCore.Identity.SecurityStampValidator`1[Raksha.Infrastructure.Identity.ApplicationUser]': Unable to resolve service for type 'System.TimeProvider' while attempting to activate 'Microsoft.AspNetCore.Identity.IdentityBuilderExtensions+PostConfigureSecurityStampValidatorOptions'.) (Error while validating the service descriptor 'ServiceType: Microsoft.AspNetCore.Identity.ITwoFactorSecurityStampValidator Lifetime: Scoped ImplementationType: Microsoft.AspNetCore.Identity.TwoFactorSecurityStampValidator`1[Raksha.Infrastructure.Identity.ApplicationUser]': Unable to resolve service for type 'System.TimeProvider' while attempting to activate 'Microsoft.AspNetCore.Identity.IdentityBuilderExtensions+PostConfigureSecurityStampValidatorOptions'.) (Error while validating the service descriptor 'ServiceType: Microsoft.Extensions.Options.IPostConfigureOptions`1[Microsoft.AspNetCore.Identity.SecurityStampValidatorOptions] Lifetime: Singleton ImplementationType: Microsoft.AspNetCore.Identity.IdentityBuilderExtensions+PostConfigureSecurityStampValidatorOptions': Unable to resolve service for type 'System.TimeProvider' while attempting to activate 'Microsoft.AspNetCore.Identity.IdentityBuilderExtensions+PostConfigureSecurityStampValidatorOptions'.) (Error while validating the service descriptor 'ServiceType: Microsoft.AspNetCore.Identity.SignInManager`1[Raksha.Infrastructure.Identity.ApplicationUser] Lifetime: Scoped ImplementationType: Microsoft.AspNetCore.Identity.SignInManager`1[Raksha.Infrastructure.Identity.ApplicationUser]': Unable to resolve service for type 'Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider' while attempting to activate 'Microsoft.AspNetCore.Identity.SignInManager`1[Raksha.Infrastructure.Identity.ApplicationUser]'.) (Error while validating the service descriptor 'ServiceType: Microsoft.AspNetCore.Identity.DataProtectorTokenProvider`1[Raksha.Infrastructure.Identity.ApplicationUser] Lifetime: Transient ImplementationType: Microsoft.AspNetCore.Identity.DataProtectorTokenProvider`1[Raksha.Infrastructure.Identity.ApplicationUser]': Unable to resolve service for type 'Microsoft.AspNetCore.DataProtection.IDataProtectionProvider' while attempting to activate 'Microsoft.AspNetCore.Identity.DataProtectorTokenProvider`1[Raksha.Infrastructure.Identity.ApplicationUser]'.)
Unable to create a 'DbContext' of type 'ApplicationDbContext'. The exception 'Unable to resolve service for type 'Microsoft.EntityFrameworkCore.DbContextOptions`1[Raksha.Infrastructure.Data.ApplicationDbContext]' while attempting to activate 'Raksha.Infrastructure.Data.ApplicationDbContext'.' was thrown while attempting to create an instance. For the different patterns supported at design time, see https://go.microsoft.com/fwlink/?linkid=851728

*/