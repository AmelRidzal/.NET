using System;
using Microsoft.AspNetCore.Identity;

namespace SocialMediaApp.Models;

public class Users : IdentityUser
{
    public required string FullName { get; set; }
}
