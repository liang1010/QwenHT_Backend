using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace QwenHT.Models
{
    public class Sales
    {
        public Guid Id { get; set; }

        public DateTimeOffset SalesDate { get; set; }

        // Foreign key to Staff (new system uses Guid)
        public Guid StaffId { get; set; }
        public Staff Staff { get; set; } = default!;

        // Outlet as string (or change to Guid if you create Outlet entity)
        [Required, MaxLength(100)]
        public string Outlet { get; set; } = default!;

        // Foreign key to Menu
        public Guid MenuId { get; set; }
        public Menu Menu { get; set; } = default!;

        public bool? Request { get; set; }

        public bool? FootCream { get; set; }

        public bool? Oil { get; set; }

        [Precision(10, 2)]
        public decimal Price { get; set; }

        [Precision(10, 2)]
        public decimal ExtraCommission { get; set; }

        [Precision(10, 2)]
        public decimal StaffCommission { get; set; } // renamed from [Staff] for clarity

        [MaxLength(500)]
        public string? Remark { get; set; }

        public byte Status { get; set; } = 0; // 0 = Inactive, 1 = Active

        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

        [MaxLength(100)]
        public string? LastModifiedBy { get; set; }
    }
}
