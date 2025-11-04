using System.ComponentModel.DataAnnotations;

namespace FishBrowser.WPF.Models;

public class TraitOption
{
    [Key]
    public int Id { get; set; }

    public int TraitDefinitionId { get; set; }
    public TraitDefinition TraitDefinition { get; set; } = null!;

    // 具体值（任意 JSON）
    public string ValueJson { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Label { get; set; }

    // 权重（用于随机）
    public double Weight { get; set; } = 1.0;

    // 可选的目标场景
    [MaxLength(50)]
    public string? Region { get; set; }

    [MaxLength(50)]
    public string? Vendor { get; set; }

    [MaxLength(50)]
    public string? DeviceClass { get; set; }
}
