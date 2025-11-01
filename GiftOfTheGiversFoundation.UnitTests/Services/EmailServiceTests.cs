using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GiftOfTheGiversFoundation.Services;
using Microsoft.Extensions.Options;

namespace GiftOfTheGiversFoundation.UnitTests.Services
{
    [TestClass]
    public class EmailServiceTests
    {
        [TestMethod]
        public async Task SendEmailAsync_ValidParameters_DoesNotThrow()
        {
            // Arrange
            var emailSettings = new EmailSettings
            {
                GmailEmail = "test@test.com",
                GmailAppPassword = "password",
                FromName = "Test",
                SmtpHost = "smtp.test.com",
                SmtpPort = 587,
                Timeout = 30000
            };

            var optionsMock = new Mock<IOptions<EmailSettings>>();
            optionsMock.Setup(x => x.Value).Returns(emailSettings);

            var emailService = new EmailService(optionsMock.Object);

            // Act & Assert
            await emailService.SendEmailAsync("recipient@test.com", "Test Subject", "Test Message");
            Assert.IsTrue(true); // If we reach here, no exception was thrown
        }
    }
}