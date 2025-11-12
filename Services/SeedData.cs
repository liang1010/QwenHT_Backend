using QwenHT.Data;
using QwenHT.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace QwenHT.Services
{
    public static class SeedData
    {
        public static async Task EnsureSeedData(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync();

            // Navigation tables are part of ApplicationDbContext now
            // We'll use the same context for navigation data

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Create roles if they don't exist
            var roles = new[] { "Admin", "Supervisor", "User", "Guest" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create default admin user if it doesn't exist
            var defaultAdminEmail = "admin@abc.com";
            var defaultAdmin = await userManager.FindByEmailAsync(defaultAdminEmail);

            if (defaultAdmin == null)
            {
                defaultAdmin = new ApplicationUser
                {
                    UserName = "2700013",
                    Email = defaultAdminEmail,
                    FirstName = "Default",
                    LastName = "Admin",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(defaultAdmin, "Password@1");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(defaultAdmin, "Admin");
                }
            }

            // Create other default users as needed
            var supervisorUser = await userManager.FindByEmailAsync("supervisor@abc.com");
            if (supervisorUser == null)
            {
                supervisorUser = new ApplicationUser
                {
                    UserName = "2700098",
                    Email = "supervisor@abc.com",
                    FirstName = "Default",
                    LastName = "Supervisor",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(supervisorUser, "Password@1");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(supervisorUser, "Supervisor");
                }
            }

            for (int i = 0; i < 100; i++)
            {
                supervisorUser = new ApplicationUser
                {
                    UserName = $"2700098{i}",
                    Email = $"supervisor{i}@abc.com",
                    FirstName = $"Default{i}",
                    LastName = $"Supervisor{i}",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(supervisorUser, "Password@1");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(supervisorUser, "Supervisor");
                }
            }

            // Seed navigation items if they don't exist
            if (!context.NavigationItems.AsNoTracking().Any())
            {
                var navigationItems = new List<NavigationItem>
                {
                    new NavigationItem
                    {
                        Name = "Dashboard",
                        Route = "/dashboard",
                        Icon = "fa fa-tachometer-alt",
                        Order = 1,
                        IsVisible = true
                    },
                    new NavigationItem
                    {
                        Name = "Users",
                        Route = "/users",
                        Icon = "fa fa-users",
                        Order = 2,
                        IsVisible = true
                    },
                    new NavigationItem
                    {
                        Name = "Reports",
                        Route = "/reports",
                        Icon = "fa fa-chart-bar",
                        Order = 3,
                        IsVisible = true
                    }
                };

                foreach (var item in navigationItems)
                {
                    context.NavigationItems.Add(item);
                }

                await context.SaveChangesAsync();
            }

            // Seed role-navigation assignments if they don't exist
            if (!context.RoleNavigations.AsNoTracking().Any())
            {
                // Assign navigation items to roles
                var allRoles = new[] { "Admin", "Supervisor", "User", "Guest" };
                var supervisorAndAboveRoles = new[] { "Admin", "Supervisor" };

                // All users can see Dashboard
                foreach (var roleName in allRoles)
                {
                    context.RoleNavigations.Add(new RoleNavigation
                    {
                        RoleName = roleName,
                        NavigationItemId = 1 // Dashboard
                    });
                    await context.SaveChangesAsync();
                }

                // Only Supervisor and above can see Users
                foreach (var roleName in supervisorAndAboveRoles)
                {
                    context.RoleNavigations.Add(new RoleNavigation
                    {
                        RoleName = roleName,
                        NavigationItemId = 2 // Users
                    });
                    await context.SaveChangesAsync();
                }

                // Only Admin can see Reports
                context.RoleNavigations.Add(new RoleNavigation
                {
                    RoleName = "Admin",
                    NavigationItemId = 3 // Reports
                });
                await context.SaveChangesAsync();

            }
        }
    }
}