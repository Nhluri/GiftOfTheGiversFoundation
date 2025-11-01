using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiftOfTheGiversFoundation.Models
{
    public class VolunteerTask
    {
        [Key]
        public int TaskID { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; set; }

        [MaxLength(50)]
        public string TaskType { get; set; }

        [MaxLength(200)]
        public string Location { get; set; }

        public int EstimatedHours { get; set; } // CHANGED FROM decimal TO int

        [MaxLength(200)]
        public string RequiredSkills { get; set; }

        [MaxLength(20)]
        public string Urgency { get; set; }

        [MaxLength(20)]
        public string Status { get; set; }

        public DateTime DateCreated { get; set; }

        // Foreign key
        public int CreatedByUserID { get; set; }

        // Navigation property to User who created this task
        public User CreatedByUser { get; set; }

        // Navigation property for contributions
        public ICollection<VolunteerContribution> Contributions { get; set; }
    }
}