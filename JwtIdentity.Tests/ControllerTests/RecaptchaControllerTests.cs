using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JwtIdentity.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using JwtIdentity.Common.ViewModels;
using Moq.Protected;

namespace JwtIdentity.Tests.ControllerTests
{
    [TestFixture]
    public class RecaptchaControllerTests : TestBase
    {
        private RecaptchaController _controller;
        private Mock<HttpMessageHandler> _mockHttpHandler;
        private const string FakeSecret = "test-secret-key";

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            _mockHttpHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _mockHttpHandler.Protected().Setup("Dispose", ItExpr.IsAny<bool>()); // Allow Dispose
            // Setup configuration to return the fake secret
            MockConfiguration.Setup(c => c["Recaptcha:SecretKey"]).Returns(FakeSecret);
            // Remove controller creation from here
        }

        private void SetRemoteIp(string ip)
        {
            HttpContext.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        }

        private void SetupHttpClientResponse(string json, HttpStatusCode status = HttpStatusCode.OK)
        {
            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = status,
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
        }

        [Test]
        public async Task Validate_ReturnsBadRequest_WhenTokenMissing()
        {
            // Arrange
            var request = new RecaptchaRequest { Token = null };
            _controller = new RecaptchaController(MockConfiguration.Object); // No HttpClient needed
            _controller.ControllerContext = new ControllerContext { HttpContext = HttpContext };
            // Act
            var result = await _controller.Validate(request);
            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badResult = result as BadRequestObjectResult;
            Assert.That(badResult.Value, Is.Not.Null);
            var success = badResult.Value.GetType().GetProperty("Success")?.GetValue(badResult.Value);
            Assert.That(success, Is.False);
        }

        [Test]
        public async Task Validate_ReturnsOkFalse_WhenRecaptchaFails()
        {
            // Arrange
            SetRemoteIp("127.0.0.1");
            var request = new RecaptchaRequest { Token = "invalid-token" };
            var recaptchaResponse = new { success = false, errorCodes = new[] { "invalid-input-response" } };
            var json = JsonSerializer.Serialize(recaptchaResponse);
            SetupHttpClientResponse(json);
            using var client = new HttpClient(_mockHttpHandler.Object);
            _controller = new RecaptchaController(MockConfiguration.Object, client);
            _controller.ControllerContext = new ControllerContext { HttpContext = HttpContext };
            // Act
            var result = await _controller.Validate(request);
            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            var success = okResult.Value.GetType().GetProperty("Success")?.GetValue(okResult.Value);
            Assert.That(success, Is.False);
        }

        [Test]
        public async Task Validate_ReturnsOkTrue_WhenRecaptchaSucceeds()
        {
            // Arrange
            SetRemoteIp("127.0.0.1");
            var request = new RecaptchaRequest { Token = "valid-token" };
            var recaptchaResponse = new { success = true };
            var json = JsonSerializer.Serialize(recaptchaResponse);
            SetupHttpClientResponse(json);
            using var client = new HttpClient(_mockHttpHandler.Object);
            _controller = new RecaptchaController(MockConfiguration.Object, client);
            _controller.ControllerContext = new ControllerContext { HttpContext = HttpContext };
            // Act
            var result = await _controller.Validate(request);
            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            var success = okResult.Value.GetType().GetProperty("Success")?.GetValue(okResult.Value);
            Assert.That(success, Is.True);
        }

        [Test]
        public async Task Validate_ReturnsOkFalse_WhenRecaptchaApiError()
        {
            // Arrange
            SetRemoteIp("127.0.0.1");
            var request = new RecaptchaRequest { Token = "any-token" };
            SetupHttpClientResponse("", HttpStatusCode.InternalServerError);
            using var client = new HttpClient(_mockHttpHandler.Object);
            _controller = new RecaptchaController(MockConfiguration.Object, client);
            _controller.ControllerContext = new ControllerContext { HttpContext = HttpContext };
            // Act
            var result = await _controller.Validate(request);
            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            var success = okResult.Value.GetType().GetProperty("Success")?.GetValue(okResult.Value);
            Assert.That(success, Is.False);
        }
    }
}
