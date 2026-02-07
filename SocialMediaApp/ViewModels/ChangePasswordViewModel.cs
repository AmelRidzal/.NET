using System;
using System.ComponentModel.DataAnnotations;

namespace SocialMediaApp.ViewModels;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; }
    
    [Required(ErrorMessage = "Confirm Password is required.")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    public string ConfirmNewPassword { get; set; }
}
