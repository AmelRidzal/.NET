using System;
using Microsoft.AspNetCore.Identity;

namespace SocialMediaApp.Models;

public class Users : IdentityUser
{
    public required string FullName { get; set; }
    public DateTime? DateOfBirth { get; set; }

    public string? ProfileImagePath { get; set; }
}
