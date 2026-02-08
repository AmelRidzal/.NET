namespace SocialMediaApp.Models;
public class PostLikes
{
    public int Id { get; set; }
    public string UserId { get; set; }  // Changed from int to string
    public Users User { get; set; }
    public int PostId { get; set; }
    public Posts Post { get; set; }
    public DateTime LikedAt { get; set; }
}