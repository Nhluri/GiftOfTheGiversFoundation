using GiftOfTheGiversFoundation.Data;
using GiftOfTheGiversFoundation.Models;
using GiftOfTheGiversFoundation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace GiftOfTheGiversFoundation.Controllers
{
    [Authorize(Roles = "User,Admin,Volunteer")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = _context.Users.FirstOrDefault(u => u.UserID == userId);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Dashboard");
            }

            var model = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(ProfileViewModel model)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fix the errors in the form.";
                return View(model);
            }

            var user = _context.Users.FirstOrDefault(u => u.UserID == userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Dashboard");
            }

            try
            {
                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;

                _context.Users.Update(user);
                _context.SaveChanges();

                TempData["Message"] = "Profile updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating profile: {ex.Message}";
            }

            return RedirectToAction("Profile");
        }

        public IActionResult Dashboard()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            ViewBag.TotalDonations = _context.Donations?.Count(d => d.UserID == userId) ?? 0;
            ViewBag.TotalResources = _context.Resources?.Count(r => r.UserID == userId) ?? 0;
            ViewBag.ReportedIncidents = _context.Incidents?.Count(i => i.UserID == userId) ?? 0;
            ViewBag.ActiveIncidents = _context.Incidents?.Count(i => i.Status != "Resolved") ?? 0;

            return View();
        }
    }
}
