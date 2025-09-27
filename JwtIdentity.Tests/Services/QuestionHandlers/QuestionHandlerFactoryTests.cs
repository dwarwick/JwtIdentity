using JwtIdentity.Common.Helpers;
using JwtIdentity.Interfaces;
using JwtIdentity.Models;
using JwtIdentity.Services.QuestionHandlers;
using NUnit.Framework;

namespace JwtIdentity.Tests.Services.QuestionHandlers
{
    [TestFixture]
    public class QuestionHandlerFactoryTests
    {
        private QuestionHandlerFactory _factory;
        private List<IQuestionHandler> _handlers;

        [SetUp]
        public void Setup()
        {
            _handlers = new List<IQuestionHandler>
            {
                new TextQuestionHandler(),
                new TrueFalseQuestionHandler(),
                new MultipleChoiceQuestionHandler(),
                new Rating1To10QuestionHandler(),
                new SelectAllThatApplyQuestionHandler()
            };

            _factory = new QuestionHandlerFactory(_handlers);
        }

        [Test]
        public void GetHandler_WithValidQuestionType_ShouldReturnCorrectHandler()
        {
            // Act
            var textHandler = _factory.GetHandler(QuestionType.Text);
            var trueFalseHandler = _factory.GetHandler(QuestionType.TrueFalse);

            // Assert
            Assert.That(textHandler, Is.InstanceOf<TextQuestionHandler>());
            Assert.That(trueFalseHandler, Is.InstanceOf<TrueFalseQuestionHandler>());
        }

        [Test]
        public void GetHandler_WithInvalidQuestionType_ShouldThrowNotSupportedException()
        {
            // Act & Assert
            Assert.Throws<NotSupportedException>(() => _factory.GetHandler((QuestionType)999));
        }

        [Test]
        public void HasHandler_WithValidQuestionType_ShouldReturnTrue()
        {
            // Act & Assert
            Assert.That(_factory.HasHandler(QuestionType.Text), Is.True);
            Assert.That(_factory.HasHandler(QuestionType.TrueFalse), Is.True);
            Assert.That(_factory.HasHandler(QuestionType.MultipleChoice), Is.True);
            Assert.That(_factory.HasHandler(QuestionType.Rating1To10), Is.True);
            Assert.That(_factory.HasHandler(QuestionType.SelectAllThatApply), Is.True);
        }

        [Test]
        public void HasHandler_WithInvalidQuestionType_ShouldReturnFalse()
        {
            // Act & Assert
            Assert.That(_factory.HasHandler((QuestionType)999), Is.False);
        }

        [Test]
        public void GetAllHandlers_ShouldReturnAllRegisteredHandlers()
        {
            // Act
            var allHandlers = _factory.GetAllHandlers();

            // Assert
            Assert.That(allHandlers.Count(), Is.EqualTo(5));
            Assert.That(allHandlers.Any(h => h is TextQuestionHandler), Is.True);
            Assert.That(allHandlers.Any(h => h is TrueFalseQuestionHandler), Is.True);
            Assert.That(allHandlers.Any(h => h is MultipleChoiceQuestionHandler), Is.True);
            Assert.That(allHandlers.Any(h => h is Rating1To10QuestionHandler), Is.True);
            Assert.That(allHandlers.Any(h => h is SelectAllThatApplyQuestionHandler), Is.True);
        }
    }
}