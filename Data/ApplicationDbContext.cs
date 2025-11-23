using QwenHT.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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
                entity.Property(e => e.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever(); // GUID, not auto-generated
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Route).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Icon).HasMaxLength(200);

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
        }
    }
}