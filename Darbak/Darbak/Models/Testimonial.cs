using Darbak.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Darbak.Models
{
    public class Testimonial
    {
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = null!;

        [Range(1, 5)]
        public int? Rating { get; set; }

        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string UserId { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;
    }
}