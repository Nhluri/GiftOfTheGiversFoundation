using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversFoundation.Data;
using GiftOfTheGiversFoundation.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace GiftOfTheGiversFoundation.Controllers
{
    [Authorize]
    public class IncidentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IncidentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Incident
        public async Task<IActionResult> Index()
        {
            IQueryable<Incident> incidentsQuery = _context.Incidents;

            if (!User.IsInRole("Admin"))
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                incidentsQuery = incidentsQuery.Where(i => i.UserID == userId); // Changed to UserID
            }

            var incidents = await incidentsQuery.OrderByDescending(i => i.IncidentDate).ToListAsync();
            return View("~/Views/Incidents/Index.cshtml", incidents);
        }

        // GET: Incident/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var incident = await _context.Incidents
                 // Changed to User
                .FirstOrDefaultAsync(m => m.IncidentID == id);

            if (incident == null) return NotFound();

            // Authorization check
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (!User.IsInRole("Admin") && incident.UserID != userId) // Changed to UserID
            {
                TempData["ErrorMessage"] = "You are not authorized to view this incident.";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Incidents/Details.cshtml", incident);
        }

        // GET: Incident/Create
        public IActionResult Create()
        {
            return View("~/Views/Incidents/Create.cshtml");
        }

        // POST: Incident/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Incident incident)
        {
            if (ModelState.IsValid)
            {
                incident.UserID = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); // Changed to UserID
                incident.DateReported = DateTime.UtcNow;

                _context.Add(incident);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Incident reported successfully!";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Please correct the errors below.";
            return View("~/Views/Incidents/Create.cshtml", incident);
        }

        // GET: Incident/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var incident = await _context.Incidents.FindAsync(id);
            if (incident == null) return NotFound();

            // Authorization check
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (!User.IsInRole("Admin") && incident.UserID != userId) // Changed to UserID
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this incident.";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Incidents/Edit.cshtml", incident);
        }

        // POST: Incident/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Incident incident)
        {
            if (id != incident.IncidentID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Get existing incident to preserve original data
                    var existingIncident = await _context.Incidents.AsNoTracking().FirstOrDefaultAsync(i => i.IncidentID == id);

                    if (existingIncident == null) return NotFound();

                    // Authorization check
                    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    if (!User.IsInRole("Admin") && existingIncident.UserID != userId) // Changed to UserID
                    {
                        TempData["ErrorMessage"] = "You are not authorized to edit this incident.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Preserve original user and date
                    incident.UserID = existingIncident.UserID; // Changed to UserID
                    incident.DateReported = existingIncident.DateReported;

                    _context.Update(incident);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Incident updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IncidentExists(incident.IncidentID))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Please correct the errors below.";
            return View("~/Views/Incidents/Edit.cshtml", incident);
        }

        // GET: Incident/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var incident = await _context.Incidents
                // Changed to User
                .FirstOrDefaultAsync(m => m.IncidentID == id);

            if (incident == null) return NotFound();

            // Authorization check
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (!User.IsInRole("Admin") && incident.UserID != userId) // Changed to UserID
            {
                TempData["ErrorMessage"] = "You are not authorized to delete this incident.";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Incidents/Delete.cshtml", incident);
        }

        // POST: Incident/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var incident = await _context.Incidents.FindAsync(id);
            if (incident == null) return NotFound();

            // Authorization check
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (!User.IsInRole("Admin") && incident.UserID != userId) // Changed to UserID
            {
                TempData["ErrorMessage"] = "You are not authorized to delete this incident.";
                return RedirectToAction(nameof(Index));
            }

            _context.Incidents.Remove(incident);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Incident deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private bool IncidentExists(int id)
        {
            return _context.Incidents.Any(e => e.IncidentID == id);
        }
    }
}