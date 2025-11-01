using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GiftOfTheGiversFoundation.Controllers;
using GiftOfTheGiversFoundation.Data;
using GiftOfTheGiversFoundation.Models;
using GiftOfTheGiversFoundation.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Text.Json;

namespace GiftOfTheGiversFoundation.UnitTests.Controllers
{
    [TestClass]
    public class AccountControllerTests
    {
        private Mock<ILogger<AccountController>>? _loggerMock;
        private Mock<IEmailSender>? _emailSenderMock;
        private ApplicationDbContext? _context;
        private AccountController? _controller;
        private Mock<ISession>? _sessionMock;
        private Dictionary<string, byte[]>? _sessionData;

        [TestInitialize]
        public void Initialize()
        {
            _loggerMock = new Mock<ILogger<AccountController>>();
            _emailSenderMock = new Mock<IEmailSender>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            // Mock Session
            _sessionMock = new Mock<ISession>();
            _sessionData = new Dictionary<string, byte[]>();

            // Setup session Set method
            _sessionMock.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
                       .Callback<string, byte[]>((key, value) => _sessionData[key] = value);

            // Setup session TryGetValue method
            _sessionMock.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny))
                       .Returns((string key, out byte[] value) =>
                       {
                           if (_sessionData.ContainsKey(key))
                           {
                               value = _sessionData[key];
                               return true;
                           }
                           value = null;
                           return false;
                       });

            _controller = new AccountController(_context, _emailSenderMock.Object, _loggerMock.Object);

            // Create HttpContext with mocked session
            var httpContext = new DefaultHttpContext();
            httpContext.Session = _sessionMock.Object;

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context?.Database.EnsureDeleted();
            _context?.Dispose();
        }

        [TestMethod]
        public void Register_GET_ReturnsView()
        {
            var result = _controller!.Register();
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public async Task Register_POST_ValidModel_ReturnsActionResult()
        {
            var model = new RegisterViewModel
            {
                FullName = "Test User",
                Email = "test@example.com",
                Phone = "0831234567",
                Password = "Test123!",
                ConfirmPassword = "Test123!",
                Role = "User"
            };

            var result = await _controller!.Register(model);
            Assert.IsInstanceOfType(result, typeof(IActionResult));
        }

        [TestMethod]
        public async Task Register_POST_InvalidModel_ReturnsView()
        {
            var model = new RegisterViewModel
            {
                FullName = "",
                Email = "invalid-email",
                Password = "123",
                ConfirmPassword = "456"
            };

            var result = await _controller!.Register(model);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public async Task Register_POST_DuplicateEmail_ReturnsError()
        {
            var existingUser = new User
            {
                FullName = "Existing User",
                Email = "existing@example.com",
                Password = "hashed",
                Role = "User"
            };
            _context!.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var model = new RegisterViewModel
            {
                FullName = "New User",
                Email = "existing@example.com",
                Phone = "0831234567",
                Password = "Test123!",
                ConfirmPassword = "Test123!",
                Role = "User"
            };

            var result = await _controller!.Register(model);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.IsTrue(_controller.ModelState.ErrorCount > 0);
        }

        [TestMethod]
        public async Task Login_POST_ValidCredentials_ReturnsActionResult()
        {
            var user = new User
            {
                FullName = "Test User",
                Email = "test@example.com",
                Password = "AQAAAAEAACcQAAAAEEXAMPLEHASHEDPASSWORD1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                Role = "User",
                EmailVerified = true
            };
            _context!.Users.Add(user);
            await _context.SaveChangesAsync();

            var model = new LoginViewModel
            {
                Email = "test@example.com",
                Password = "Test123!"
            };

            var result = await _controller!.Login(model);
            Assert.IsInstanceOfType(result, typeof(IActionResult));
        }

        [TestMethod]
        public async Task Login_POST_InvalidCredentials_ReturnsView()
        {
            var model = new LoginViewModel
            {
                Email = "nonexistent@example.com",
                Password = "WrongPassword123!"
            };

            var result = await _controller!.Login(model);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public async Task TwoFactor_POST_ValidCode_ReturnsActionResult()
        {
            // Arrange - Set session data first
            var userId = 1;
            var userIdBytes = BitConverter.GetBytes(userId);
            _sessionData!["TwoFactorUserId"] = userIdBytes;

            var user = new User
            {
                UserID = userId,
                FullName = "Test User",
                Email = "test@example.com",
                Password = "hashed",
                Role = "User",
                TwoFactorCode = "123456",
                TwoFactorExpiry = DateTime.UtcNow.AddMinutes(10)
            };
            _context!.Users.Add(user);
            await _context.SaveChangesAsync();

            var model = new TwoFactorViewModel { Code = "123456" };

            // Act
            var result = await _controller!.TwoFactor(model);

            // Assert
            Assert.IsInstanceOfType(result, typeof(IActionResult));
        }

        [TestMethod]
        public async Task TwoFactor_POST_InvalidCode_ReturnsView()
        {
            // Arrange - Set session data first
            var userId = 1;
            var userIdBytes = BitConverter.GetBytes(userId);
            _sessionData!["TwoFactorUserId"] = userIdBytes;

            var user = new User
            {
                UserID = userId,
                FullName = "Test User",
                Email = "test@example.com",
                Password = "hashed",
                Role = "User",
                TwoFactorCode = "123456",
                TwoFactorExpiry = DateTime.UtcNow.AddMinutes(10)
            };
            _context!.Users.Add(user);
            await _context.SaveChangesAsync();

            var model = new TwoFactorViewModel { Code = "999999" };

            // Act
            var result = await _controller!.TwoFactor(model);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public async Task TwoFactor_POST_NoSession_RedirectsToLogin()
        {
            // Arrange - Don't set session data
            var model = new TwoFactorViewModel { Code = "123456" };

            // Act
            var result = await _controller!.TwoFactor(model);

            // Assert - Should redirect to login when no session
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirectResult = (RedirectToActionResult)result;
            Assert.AreEqual("Login", redirectResult.ActionName);
        }
    }
}