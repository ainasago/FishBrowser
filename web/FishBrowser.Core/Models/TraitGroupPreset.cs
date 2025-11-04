using System.ComponentModel.DataAnnotations;

namespace FishBrowser.WPF.Models;

public class TraitGroupPreset
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? Tags { get; set; } // 逗号分隔标签

    [MaxLength(50)]
    public string? Version { get; set; }

    // 该预设包含的 trait 值集合（key->value）
    public string ItemsJson { get; set; } = "{}";
    
    // 创建时间
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
