using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialMediaApp.Models
{
    public class Message
    {
        [Key]
        public int MessageId { get; set; }

        [Required]
        public string SenderId { get; set; }

        [Required]
        public string ReceiverId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; }

        public DateTime SentAt { get; set; }

        public bool IsRead { get; set; }

        public DateTime? ReadAt { get; set; }

        // Navigation properties
        [ForeignKey("SenderId")]
        public virtual Users Sender { get; set; }

        [ForeignKey("ReceiverId")]
        public virtual Users Receiver { get; set; }
    }
}