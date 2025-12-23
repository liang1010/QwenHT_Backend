using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace QwenHT.Models
{
    public class CreateIncentive
    {
        public DateTimeOffset IncentiveDate { get; set; }

        // Foreign key to Staff (new system uses Guid)
        public Guid StaffId { get; set; }

        // Outlet as string (or change to Guid if you create Outlet entity)
        [Required, MaxLength(100)]
        public string Description { get; set; } = default!;

        [MaxLength(500)]
        public string? Remark { get; set; }

        [Precision(10, 2)]
        public decimal Amount { get; set; }
    }
    public class Incentive
    {
        public Guid Id { get; set; }

        public DateTimeOffset IncentiveDate { get; set; }

        // Foreign key to Staff (new system uses Guid)
        public Guid StaffId { get; set; }
        public Staff Staff { get; set; } = default!;

        // Outlet as string (or change to Guid if you create Outlet entity)
        [Required, MaxLength(100)]
        public string Description { get; set; } = default!;

        [MaxLength(500)]
        public string? Remark { get; set; }

        [Precision(10, 2)]
        public decimal Amount { get; set; }

        public byte Status { get; set; } = 0; // 0 = Inactive, 1 = Active

        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

        [MaxLength(100)]
        public string? LastModifiedBy { get; set; }

    }
}
