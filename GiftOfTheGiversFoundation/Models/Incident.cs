using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGiversFoundation.Models
{
    public class Incident
    {
        [Key]
        public int IncidentID { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Incident Type")]
        public string IncidentType { get; set; }

        [Required]
        public string Severity { get; set; } = "Medium";

        [Required]
        public string Location { get; set; }

        [Display(Name = "Incident Date")]
        [DataType(DataType.DateTime)]
        public DateTime IncidentDate { get; set; } = DateTime.Now;

        [Display(Name = "People Affected")]
        public int? PeopleAffected { get; set; }

        [Display(Name = "Emergency Needs")]
        public string? EmergencyNeeds { get; set; }

        public string Status { get; set; } = "Reported";

        [Display(Name = "Date Reported")]
        public DateTime DateReported { get; set; } = DateTime.UtcNow;
        
        public int UserID { get; set; }
    }
}