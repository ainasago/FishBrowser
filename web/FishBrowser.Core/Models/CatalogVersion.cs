using System;
using System.ComponentModel.DataAnnotations;

namespace FishBrowser.WPF.Models;

public class CatalogVersion
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Version { get; set; } = "0.0.1";

    public DateTime PublishedAt { get; set; } = DateTime.Now;

    public string? Changelog { get; set; }

    [MaxLength(200)]
    public string? SourceUrl { get; set; }

    [MaxLength(200)]
    public string? Signature { get; set; }
}
