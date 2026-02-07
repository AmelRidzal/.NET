using System;
using System.ComponentModel.DataAnnotations;

namespace SocialMediaApp.ViewModels;

public class VerifyEmailViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; }
}
