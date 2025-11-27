using System.ComponentModel.DataAnnotations;

namespace QwenHT.Models
{
    public class OptionValue
    {
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string Category { get; set; } = default!; // e.g., "Outlet", "Type", "Nationality", "Bank"

        [Required, MaxLength(255)]
        public string Value { get; set; } = default!; // e.g., "RIA SELANGOR", "Therapist", "MALAYSIA"

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}