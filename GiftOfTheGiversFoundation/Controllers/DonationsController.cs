using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversFoundation.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GiftOfTheGiversFoundation.Data;

namespace GiftOfTheGiversFoundation.Controllers
{
    [Authorize]
    public class DonationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DonationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Donation
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var donations = await _context.Donations
                .Where(d => d.UserID == userId)
                .OrderByDescending(d => d.DonationDate)
                .ToListAsync();

            return View(donations);
        }

        // GET: Donation/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Donation/Create 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Donation donation)
        {
            // Manual validation
            if (string.IsNullOrEmpty(donation.DonationType) || string.IsNullOrEmpty(donation.Description))
            {
                TempData["ErrorMessage"] = "Please fill in all required fields.";
                return View(donation);
            }

            try
            {
                // Get user ID
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                
                var newDonation = new Donation
                {
                    UserID = userId,
                    DonationType = donation.DonationType.Trim(),
                    Amount = donation.Amount,
                    Description = donation.Description.Trim(),
                    DonationDate = DateTime.Now, // Use local time for testing
                    Status = "Pending"
                };

                // Add to context
                _context.Donations.Add(newDonation);

                // Save changes
                int recordsAffected = await _context.SaveChangesAsync();

                if (recordsAffected > 0)
                {
                    TempData["SuccessMessage"] = $"Donation submitted successfully! ID: {newDonation.DonationID}";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to save donation. No records were affected.";
                    return View(donation);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += $" - {ex.InnerException.Message}";
                }
                return View(donation);
            }
        }

        // GET: Donation/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return RedirectToAction(nameof(Index));

            var donation = await _context.Donations.FirstOrDefaultAsync(m => m.DonationID == id);
            if (donation == null) return RedirectToAction(nameof(Index));

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (donation.UserID != userId)
            {
                TempData["ErrorMessage"] = "Not authorized.";
                return RedirectToAction(nameof(Index));
            }

            return View(donation);
        }

        // GET: Donation/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return RedirectToAction(nameof(Index));

            var donation = await _context.Donations.FindAsync(id);
            if (donation == null) return RedirectToAction(nameof(Index));

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (donation.UserID != userId)
            {
                TempData["ErrorMessage"] = "Not authorized.";
                return RedirectToAction(nameof(Index));
            }

            return View(donation);
        }

        // POST: Donation/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Donation donation)
        {
            if (id != donation.DonationID) return RedirectToAction(nameof(Index));

            try
            {
                var existingDonation = await _context.Donations.FindAsync(id);
                if (existingDonation == null) return RedirectToAction(nameof(Index));

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (existingDonation.UserID != userId)
                {
                    TempData["ErrorMessage"] = "Not authorized.";
                    return RedirectToAction(nameof(Index));
                }

                // Update fields
                existingDonation.DonationType = donation.DonationType;
                existingDonation.Amount = donation.Amount;
                existingDonation.Description = donation.Description;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Donation updated!";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                TempData["ErrorMessage"] = "Error updating donation.";
                return View(donation);
            }
        }

        // GET: Donation/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return RedirectToAction(nameof(Index));

            var donation = await _context.Donations.FirstOrDefaultAsync(m => m.DonationID == id);
            if (donation == null) return RedirectToAction(nameof(Index));

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (donation.UserID != userId)
            {
                TempData["ErrorMessage"] = "Not authorized.";
                return RedirectToAction(nameof(Index));
            }

            return View(donation);
        }

        // POST: Donation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var donation = await _context.Donations.FindAsync(id);
            if (donation == null) return RedirectToAction(nameof(Index));

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (donation.UserID != userId)
            {
                TempData["ErrorMessage"] = "Not authorized.";
                return RedirectToAction(nameof(Index));
            }

            _context.Donations.Remove(donation);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Donation deleted!";
            return RedirectToAction(nameof(Index));
        }

        // DIRECT DATABASE TEST
        [HttpPost]
        public async Task<IActionResult> TestDirectInsert()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Direct SQL insert (bypass Entity Framework if needed)
                var donation = new Donation
                {
                    UserID = userId,
                    DonationType = "DIRECT TEST",
                    Description = "Direct insert test",
                    DonationDate = DateTime.Now,
                    Status = "Pending",
                    Amount = 25.00m
                };

                _context.Donations.Add(donation);
                int result = await _context.SaveChangesAsync();

                return Content($"DIRECT INSERT RESULT: {result} rows affected, DonationID: {donation.DonationID}");
            }
            catch (Exception ex)
            {
                return Content($"DIRECT INSERT ERROR: {ex.Message} --- {ex.InnerException?.Message}");
            }
        }

        // CHECK DATABASE STATUS
        public async Task<IActionResult> CheckDbStatus()
        {
            var info = $"<h1>Database Status</h1>";

            try
            {
                info += $"<p>Can Connect: {_context.Database.CanConnect()}</p>";

                var donations = await _context.Donations.ToListAsync();
                info += $"<p>Total Donations: {donations.Count}</p>";

                foreach (var d in donations)
                {
                    info += $"<p>Donation {d.DonationID}: {d.DonationType} - {d.Description}</p>";
                }

                return Content(info, "text/html");
            }
            catch (Exception ex)
            {
                info += $"<p style='color:red'>ERROR: {ex.Message}</p>";
                return Content(info, "text/html");
            }
        }
    }
}