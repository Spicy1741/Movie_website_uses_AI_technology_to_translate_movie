using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Film_website.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
        public string Content { get; set; } = string.Empty;

        [Required]
        public int MovieId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsEdited { get; set; } = false;

        public int LikeCount { get; set; } = 0;

        // Navigation properties
        [ForeignKey("MovieId")]
        public virtual Movie Movie { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        // Optional: For reply functionality (parent-child relationship)
        public int? ParentCommentId { get; set; }

        [ForeignKey("ParentCommentId")]
        public virtual Comment? ParentComment { get; set; }

        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();

        // For tracking user likes on comments
        public virtual ICollection<CommentLike> CommentLikes { get; set; } = new List<CommentLike>();
    }
}