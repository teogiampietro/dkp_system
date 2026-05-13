using System.ComponentModel.DataAnnotations;

namespace DkpSystem.Models.ViewModels;

public class ChangePasswordModel
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
