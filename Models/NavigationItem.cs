using System.ComponentModel.DataAnnotations;

namespace QwenHT.Models
{
    public class NavigationItem
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Icon { get; set; }

        [Required]
        [MaxLength(200)]
        public string Route { get; set; } = string.Empty;

        public Guid? ParentId { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public NavigationItem? Parent { get; set; }

        public ICollection<NavigationItem> Children { get; set; } = new List<NavigationItem>();

        public int Order { get; set; } = 0;

        public bool IsVisible { get; set; } = true;


        [MaxLength(100)]
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public string? LastModifiedBy { get; set; }

        public ICollection<RoleNavigation> RoleNavigations { get; set; } = new List<RoleNavigation>();
    }
}