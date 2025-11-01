using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversFoundation.Data;
using GiftOfTheGiversFoundation.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Security.Cryptography;
using GiftOfTheGiversFoundation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace GiftOfTheGiversFoundation.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, IEmailSender emailSender, ILogger<AccountController> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
        }

        // ================= REGISTER =================

        // ================= REGISTER =================
        [HttpGet]
        public IActionResult Register()
        {
            // Clear any existing session when accessing register page
            HttpContext.Session.Remove("TwoFactorUserId");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Register: Model state invalid");
                return View(model);
            }

            try
            {
                _logger.LogInformation("Starting registration for email: {Email}", model.Email);

                // Prevent unauthorized Admin registration
                if (model.Role == "Admin")
                {
                    var adminExists = await _context.Users.AnyAsync(u => u.Role == "Admin");
                    if (adminExists)
                    {
                        ModelState.AddModelError("Role", "Admin registration is restricted.");
                        return View(model);
                    }
                }

                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email already registered.");
                    return View(model);
                }

                var hashedPassword = HashPassword(model.Password);
                var code = new Random().Next(100000, 999999).ToString();

                var user = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    PhoneNumber = model.Phone,
                    Password = hashedPassword,
                    Role = model.Role,
                    DateCreated = DateTime.UtcNow,
                    EmailVerified = false,
                    TwoFactorCode = code, // Set code here - SINGLE SAVE
                    TwoFactorExpiry = DateTime.UtcNow.AddMinutes(10)
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync(); // SINGLE SAVE OPERATION
                _logger.LogInformation("User saved to database with ID: {UserId}", user.UserID);

                // Send verification email (improved - don't break registration if email fails)
                try
                {
                    await _emailSender.SendEmailAsync(user.Email, "Email Verification Code - Gift of the Givers",
                        $"Hello {user.FullName},<br><br>" +
                        $"Your verification code is: <strong>{code}</strong><br><br>" +
                        $"This code will expire in 10 minutes.<br><br>" +
                        $"Thank you for registering with Gift of the Givers!");
                    _logger.LogInformation("Verification email sent to: {Email}", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send verification email to: {Email}", user.Email);
                    // Don't fail the registration if email fails - just log it
                }

                // Set session and redirect
                HttpContext.Session.SetInt32("TwoFactorUserId", user.UserID);
                TempData["SuccessMessage"] = "Registration successful! Please check your email for the verification code.";

                _logger.LogInformation("Registration completed successfully for user: {UserId}", user.UserID);
                return RedirectToAction("TwoFactor");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error during registration for email: {Email}", model.Email);
                ModelState.AddModelError("", "A database error occurred. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for email: {Email}", model.Email);
                ModelState.AddModelError("", "An unexpected error occurred during registration. Please try again.");
                return View(model);
            }
        }
        // ================= LOGIN =================
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user == null || !VerifyPassword(model.Password, user.Password))
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View(model);
                }

                // If email already verified, log in directly
                if (user.EmailVerified)
                {
                    await SignInUser(user);
                    return RedirectToDashboard(user.Role);
                }

                // If not verified, send verification code
                var code = new Random().Next(100000, 999999).ToString();
                user.TwoFactorCode = code;
                user.TwoFactorExpiry = DateTime.UtcNow.AddMinutes(5);

                await _context.SaveChangesAsync();

                // Send verification email 
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailSender.SendEmailAsync(user.Email, "Email Verification Code - Gift of the Givers",
                            $"Hello {user.FullName},<br><br>" +
                            $"Your verification code is: <strong>{code}</strong><br><br>" +
                            $"This code will expire in 5 minutes.<br><br>" +
                            $"Thank you for using Gift of the Givers!");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send verification email during login for: {Email}", user.Email);
                    }
                });

                HttpContext.Session.SetInt32("TwoFactorUserId", user.UserID);
                TempData["SuccessMessage"] = "Please verify your email with the code we sent to your inbox.";
                return RedirectToAction("TwoFactor");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", model.Email);
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                return View(model);
            }
        }

        // ================= TWO-FACTOR / VERIFY =================
        [HttpGet]
        public IActionResult TwoFactor()
        {
            var userId = HttpContext.Session.GetInt32("TwoFactorUserId");
            if (userId == null)
            {
                _logger.LogWarning("TwoFactor: No user ID in session, redirecting to login");
                return RedirectToAction("Login");
            }

            return View(new TwoFactorViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TwoFactor(TwoFactorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("TwoFactor: Model state invalid");
                return View(model);
            }

            var userId = HttpContext.Session.GetInt32("TwoFactorUserId");
            if (userId == null)
            {
                _logger.LogWarning("TwoFactor: No user ID in session");
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("TwoFactor: User not found for ID: {UserId}", userId);
                return RedirectToAction("Login");
            }

            if (user.TwoFactorCode != model.Code || user.TwoFactorExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning("TwoFactor: Invalid or expired code for user: {UserId}", userId);
                ModelState.AddModelError("Code", "Invalid or expired verification code.");
                return View(model);
            }

            // Clear 2FA data and mark email as verified
            user.TwoFactorCode = null;
            user.TwoFactorExpiry = null;
            user.EmailVerified = true;
            await _context.SaveChangesAsync();

            // Clear session
            HttpContext.Session.Remove("TwoFactorUserId");

            await SignInUser(user);
            TempData["SuccessMessage"] = "Email verified successfully!";
            _logger.LogInformation("TwoFactor: Email verified successfully for user: {UserId}", userId);
            return RedirectToDashboard(user.Role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendCode()
        {
            var userId = HttpContext.Session.GetInt32("TwoFactorUserId");
            if (userId == null)
            {
                _logger.LogWarning("ResendCode: No user ID in session");
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("ResendCode: User not found for ID: {UserId}", userId);
                return RedirectToAction("Login");
            }

            var code = new Random().Next(100000, 999999).ToString();
            user.TwoFactorCode = code;
            user.TwoFactorExpiry = DateTime.UtcNow.AddMinutes(5);

            await _context.SaveChangesAsync();

            // Send email (fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailSender.SendEmailAsync(user.Email, "New Verification Code - Gift of the Givers",
                        $"Hello {user.FullName},<br><br>" +
                        $"Your new verification code is: <strong>{code}</strong><br><br>" +
                        $"This code will expire in 5 minutes.<br><br>" +
                        $"Thank you for using Gift of the Givers!");
                    _logger.LogInformation("ResendCode: New verification code sent to: {Email}", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ResendCode: Failed to send email to: {Email}", user.Email);
                }
            });

            TempData["SuccessMessage"] = "New verification code sent to your email!";
            return RedirectToAction("TwoFactor");
        }

        // ================= PROFILE =================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (email == null) return RedirectToAction("Login");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return RedirectToAction("Login");

            var model = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fix the validation errors.";
                return View(model);
            }

            try
            {
                var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                if (email == null) return RedirectToAction("Login");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null) return RedirectToAction("Login");

                // Store old name for comparison
                var oldName = user.FullName;

                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                await _context.SaveChangesAsync();

                // If name changed, update the authentication cookie immediately
                if (oldName != model.FullName)
                {
                    await UpdateAuthenticationCookie(user);
                }

                TempData["SuccessMessage"] = "Profile updated successfully!";

                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user");
                TempData["Error"] = "An error occurred while updating your profile. Please try again.";
                return View(model);
            }
        }

        // ================= LOGOUT =================
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "You have been logged out.";
            return RedirectToAction("Login");
        }

        // ================= ACCESS DENIED =================
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ================= DASHBOARD REDIRECT =================
        private IActionResult RedirectToDashboard(string role)
        {
            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Volunteer" => RedirectToAction("Dashboard", "Volunteer"),
                "User" => RedirectToAction("Dashboard", "User"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        // ================= HELPERS =================
        private async Task SignInUser(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }

        private async Task UpdateAuthenticationCookie(User user)
        {
            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);


            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }

        private string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);

            var hashBytes = new byte[48];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 32);

            return Convert.ToBase64String(hashBytes);
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                var hashBytes = Convert.FromBase64String(storedHash);
                var salt = new byte[16];
                Array.Copy(hashBytes, 0, salt, 0, 16);

                var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
                var hash = pbkdf2.GetBytes(32);

                for (int i = 0; i < 32; i++)
                {
                    if (hashBytes[i + 16] != hash[i])
                        return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}