using System.Diagnostics;
using GiftOfTheGiversFoundation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace GiftOfTheGiversFoundation.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize]
        public IActionResult Dashboard()
        {
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            _logger.LogInformation("Dashboard redirect - User role: {Role}", role);

            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Volunteer" => RedirectToAction("Dashboard", "Volunteer"),
                "User" => RedirectToAction("Dashboard", "User"),
                _ => RedirectToAction("Login", "Account")
            };
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}