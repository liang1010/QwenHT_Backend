using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QwenHT.Authorization;
using QwenHT.Data;
using QwenHT.Models;
using QwenHT.Services;
using System.Text;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
            ValidAudience = builder.Configuration["JWT:ValidAudience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"])),
            ClockSkew = TimeSpan.Zero, // ← ADD THIS
        };
    });
    //.AddGoogle(options =>
    //{
    //    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    //    options.ClientSecret = builder.Configuration["Authentication:Google:Secret"];
    //});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("*") // Add other origins as needed
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddScoped<INavigationService, NavigationService>();
builder.Services.AddScoped<IOptionValueService, OptionValueService>();

// Add custom authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("NavigationAccess", policy =>
        policy.Requirements.Add(new NavigationRequirement()));
});

builder.Services.AddScoped<IAuthorizationHandler, NavigationAuthorizationHandler>();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
        // Add custom DateTime converters to ensure proper UTC handling
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new QwenHT.Utilities.UtcJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new QwenHT.Utilities.NullableUtcJsonConverter());
    });

// Set the default culture to UTC to ensure consistent date/time handling
var utcTimeZone = TimeZoneInfo.Utc;
var cultureInfo = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

// Add CORS middleware after UseRouting and before UseAuthentication
app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

// API only - no static file serving for Angular app
// Angular app will be served separately during development
// or built and served from a different location in production

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Apply migrations and seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    
    // Apply pending migrations
    await context.Database.MigrateAsync();
    
    // Seed the data
    await SeedData.EnsureSeedData(services);
}

app.Run();