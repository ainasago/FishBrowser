using System;
using System.ComponentModel.DataAnnotations;

namespace FishBrowser.WPF.Models;

public class FingerprintMetaProfile
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Version { get; set; }

    [MaxLength(20)]
    public string Source { get; set; } = "user"; // user/preset/import

    public int? BasePresetId { get; set; }
    public TraitGroupPreset? BasePreset { get; set; }

    public string TraitsJson { get; set; } = "{}"; // key->value
    public string? LockFlagsJson { get; set; }
    public string? RandomizationPlanJson { get; set; }

    // audit timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Fonts configuration
    // real | custom
    public string FontsMode { get; set; } = "real";
    // JSON array of font family names when mode=custom
    public string? FontsJson { get; set; }

    // WebGL / WebGPU / Audio / Speech (iter1)
    // WebGL image: noise | real
    public string WebGLImageMode { get; set; } = "noise";
    // WebGL info: ua_based | custom | disable_hwaccel | real
    public string WebGLInfoMode { get; set; } = "ua_based";
    public string? WebGLVendor { get; set; }
    public string? WebGLRenderer { get; set; }
    // WebGPU: match_webgl | real | disable
    public string WebGPUMode { get; set; } = "match_webgl";
    // AudioContext: noise | real
    public string AudioContextMode { get; set; } = "noise";
    // SpeechVoices
    public bool SpeechVoicesEnabled { get; set; } = true;
}
