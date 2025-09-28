using System.Text.Json;
using JwtIdentity.Common.Helpers;
using JwtIdentity.Common.ViewModels;
using NUnit.Framework;

namespace JwtIdentity.Tests.ViewModelTests;

[TestFixture]
public class QuestionViewModelConverterTests
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
    public void Read_SelectAllThatApplyQuestionType_ShouldDeserializeCorrectly()
    {
        // Arrange - SelectAllThatApply should be question type 5
        var json = """
        {
            "id": 1,
            "text": "Select all that apply:",
            "questionType": 5,
            "options": [
                {"id": 1, "optionText": "Option 1", "order": 1},
                {"id": 2, "optionText": "Option 2", "order": 2}
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<QuestionViewModel>(json, _options);

        // Assert
        Assert.That(result, Is.InstanceOf<SelectAllThatApplyQuestionViewModel>());
        Assert.That(result.QuestionType, Is.EqualTo(QuestionType.SelectAllThatApply));
        
        var selectAllQuestion = result as SelectAllThatApplyQuestionViewModel;
        Assert.That(selectAllQuestion?.Options, Is.Not.Null);
        Assert.That(selectAllQuestion?.Options.Count, Is.EqualTo(2));
    }

    [Test]
    public void Read_MultipleChoiceQuestionType_ShouldDeserializeCorrectly()
    {
        // Arrange - MultipleChoice should be question type 3
        var json = """
        {
            "id": 1,
            "text": "Choose one:",
            "questionType": 3,
            "options": [
                {"id": 1, "optionText": "Option 1", "order": 1},
                {"id": 2, "optionText": "Option 2", "order": 2}
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<QuestionViewModel>(json, _options);

        // Assert
        Assert.That(result, Is.InstanceOf<MultipleChoiceQuestionViewModel>());
        Assert.That(result.QuestionType, Is.EqualTo(QuestionType.MultipleChoice));
        
        var multipleChoiceQuestion = result as MultipleChoiceQuestionViewModel;
        Assert.That(multipleChoiceQuestion?.Options, Is.Not.Null);
        Assert.That(multipleChoiceQuestion?.Options.Count, Is.EqualTo(2));
    }

    [Test]
    public void Read_Rating1To10QuestionType_ShouldDeserializeCorrectly()
    {
        // Arrange - Rating1To10 should be question type 4
        var json = """
        {
            "id": 1,
            "text": "Rate from 1 to 10:",
            "questionType": 4
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<QuestionViewModel>(json, _options);

        // Assert
        Assert.That(result, Is.InstanceOf<Rating1To10QuestionViewModel>());
        Assert.That(result.QuestionType, Is.EqualTo(QuestionType.Rating1To10));
    }

    [Test]
    public void Read_TextQuestionType_ShouldDeserializeCorrectly()
    {
        // Arrange - Text should be question type 1
        var json = """
        {
            "id": 1,
            "text": "Please describe:",
            "questionType": 1
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<QuestionViewModel>(json, _options);

        // Assert
        Assert.That(result, Is.InstanceOf<TextQuestionViewModel>());
        Assert.That(result.QuestionType, Is.EqualTo(QuestionType.Text));
    }

    [Test]
    public void Read_TrueFalseQuestionType_ShouldDeserializeCorrectly()
    {
        // Arrange - TrueFalse should be question type 2
        var json = """
        {
            "id": 1,
            "text": "This statement is true:",
            "questionType": 2
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<QuestionViewModel>(json, _options);

        // Assert
        Assert.That(result, Is.InstanceOf<TrueFalseQuestionViewModel>());
        Assert.That(result.QuestionType, Is.EqualTo(QuestionType.TrueFalse));
    }

    [Test]
    public void Read_InvalidQuestionType_ShouldThrowException()
    {
        // Arrange - Question type 6 should not be valid for QuestionType enum
        var json = """
        {
            "id": 1,
            "text": "Invalid question type:",
            "questionType": 6
        }
        """;

        // Act & Assert
        var ex = Assert.Throws<NotSupportedException>(() => 
            JsonSerializer.Deserialize<QuestionViewModel>(json, _options));
        
        Assert.That(ex?.Message, Does.Contain("Unsupported QuestionType: 6"));
    }
}