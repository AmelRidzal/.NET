using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SocialMediaApp.Models;

namespace SocialMediaApp.Data;

public class AppDbContext : IdentityDbContext<Users>
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Posts> Posts { get; set; }
    public DbSet<PostLikes> PostLikes { get; set; }
    public DbSet<PostComments> PostComments { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);
    
    // Prevent a user from liking the same post multiple times
    builder.Entity<PostLikes>()
        .HasIndex(pl => new { pl.UserId, pl.PostId })
        .IsUnique();

    // Configure relationships
    builder.Entity<Posts>()
        .HasOne(p => p.User)
        .WithMany(u => u.Posts)
        .HasForeignKey(p => p.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.Entity<PostLikes>()
        .HasOne(pl => pl.User)
        .WithMany(u => u.LikedPosts)
        .HasForeignKey(pl => pl.UserId)
        .OnDelete(DeleteBehavior.NoAction); // Changed to NoAction

    builder.Entity<PostLikes>()
        .HasOne(pl => pl.Post)
        .WithMany(p => p.Likes)
        .HasForeignKey(pl => pl.PostId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.Entity<PostComments>()
        .HasOne(pc => pc.User)
        .WithMany(u => u.Comments)
        .HasForeignKey(pc => pc.UserId)
        .OnDelete(DeleteBehavior.NoAction); // Changed to NoAction

    builder.Entity<PostComments>()
        .HasOne(pc => pc.Post)
        .WithMany(p => p.Comments)
        .HasForeignKey(pc => pc.PostId)
        .OnDelete(DeleteBehavior.Cascade);
}
}