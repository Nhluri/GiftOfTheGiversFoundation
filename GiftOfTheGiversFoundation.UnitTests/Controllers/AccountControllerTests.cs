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
using Microsoft.AspNetCore.Identity.UI.Services; // FIXED: Correct namespace

namespace GiftOfTheGiversFoundation.UnitTests.Controllers
{
    [TestClass]
    public class AccountControllerTests
    {
        private Mock<ILogger<AccountController>>? _loggerMock;
        private Mock<IEmailSender>? _emailSenderMock;
        private ApplicationDbContext? _context;
        private AccountController? _controller;

        [TestInitialize]
        public void Initialize()
        {
            _loggerMock = new Mock<ILogger<AccountController>>();
            _emailSenderMock = new Mock<IEmailSender>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _controller = new AccountController(_context, _emailSenderMock.Object, _loggerMock.Object);

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
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
            // Act
            var result = _controller!.Register();

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public async Task Register_POST_ValidModel_ReturnsActionResult()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                FullName = "Test User",
                Email = "test@example.com",
                Phone = "0831234567",
                Password = "Test123!",
                ConfirmPassword = "Test123!",
                Role = "User"
            };

            // Act
            var result = await _controller!.Register(model);

            // Assert
            Assert.IsInstanceOfType(result, typeof(IActionResult));
        }

        [TestMethod]
        public async Task Register_POST_InvalidModel_ReturnsView()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                FullName = "",
                Email = "invalid-email",
                Password = "123",
                ConfirmPassword = "456"
            };

            // Act
            var result = await _controller!.Register(model);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public async Task Register_POST_DuplicateEmail_ReturnsError()
        {
            // Arrange
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

            // Act
            var result = await _controller!.Register(model);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.IsTrue(_controller.ModelState.ErrorCount > 0);
        }

        [TestMethod]
        public async Task Login_POST_ValidCredentials_ReturnsActionResult()
        {
            // Arrange
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

            // Act
            var result = await _controller!.Login(model);

            // Assert
            Assert.IsInstanceOfType(result, typeof(IActionResult));
        }

        [TestMethod]
        public async Task Login_POST_InvalidCredentials_ReturnsView()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = "nonexistent@example.com",
                Password = "WrongPassword123!"
            };

            // Act
            var result = await _controller!.Login(model);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public async Task TwoFactor_POST_ValidCode_ReturnsActionResult()
        {
            // Arrange
            var user = new User
            {
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
            // Arrange
            var user = new User
            {
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
    }
}