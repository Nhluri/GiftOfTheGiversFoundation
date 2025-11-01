using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

using GiftOfTheGiversFoundation.Data;

namespace GiftOfTheGiversFoundation.Controllers
{
    [Authorize(Roles = "Volunteer")]
    public class VolunteerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VolunteerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Get volunteer statistics
            ViewBag.CompletedTasks = _context.TaskAssignments?.Count(t => t.VolunteerId == userId && t.Status == "Completed") ?? 0;
            ViewBag.ActiveTasks = _context.TaskAssignments?.Count(t => t.VolunteerId == userId && t.Status == "In Progress") ?? 0;

            // Fix: Convert decimal to int for total hours
            var totalHoursDecimal = _context.VolunteerContributions?.Where(v => v.UserID == userId).Sum(v => v.HoursWorked) ?? 0m;
            ViewBag.TotalHours = (int)Math.Round(totalHoursDecimal); // Round and convert to int

            ViewBag.ReportedIncidents = _context.Incidents?.Count(i => i.UserID == userId) ?? 0;

            return View();
        }
    }
}