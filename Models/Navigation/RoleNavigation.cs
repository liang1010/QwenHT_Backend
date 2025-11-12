using QwenHT.Models;

namespace QwenHT.Models
{
    public class RoleNavigation
    {
        public string RoleName { get; set; } = string.Empty;
        public int NavigationItemId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public NavigationItem NavigationItem { get; set; } = null!;
    }
}