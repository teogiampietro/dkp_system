using System.ComponentModel.DataAnnotations;

namespace DkpSystem.Models.ViewModels;

public class ResetPasswordModel
{
    [Required(ErrorMessage = "Temporary password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string TemporaryPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm the password.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
