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
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _context = new ApplicationDbContext(options);

            _controller = new AccountController(_context, _emailSenderMock.Object, _loggerMock.Object);

            var httpContext = new DefaultHttpContext();
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
            // Act
            var result = _controller!.Register();

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public async Task Register_POST_ValidModel_CreatesUser()
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
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
            var redirectResult = (RedirectToActionResult)result;
            Assert.AreEqual("TwoFactor", redirectResult.ActionName);
        }

        [TestMethod]
        public async Task Register_POST_InvalidModel_ReturnsView()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                FullName = "", // Invalid
                Email = "invalid-email",
                Password = "123",
                ConfirmPassword = "456"
            };
            _controller!.ModelState.AddModelError("Error", "Model error");

            // Act
            var result = await _controller.Register(model);

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
                Email = "existing@example.com", // Duplicate email
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
    }
}