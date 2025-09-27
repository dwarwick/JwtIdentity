using JwtIdentity.Common.Helpers;
using JwtIdentity.Interfaces;
using JwtIdentity.Services.AnswerHandlers;
using NUnit.Framework;

namespace JwtIdentity.Tests.Services.AnswerHandlers
{
    [TestFixture]
    public class AnswerHandlerFactoryTests
    {
        private AnswerHandlerFactory _factory;
        private List<IAnswerHandler> _handlers;

        [SetUp]
        public void Setup()
        {
            _handlers = new List<IAnswerHandler>
            {
                new TextAnswerHandler(),
                new TrueFalseAnswerHandler(),
                new SingleChoiceAnswerHandler(),
                new MultipleChoiceAnswerHandler(),
                new Rating1To10AnswerHandler(),
                new SelectAllThatApplyAnswerHandler()
            };

            _factory = new AnswerHandlerFactory(_handlers);
        }

        [Test]
        public void GetHandler_WithValidAnswerType_ShouldReturnCorrectHandler()
        {
            // Act
            var textHandler = _factory.GetHandler(AnswerType.Text);
            var trueFalseHandler = _factory.GetHandler(AnswerType.TrueFalse);

            // Assert
            Assert.That(textHandler, Is.InstanceOf<TextAnswerHandler>());
            Assert.That(trueFalseHandler, Is.InstanceOf<TrueFalseAnswerHandler>());
        }

        [Test]
        public void GetHandler_WithInvalidAnswerType_ShouldThrowNotSupportedException()
        {
            // Act & Assert
            Assert.Throws<NotSupportedException>(() => _factory.GetHandler((AnswerType)999));
        }

        [Test]
        public void HasHandler_WithValidAnswerType_ShouldReturnTrue()
        {
            // Act & Assert
            Assert.That(_factory.HasHandler(AnswerType.Text), Is.True);
            Assert.That(_factory.HasHandler(AnswerType.TrueFalse), Is.True);
            Assert.That(_factory.HasHandler(AnswerType.SingleChoice), Is.True);
            Assert.That(_factory.HasHandler(AnswerType.MultipleChoice), Is.True);
            Assert.That(_factory.HasHandler(AnswerType.Rating1To10), Is.True);
            Assert.That(_factory.HasHandler(AnswerType.SelectAllThatApply), Is.True);
        }

        [Test]
        public void HasHandler_WithInvalidAnswerType_ShouldReturnFalse()
        {
            // Act & Assert
            Assert.That(_factory.HasHandler((AnswerType)999), Is.False);
        }

        [Test]
        public void GetAllHandlers_ShouldReturnAllRegisteredHandlers()
        {
            // Act
            var allHandlers = _factory.GetAllHandlers();

            // Assert
            Assert.That(allHandlers.Count(), Is.EqualTo(6));
            Assert.That(allHandlers.Any(h => h is TextAnswerHandler), Is.True);
            Assert.That(allHandlers.Any(h => h is TrueFalseAnswerHandler), Is.True);
            Assert.That(allHandlers.Any(h => h is SingleChoiceAnswerHandler), Is.True);
            Assert.That(allHandlers.Any(h => h is MultipleChoiceAnswerHandler), Is.True);
            Assert.That(allHandlers.Any(h => h is Rating1To10AnswerHandler), Is.True);
            Assert.That(allHandlers.Any(h => h is SelectAllThatApplyAnswerHandler), Is.True);
        }
    }
}