using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiftOfTheGiversFoundation.Models
{
    public class Donation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DonationID { get; set; }

        [Required]
        public int UserID { get; set; }

        // Navigation property
        [ForeignKey("UserID")]
        public User User { get; set; }

        [Required(ErrorMessage = "Please select a donation type")]
        [Display(Name = "Donation Type")]
        [StringLength(100)]
        public string DonationType { get; set; }

        [Display(Name = "Amount (ZAR)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Amount { get; set; }

        [Required(ErrorMessage = "Please provide a description")]
        [Display(Name = "Description")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }

        [Display(Name = "Donation Date")]
        public DateTime DonationDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";
    }
}