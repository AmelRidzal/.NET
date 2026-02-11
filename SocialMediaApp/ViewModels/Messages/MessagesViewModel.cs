using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SocialMediaApp.ViewModels
{
    // View model for displaying a message
    public class MessageViewModel
    {
        public int MessageId { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string SenderProfilePicture { get; set; }
        public string ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public bool IsSentByCurrentUser { get; set; }
    }

    // View model for the chat view with a specific friend
    public class ChatViewModel
    {
        public string FriendId { get; set; }
        public string FriendName { get; set; }
        public string FriendEmail { get; set; }
        public string FriendProfilePicture { get; set; }
        public List<MessageViewModel> Messages { get; set; }
        public int UnreadCount { get; set; }

        public ChatViewModel()
        {
            Messages = new List<MessageViewModel>();
        }
    }

    // View model for sending a new message
    public class SendMessageViewModel
    {
        [Required]
        public string ReceiverId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; }
    }

    // View model for displaying conversation preview in inbox
    public class ConversationPreviewViewModel
    {
        public string FriendId { get; set; }
        public string FriendName { get; set; }
        public string FriendProfilePicture { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
        public bool IsLastMessageFromCurrentUser { get; set; }
    }

    // View model for the inbox/messages list
    public class InboxViewModel
    {
        public List<ConversationPreviewViewModel> Conversations { get; set; }
        public int TotalUnreadCount { get; set; }

        public InboxViewModel()
        {
            Conversations = new List<ConversationPreviewViewModel>();
        }
    }
}