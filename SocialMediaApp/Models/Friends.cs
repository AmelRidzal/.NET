using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialMediaApp.Models
{
    public class Friends
    {
        [Key]
        public int FriendId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string FriendUserId { get; set; }

        [Required]
        public FriendshipStatus Status { get; set; }

        public DateTime RequestedDate { get; set; }

        public DateTime? AcceptedDate { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual Users User { get; set; }

        [ForeignKey("FriendUserId")]
        public virtual Users FriendUser { get; set; }
    }

    public enum FriendshipStatus
    {
        Pending = 0,
        Accepted = 1,
        Rejected = 2,
        Blocked = 3
    }
}