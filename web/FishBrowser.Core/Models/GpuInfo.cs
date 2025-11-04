using System;
using System.ComponentModel.DataAnnotations;

namespace FishBrowser.WPF.Models
{
    public class GpuInfo
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(120)]
        public string Vendor { get; set; } = string.Empty; // e.g., Google Inc. (NVIDIA)

        [MaxLength(300)]
        public string Renderer { get; set; } = string.Empty; // e.g., ANGLE (NVIDIA, GeForce RTX, D3D11)

        [MaxLength(120)]
        public string? Adapter { get; set; } // PCI id, etc.

        [MaxLength(64)]
        public string? DriverVersion { get; set; }

        [MaxLength(16)]
        public string Api { get; set; } = "WebGL"; // WebGL | WebGPU

        [MaxLength(32)]
        public string Backend { get; set; } = "ANGLE-D3D11"; // ANGLE-D3D11|OpenGL|Vulkan|Metal

        public string? LimitsJson { get; set; }
        public string? ExtensionsJson { get; set; }
        public string? OsSupportJson { get; set; } // ["Windows","Linux",...]

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
