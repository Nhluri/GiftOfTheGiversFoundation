using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace GiftOfTheGiversFoundation.IntegrationTests.Controllers
{
    [TestClass]
    public class AccountIntegrationTests
    {
        private WebApplicationFactory<Program>? _factory;
        private HttpClient? _client;

        [TestInitialize]
        public void Initialize()
        {
            _factory = new WebApplicationFactory<Program>();
            _client = _factory.CreateClient();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _client?.Dispose();
            _factory?.Dispose();
        }

        [TestMethod]
        public async Task Login_GET_ReturnsSuccess()
        {
            // Act
            var response = await _client!.GetAsync("/Account/Login");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.AreEqual("text/html; charset=utf-8",
                response.Content.Headers.ContentType?.ToString());
        }

        [TestMethod]
        public async Task Register_GET_ReturnsSuccess()
        {
            // Act
            var response = await _client!.GetAsync("/Account/Register");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.AreEqual("text/html; charset=utf-8",
                response.Content.Headers.ContentType?.ToString());
        }

        [TestMethod]
        public async Task Home_Index_ReturnsSuccess()
        {
            // Act
            var response = await _client!.GetAsync("/");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.AreEqual("text/html; charset=utf-8",
                response.Content.Headers.ContentType?.ToString());
        }
    }
}