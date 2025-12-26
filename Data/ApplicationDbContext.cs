using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using QwenHT.Models;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QwenHT.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<NavigationItem> NavigationItems { get; set; }
        public DbSet<RoleNavigation> RoleNavigations { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<StaffEmployment> StaffEmployments { get; set; }
        public DbSet<StaffCompensation> StaffCompensations { get; set; }
        public DbSet<StaffBankAccount> StaffBankAccounts { get; set; }
        public DbSet<OptionValue> OptionValues { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Sales> Sales { get; set; }
        public DbSet<Incentive> Incentives { get; set; }
        public DbSet<TherapistPayout> TherapistPayouts { get; set; }
        public DbSet<ConsultantPayout> ConsultantPayouts { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public override int SaveChanges()
        {
            var auditEntries = CollectAuditEntries();
            var result = base.SaveChanges();

            if (auditEntries.Any())
            {
                // Insert audit logs (they are regular entities)
                AuditLogs.AddRange(auditEntries);
                base.SaveChanges(); // Save audit logs in a second roundtrip
            }

            return result;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var auditEntries = CollectAuditEntries();
            var result = await base.SaveChangesAsync(cancellationToken);

            if (auditEntries.Any())
            {
                AuditLogs.AddRange(auditEntries);
                await base.SaveChangesAsync(cancellationToken);
            }

            return result;
        }

        private List<AuditLog> CollectAuditEntries()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                // 🔥 Exclude AuditLog itself to prevent infinite loop
                .Where(e => e.Entity.GetType() != typeof(AuditLog))
                .ToList();

            var auditLogs = new List<AuditLog>();

            foreach (var entry in entries)
            {
                var entityName = entry.Entity.GetType().Name;
                var id = GetEntityKey(entry)?.ToString() ?? "unknown";

                var audit = new AuditLog
                {
                    TableName = entityName,
                    RecordId = id,
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = GetCurrentUser(), // Implement as needed
                    ActionType = entry.State switch
                    {
                        EntityState.Added => "CREATE",
                        EntityState.Modified => "UPDATE",
                        EntityState.Deleted => "DELETE",
                        _ => "UNKNOWN"
                    }
                };

                var currentJson = JsonSerializer.Serialize(entry.Entity, StandardJsonOptions);

                if (entry.State == EntityState.Added)
                {
                    audit.NewValues = currentJson;
                }
                else if (entry.State == EntityState.Modified)
                {
                    var originalObj = GetDatabaseValuesAsObject(entry);
                    audit.OriginalValues = originalObj != null ? JsonSerializer.Serialize(originalObj, StandardJsonOptions) : null;
                    audit.NewValues = currentJson;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    var originalObj = GetDatabaseValuesAsObject(entry);
                    audit.OriginalValues = originalObj != null ? JsonSerializer.Serialize(originalObj, StandardJsonOptions) : null;
                }

                auditLogs.Add(audit);
            }

            return auditLogs;
        }

        private static readonly JsonSerializerOptions StandardJsonOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        private object? GetDatabaseValuesAsObject(EntityEntry entry)
        {
            try
            {
                var dbValues = entry.State == EntityState.Deleted
                    ? entry.GetDatabaseValues()  // Issues SELECT!
                    : entry.OriginalValues;

                if (dbValues == null) return null;

                // ✅ Correct way: iterate PropertyNames
                var dict = new Dictionary<string, object?>();
                foreach (var propName in dbValues.Properties.Select(p => p.Name))
                {
                    dict[propName] = dbValues[propName];
                }

                return dict;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get original values: {ex.Message}");
                return null;
            }
        }

        private object? GetEntityKey(EntityEntry entry)
        {
            // Better: use EF Core's metadata to get key
            var keyProp = entry.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault();
            if (keyProp != null)
            {
                return entry.Property(keyProp.Name).CurrentValue;
            }

            // Fallback to "Id" property
            return entry.Entity.GetType().GetProperty("Id")?.GetValue(entry.Entity);
        }

        private string? GetCurrentUser()
        {
            // Example: if using ASP.NET Core with claims
            return _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);
            //return "SYSTEM"; // Replace with real user context
        }


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
            builder.Entity<StaffBankAccount>(entity =>
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



            builder.Entity<TherapistPayout>(entity =>
            {
                entity.HasKey(s => s.Id);

                // Relationships
                entity.HasOne(s => s.Staff)
                      .WithMany(st => st.TherapistPayouts)
                      .HasForeignKey(s => s.StaffId)
                      .OnDelete(DeleteBehavior.Restrict); // Avoid cascade delete

                // Configure timestamp fields for Sales
                entity.Property(e => e.CreatedAt);
                entity.Property(e => e.LastUpdated);
            });

            builder.Entity<ConsultantPayout>(entity =>
            {
                entity.HasKey(s => s.Id);

                // Relationships
                entity.HasOne(s => s.Staff)
                      .WithMany(st => st.ConsultantPayouts)
                      .HasForeignKey(s => s.StaffId)
                      .OnDelete(DeleteBehavior.Restrict); // Avoid cascade delete

                // Configure timestamp fields for Sales
                entity.Property(e => e.CreatedAt);
                entity.Property(e => e.LastUpdated);
            });

        }
    }
}
public class AuditLog
{
    public long Id { get; set; }
    public string TableName { get; set; } = null!;
    public string RecordId { get; set; } = null!;
    public string ActionType { get; set; } = null!; // "CREATE", "UPDATE", "DELETE"
    public string? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? OriginalValues { get; set; } // JSON
    public string? NewValues { get; set; }      // JSON
}