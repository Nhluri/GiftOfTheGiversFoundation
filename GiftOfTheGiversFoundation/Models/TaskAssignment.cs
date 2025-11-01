using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiftOfTheGiversFoundation.Models
{
    public class TaskAssignment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Add this line
        public int TaskID { get; set; }

        // Link to Volunteer (User table)
        public int VolunteerId { get; set; }

        [ForeignKey("VolunteerId")]
        public virtual User Volunteer { get; set; }

        [Required]
        [MaxLength(100)]
        public string TaskTitle { get; set; }

        [Required]
        [MaxLength(500)]
        public string TaskDescription { get; set; }

        public DateTime DueDate { get; set; }

        [MaxLength(20)]
        public string Priority { get; set; }

        [MaxLength(20)]
        public string Status { get; set; }

        public DateTime DateAssigned { get; set; }

       
    }
}