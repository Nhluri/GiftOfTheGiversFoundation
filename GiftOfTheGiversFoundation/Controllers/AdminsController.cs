using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversFoundation.Models;
using GiftOfTheGiversFoundation.Data;
using System.Security.Claims;

namespace GiftOfTheGiversFoundation.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // DASHBOARD
        public IActionResult Dashboard()
        {
            return View();
        }

        // ADD THIS GET METHOD - IT WAS MISSING!
        [HttpGet]
        public async Task<IActionResult> AssignTasks()
        {
            try
            {
                var volunteers = await _context.Users
                    .Where(u => u.Role == "Volunteer")
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                var activeTasks = await _context.TaskAssignments
                    .Include(t => t.Volunteer)
                    .Where(t => t.Status == "Assigned" || t.Status == "In Progress")
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();

                ViewBag.Volunteers = volunteers;
                ViewBag.ActiveTasks = activeTasks;
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading tasks: {ex.Message}";
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTask(int volunteerId, string taskTitle, string taskDescription, DateTime dueDate, string priority)
        {
            try
            {
                // Manual validation
                if (volunteerId <= 0)
                {
                    TempData["ErrorMessage"] = "Please select a volunteer.";
                    return RedirectToAction(nameof(AssignTasks));
                }

                if (string.IsNullOrWhiteSpace(taskTitle))
                {
                    TempData["ErrorMessage"] = "Task title is required.";
                    return RedirectToAction(nameof(AssignTasks));
                }

                if (string.IsNullOrWhiteSpace(taskDescription))
                {
                    TempData["ErrorMessage"] = "Task description is required.";
                    return RedirectToAction(nameof(AssignTasks));
                }

                var volunteer = await _context.Users.FindAsync(volunteerId);
                if (volunteer == null)
                {
                    TempData["ErrorMessage"] = "Volunteer not found.";
                    return RedirectToAction(nameof(AssignTasks));
                }

                var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Create a VolunteerTask - FIXED: EstimatedHours as int
                var volunteerTask = new VolunteerTask
                {
                    Title = taskTitle.Trim(),
                    Description = taskDescription.Trim(),
                    TaskType = "Assigned Task",
                    Location = "Various Locations",
                    EstimatedHours = 4,
                    RequiredSkills = "As assigned",
                    Urgency = priority,
                    Status = "Assigned",
                    DateCreated = DateTime.UtcNow,
                    CreatedByUserID = adminId
                };

                _context.VolunteerTasks.Add(volunteerTask);
                await _context.SaveChangesAsync(); 

                
                var taskAssignment = new TaskAssignment
                {
                    VolunteerId = volunteerId,
                    TaskTitle = taskTitle.Trim(),
                    TaskDescription = taskDescription.Trim(),
                    DueDate = dueDate,
                    Priority = priority,
                    Status = "Assigned",
                    DateAssigned = DateTime.UtcNow
                };

                _context.TaskAssignments.Add(taskAssignment);
                await _context.SaveChangesAsync();

              
                var contribution = new VolunteerContribution
                {
                    UserID = volunteerId,
                    TaskID = volunteerTask.TaskID, 
                    HoursWorked = 0.0m,
                    Description = $"Assigned task: {taskTitle}",
                    Status = "Assigned",
                    ContributionDate = DateTime.UtcNow
                };

                _context.VolunteerContributions.Add(contribution);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Task '{taskTitle}' assigned to {volunteer.FullName} successfully!";
                return RedirectToAction(nameof(AssignTasks));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error assigning task: {ex.Message}";
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += $" - {ex.InnerException.Message}";
                }
                return RedirectToAction(nameof(AssignTasks));
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTaskStatus(int taskId, string status)
        {
            try
            {
                var task = await _context.TaskAssignments.FindAsync(taskId);
                if (task != null)
                {
                    task.Status = status;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Task status updated successfully!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating task status: {ex.Message}";
            }
            return RedirectToAction(nameof(AssignTasks));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            try
            {
                var task = await _context.TaskAssignments.FindAsync(taskId);
                if (task != null)
                {
                    _context.TaskAssignments.Remove(task);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Task deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting task: {ex.Message}";
            }
            return RedirectToAction(nameof(AssignTasks));
        }

        // ... rest of your methods remain the same
        [HttpGet]
        public async Task<IActionResult> ManageSchedules()
        {
            var volunteers = await _context.Users
                .Where(u => u.Role == "Volunteer")
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var schedules = await _context.VolunteerSchedules
                .Include(s => s.Volunteer)
                .Where(s => s.ShiftDate >= DateTime.Today.Date)
                .OrderBy(s => s.ShiftDate)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            ViewBag.Volunteers = volunteers;
            ViewBag.Schedules = schedules;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSchedule(int volunteerId, DateTime shiftDate, TimeSpan startTime, TimeSpan endTime, string assignment)
        {
            if (ModelState.IsValid)
            {
                var schedule = new VolunteerSchedule
                {
                    VolunteerId = volunteerId,
                    ShiftDate = shiftDate.Date,
                    StartTime = startTime,
                    EndTime = endTime,
                    Assignment = assignment,
                    Status = "Scheduled"
                };

                _context.VolunteerSchedules.Add(schedule);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Schedule added successfully!";
                return RedirectToAction(nameof(ManageSchedules));
            }

            TempData["ErrorMessage"] = "Please fill in all required fields.";
            return RedirectToAction(nameof(ManageSchedules));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateScheduleStatus(int scheduleId, string status)
        {
            var schedule = await _context.VolunteerSchedules.FindAsync(scheduleId);
            if (schedule != null)
            {
                schedule.Status = status;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Schedule status updated successfully!";
            }
            return RedirectToAction(nameof(ManageSchedules));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSchedule(int scheduleId)
        {
            var schedule = await _context.VolunteerSchedules.FindAsync(scheduleId);
            if (schedule != null)
            {
                _context.VolunteerSchedules.Remove(schedule);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Schedule deleted successfully!";
            }
            return RedirectToAction(nameof(ManageSchedules));
        }

        [HttpGet]
        public async Task<IActionResult> ManageDonations()
        {
            var donations = await _context.Donations
                .Include(d => d.User)
                .OrderByDescending(d => d.DonationDate)
                .ToListAsync();

            ViewBag.TotalDonations = donations.Count;
            ViewBag.TotalAmount = donations.Where(d => d.Amount.HasValue).Sum(d => d.Amount.Value);
            ViewBag.PendingCount = donations.Count(d => d.Status == "Pending");
            ViewBag.CompletedCount = donations.Count(d => d.Status == "Completed");

            return View(donations);
        }

        [HttpGet]
        public async Task<IActionResult> DonationDetails(int id)
        {
            var donation = await _context.Donations
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DonationID == id);

            if (donation == null)
            {
                return NotFound();
            }

            return View(donation);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDonationStatus(int donationId, string status)
        {
            try
            {
                var donation = await _context.Donations.FindAsync(donationId);
                if (donation != null)
                {
                    donation.Status = status;
                    _context.Donations.Update(donation);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Donation status updated to {status} successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Donation not found!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating donation status: {ex.Message}";
            }
            return RedirectToAction(nameof(ManageDonations));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDonation(int donationId)
        {
            var donation = await _context.Donations.FindAsync(donationId);
            if (donation != null)
            {
                _context.Donations.Remove(donation);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Donation deleted successfully!";
            }
            return RedirectToAction(nameof(ManageDonations));
        }
    }
}