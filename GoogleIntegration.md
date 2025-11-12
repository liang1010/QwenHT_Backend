// Google Integration Documentation for QwenHT
//
// To enable Google authentication in the future, follow these steps:
//
// 1. Register your application with Google:
//    - Go to https://console.developers.google.com/
//    - Create a new project or select an existing one
//    - Enable the Google+ API
//    - Create credentials (OAuth 2.0 client ID)
//    - Add your application's domain to authorized domains
//    - Add redirect URIs (e.g., https://localhost:5001/signin-google)
//
// 2. Add Google ClientId and ClientSecret to your configuration:
//    - Update appsettings.json with your Google credentials
//    - Use User Secrets in development: dotnet user-secrets set "Authentication:Google:ClientId" "your-client-id"
//    - Use environment variables or Azure Key Vault in production
//
// 3. The Google authentication is already added in Program.cs:
//    builder.Services.AddAuthentication()
//        .AddGoogle(options =>
//        {
//            options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
//            options.ClientSecret = builder.Configuration["Authentication:Google:Secret"];
//        });
//
// 4. When a user signs in with Google, their information will be processed by the
//    QwenHT Identity system and can be mapped to your ApplicationUser model.
//
// 5. You may need to handle the creation of users that sign in via Google.
//    Consider implementing an IUserClaimsPrincipalFactory<ApplicationUser> 
//    to customize claims for Google-authenticated users.
//
// 6. For role assignment, you might want to implement custom logic in your
//    Account controller to assign appropriate roles to Google-authenticated users.
//
// 7. Make sure to test the integration thoroughly, especially the user creation
//    and role assignment processes.