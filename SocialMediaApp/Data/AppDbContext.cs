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
    public DbSet<Friends> Friends { get; set; }

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
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<PostLikes>()
            .HasOne(pl => pl.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(pl => pl.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PostComments>()
            .HasOne(pc => pc.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(pc => pc.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<PostComments>()
            .HasOne(pc => pc.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(pc => pc.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // ADD FRIENDS CONFIGURATION
        // This prevents cascade delete conflicts since both FKs point to Users table
        builder.Entity<Friends>()
            .HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.Entity<Friends>()
            .HasOne(f => f.FriendUser)
            .WithMany()
            .HasForeignKey(f => f.FriendUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional: Prevent duplicate friend requests
        builder.Entity<Friends>()
            .HasIndex(f => new { f.UserId, f.FriendUserId })
            .IsUnique();
    }
}