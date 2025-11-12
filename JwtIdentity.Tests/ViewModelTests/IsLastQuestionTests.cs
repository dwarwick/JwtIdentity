using System.Text.Json;
using JwtIdentity.Common.Helpers;
using JwtIdentity.Common.ViewModels;
using NUnit.Framework;

namespace JwtIdentity.Tests.ViewModelTests;

[TestFixture]
public class IsLastQuestionTests
{
    private JsonSerializerOptions _options;

    [SetUp]
    public void Setup()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        _options.Converters.Add(new QuestionViewModelConverter());
    }

    [Test]
    public void TextQuestion_IsLastQuestion_ShouldDefaultToFalse()
    {
        // Arrange & Act
        var question = new TextQuestionViewModel
        {
            Id = 1,
            Text = "Sample text question"
        };

        // Assert
        Assert.That(question.IsLastQuestion, Is.False);
    }

    [Test]
    public void MultipleChoiceQuestion_IsLastQuestion_ShouldDefaultToFalse()
    {
        // Arrange & Act
        var question = new MultipleChoiceQuestionViewModel
        {
            Id = 1,
            Text = "Sample multiple choice question"
        };

        // Assert
        Assert.That(question.IsLastQuestion, Is.False);
    }

    [Test]
    public void TrueFalseQuestion_IsLastQuestion_ShouldDefaultToFalse()
    {
        // Arrange & Act
        var question = new TrueFalseQuestionViewModel
        {
            Id = 1,
            Text = "Sample true/false question"
        };

        // Assert
        Assert.That(question.IsLastQuestion, Is.False);
    }

    [Test]
    public void Rating1To10Question_IsLastQuestion_ShouldDefaultToFalse()
    {
        // Arrange & Act
        var question = new Rating1To10QuestionViewModel
        {
            Id = 1,
            Text = "Sample rating question"
        };

        // Assert
        Assert.That(question.IsLastQuestion, Is.False);
    }

    [Test]
    public void SelectAllThatApplyQuestion_IsLastQuestion_ShouldDefaultToFalse()
    {
        // Arrange & Act
        var question = new SelectAllThatApplyQuestionViewModel
        {
            Id = 1,
            Text = "Sample select all that apply question"
        };

        // Assert
        Assert.That(question.IsLastQuestion, Is.False);
    }

    [Test]
    public void TextQuestion_IsLastQuestion_CanBeSetToTrue()
    {
        // Arrange
        var question = new TextQuestionViewModel
        {
            Id = 1,
            Text = "Is there anything else you would like to add?",
            GroupId = 0,
            IsLastQuestion = true
        };

        // Assert
        Assert.That(question.IsLastQuestion, Is.True);
        Assert.That(question.GroupId, Is.EqualTo(0));
    }

    [Test]
    public void MultipleChoiceQuestion_IsLastQuestion_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var question = new MultipleChoiceQuestionViewModel
        {
            Id = 1,
            Text = "Any final comments?",
            QuestionType = QuestionType.MultipleChoice,
            GroupId = 0,
            IsLastQuestion = true,
            Options = new List<ChoiceOptionViewModel>
            {
                new ChoiceOptionViewModel { Id = 1, OptionText = "Yes", Order = 1 },
                new ChoiceOptionViewModel { Id = 2, OptionText = "No", Order = 2 }
            }
        };

        // Act
        var json = JsonSerializer.Serialize<QuestionViewModel>(question, _options);
        var deserialized = JsonSerializer.Deserialize<QuestionViewModel>(json, _options);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized, Is.InstanceOf<MultipleChoiceQuestionViewModel>());
        Assert.That(deserialized.IsLastQuestion, Is.True);
        Assert.That(deserialized.GroupId, Is.EqualTo(0));
    }

    [Test]
    public void Question_IsLastQuestion_DeserializesFromJsonCorrectly()
    {
        // Arrange - Question with IsLastQuestion set to true
        var json = """
        {
            "id": 1,
            "text": "Is there anything else you would like us to know?",
            "questionType": 1,
            "groupId": 0,
            "isLastQuestion": true
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<QuestionViewModel>(json, _options);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<TextQuestionViewModel>());
        Assert.That(result.IsLastQuestion, Is.True);
        Assert.That(result.GroupId, Is.EqualTo(0));
    }

    [Test]
    public void Question_WithoutIsLastQuestion_DeserializesToFalse()
    {
        // Arrange - Question without IsLastQuestion property
        var json = """
        {
            "id": 1,
            "text": "Regular question",
            "questionType": 1,
            "groupId": 0
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<QuestionViewModel>(json, _options);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsLastQuestion, Is.False);
    }

    [Test]
    public void Question_Group0_CanBeMarkedAsLastQuestion()
    {
        // Arrange & Act
        var question = new TextQuestionViewModel
        {
            Id = 1,
            Text = "Any other feedback?",
            GroupId = 0,
            IsLastQuestion = true
        };

        // Assert
        Assert.That(question.GroupId, Is.EqualTo(0));
        Assert.That(question.IsLastQuestion, Is.True);
    }

    [Test]
    public void Question_NonGroup0_CanStillHaveIsLastQuestionProperty()
    {
        // This test verifies the property exists but note that business logic
        // should prevent this from being saved (validation in UI/API)
        // Arrange & Act
        var question = new TextQuestionViewModel
        {
            Id = 1,
            Text = "Question in group 1",
            GroupId = 1,
            IsLastQuestion = true
        };

        // Assert - property can be set but should be validated by business logic
        Assert.That(question.GroupId, Is.EqualTo(1));
        Assert.That(question.IsLastQuestion, Is.True);
    }
}
