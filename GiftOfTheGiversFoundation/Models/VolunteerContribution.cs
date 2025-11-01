using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiftOfTheGiversFoundation.Models
{
    public class VolunteerContribution
    {
        [Key]
        public int ContributionID { get; set; }

        [Required]
        public int UserID { get; set; }

        [ForeignKey("UserID")]
        public User User { get; set; }

        public int? TaskID { get; set; } // Changed from int? to just int? (keep it as is)

        [ForeignKey("TaskID")]
        public VolunteerTask Task { get; set; }

        [Display(Name = "Hours Worked")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal HoursWorked { get; set; } = 0m; // Add default value

        [Display(Name = "Contribution Date")]
        public DateTime ContributionDate { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "In Progress";
    }
}