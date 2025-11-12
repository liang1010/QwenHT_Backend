// This file contains database configuration and will be referenced in the project
// The schema is automatically handled by ASP.NET Core Identity and Entity Framework
// 
// Key tables that will be created:
// - AspNetUsers: Stores user information (Id, UserName, Email, etc.)
// - AspNetRoles: Stores role definitions (Id, Name, NormalizedName)
// - AspNetUserRoles: Junction table linking users to roles
// - AspNetUserClaims: Stores user claims
// - AspNetRoleClaims: Stores role claims
// - AspNetUserLogins: Stores user login providers (for external auth)
// - AspNetUserTokens: Stores user tokens
//
// Identity Server 8 additional tables:
// - Clients: Identity Server clients
// - ClientGrantTypes: Grant types for clients
// - ClientScopes: Scopes for clients
// - IdentityResources: Identity resources
// - ApiResources: API resources
// - ApiScopes: API scopes
// - PersistedGrants: Token storage
//
// The ApplicationDbContext handles the Identity tables
// The ConfigurationDbContext handles Identity Server configuration
// The PersistedGrantDbContext handles tokens and consents