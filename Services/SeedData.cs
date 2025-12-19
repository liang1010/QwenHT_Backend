using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

            for (int i = 0; i < 1; i++)
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

            Guid HomeGuid = Guid.NewGuid();
            Guid ManageGuid = Guid.NewGuid();
            Guid DashboardGuid = Guid.NewGuid();
            Guid UserGuid = Guid.NewGuid();
            Guid LandingGuid = Guid.NewGuid();
            Guid NaviGuid = Guid.NewGuid();
            Guid StaffGuid = Guid.NewGuid();
            Guid OptionValueGuid = Guid.NewGuid();
            Guid RoleGuid = Guid.NewGuid();
            Guid MenuGuid = Guid.NewGuid();
            Guid KeyInGuid = Guid.NewGuid();
            Guid SalesGuid = Guid.NewGuid();
            Guid SalesInquiryGuid = Guid.NewGuid();
            Guid SalesSammaryGuid = Guid.NewGuid();
            Guid CommissionGuid = Guid.NewGuid();
            Guid CommissionTherapistGuid = Guid.NewGuid();

            // Seed navigation items if they don't exist
            if (!context.NavigationItems.AsNoTracking().Any())
            {
                // First, add the parent navigation items
                var parentItems = new List<NavigationItem>
                {
                    new NavigationItem
                    {
                        Id = HomeGuid,
                        Name = "Home",
                        Route = "/",
                        Icon = "",
                        Order = 1,
                        IsVisible = true
                    },
                    new NavigationItem
                    {
                        Id = ManageGuid,
                        Name = "Manage",
                        Route = "",
                        Icon = "",
                        Order = 3,
                        IsVisible = true
                    },
                    new NavigationItem
                    {
                        Id = SalesGuid,
                        Name = "Sales",
                        Route = "",
                        Icon = "",
                        Order = 2,
                        IsVisible = true
                    },
                    new NavigationItem
                    {
                        Id = CommissionGuid,
                        Name = "Commission",
                        Route = "",
                        Icon = "",
                        Order = 4,
                        IsVisible = true
                    }
                };

                foreach (var item in parentItems)
                {
                    context.NavigationItems.Add(item);
                }

                // Save the parent items to get their IDs
                await context.SaveChangesAsync();

                // Now add the child navigation items
                var childItems = new List<NavigationItem>
                {
                    new NavigationItem
                    {
                        Id=DashboardGuid,
                        Name = "Dashboard",
                        Route = "/app/dashboard",
                        Icon = "pi pi-fw pi-home",
                        Order = 1,
                        IsVisible = true,
                        ParentId = HomeGuid // Home
                    },
                    new NavigationItem
                    {
                        Id = UserGuid,
                        Name = "User Management",
                        Route = "/app/manage/user",
                        Icon = "pi pi-fw pi-user",
                        Order = 1,
                        IsVisible = true,
                        ParentId = ManageGuid // Manage
                    },
                    new NavigationItem
                    {
                        Id=LandingGuid,
                        Name = "Landing",
                        Route = "/app/landing",
                        Icon = "pi pi-fw pi-globe",
                        Order = 3,
                        IsVisible = true,
                        ParentId = HomeGuid // Home
                    },
                    new NavigationItem
                    {
                        Id = NaviGuid,
                        Name = "Navigation Management",
                        Route = "/app/manage/navigation",
                        Icon = "pi pi-fw pi-sitemap",
                        Order = 4,
                        IsVisible = true,
                        ParentId = ManageGuid // Manage
                    },
                    new NavigationItem
                    {
                        Id = StaffGuid,
                        Name = "Staff Management",
                        Route = "/app/manage/staff",
                        Icon = "pi pi-fw pi-users",
                        Order = 2,
                        IsVisible = true,
                        ParentId = ManageGuid // Manage
                    },
                    new NavigationItem
                    {
                        Id = OptionValueGuid,
                        Name = "Option Values",
                        Route = "/app/manage/option-values",
                        Icon = "pi pi-fw pi-sliders-v", // Using sliders icon for option values
                        Order = 5,
                        IsVisible = true,
                        ParentId = ManageGuid // Manage
                    },
                    new NavigationItem
                    {
                        Id = RoleGuid,
                        Name = "Role Management",
                        Route = "/app/manage/role",
                        Icon = "pi pi-fw pi-shield", // Using sliders icon for option values
                        Order = 5,
                        IsVisible = true,
                        ParentId = ManageGuid // Manage
                    },
                    new NavigationItem
                    {
                        Id=MenuGuid,
                        Name = "Menu Management",
                        Route = "/app/manage/menus",
                        Icon = "pi pi-fw pi-book",
                        Order = 1,
                        IsVisible = true,
                        ParentId = ManageGuid // Home
                    },
                    new NavigationItem
                    {
                        Id=KeyInGuid,
                        Name = "Sales Key In",
                        Route = "/app/sales/key-in",
                        Icon = "pi pi-fw pi-credit-card",
                        Order = 1,
                        IsVisible = true,
                        ParentId = SalesGuid // Home
                    },
                    new NavigationItem
                    {
                        Id=SalesInquiryGuid,
                        Name = "Sales Inquiry",
                        Route = "/app/sales/inquiry",
                        Icon = "pi pi-fw pi-credit-card",
                        Order = 2,
                        IsVisible = true,
                        ParentId = SalesGuid // Home
                    },
                    new NavigationItem
                    {
                        Id=SalesSammaryGuid,
                        Name = "Sales Summary",
                        Route = "/app/sales/summary",
                        Icon = "pi pi-fw pi-credit-card",
                        Order = 2,
                        IsVisible = true,
                        ParentId = SalesGuid // Home
                    },
                    new NavigationItem
                    {
                        Id=CommissionTherapistGuid,
                        Name = "Therapist",
                        Route = "/app/commission/therapist",
                        Icon = "",
                        Order = 1,
                        IsVisible = true,
                        ParentId = CommissionGuid // Home
                    },
                };

                foreach (var item in childItems)
                {
                    context.NavigationItems.Add(item);
                }

                await context.SaveChangesAsync();
            }

            // Seed role-navigation assignments if they don't exist
            if (!context.RoleNavigations.AsNoTracking().Any())
            {
                // Assign navigation items to roles
                var allRoles = new[] { "Admin" };

                // Home - available to all (Parent item - item ID 1)
                foreach (var roleName in allRoles)
                {
                    context.RoleNavigations.Add(new RoleNavigation
                    {
                        RoleName = roleName,
                        NavigationItemId = HomeGuid // Home
                    });
                    context.RoleNavigations.Add(new RoleNavigation
                    {
                        RoleName = roleName,
                        NavigationItemId = ManageGuid // Home
                    });
                    context.RoleNavigations.Add(new RoleNavigation
                    {
                        RoleName = roleName,
                        NavigationItemId = DashboardGuid // Home
                    });
                    context.RoleNavigations.Add(new RoleNavigation
                    {
                        RoleName = roleName,
                        NavigationItemId = UserGuid // Home
                    });
                    context.RoleNavigations.Add(new RoleNavigation
                    {
                        RoleName = roleName,
                        NavigationItemId = LandingGuid // Home
                    });
                    context.RoleNavigations.Add(new RoleNavigation
                    {
                        RoleName = roleName,
                        NavigationItemId = NaviGuid // Home
                    });
                    context.RoleNavigations.Add(new RoleNavigation
                    {
                        RoleName = roleName,
                        NavigationItemId = StaffGuid // Home
                    });
                    context.RoleNavigations.Add(new RoleNavigation
                    {
                        RoleName = roleName,
                        NavigationItemId = OptionValueGuid // Home
                    });
                    context.RoleNavigations.Add(new RoleNavigation
                    {
                        RoleName = roleName,
                        NavigationItemId = RoleGuid // Home
                    });
                    context.RoleNavigations.Add(new RoleNavigation
                    {
                        RoleName = roleName,
                        NavigationItemId = MenuGuid // Home
                    });
                    context.RoleNavigations.Add(new RoleNavigation
                    {
                        RoleName = roleName,
                        NavigationItemId = KeyInGuid // Home
                    });
                    context.RoleNavigations.Add(new RoleNavigation
                    {
                        RoleName = roleName,
                        NavigationItemId = SalesGuid // Home
                    });
                    context.RoleNavigations.Add(new RoleNavigation
                    {
                        RoleName = roleName,
                        NavigationItemId = SalesInquiryGuid // Home
                    });
                    context.RoleNavigations.Add(new RoleNavigation
                    {
                        RoleName = roleName,
                        NavigationItemId = SalesSammaryGuid // Home
                    });
                }
                await context.SaveChangesAsync();

            }

            // Seed option values if they don't exist
            if (!context.OptionValues.AsNoTracking().Any())
            {
                var optionValues = new List<OptionValue>
                {
                    new OptionValue { Id = Guid.NewGuid(), Category = "Hostel", Value = "RIA SELANGOR", Description = "RIA Selangor hostel", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Hostel", Value = "RIA PAHANG", Description = "RIA Pahang hostel", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Hostel", Value = "KAYANGAN", Description = "Kayangan hostel", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Hostel", Value = "--NO HOSTEL NAME--", Description = "No hostel assigned", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },

                    // Bank Names
                    new OptionValue { Id = Guid.NewGuid(), Category = "Bank", Value = "CIMB", Description = "CIMB Bank", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Bank", Value = "HLB", Description = "HLB Bank", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Bank", Value = "MAYBANK - PETTY CASH", Description = "Maybank - Petty Cash", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Bank", Value = "MAYBANK OWN", Description = "Maybank Own", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Bank", Value = "--NO--", Description = "No bank account", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Bank", Value = "PUBLIC BANK OWN", Description = "Public Bank Own", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Bank", Value = "RHB", Description = "RHB Bank", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Bank", Value = "TOUCH N GO", Description = "Touch n Go eWallet", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },

                    // Outlet Names
                    new OptionValue { Id = Guid.NewGuid(), Category = "Outlet", Value = "ALL", Description = "All outlets", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Outlet", Value = "HTA", Description = "HTA outlet", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Outlet", Value = "HTG", Description = "HTG outlet", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Outlet", Value = "HTL", Description = "HTL outlet", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Outlet", Value = "HTSA", Description = "HTSA outlet", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },

                    // Nationalities
                    new OptionValue { Id = Guid.NewGuid(), Category = "Nationality", Value = "CHINA", Description = "Chinese nationality", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Nationality", Value = "INDONESIA", Description = "Indonesian nationality", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Nationality", Value = "MALAYSIA", Description = "Malaysian nationality", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Nationality", Value = "MYANMAR UN", Description = "Myanmar UN", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Nationality", Value = "OWN PERMIT", Description = "Own Permit", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Nationality", Value = "THAILAND", Description = "Thai nationality", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },

                    // Staff Types
                    new OptionValue { Id = Guid.NewGuid(), Category = "Type", Value = "FULLTIME", Description = "Fulltime staff type", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" },
                    new OptionValue { Id = Guid.NewGuid(), Category = "Type", Value = "THERAPIST", Description = "Therapist staff type", IsActive = true, CreatedAt = DateTime.UtcNow, LastUpdated = DateTime.UtcNow, LastModifiedBy = "Seeder" }
                };

                foreach (var optionValue in optionValues)
                {
                    context.OptionValues.Add(optionValue);
                }

                await context.SaveChangesAsync();
            }
        }

    }
}