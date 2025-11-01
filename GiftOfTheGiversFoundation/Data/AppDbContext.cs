using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversFoundation.Models;

namespace GiftOfTheGiversFoundation.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Core tables only
        public DbSet<User> Users { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<Donation> Donations { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<TaskAssignment> TaskAssignments { get; set; }
        public DbSet<VolunteerSchedule> VolunteerSchedules { get; set; }
        

        public DbSet<VolunteerTask> VolunteerTasks { get; set; }
        public DbSet<VolunteerContribution> VolunteerContributions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Donation entity
            modelBuilder.Entity<Donation>(entity =>
            {
                entity.HasKey(e => e.DonationID);

                entity.Property(e => e.DonationType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("Pending");

                entity.Property(e => e.Amount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.DonationDate)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Relationship with User
                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure other entities...
            modelBuilder.Entity<TaskAssignment>()
                .HasOne(t => t.Volunteer)
                .WithMany()
                .HasForeignKey(t => t.VolunteerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VolunteerSchedule>()
                .HasOne(v => v.Volunteer)
                .WithMany()
                .HasForeignKey(v => v.VolunteerId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);

            // VolunteerTask configuration
            modelBuilder.Entity<VolunteerTask>(entity =>
            {
                entity.HasKey(e => e.TaskID);

                // Other configurations...

                // Relationship with User who created the task
                entity.HasOne(t => t.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(t => t.CreatedByUserID)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            base.OnModelCreating(modelBuilder);
            // VolunteerContribution configuration
            modelBuilder.Entity<VolunteerContribution>(entity =>
            {
                entity.HasKey(e => e.ContributionID);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasDefaultValue("In Progress");

                entity.Property(e => e.HoursWorked)
                    .HasColumnType("decimal(5,2)");

                // Relationships
                entity.HasOne(vc => vc.User)
                    .WithMany()
                    .HasForeignKey(vc => vc.UserID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(vc => vc.Task)
                    .WithMany(t => t.Contributions)
                    .HasForeignKey(vc => vc.TaskID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}