using System;
using Microsoft.AspNetCore.Identity;

namespace SocialMediaApp.Models;

public class Users : IdentityUser
{
    public required string FullName { get; set; }
    public DateTime? DateOfBirth { get; set; }

    public string? ProfileImagePath { get; set; }

    public ICollection<Posts> Posts { get; set; }  // Posts created by user
    public ICollection<PostLikes> LikedPosts { get; set; }  // Posts liked by user
    public ICollection<PostComments> Comments { get; set; }

}
