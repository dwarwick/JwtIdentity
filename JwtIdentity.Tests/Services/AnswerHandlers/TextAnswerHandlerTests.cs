using JwtIdentity.Common.Helpers;
using JwtIdentity.Models;
using JwtIdentity.Services.AnswerHandlers;
using NUnit.Framework;

namespace JwtIdentity.Tests.Services.AnswerHandlers
{
    [TestFixture]
    public class TextAnswerHandlerTests
    {
        private TextAnswerHandler _handler;

        [SetUp]
        public void Setup()
        {
            _handler = new TextAnswerHandler();
        }

        [Test]
        public void SupportedType_ShouldReturnText()
        {
            // Act
            var result = _handler.SupportedType;

            // Assert
            Assert.That(result, Is.EqualTo(AnswerType.Text));
        }

        [Test]
        public void HasChanged_WhenTextIsDifferent_ShouldReturnTrue()
        {
            // Arrange
            var newAnswer = new TextAnswer { Text = "New text", Complete = true };
            var existingAnswer = new TextAnswer { Text = "Old text", Complete = true };

            // Act
            var result = _handler.HasChanged(newAnswer, existingAnswer);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void HasChanged_WhenCompleteStatusIsDifferent_ShouldReturnTrue()
        {
            // Arrange
            var newAnswer = new TextAnswer { Text = "Same text", Complete = true };
            var existingAnswer = new TextAnswer { Text = "Same text", Complete = false };

            // Act
            var result = _handler.HasChanged(newAnswer, existingAnswer);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void HasChanged_WhenNothingChanged_ShouldReturnFalse()
        {
            // Arrange
            var newAnswer = new TextAnswer { Text = "Same text", Complete = true };
            var existingAnswer = new TextAnswer { Text = "Same text", Complete = true };

            // Act
            var result = _handler.HasChanged(newAnswer, existingAnswer);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValid_WhenCompleteWithText_ShouldReturnTrue()
        {
            // Arrange
            var answer = new TextAnswer { AnswerType = AnswerType.Text, Text = "Some text", Complete = true };

            // Act
            var result = _handler.IsValid(answer);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsValid_WhenCompleteWithoutText_ShouldReturnFalse()
        {
            // Arrange
            var answer = new TextAnswer { AnswerType = AnswerType.Text, Text = "", Complete = true };

            // Act
            var result = _handler.IsValid(answer);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValid_WhenIncompleteWithoutText_ShouldReturnTrue()
        {
            // Arrange
            var answer = new TextAnswer { AnswerType = AnswerType.Text, Text = "", Complete = false };

            // Act
            var result = _handler.IsValid(answer);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void GetDisplayValue_WithText_ShouldReturnText()
        {
            // Arrange
            var answer = new TextAnswer { Text = "Sample answer" };

            // Act
            var result = _handler.GetDisplayValue(answer);

            // Assert
            Assert.That(result, Is.EqualTo("Sample answer"));
        }

        [Test]
        public void GetDisplayValue_WithoutText_ShouldReturnPlaceholder()
        {
            // Arrange
            var answer = new TextAnswer { Text = "" };

            // Act
            var result = _handler.GetDisplayValue(answer);

            // Assert
            Assert.That(result, Is.EqualTo("[No text provided]"));
        }
    }
}