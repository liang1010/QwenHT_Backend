using QwenHT.Models;

namespace QwenHT.Models
{
    public class RoleNavigation
    {
        public string RoleName { get; set; } = string.Empty;
        public Guid NavigationItemId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public NavigationItem NavigationItem { get; set; } = null!;
    }
}