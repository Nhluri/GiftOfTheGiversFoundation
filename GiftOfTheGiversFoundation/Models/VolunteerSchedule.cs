using System;
using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGiversFoundation.Models
{
    public class VolunteerSchedule
    {
        [Key]
        public int ScheduleId { get; set; }

        [Required]
        public int VolunteerId { get; set; }
        public User Volunteer { get; set; }

        [Required]
        public DateTime ShiftDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        public string Assignment { get; set; }

        public string Status { get; set; } = "Scheduled";
    }
}