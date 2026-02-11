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
    public class MessagesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public MessagesController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Messages (Inbox)
        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);
            var viewModel = new InboxViewModel();

            // Get all unique conversation partners
            var sentToUsers = await _context.Messages
                .Where(m => m.SenderId == currentUserId)
                .Select(m => m.ReceiverId)
                .Distinct()
                .ToListAsync();

            var receivedFromUsers = await _context.Messages
                .Where(m => m.ReceiverId == currentUserId)
                .Select(m => m.SenderId)
                .Distinct()
                .ToListAsync();

            var conversationPartnerIds = sentToUsers.Union(receivedFromUsers).Distinct().ToList();

            // Get conversation previews
            foreach (var partnerId in conversationPartnerIds)
            {
                var partner = await _userManager.FindByIdAsync(partnerId);
                if (partner == null) continue;

                // Get the last message in this conversation
                var lastMessage = await _context.Messages
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == partnerId) ||
                               (m.SenderId == partnerId && m.ReceiverId == currentUserId))
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                if (lastMessage == null) continue;

                // Count unread messages from this partner
                var unreadCount = await _context.Messages
                    .Where(m => m.SenderId == partnerId && m.ReceiverId == currentUserId && !m.IsRead)
                    .CountAsync();

                viewModel.Conversations.Add(new ConversationPreviewViewModel
                {
                    FriendId = partnerId,
                    FriendName = partner.FullName,
                    FriendProfilePicture = partner.ProfileImagePath ?? "/images/default-avatar.png",
                    LastMessage = lastMessage.Content.Length > 50 
                        ? lastMessage.Content.Substring(0, 50) + "..." 
                        : lastMessage.Content,
                    LastMessageTime = lastMessage.SentAt,
                    UnreadCount = unreadCount,
                    IsLastMessageFromCurrentUser = lastMessage.SenderId == currentUserId
                });
            }

            // Sort by most recent message
            viewModel.Conversations = viewModel.Conversations
                .OrderByDescending(c => c.LastMessageTime)
                .ToList();

            viewModel.TotalUnreadCount = viewModel.Conversations.Sum(c => c.UnreadCount);

            return View(viewModel);
        }

        // GET: Messages/Chat/userId
        public async Task<IActionResult> Chat(string friendId)
        {
            if (string.IsNullOrEmpty(friendId))
            {
                TempData["Error"] = "Friend not specified.";
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = _userManager.GetUserId(User);

            // Verify they are friends
            var areFriends = await _context.Friends
                .AnyAsync(f => ((f.UserId == currentUserId && f.FriendUserId == friendId) ||
                               (f.UserId == friendId && f.FriendUserId == currentUserId)) &&
                              f.Status == FriendshipStatus.Accepted);

            if (!areFriends)
            {
                TempData["Error"] = "You can only message your friends.";
                return RedirectToAction("Index", "Friends");
            }

            var friend = await _userManager.FindByIdAsync(friendId);
            if (friend == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            // Get all messages between current user and friend
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == friendId) ||
                           (m.SenderId == friendId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            // Mark messages from friend as read
            var unreadMessages = messages.Where(m => m.SenderId == friendId && !m.IsRead).ToList();
            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }
            if (unreadMessages.Any())
            {
                await _context.SaveChangesAsync();
            }

            var viewModel = new ChatViewModel
            {
                FriendId = friendId,
                FriendName = friend.FullName,
                FriendEmail = friend.Email,
                FriendProfilePicture = friend.ProfileImagePath ?? "/images/default-avatar.png",
                Messages = messages.Select(m => new MessageViewModel
                {
                    MessageId = m.MessageId,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.FullName,
                    SenderProfilePicture = m.Sender.ProfileImagePath ?? "/images/default-avatar.png",
                    ReceiverId = m.ReceiverId,
                    ReceiverName = m.Receiver.FullName,
                    Content = m.Content,
                    SentAt = m.SentAt,
                    IsRead = m.IsRead,
                    IsSentByCurrentUser = m.SenderId == currentUserId
                }).ToList(),
                UnreadCount = 0 // All messages are now read
            };

            return View(viewModel);
        }

        // POST: Messages/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(string friendId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Message cannot be empty.";
                return RedirectToAction(nameof(Chat), new { friendId });
            }

            var currentUserId = _userManager.GetUserId(User);

            // Verify they are friends
            var areFriends = await _context.Friends
                .AnyAsync(f => ((f.UserId == currentUserId && f.FriendUserId == friendId) ||
                               (f.UserId == friendId && f.FriendUserId == currentUserId)) &&
                              f.Status == FriendshipStatus.Accepted);

            if (!areFriends)
            {
                TempData["Error"] = "You can only message your friends.";
                return RedirectToAction("Index", "Friends");
            }

            var message = new Message
            {
                SenderId = currentUserId,
                ReceiverId = friendId,
                Content = content.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Chat), new { friendId });
        }

        // POST: Messages/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int messageId, string friendId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var message = await _context.Messages.FindAsync(messageId);

            if (message == null)
            {
                TempData["Error"] = "Message not found.";
                return RedirectToAction(nameof(Chat), new { friendId });
            }

            // Only sender can delete their own message
            if (message.SenderId != currentUserId)
            {
                TempData["Error"] = "You can only delete your own messages.";
                return RedirectToAction(nameof(Chat), new { friendId });
            }

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Message deleted.";
            return RedirectToAction(nameof(Chat), new { friendId });
        }

        // GET: Get unread message count (for navbar badge)
        public async Task<IActionResult> GetUnreadCount()
        {
            var currentUserId = _userManager.GetUserId(User);
            var unreadCount = await _context.Messages
                .Where(m => m.ReceiverId == currentUserId && !m.IsRead)
                .CountAsync();

            return Json(new { count = unreadCount });
        }
    }
}