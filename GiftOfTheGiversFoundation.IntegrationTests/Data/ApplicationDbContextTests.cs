using Microsoft.VisualStudio.TestTools.UnitTesting;
using GiftOfTheGiversFoundation.Data;
using GiftOfTheGiversFoundation.Models;
using Microsoft.EntityFrameworkCore;

namespace GiftOfTheGiversFoundation.IntegrationTests.Data
{
    [TestClass]
    public class ApplicationDbContextTests
    {
        private ApplicationDbContext? _context;
        private DbContextOptions<ApplicationDbContext>? _options;

        [TestInitialize]
        public void Initialize()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=GiftOfTheGiversTest;Trusted_Connection=true;MultipleActiveResultSets=true")
                .Options;

            _context = new ApplicationDbContext(_options);
            _context.Database.EnsureCreated();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context?.Database.EnsureDeleted();
            _context?.Dispose();
        }

        [TestMethod]
        public async Task CreateUser_SavesToDatabase()
        {
            // Arrange
            var user = new User
            {
                FullName = "Integration Test User",
                Email = "integration@test.com",
                PhoneNumber = "0831234567",
                Password = "hashedpassword",
                Role = "User",
                DateCreated = DateTime.UtcNow
            };

            // Act
            _context!.Users.Add(user);
            await _context.SaveChangesAsync();

            // Assert
            var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "integration@test.com");
            Assert.IsNotNull(savedUser);
            Assert.AreEqual("Integration Test User", savedUser.FullName);
        }
    }
}