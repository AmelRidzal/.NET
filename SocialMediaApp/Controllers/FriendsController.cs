using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialMediaApp.Data;
using SocialMediaApp.Models;
using SocialMediaApp.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SocialMediaApp.Controllers
{
    [Authorize]
    public class FriendsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public FriendsController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Friends
        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);
            var viewModel = new FriendsListViewModel();

            // Get accepted friends
            var friends = await _context.Friends
                .Include(f => f.User)
                .Include(f => f.FriendUser)
                .Where(f => (f.UserId == currentUserId || f.FriendUserId == currentUserId) 
                            && f.Status == FriendshipStatus.Accepted)
                .ToListAsync();

            viewModel.Friends = friends.Select(f => new FriendViewModel
            {
                FriendId = f.FriendId,
                UserId = f.UserId == currentUserId ? f.FriendUserId : f.UserId,
                UserName = f.UserId == currentUserId ? f.FriendUser.UserName : f.User.UserName,
                Email = f.UserId == currentUserId ? f.FriendUser.Email : f.User.Email,
                ProfilePictureUrl = f.UserId == currentUserId ? f.FriendUser.ProfileImagePath : f.User.ProfileImagePath,
                Status = f.Status,
                FriendsSince = f.AcceptedDate ?? f.RequestedDate,
                MutualFriendsCount = GetMutualFriendsCount(currentUserId, f.UserId == currentUserId ? f.FriendUserId : f.UserId)
            }).ToList();

            // Get pending requests (received)
            var pendingRequests = await _context.Friends
                .Include(f => f.User)
                .Where(f => f.FriendUserId == currentUserId && f.Status == FriendshipStatus.Pending)
                .ToListAsync();

            viewModel.PendingRequests = pendingRequests.Select(f => new FriendViewModel
            {
                FriendId = f.FriendId,
                UserId = f.UserId,
                UserName = f.User.UserName,
                Email = f.User.Email,
                ProfilePictureUrl = f.User.ProfileImagePath,
                Status = f.Status,
                FriendsSince = f.RequestedDate,
                MutualFriendsCount = GetMutualFriendsCount(currentUserId, f.UserId)
            }).ToList();

            // Get sent requests
            var sentRequests = await _context.Friends
                .Include(f => f.FriendUser)
                .Where(f => f.UserId == currentUserId && f.Status == FriendshipStatus.Pending)
                .ToListAsync();

            viewModel.SentRequests = sentRequests.Select(f => new FriendViewModel
            {
                FriendId = f.FriendId,
                UserId = f.FriendUserId,
                UserName = f.FriendUser.UserName,
                Email = f.FriendUser.Email,
                ProfilePictureUrl = f.FriendUser.ProfileImagePath,
                Status = f.Status,
                FriendsSince = f.RequestedDate,
                MutualFriendsCount = GetMutualFriendsCount(currentUserId, f.FriendUserId)
            }).ToList();

            viewModel.TotalFriends = viewModel.Friends.Count;
            viewModel.PendingRequestsCount = viewModel.PendingRequests.Count;

            return View(viewModel);
        }

        // GET: Friends/Search
        public async Task<IActionResult> Search(string searchQuery)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return View(new List<UserSearchViewModel>());
            }

            var users = await _userManager.Users
                .Where(u => u.Id != currentUserId && 
                           (u.UserName.Contains(searchQuery) || u.Email.Contains(searchQuery)))
                .Take(20)
                .ToListAsync();

            var searchResults = users.Select(u => new UserSearchViewModel
            {
                UserId = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                ProfilePictureUrl = u.ProfileImagePath,
                IsFriend = IsFriend(currentUserId, u.Id),
                HasPendingRequest = HasPendingRequest(currentUserId, u.Id),
                HasSentRequest = HasSentRequest(currentUserId, u.Id),
                MutualFriendsCount = GetMutualFriendsCount(currentUserId, u.Id)
            }).ToList();

            return View(searchResults);
        }

        // POST: Friends/SendRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRequest(string friendUserId)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == friendUserId)
            {
                TempData["Error"] = "You cannot send a friend request to yourself.";
                return RedirectToAction(nameof(Index));
            }

            // Check if friendship already exists
            var existingFriendship = await _context.Friends
                .FirstOrDefaultAsync(f => (f.UserId == currentUserId && f.FriendUserId == friendUserId) ||
                                         (f.UserId == friendUserId && f.FriendUserId == currentUserId));

            if (existingFriendship != null)
            {
                TempData["Error"] = "A friend request already exists or you are already friends.";
                return RedirectToAction(nameof(Index));
            }

            var friendRequest = new Friends
            {
                UserId = currentUserId,
                FriendUserId = friendUserId,
                Status = FriendshipStatus.Pending,
                RequestedDate = DateTime.UtcNow
            };

            _context.Friends.Add(friendRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Friend request sent successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Friends/AcceptRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(int friendId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var friendRequest = await _context.Friends
                .FirstOrDefaultAsync(f => f.FriendId == friendId && f.FriendUserId == currentUserId);

            if (friendRequest == null)
            {
                TempData["Error"] = "Friend request not found.";
                return RedirectToAction(nameof(Index));
            }

            friendRequest.Status = FriendshipStatus.Accepted;
            friendRequest.AcceptedDate = DateTime.UtcNow;

            _context.Update(friendRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Friend request accepted!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Friends/RejectRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int friendId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var friendRequest = await _context.Friends
                .FirstOrDefaultAsync(f => f.FriendId == friendId && f.FriendUserId == currentUserId);

            if (friendRequest == null)
            {
                TempData["Error"] = "Friend request not found.";
                return RedirectToAction(nameof(Index));
            }

            friendRequest.Status = FriendshipStatus.Rejected;

            _context.Update(friendRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Friend request rejected.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Friends/CancelRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(int friendId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var friendRequest = await _context.Friends
                .FirstOrDefaultAsync(f => f.FriendId == friendId && f.UserId == currentUserId);

            if (friendRequest == null)
            {
                TempData["Error"] = "Friend request not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.Friends.Remove(friendRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Friend request cancelled.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Friends/Unfriend
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unfriend(int friendId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var friendship = await _context.Friends
                .FirstOrDefaultAsync(f => f.FriendId == friendId && 
                                         (f.UserId == currentUserId || f.FriendUserId == currentUserId));

            if (friendship == null)
            {
                TempData["Error"] = "Friendship not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.Friends.Remove(friendship);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Friend removed successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Friends/Suggestions
        public async Task<IActionResult> Suggestions()
        {
            var currentUserId = _userManager.GetUserId(User);
            var viewModel = new FriendSuggestionsViewModel();

            // Get friends of friends
            var friendsOfFriends = await _context.Friends
                .Where(f => (f.UserId == currentUserId || f.FriendUserId == currentUserId) 
                            && f.Status == FriendshipStatus.Accepted)
                .SelectMany(f => _context.Friends
                    .Where(ff => (ff.UserId == (f.UserId == currentUserId ? f.FriendUserId : f.UserId) ||
                                  ff.FriendUserId == (f.UserId == currentUserId ? f.FriendUserId : f.UserId))
                                 && ff.Status == FriendshipStatus.Accepted
                                 && ff.UserId != currentUserId
                                 && ff.FriendUserId != currentUserId))
                .Select(ff => ff.UserId == currentUserId ? ff.FriendUserId : ff.UserId)
                .Distinct()
                .Take(10)
                .ToListAsync();

            var suggestedUsers = await _userManager.Users
                .Where(u => friendsOfFriends.Contains(u.Id) && !IsFriend(currentUserId, u.Id))
                .ToListAsync();

            viewModel.SuggestedFriends = suggestedUsers.Select(u => new UserSearchViewModel
            {
                UserId = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                ProfilePictureUrl = u.ProfileImagePath,
                IsFriend = false,
                HasPendingRequest = HasPendingRequest(currentUserId, u.Id),
                HasSentRequest = HasSentRequest(currentUserId, u.Id),
                MutualFriendsCount = GetMutualFriendsCount(currentUserId, u.Id)
            }).ToList();

            return View(viewModel);
        }

        // Helper methods
        private bool IsFriend(string userId, string friendUserId)
        {
            return _context.Friends.Any(f => 
                ((f.UserId == userId && f.FriendUserId == friendUserId) ||
                 (f.UserId == friendUserId && f.FriendUserId == userId)) &&
                f.Status == FriendshipStatus.Accepted);
        }

        private bool HasPendingRequest(string userId, string friendUserId)
        {
            return _context.Friends.Any(f => 
                f.UserId == friendUserId && 
                f.FriendUserId == userId && 
                f.Status == FriendshipStatus.Pending);
        }

        private bool HasSentRequest(string userId, string friendUserId)
        {
            return _context.Friends.Any(f => 
                f.UserId == userId && 
                f.FriendUserId == friendUserId && 
                f.Status == FriendshipStatus.Pending);
        }

        private int GetMutualFriendsCount(string userId, string friendUserId)
        {
            var userFriends = _context.Friends
                .Where(f => (f.UserId == userId || f.FriendUserId == userId) && 
                           f.Status == FriendshipStatus.Accepted)
                .Select(f => f.UserId == userId ? f.FriendUserId : f.UserId)
                .ToList();

            var friendFriends = _context.Friends
                .Where(f => (f.UserId == friendUserId || f.FriendUserId == friendUserId) && 
                           f.Status == FriendshipStatus.Accepted)
                .Select(f => f.UserId == friendUserId ? f.FriendUserId : f.UserId)
                .ToList();

            return userFriends.Intersect(friendFriends).Count();
        }
    }
}