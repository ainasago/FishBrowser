using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishBrowser.WPF.Models;

public enum TraitValueType
{
    String = 0,
    Number = 1,
    Bool = 2,
    Enum = 3,
    Array = 4,
    Object = 5,
    Script = 6
}

public class TraitDefinition
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Key { get; set; } = string.Empty; // 唯一键，如 browser.userAgent

    [Required, MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public TraitCategory Category { get; set; } = null!;

    public TraitValueType ValueType { get; set; }

    // JSON 字段
    public string? DefaultValueJson { get; set; }
    public string? ConstraintsJson { get; set; }
    public string? DependenciesJson { get; set; }
    public string? ConflictsJson { get; set; }
    public string? DistributionsJson { get; set; }

    [MaxLength(500)]
    public string? DocUrl { get; set; }

    public string? Notes { get; set; }

    public bool IsExperimental { get; set; }

    [MaxLength(50)]
    public string? VersionIntroduced { get; set; }

    public ICollection<TraitOption> Options { get; set; } = new List<TraitOption>();
}
