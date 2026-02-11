using System.Security.Claims;
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
        public async Task<IActionResult> ToggleLike(int postId, string returnUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var like = await _context.PostLikes
                .Where(l => l.PostId == postId && l.UserId == user.Id)
                .FirstOrDefaultAsync();

            bool isLiked;
            if (like != null)
            {
                _context.PostLikes.Remove(like);
                isLiked = false;
            }
            else
            {
                _context.PostLikes.Add(new PostLikes
                {
                    PostId = postId,
                    UserId = user.Id,
                    LikedAt = DateTime.UtcNow
                });
                isLiked = true;
            }

            await _context.SaveChangesAsync();

            // Get the updated like count
            var likesCount = await _context.PostLikes.CountAsync(l => l.PostId == postId);

            // Check if this is an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, isLiked, likesCount });
            }

            // Redirect back to where the user came from (fallback for non-AJAX)
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Feed");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComment(int postId, string content, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Comment is required." });
                }
                return BadRequest("Comment is required.");
            }

            var user = await _userManager.GetUserAsync(User);

            var comment = new PostComments
            {
                PostId = postId,
                UserId = user.Id,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.PostComments.Add(comment);
            await _context.SaveChangesAsync();

            // Get the updated comment count
            var commentsCount = await _context.PostComments.CountAsync(c => c.PostId == postId);

            // Check if this is an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    comment = new
                    {
                        id = comment.Id,
                        content = comment.Content,
                        userName = user.FullName,
                        createdAt = comment.CreatedAt.ToLocalTime().ToString("g")
                    },
                    commentsCount
                });
            }

            // Redirect back to where the user came from (fallback for non-AJAX)
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Feed");
        }


        public async Task<IActionResult> MyPosts()
        {
            // Get the current user's ID
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var posts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .Where(p => p.UserId == currentUserId) // Filter by current user
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
                    CanDelete = true,
                    IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),
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


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var post = await _context.Posts
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == currentUserId);

            if (post == null)
                return NotFound();

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyPosts));
        }

    }
}