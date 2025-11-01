using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversFoundation.Data;
using GiftOfTheGiversFoundation.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace GiftOfTheGiversFoundation.Controllers
{
    [Authorize]
    public class ResourceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResourceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Resource
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var resources = await _context.Resources
                .Where(r => r.UserID == userId)
                .OrderByDescending(r => r.DateSubmitted)
                .ToListAsync();

            return View(resources);
        }

        // GET: Resource/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resource = await _context.Resources
                .FirstOrDefaultAsync(m => m.ResourceID == id);

            if (resource == null)
            {
                return NotFound();
            }

            // Authorization check
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (resource.UserID != userId)
            {
                TempData["ErrorMessage"] = "You are not authorized to view this resource.";
                return RedirectToAction(nameof(Index));
            }

            return View(resource);
        }

        // GET: Resource/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Resource/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Resource resource)
        {
            if (ModelState.IsValid)
            {
                resource.UserID = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                resource.DateSubmitted = DateTime.UtcNow;
                resource.Availability = "Available";

                _context.Resources.Add(resource);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Resource submitted successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(resource);
        }

        // GET: Resource/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resource = await _context.Resources
                .FirstOrDefaultAsync(m => m.ResourceID == id);

            if (resource == null)
            {
                return NotFound();
            }

            // Authorization check
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (resource.UserID != userId)
            {
                TempData["ErrorMessage"] = "You are not authorized to delete this resource.";
                return RedirectToAction(nameof(Index));
            }

            return View(resource);
        }

        // POST: Resource/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource == null)
            {
                return NotFound();
            }

            // Authorization check
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (resource.UserID != userId)
            {
                TempData["ErrorMessage"] = "You are not authorized to delete this resource.";
                return RedirectToAction(nameof(Index));
            }

            _context.Resources.Remove(resource);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Resource deleted successfully!";
            return RedirectToAction(nameof(Index));
        }


        // GET: Resource/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var resource = await _context.Resources.FindAsync(id);
            if (resource == null) return NotFound();

            // Authorization check
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (resource.UserID != userId)
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this resource.";
                return RedirectToAction(nameof(Index));
            }

            return View(resource);
        }

        // POST: Resource/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Resource resource)
        {
            if (id != resource.ResourceID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Get existing resource to preserve original data
                    var existingResource = await _context.Resources.AsNoTracking().FirstOrDefaultAsync(r => r.ResourceID == id);

                    if (existingResource == null) return NotFound();

                    // Authorization check
                    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    if (existingResource.UserID != userId)
                    {
                        TempData["ErrorMessage"] = "You are not authorized to edit this resource.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Preserve original user and date
                    resource.UserID = existingResource.UserID;
                    resource.DateSubmitted = existingResource.DateSubmitted;
                    resource.Availability = existingResource.Availability; // Users can't change availability

                    _context.Update(resource);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Resource updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ResourceExists(resource.ResourceID))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(resource);
        }

        private bool ResourceExists(int id)
        {
            return _context.Resources.Any(e => e.ResourceID == id);
        }
    }
}