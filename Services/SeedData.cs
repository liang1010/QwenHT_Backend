using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QwenHT.Data;
using QwenHT.Models;

namespace QwenHT.Services
{
    public static class SeedData
    {
        public static async Task EnsureSeedData(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.EnsureCreatedAsync();

            await SeedRolesAsync(roleManager);
            await SeedUsersAsync(userManager);
            await SeedNavigationItemsAsync(context);
            await SeedRoleNavigationPermissionsAsync(context);
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            var roles = new[] { "Admin", "Supervisor", "User", "Guest" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
        {
            var users = new[]
            {
                new { Email = "admin@abc.com", Username = "admin", FirstName = "Default", LastName = "Admin", Role = "Admin" },
            };

            foreach (var userDef in users)
            {
                if (await userManager.FindByEmailAsync(userDef.Email) == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = userDef.Username,
                        Email = userDef.Email,
                        FirstName = userDef.FirstName,
                        LastName = userDef.LastName,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, "Password@1");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, userDef.Role);
                    }
                }
            }
        }

        private static async Task SeedNavigationItemsAsync(ApplicationDbContext context)
        {
            if (context.NavigationItems.Any()) return;

            // Define parent items
            var home = new NavigationItem { Name = "Home", Route = "/", Order = 1, IsVisible = true };
            var manage = new NavigationItem { Name = "Manage", Route = "", Order = 5, IsVisible = true };
            var sales = new NavigationItem { Name = "Sales", Route = "", Order = 2, IsVisible = true };
            var commission = new NavigationItem { Name = "Commission", Route = "", Order = 3, IsVisible = true };
            var payout = new NavigationItem { Name = "Payout", Route = "", Order = 4, IsVisible = true };

            // Add parents first
            context.NavigationItems.AddRange(home, manage, sales, commission, payout);
            await context.SaveChangesAsync(); // Ensures IDs are generated

            // Define children
            var children = new List<NavigationItem>
            {
                new() { Name = "Dashboard", Route = "/app/dashboard", Icon = "pi pi-fw pi-home", Order = 1, IsVisible = true, ParentId = home.Id },
                new() { Name = "User Management", Route = "/app/manage/user", Icon = "pi pi-fw pi-user", Order = 1, IsVisible = true, ParentId = manage.Id },
                new() { Name = "Navigation Management", Route = "/app/manage/navigation", Icon = "pi pi-fw pi-sitemap", Order = 4, IsVisible = true, ParentId = manage.Id },
                new() { Name = "Staff Management", Route = "/app/manage/staff", Icon = "pi pi-fw pi-users", Order = 2, IsVisible = true, ParentId = manage.Id },
                new() { Name = "Option Values", Route = "/app/manage/option-values", Icon = "pi pi-fw pi-sliders-v", Order = 5, IsVisible = true, ParentId = manage.Id },
                new() { Name = "Role Management", Route = "/app/manage/role", Icon = "pi pi-fw pi-shield", Order = 6, IsVisible = true, ParentId = manage.Id },
                new() { Name = "Menu Management", Route = "/app/manage/menus", Icon = "pi pi-fw pi-book", Order = 7, IsVisible = true, ParentId = manage.Id },

                new() { Name = "Sales Key In", Route = "/app/sales/key-in", Icon = "pi pi-pencil", Order = 1, IsVisible = true, ParentId = sales.Id },
                new() { Name = "Sales Inquiry", Route = "/app/sales/inquiry", Icon = "pi pi-fw pi-credit-card", Order = 2, IsVisible = true, ParentId = sales.Id },
                new() { Name = "Sales Summary", Route = "/app/sales/summary", Icon = "pi pi-fw pi-chart-line", Order = 3, IsVisible = true, ParentId = sales.Id },

                new() { Name = "Therapist", Route = "/app/commission/therapist", Icon = "pi pi-calculator", Order = 1, IsVisible = true, ParentId = commission.Id },
                new() { Name = "Consultant", Route = "/app/commission/consultant", Icon = "pi pi-calculator", Order = 2, IsVisible = true, ParentId = commission.Id },
                new() { Name = "Settings", Route = "/app/commission/setting", Icon = "pi pi-cog", Order = 3, IsVisible = true, ParentId = commission.Id },

                new() { Name = "Therapist", Route = "/app/payout/therapist", Icon = "pi pi-money-bill", Order = 1, IsVisible = true, ParentId = payout.Id },
                new() { Name = "Consultant", Route = "/app/payout/consultant", Icon = "pi pi-money-bill", Order = 2, IsVisible = true, ParentId = payout.Id }
            };

            context.NavigationItems.AddRange(children);
            await context.SaveChangesAsync();
        }

        private static async Task SeedRoleNavigationPermissionsAsync(ApplicationDbContext context)
        {
            if (context.RoleNavigations.Any()) return;

            // Get all navigation item IDs
            var allNavIds = await context.NavigationItems.Select(n => n.Id).ToListAsync();

            // Assign all nav items to "Admin" (or customize per role)
            var adminPermissions = allNavIds.Select(navId => new RoleNavigation
            {
                RoleName = "Admin",
                NavigationItemId = navId
            });

            context.RoleNavigations.AddRange(adminPermissions);

            await context.SaveChangesAsync();
        }
    }
}