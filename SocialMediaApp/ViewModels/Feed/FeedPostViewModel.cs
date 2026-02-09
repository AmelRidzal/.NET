using System;

namespace SocialMediaApp.Models.ViewModels
{
    public class FeedPostViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public string UserName { get; set; }

        public DateTime CreatedAt { get; set; }

        public int LikesCount { get; set; }

        public int CommentsCount { get; set; }
    }
}
