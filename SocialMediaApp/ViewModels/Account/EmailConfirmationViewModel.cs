using System;
using System.ComponentModel.DataAnnotations;

namespace SocialMediaApp.ViewModels;

public class EmailConfirmationViewModel
{

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Confirmation Code")]
    public string Code { get; set; } = string.Empty;
}
