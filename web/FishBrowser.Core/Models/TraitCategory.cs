using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishBrowser.WPF.Models;

public class TraitCategory
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int Order { get; set; }

    public ICollection<TraitDefinition> Traits { get; set; } = new List<TraitDefinition>();
}
