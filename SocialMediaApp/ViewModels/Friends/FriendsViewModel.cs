using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SocialMediaApp.Models;

namespace SocialMediaApp.ViewModels
{
    // View model for displaying a friend in the list
    public class FriendViewModel
    {
        public int FriendId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string Email { get; set; }
        public FriendshipStatus Status { get; set; }
        public DateTime FriendsSince { get; set; }
        public int MutualFriendsCount { get; set; }
    }

    // View model for the friends list page
    public class FriendsListViewModel
    {
        public List<FriendViewModel> Friends { get; set; }
        public List<FriendViewModel> PendingRequests { get; set; }
        public List<FriendViewModel> SentRequests { get; set; }
        public int TotalFriends { get; set; }
        public int PendingRequestsCount { get; set; }

        public FriendsListViewModel()
        {
            Friends = new List<FriendViewModel>();
            PendingRequests = new List<FriendViewModel>();
            SentRequests = new List<FriendViewModel>();
        }
    }

    // View model for searching users
    public class UserSearchViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string ProfilePictureUrl { get; set; }
        public bool IsFriend { get; set; }
        public bool HasPendingRequest { get; set; }
        public bool HasSentRequest { get; set; }
        public int MutualFriendsCount { get; set; }
    }

    // View model for sending friend request
    public class SendFriendRequestViewModel
    {
        [Required]
        public string FriendUserId { get; set; }
    }

    // View model for friend suggestions
    public class FriendSuggestionsViewModel
    {
        public List<UserSearchViewModel> SuggestedFriends { get; set; }

        public FriendSuggestionsViewModel()
        {
            SuggestedFriends = new List<UserSearchViewModel>();
        }
    }
}