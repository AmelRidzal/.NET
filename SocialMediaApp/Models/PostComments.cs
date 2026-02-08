namespace SocialMediaApp.Models;
public class PostComments
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public Users User { get; set; } = null!;
    public int PostId { get; set; }
    public Posts Post { get; set; } = null!;
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}