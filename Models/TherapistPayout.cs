using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace QwenHT.Models
{
    public class CreateTherapistPayout
    {
        public DateTimeOffset PayoutDate { get; set; }

        // Foreign key to Staff (new system uses Guid)
        public Guid StaffId { get; set; }

        [Precision(10, 2)]
        public decimal FootAmount { get; set; }
        [Precision(10, 2)]
        public decimal BodyAmount { get; set; }
        [Precision(10, 2)]
        public decimal StaffAmount { get; set; }
        [Precision(10, 2)]
        public decimal ExtraAmount { get; set; }
        [Precision(10, 2)]
        public decimal IncentiveAmount { get; set; }
        [Precision(10, 2)]
        public decimal TotalAmount { get; set; }
    }

    public class TherapistPayout
    {
        public Guid Id { get; set; }

        public DateTimeOffset PayoutDate { get; set; }

        // Foreign key to Staff (new system uses Guid)
        public Guid StaffId { get; set; }
        public Staff Staff { get; set; } = default!;

        [Precision(10, 2)]
        public decimal FootAmount { get; set; }
        [Precision(10, 2)]
        public decimal BodyAmount { get; set; }
        [Precision(10, 2)]
        public decimal StaffAmount { get; set; }
        [Precision(10, 2)]
        public decimal ExtraAmount { get; set; }
        [Precision(10, 2)]
        public decimal IncentiveAmount { get; set; }
        [Precision(10, 2)]
        public decimal TotalAmount { get; set; }

        public byte Status { get; set; } = 0; // 0 = Inactive, 1 = Active

        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

        [MaxLength(100)]
        public string? LastModifiedBy { get; set; }

    }
}
