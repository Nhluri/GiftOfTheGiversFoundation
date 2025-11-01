using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiftOfTheGiversFoundation.Models
{
    public class Resource
    {
        [Key]
        public int ResourceID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [Display(Name = "Resource Type")]
        public string ResourceType { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public string Location { get; set; }

        public string Availability { get; set; } = "Available";

        [Display(Name = "Date Submitted")]
        public DateTime DateSubmitted { get; set; } = DateTime.UtcNow;

    }
}