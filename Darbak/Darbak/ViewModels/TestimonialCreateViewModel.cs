using System.ComponentModel.DataAnnotations;

namespace Darbak.ViewModels
{
    public class TestimonialCreateViewModel
    {
        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a rating.")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        public int? Rating { get; set; }
    }
}
