using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialMediaApp.Data;
using SocialMediaApp.Models.ViewModels;

namespace SocialMediaApp.Controllers
{
    public class FeedController : Controller
    {
        private readonly AppDbContext _context;

        public FeedController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var posts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User) // Add this to include comment users
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new FeedPostViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    UserName = p.User.FullName,
                    CreatedAt = p.CreatedAt,
                    LikesCount = p.Likes.Count,
                    CommentsCount = p.Comments.Count,
                    
                    // Add this to map comments
                    Comments = p.Comments
                        .OrderBy(c => c.CreatedAt)
                        .Select(c => new CommentViewModel
                        {
                            Id = c.Id,
                            Content = c.Content,
                            UserName = c.User.FullName,
                            CreatedAt = c.CreatedAt
                        })
                        .ToList()
                })
                .ToListAsync();

            var model = new FeedViewModel
            {
                Posts = posts
            };

            return View(model);
        }
    }
}
