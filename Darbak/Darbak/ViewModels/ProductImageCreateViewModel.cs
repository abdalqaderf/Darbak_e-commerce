using System.ComponentModel.DataAnnotations;

namespace Darbak.ViewModels
{
    public sealed class ProductImageCreateViewModel
    {
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Invalid product.")]
        public int ProductId { get; set; }

        [Required(
            ErrorMessage = "Please select an image.")]
        [Display(Name = "Product Image")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Main Image")]
        public bool IsMain { get; set; }
    }
}