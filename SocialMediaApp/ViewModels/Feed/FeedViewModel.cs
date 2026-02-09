using System.Collections.Generic;

namespace SocialMediaApp.Models.ViewModels
{
    public class FeedViewModel
    {
        public List<FeedPostViewModel> Posts { get; set; } = new();
    }
}
