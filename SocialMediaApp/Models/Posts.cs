using System.ComponentModel.DataAnnotations;

namespace SocialMediaApp.Models;
public class Posts
{
    public int Id { get; set; }
    [Required]
    public required string Title { get; set; }
    [Required]
    public required string Content { get; set; }
    public required string UserId { get; set; }
    public Users User { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<PostLikes> Likes { get; set; } = new List<PostLikes>();
    public ICollection<PostComments> Comments { get; set; } = new List<PostComments>();
}