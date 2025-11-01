using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGiversFoundation.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        public string? PhoneNumber { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Role { get; set; } = "User";

        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; } = null;

        // For 2FA
        public string? TwoFactorCode { get; set; }
        public DateTime? TwoFactorExpiry { get; set; }

        // Make sure this matches your SQL column name
        public bool EmailVerified { get; set; } = false;

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        // Relationships
       
        public ICollection<Incident>? IncidentsReported { get; set; }
    }
}