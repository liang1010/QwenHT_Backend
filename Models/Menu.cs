using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace QwenHT.Models
{
    public class Menu
    {
        public Guid Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = default!;

        [Required, MaxLength(255)]
        public string Description { get; set; } = default!;

        [Required, MaxLength(50)]
        public string Category { get; set; } = default!; // "PRODUCT" or "TREATMENT"

        public int FootMins { get; set; }

        public int BodyMins { get; set; }

        [Precision(10, 2)]
        public decimal StaffCommission { get; set; }

        [Precision(10, 2)]
        public decimal ExtraCommission { get; set; }

        [Precision(10, 2)]
        [Required]
        public decimal Price { get; set; }

        public byte Status { get; set; } = 1; // 1 = Active, 0 = Inactive

        [MaxLength(100)]
        public string? CreatedBy { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
        public string? LastModifiedBy { get; set; }
        public ICollection<Sales> SalesRecords { get; set; } = new List<Sales>();
    }
}
