using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialMediaApp.Data;
using SocialMediaApp.Models;
using SocialMediaApp.Models.ViewModels;

namespace SocialMediaApp.Controllers
{
    [Authorize]
    public class PostsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public PostsController(
            AppDbContext context,
            UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET
        public IActionResult Create()
        {
            return View(new CreatePostViewModel());
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePostViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            var posts = new Posts
            {
                Title = model.Title,
                Content = model.Content,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(posts);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Feed");
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleLike(int postId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var like = await _context.PostLikes
                .Where(l => l.PostId == postId && l.UserId == user.Id)
                .FirstOrDefaultAsync();

            if (like != null)
            {
                _context.PostLikes.Remove(like);
            }
            else
            {
                _context.PostLikes.Add(new PostLikes
                {
                    PostId = postId,
                    UserId = user.Id,
                    LikedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Feed");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComment(int postId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return BadRequest("Comment is required.");

            var user = await _userManager.GetUserAsync(User);

            _context.PostComments.Add(new PostComments
            {
                PostId = postId,
                UserId = user.Id,
                Content = content,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Feed");
        }


    }
}
