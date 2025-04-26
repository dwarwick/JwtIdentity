using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using JwtIdentity.Controllers;
using JwtIdentity.Common.ViewModels;

namespace JwtIdentity.Tests.ControllerTests
{
    [TestFixture]
    public class AppSettingsControllerTests : TestBase<AppSettingsController>
    {
        private AppSettingsController _controller;
        private Mock<IOptions<AppSettings>> _mockOptions;
        private AppSettings _appSettings;

        [SetUp]
        public override void BaseSetUp()
        {
            // Call base setup to initialize base mocks and in-memory database
            base.BaseSetUp();

            // Create test app settings
            _appSettings = new AppSettings
            {
                ApiBaseAddress = "https://api.example.com",
                DetailedErrors = true,
                ConnectionStrings = new ConnectionStringsOptions
                {
                    DefaultConnection = "Server=test;Database=TestDb;Trusted_Connection=True;"
                },
                Jwt = new JwtOptions
                {
                    Key = "test-secret-key-for-testing-purposes-only",
                    Issuer = "test-issuer",
                    Audience = "test-audience",
                    ExpirationMinutes = 60
                },
                Logging = new LoggingOptions
                {
                    LogLevel = new LogLevelOptions
                    {
                        Default = "Information",
                        MicrosoftAspNetCore = "Warning",
                        MicrosoftEntityFrameworkCore = "Warning"
                    }
                },
                EmailSettings = new EmailSettings
                {
                    CustomerServiceEmail = "test@example.com",
                    Domain = "example.com",
                    Server = "smtp.example.com",
                    Password = "test-password"
                }
            };

            // Setup options mock
            _mockOptions = new Mock<IOptions<AppSettings>>();
            _mockOptions.Setup(m => m.Value).Returns(_appSettings);
            
            // Set up controller with mocked options
            _controller = new AppSettingsController(_mockOptions.Object, MockLogger.Object);
        }

        [TearDown]
        public override void BaseTearDown()
        {
            base.BaseTearDown();
        }

        [Test]
        public void Get_ReturnsAppSettings()
        {
            // Act
            var result = _controller.Get();

            // Assert
            Assert.That(result.Result, Is.TypeOf<OkObjectResult>(), "Should return OkObjectResult");
            
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null, "OkObjectResult should not be null");
            
            var returnedSettings = okResult.Value as AppSettings;
            Assert.That(returnedSettings, Is.Not.Null, "Returned value should be AppSettings");
            Assert.That(returnedSettings, Is.SameAs(_appSettings), "Should return the same AppSettings instance");
        }

        [Test]
        public void Get_VerifyAppSettingsProperties()
        {
            // Act
            var result = _controller.Get();
            var okResult = result.Result as OkObjectResult;
            var returnedSettings = okResult.Value as AppSettings;

            // Assert
            Assert.That(returnedSettings.ApiBaseAddress, Is.EqualTo(_appSettings.ApiBaseAddress), "ApiBaseAddress should match");
            Assert.That(returnedSettings.DetailedErrors, Is.EqualTo(_appSettings.DetailedErrors), "DetailedErrors should match");
            
            // Verify ConnectionStrings
            Assert.That(returnedSettings.ConnectionStrings.DefaultConnection, 
                Is.EqualTo(_appSettings.ConnectionStrings.DefaultConnection), 
                "ConnectionStrings.DefaultConnection should match");
            
            // Verify JWT settings
            Assert.That(returnedSettings.Jwt.Key, Is.EqualTo(_appSettings.Jwt.Key), "Jwt.Key should match");
            Assert.That(returnedSettings.Jwt.Issuer, Is.EqualTo(_appSettings.Jwt.Issuer), "Jwt.Issuer should match");
            Assert.That(returnedSettings.Jwt.Audience, Is.EqualTo(_appSettings.Jwt.Audience), "Jwt.Audience should match");
            Assert.That(returnedSettings.Jwt.ExpirationMinutes, Is.EqualTo(_appSettings.Jwt.ExpirationMinutes), "Jwt.ExpirationMinutes should match");
            
            // Verify Logging settings
            Assert.That(returnedSettings.Logging.LogLevel.Default, Is.EqualTo(_appSettings.Logging.LogLevel.Default), "Logging.LogLevel.Default should match");
            
            // Verify Email settings
            Assert.That(returnedSettings.EmailSettings.CustomerServiceEmail, 
                Is.EqualTo(_appSettings.EmailSettings.CustomerServiceEmail), 
                "EmailSettings.CustomerServiceEmail should match");
        }
    }
}