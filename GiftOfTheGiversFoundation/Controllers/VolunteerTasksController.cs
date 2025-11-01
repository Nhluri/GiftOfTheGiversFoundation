using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversFoundation.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GiftOfTheGiversFoundation.Data;
using System.Threading.Tasks;

namespace GiftOfTheGiversFoundation.Controllers
{
    [Authorize(Roles = "Volunteer,Admin")]
    public class VolunteerTaskController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VolunteerTaskController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Browse Available Tasks
        public async Task<IActionResult> BrowseTasks()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Tasks assigned specifically to this volunteer
                var assignedTasks = await _context.TaskAssignments
                    .Include(ta => ta.Volunteer)
                    .Where(ta => ta.VolunteerId == userId && ta.Status == "Assigned")
                    .OrderByDescending(ta => ta.Priority == "Critical")
                    .ThenByDescending(ta => ta.Priority == "High")
                    .ThenByDescending(ta => ta.Priority == "Medium")
                    .ThenBy(ta => ta.DueDate)
                    .ToListAsync();

                // General available volunteer tasks
                var availableTasks = await _context.VolunteerTasks
                    .Where(t => t.Status == "Available")
                    .OrderByDescending(t => t.Urgency == "Critical")
                    .ThenByDescending(t => t.Urgency == "High")
                    .ThenByDescending(t => t.Urgency == "Medium")
                    .ThenBy(t => t.DateCreated)
                    .ToListAsync();

                ViewBag.AssignedTasks = assignedTasks;
                ViewBag.AvailableTasks = availableTasks;
                ViewBag.AssignedTasksCount = assignedTasks.Count;
                ViewBag.AvailableTasksCount = availableTasks.Count;

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading available tasks.";
                Console.WriteLine($"Error in BrowseTasks: {ex.Message}");
                return View();
            }
        }

        // POST: Start an assigned task - FIXED
        // POST: Start an assigned task - FINAL FIXED VERSION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartAssignedTask(int taskId)
        {
            try
            {
                Console.WriteLine($"Starting task with ID: {taskId}");

                var taskAssignment = await _context.TaskAssignments
                    .Include(ta => ta.Volunteer)
                    .FirstOrDefaultAsync(ta => ta.TaskID == taskId);

                if (taskAssignment == null)
                {
                    TempData["ErrorMessage"] = "Task assignment not found.";
                    return RedirectToAction(nameof(BrowseTasks));
                }

                // Authorization check
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (taskAssignment.VolunteerId != userId)
                {
                    TempData["ErrorMessage"] = "You are not authorized to start this task.";
                    return RedirectToAction(nameof(BrowseTasks));
                }

                // Update task assignment status
                taskAssignment.Status = "In Progress";
                _context.TaskAssignments.Update(taskAssignment);

                // Find existing VolunteerTask or create new one
                VolunteerTask volunteerTask;

                var existingTask = await _context.VolunteerTasks
                    .FirstOrDefaultAsync(vt => vt.Title == taskAssignment.TaskTitle);

                if (existingTask != null)
                {
                    volunteerTask = existingTask;
                    volunteerTask.Status = "In Progress";
                    _context.VolunteerTasks.Update(volunteerTask);
                }
                else
                {
                    // Create a new VolunteerTask - FIXED: EstimatedHours as int
                    volunteerTask = new VolunteerTask
                    {
                        Title = taskAssignment.TaskTitle,
                        Description = taskAssignment.TaskDescription,
                        TaskType = "Assigned Task",
                        Location = "Various Locations",
                        EstimatedHours = 4, // CHANGED TO int
                        RequiredSkills = "As assigned",
                        Urgency = taskAssignment.Priority,
                        Status = "In Progress",
                        DateCreated = DateTime.UtcNow,
                        CreatedByUserID = userId
                    };

                    _context.VolunteerTasks.Add(volunteerTask);
                }

                // Save changes to get the TaskID
                await _context.SaveChangesAsync();

                // Check if contribution already exists
                var existingContribution = await _context.VolunteerContributions
                    .FirstOrDefaultAsync(vc => vc.UserID == userId && vc.TaskID == volunteerTask.TaskID);

                if (existingContribution != null)
                {
                    // Update existing contribution
                    existingContribution.Status = "In Progress";
                    existingContribution.Description = $"Working on assigned task: {taskAssignment.TaskTitle}";
                    existingContribution.HoursWorked = 0.0m;
                    _context.VolunteerContributions.Update(existingContribution);
                }
                else
                {
                    // Create new contribution record
                    var contribution = new VolunteerContribution
                    {
                        UserID = userId,
                        TaskID = volunteerTask.TaskID,
                        HoursWorked = 0.0m,
                        Description = $"Working on assigned task: {taskAssignment.TaskTitle}",
                        Status = "In Progress",
                        ContributionDate = DateTime.UtcNow
                    };

                    _context.VolunteerContributions.Add(contribution);
                }

                // Final save
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"You have started: {taskAssignment.TaskTitle}";
                return RedirectToAction("MyContributions", "VolunteerContribution");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in StartAssignedTask: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }

                TempData["ErrorMessage"] = $"Error starting the task: {ex.Message}";
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += $" - {ex.InnerException.Message}";
                }
                return RedirectToAction(nameof(BrowseTasks));
            }
        }
        // GET: Task Details
        public async Task<IActionResult> TaskDetails(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Task ID not provided.";
                return RedirectToAction(nameof(BrowseTasks));
            }

            try
            {
                var task = await _context.VolunteerTasks
                    .FirstOrDefaultAsync(m => m.TaskID == id);

                if (task == null)
                {
                    TempData["ErrorMessage"] = "Task not found.";
                    return RedirectToAction(nameof(BrowseTasks));
                }

                return View(task);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading task details.";
                Console.WriteLine($"Error in TaskDetails: {ex.Message}");
                return RedirectToAction(nameof(BrowseTasks));
            }
        }

        // POST: Sign up for a task
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUpForTask(int taskId)
        {
            try
            {
                var task = await _context.VolunteerTasks.FindAsync(taskId);
                if (task == null)
                {
                    TempData["ErrorMessage"] = "Task not found.";
                    return RedirectToAction(nameof(BrowseTasks));
                }

                if (task.Status != "Available")
                {
                    TempData["ErrorMessage"] = "This task is no longer available.";
                    return RedirectToAction(nameof(BrowseTasks));
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Prevent duplicate signup
                var existingContribution = await _context.VolunteerContributions
                    .FirstOrDefaultAsync(c => c.UserID == userId && c.TaskID == taskId);

                if (existingContribution != null)
                {
                    TempData["ErrorMessage"] = "You have already signed up for this task.";
                    return RedirectToAction(nameof(BrowseTasks));
                }

                // New contribution record
                var contribution = new VolunteerContribution
                {
                    UserID = userId,
                    TaskID = taskId,
                    HoursWorked = 0,
                    Description = $"Signed up for: {task.Title}",
                    Status = "In Progress",
                    ContributionDate = DateTime.UtcNow
                };

                // Update task
                task.Status = "Assigned";

                _context.VolunteerContributions.Add(contribution);
                _context.VolunteerTasks.Update(task);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Successfully signed up for: {task.Title}";
                return RedirectToAction("MyContributions", "VolunteerContribution");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error signing up for task. Please try again.";
                Console.WriteLine($"Error in SignUpForTask: {ex.Message}");
                return RedirectToAction(nameof(BrowseTasks));
            }
        }

        // ADMIN CRUD -------------------------

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var tasks = await _context.VolunteerTasks
                .Include(t => t.CreatedByUser)
                .OrderByDescending(t => t.DateCreated)
                .ToListAsync();
            return View(tasks);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var task = new VolunteerTask { CreatedByUserID = int.Parse(currentUserId) };
                return View(task);
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(VolunteerTask volunteerTask)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userExists = await _context.Users.AnyAsync(u => u.UserID == volunteerTask.CreatedByUserID);
                    if (!userExists)
                    {
                        ModelState.AddModelError("CreatedByUserID", "The specified User ID does not exist.");
                        return View(volunteerTask);
                    }

                    volunteerTask.DateCreated = DateTime.UtcNow;

                    _context.Add(volunteerTask);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Volunteer task created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error creating task: {ex.Message}";
                }
            }
            return View(volunteerTask);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var volunteerTask = await _context.VolunteerTasks.FindAsync(id);
            if (volunteerTask == null) return NotFound();

            return View(volunteerTask);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, VolunteerTask volunteerTask)
        {
            if (id != volunteerTask.TaskID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var userExists = await _context.Users.AnyAsync(u => u.UserID == volunteerTask.CreatedByUserID);
                    if (!userExists)
                    {
                        ModelState.AddModelError("CreatedByUserID", "The specified User ID does not exist.");
                        return View(volunteerTask);
                    }

                    _context.Update(volunteerTask);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Task updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VolunteerTaskExists(volunteerTask.TaskID))
                    {
                        return NotFound();
                    }
                    else throw;
                }
            }
            return View(volunteerTask);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var volunteerTask = await _context.VolunteerTasks
                .Include(t => t.CreatedByUser)
                .FirstOrDefaultAsync(m => m.TaskID == id);
            if (volunteerTask == null) return NotFound();

            return View(volunteerTask);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var volunteerTask = await _context.VolunteerTasks.FindAsync(id);
            if (volunteerTask != null)
            {
                _context.VolunteerTasks.Remove(volunteerTask);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Task deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var volunteerTask = await _context.VolunteerTasks
                .Include(t => t.CreatedByUser)
                .FirstOrDefaultAsync(m => m.TaskID == id);
            if (volunteerTask == null) return NotFound();

            return View(volunteerTask);
        }

        private bool VolunteerTaskExists(int id)
        {
            return _context.VolunteerTasks.Any(e => e.TaskID == id);
        }
    }
}
