using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace QwenHT.Models
{
    public class Staff
    {
        public Guid Id { get; set; }

        [MaxLength(100)]
        public string? NickName { get; set; }

        [Required, MaxLength(255)]
        public string FullName { get; set; } = default!;

        [MaxLength(100)]
        public string? Gender { get; set; }

        [MaxLength(20)]
        public string? PhoneNo { get; set; }

        [MaxLength(20)]
        public string? Nationality { get; set; }

        [MaxLength(20)]
        public string? HostelName { get; set; }

        [MaxLength(50)]
        public string? HostelRoom { get; set; }

        public string? Reference { get; set; }

        public byte Status { get; set; } = 0; // 0 = Inactive, 1 = Active

        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

        [MaxLength(100)]
        public string? LastModifiedBy { get; set; }

        // Navigation properties
        public ICollection<StaffEmployment> Employments { get; set; } = new List<StaffEmployment>();
        public ICollection<StaffCompensation> Compensations { get; set; } = new List<StaffCompensation>();
        public ICollection<BankAccount> BankAccounts { get; set; } = new List<BankAccount>();
        public ICollection<Sales> SalesRecords { get; set; } = new List<Sales>();
    }

    public class StaffEmployment
    {
        public Guid Id { get; set; }

        public Guid StaffId { get; set; }
        public Staff Staff { get; set; } = default!;

        [Required, MaxLength(255)]
        public string Outlet { get; set; } = default!;

        [Required, MaxLength(50)]
        public string Type { get; set; } = default!; // e.g., "Therapist", "Consultant"

        public DateTimeOffset? CheckIn { get; set; }
        public DateTimeOffset? CheckOut { get; set; }
    }

    public class StaffCompensation
    {
        public Guid Id { get; set; }

        public Guid StaffId { get; set; }
        public Staff Staff { get; set; } = default!;

        public bool IsRate { get; set; } = false;

        [Precision(5, 2)]
        public decimal FootRatePerHour { get; set; }

        [Precision(5, 2)]
        public decimal BodyRatePerHour { get; set; }

        public bool IsCommissionPercentage { get; set; } = false;

        [Range(0, 100)]
        public int CommissionBasePercentage { get; set; }

        public bool IsGuaranteeIncome { get; set; } = false;

        [Precision(10, 2)]
        public decimal GuaranteeIncome { get; set; }
    }

    public class BankAccount
    {
        public Guid Id { get; set; }

        public Guid StaffId { get; set; }
        public Staff Staff { get; set; } = default!;

        [Required, MaxLength(255)]
        public string BankName { get; set; } = default!;

        [Required, MaxLength(255)]
        public string AccountHolderName { get; set; } = default!;

        [Required, MaxLength(50)]
        public string AccountNumber { get; set; } = default!;
    }
}
