using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QwenHT.Models;
using System.Reflection.Emit;

namespace QwenHT.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<NavigationItem> NavigationItems { get; set; }
        public DbSet<RoleNavigation> RoleNavigations { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<StaffEmployment> StaffEmployments { get; set; }
        public DbSet<StaffCompensation> StaffCompensations { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<OptionValue> OptionValues { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Sales> Sales { get; set; }
        public DbSet<Incentive> Incentives { get; set; }



        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure the custom ApplicationUser entity
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FirstName)
                    .HasMaxLength(100);

                entity.Property(e => e.LastName)
                    .HasMaxLength(100);
            });

            // Configure NavigationItem
            builder.Entity<NavigationItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever(); // GUID, not auto-generated
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Route).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Icon).HasMaxLength(200);

                // Configure timestamp and modifier fields
                entity.Property(e => e.CreatedBy).HasDefaultValue("Migration");
                // Using database-agnostic default value for UTC timestamp
                entity.Property(e => e.CreatedAt);
                entity.Property(e => e.LastUpdated);

                // Configure the self-referencing relationship
                entity.HasOne(n => n.Parent)
                      .WithMany(n => n.Children)
                      .HasForeignKey(n => n.ParentId)
                      .HasConstraintName("FK_NavigationItem_Parent_NavigationItem") // Add constraint name for clarity
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure RoleNavigation
            builder.Entity<RoleNavigation>(entity =>
            {
                entity.HasKey(e => new { e.RoleName, e.NavigationItemId });
                entity.Property(e => e.RoleName).IsRequired().HasMaxLength(256);

                entity.HasOne(rn => rn.NavigationItem)
                      .WithMany(ni => ni.RoleNavigations)
                      .HasForeignKey(rn => rn.NavigationItemId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Staff
            builder.Entity<Staff>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever(); // Guid generated in C#
            });

            // StaffEmployment
            builder.Entity<StaffEmployment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.HasOne(e => e.Staff)
                      .WithMany(s => s.Employments)
                      .HasForeignKey(e => e.StaffId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // StaffCompensation
            builder.Entity<StaffCompensation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.HasOne(e => e.Staff)
                      .WithMany(s => s.Compensations)
                      .HasForeignKey(e => e.StaffId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // BankAccount
            builder.Entity<BankAccount>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.HasOne(e => e.Staff)
                      .WithMany(s => s.BankAccounts)
                      .HasForeignKey(e => e.StaffId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // OptionValue
            builder.Entity<OptionValue>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Value).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.CreatedBy).HasDefaultValue("Migration");
                entity.Property(e => e.CreatedAt);
                entity.Property(e => e.LastUpdated);
                entity.Property(e => e.IsDeletable).HasDefaultValue(true);

                entity.HasIndex(e => new { e.Category, e.Value }).IsUnique(); // Prevent duplicate values in same category
            });

            // Menu
            builder.Entity<Menu>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Optional: Add index for common queries
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.Status);

                // Ensure Category is only PRODUCT or TREATMENT (optional, enforced in app layer)
                // EF Core doesn't support CHECK constraints via attributes (use Fluent API if needed)
            });

            builder.Entity<Sales>(entity =>
            {
                entity.HasKey(s => s.Id);

                // Relationships
                entity.HasOne(s => s.Staff)
                      .WithMany(st => st.SalesRecords)
                      .HasForeignKey(s => s.StaffId)
                      .OnDelete(DeleteBehavior.Restrict); // Avoid cascade delete

                entity.HasOne(s => s.Menu)
                      .WithMany(m => m.SalesRecords)
                      .HasForeignKey(s => s.MenuId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Indexes for performance
                entity.HasIndex(s => s.SalesDate);
                entity.HasIndex(s => s.Outlet);
                entity.HasIndex(s => s.Status);
                entity.HasIndex(s => s.StaffId);
                entity.HasIndex(s => s.MenuId);

                // Configure timestamp fields for Sales
                entity.Property(e => e.CreatedAt);
                entity.Property(e => e.LastUpdated);
            });

            // Menu
            builder.Entity<Menu>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configure timestamp fields for Menu
                entity.Property(e => e.CreatedAt);
                entity.Property(e => e.LastUpdated);

                // Optional: Add index for common queries
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.Status);
            });

            // Staff
            builder.Entity<Staff>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever(); // Guid generated in C#

                // Configure timestamp fields for Staff
                entity.Property(e => e.CreatedAt);
                entity.Property(e => e.LastUpdated);
            });

            // ApplicationUser
            builder.Entity<ApplicationUser>(entity =>
            {
                // Configure timestamp fields for ApplicationUser
                entity.Property(e => e.CreatedAt);
            });

            builder.Entity<Incentive>(entity =>
            {
                entity.HasKey(s => s.Id);

                // Relationships
                entity.HasOne(s => s.Staff)
                      .WithMany(st => st.IncentiveRecords)
                      .HasForeignKey(s => s.StaffId)
                      .OnDelete(DeleteBehavior.Restrict); // Avoid cascade delete

                // Configure timestamp fields for Sales
                entity.Property(e => e.CreatedAt);
                entity.Property(e => e.LastUpdated);
            });

        }
    }
}