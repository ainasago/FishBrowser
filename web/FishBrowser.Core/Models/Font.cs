using System;
using System.ComponentModel.DataAnnotations;

namespace FishBrowser.WPF.Models
{
    public class Font
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(160)]
        public string Name { get; set; } = string.Empty;

        // 可选别名，JSON 数组字符串
        public string? AliasesJson { get; set; }

        // Serif | Sans | Mono | Symbol | Emoji
        [MaxLength(16)]
        public string Category { get; set; } = "Sans";

        // Microsoft | Google | Apple | Adobe | Other
        [MaxLength(24)]
        public string Vendor { get; set; } = "Other";

        // ["Windows","macOS","Linux","Android","iOS"]
        public string? OsSupportJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
