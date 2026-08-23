using System.ComponentModel.DataAnnotations;

namespace Darbak.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(300)]
        public string? ImageUrl { get; set; }

        public ICollection<Product> Products
        {
            get;
            set;
        } = new List<Product>();
    }
}
