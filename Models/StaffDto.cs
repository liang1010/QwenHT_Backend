using System.ComponentModel.DataAnnotations;

namespace QwenHT.Models
{
    public class StaffDto
    {
        public Guid Id { get; set; }

        [MaxLength(100)]
        public string? NickName { get; set; }

        [Required, MaxLength(255)]
        public string FullName { get; set; } = default!;

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

        public string? Outlet { get; set; }

        public string? Type { get; set; }

        public DateTime? CheckIn { get; set; }

        public DateTime? CheckOut { get; set; }

        public decimal? FootRatePerHour { get; set; }

        public decimal? BodyRatePerHour { get; set; }

        public int? CommissionBasePercentage { get; set; }

        public decimal? GuaranteeIncome { get; set; }

        public string? BankName { get; set; }

        public string? AccountHolderName { get; set; }

        public string? AccountNumber { get; set; }

        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? LastModifiedBy { get; set; }
    }

    public class CreateStaffDto
    {
        [MaxLength(100)]
        public string? NickName { get; set; }

        [Required, MaxLength(255)]
        public string FullName { get; set; } = default!;

        [MaxLength(20)]
        public string? PhoneNo { get; set; }

        [MaxLength(20)]
        public string? Nationality { get; set; }

        [MaxLength(20)]
        public string? HostelName { get; set; }

        [MaxLength(50)]
        public string? HostelRoom { get; set; }

        public string? Reference { get; set; }
    }

    public class UpdateStaffDto
    {
        [MaxLength(100)]
        public string? NickName { get; set; }

        [MaxLength(255)]
        public string FullName { get; set; } = default!;

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

        public string? Outlet { get; set; }

        public string? Type { get; set; }

        public DateTime? CheckIn { get; set; }

        public DateTime? CheckOut { get; set; }

        public decimal? FootRatePerHour { get; set; }

        public decimal? BodyRatePerHour { get; set; }

        public int? CommissionBasePercentage { get; set; }

        public decimal? GuaranteeIncome { get; set; }

        public string? BankName { get; set; }

        public string? AccountHolderName { get; set; }

        public string? AccountNumber { get; set; }
    }
}