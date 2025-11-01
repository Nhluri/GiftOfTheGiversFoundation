using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversFoundation.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GiftOfTheGiversFoundation.Data;

namespace GiftOfTheGiversFoundation.Controllers
{
    [Authorize(Roles = "Volunteer,Admin")]
    public class VolunteerContributionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VolunteerContributionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: My Contributions
        public async Task<IActionResult> MyContributions()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var contributions = await _context.VolunteerContributions
                    .Include(c => c.User)
                    .Include(c => c.Task) // Include the Task
                    .Where(c => c.UserID == userId)
                    .OrderByDescending(c => c.ContributionDate)
                    .ToListAsync();

                // Calculate statistics - handle null cases
                var totalHours = contributions
                    .Where(c => c.Status == "Completed")
                    .Sum(c => c.HoursWorked);

                var totalTasks = contributions.Count;
                var completedTasks = contributions.Count(c => c.Status == "Completed");

                ViewBag.TotalHours = totalHours;
                ViewBag.TotalTasks = totalTasks;
                ViewBag.CompletedTasks = completedTasks;

                return View(contributions);
            }
            catch (Exception ex)
            {
                // Log the actual error for debugging
                Console.WriteLine($"ERROR in MyContributions: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                TempData["ErrorMessage"] = $"Error loading contributions: {ex.Message}";
                return View(new List<VolunteerContribution>());
            }
        }

        // GET: Mark as Completed
        public async Task<IActionResult> MarkCompleted(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Contribution ID not provided.";
                return RedirectToAction(nameof(MyContributions));
            }

            try
            {
                var contribution = await _context.VolunteerContributions
                    .Include(c => c.Task)
                    .FirstOrDefaultAsync(c => c.ContributionID == id);

                if (contribution == null)
                {
                    TempData["ErrorMessage"] = "Contribution not found.";
                    return RedirectToAction(nameof(MyContributions));
                }

                // Authorization check
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (contribution.UserID != userId)
                {
                    TempData["ErrorMessage"] = "You are not authorized to update this contribution.";
                    return RedirectToAction(nameof(MyContributions));
                }

                if (contribution.Status == "Completed")
                {
                    TempData["ErrorMessage"] = "This task is already completed.";
                    return RedirectToAction(nameof(MyContributions));
                }

                return View(contribution);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading contribution: {ex.Message}";
                Console.WriteLine($"Error in MarkCompleted GET: {ex.Message}");
                return RedirectToAction(nameof(MyContributions));
            }
        }

        // POST: Mark as Completed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkCompleted(int id, decimal hoursWorked, string workDescription)
        {
            try
            {
                var contribution = await _context.VolunteerContributions
                    .FirstOrDefaultAsync(c => c.ContributionID == id);

                if (contribution == null)
                {
                    TempData["ErrorMessage"] = "Contribution not found.";
                    return RedirectToAction(nameof(MyContributions));
                }

                // Authorization check
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (contribution.UserID != userId)
                {
                    TempData["ErrorMessage"] = "You are not authorized to update this contribution.";
                    return RedirectToAction(nameof(MyContributions));
                }

                if (hoursWorked <= 0)
                {
                    TempData["ErrorMessage"] = "Hours worked must be greater than 0.";
                    return View(contribution);
                }

                // Update contribution
                contribution.HoursWorked = hoursWorked;
                contribution.Description = workDescription;
                contribution.Status = "Completed";

                _context.VolunteerContributions.Update(contribution);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Task marked as completed! {hoursWorked} hours logged.";
                return RedirectToAction(nameof(MyContributions));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating task.";
                Console.WriteLine($"Error in MarkCompleted POST: {ex.Message}");
                return RedirectToAction(nameof(MyContributions));
            }
        }
    }
}