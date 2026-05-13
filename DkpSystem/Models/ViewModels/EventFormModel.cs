using System.ComponentModel.DataAnnotations;

namespace DkpSystem.Models.ViewModels;

public class EventFormModel
{
    [Required(ErrorMessage = "Event name is required.")]
    [StringLength(150, ErrorMessage = "Event name cannot exceed 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }
}
