# Adding New Question and Answer Types

The JwtIdentity application now uses a **Handler Pattern** for managing different question and answer types. This approach significantly simplifies adding new types while maintaining clean, testable code.

## How It Works

### Architecture Overview
- **IAnswerHandler**: Interface defining common operations for all answer types
- **BaseAnswerHandler**: Abstract base class with shared functionality  
- **Concrete Handlers**: Type-specific implementations (TextAnswerHandler, TrueFalseAnswerHandler, etc.)
- **IAnswerHandlerFactory**: Factory for resolving handlers by answer type
- **Dependency Injection**: Automatic registration of all handlers

### Key Benefits
- ✅ **No more large switch statements** in controllers
- ✅ **Single place to update** when adding new types
- ✅ **Type-specific validation** and logic encapsulation
- ✅ **Independent unit testing** for each type
- ✅ **Compile-time safety** with dependency injection

## Adding a New Question/Answer Type

### Step 1: Add to Enums
Update the enums to include your new type:

```csharp
// JwtIdentity.Common/Helpers/AnswerType.cs
public enum AnswerType
{
    Text = 1,
    TrueFalse = 2,
    SingleChoice = 3,
    MultipleChoice = 4,
    Rating1To10 = 5,
    SelectAllThatApply = 6,
    DatePicker = 7,          // NEW TYPE
    NumberRange = 8          // ANOTHER NEW TYPE
}

// JwtIdentity.Common/Helpers/QuestionType.cs  
public enum QuestionType
{
    Text = 1,
    TrueFalse = 2,
    MultipleChoice = 3,
    Rating1To10 = 4,
    SelectAllThatApply = 5,
    DatePicker = 6,          // NEW TYPE
    NumberRange = 7          // ANOTHER NEW TYPE
}
```

### Step 2: Create Model Classes
Add your new question and answer model classes:

```csharp
// In JwtIdentity/Models/Question.cs
public class DatePickerQuestion : Question
{
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    public bool AllowTimeSelection { get; set; }
}

// In JwtIdentity/Models/Answer.cs  
public class DatePickerAnswer : Answer
{
    public DateTime? SelectedDate { get; set; }
}
```

### Step 3: Create ViewModels
Add corresponding ViewModels in the Common project:

```csharp
// In JwtIdentity.Common/ViewModels/QuestionViewModel.cs
public class DatePickerQuestionViewModel : QuestionViewModel
{
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    public bool AllowTimeSelection { get; set; }
}

// In JwtIdentity.Common/ViewModels/AnswerViewModel.cs
public class DatePickerAnswerViewModel : AnswerViewModel
{
    public DateTime? SelectedDate { get; set; }
}
```

### Step 4: Create Answer Handler
This is the **key step** - create a handler for your new type:

```csharp
// JwtIdentity/Services/AnswerHandlers/DatePickerAnswerHandler.cs
using JwtIdentity.Common.Helpers;
using JwtIdentity.Models;

namespace JwtIdentity.Services.AnswerHandlers
{
    public class DatePickerAnswerHandler : BaseAnswerHandler
    {
        public override AnswerType SupportedType => AnswerType.DatePicker;

        public override bool HasChanged(Answer newAnswer, Answer existingAnswer)
        {
            var newDate = (DatePickerAnswer)newAnswer;
            var existingDate = (DatePickerAnswer)existingAnswer;

            return newDate.SelectedDate != existingDate.SelectedDate || 
                   BasePropertiesChanged(newAnswer, existingAnswer);
        }

        public override bool IsValid(Answer answer)
        {
            if (!base.IsValid(answer) || answer is not DatePickerAnswer dateAnswer)
                return false;

            // Date answers should have a date if marked as complete
            return !dateAnswer.Complete || dateAnswer.SelectedDate.HasValue;
        }

        public override string GetDisplayValue(Answer answer)
        {
            if (answer is not DatePickerAnswer dateAnswer)
                return string.Empty;

            return dateAnswer.SelectedDate?.ToString("yyyy-MM-dd") ?? "[No date selected]";
        }
    }
}
```

### Step 5: Register in Dependency Injection
Add your handler to the DI container:

```csharp
// In JwtIdentity/Program.cs
// Add this line with the other handler registrations:
builder.Services.AddScoped<IAnswerHandler, JwtIdentity.Services.AnswerHandlers.DatePickerAnswerHandler>();
```

### Step 6: Update AutoMapper
Add mapping configurations:

```csharp
// In JwtIdentity/Configurations/MapperConfig.cs
public MapperConfig()
{
    // Add these mappings with the existing ones:
    _ = CreateMap<DatePickerQuestion, DatePickerQuestionViewModel>();
    _ = CreateMap<DatePickerQuestionViewModel, DatePickerQuestion>()
        .ForMember(x => x.CreatedBy, options => options.Ignore());

    _ = CreateMap<DatePickerAnswer, DatePickerAnswerViewModel>();
    _ = CreateMap<DatePickerAnswerViewModel, DatePickerAnswer>()
        .ForMember(x => x.CreatedBy, options => options.Ignore());
}
```

### Step 7: Update JSON Converter
Add your new type to the AnswerViewModelConverter:

```csharp
// In JwtIdentity.Common/ViewModels/AnswerViewModel.cs
public override AnswerViewModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
{
    // Add your case to the existing switch statement:
    return answerType switch
    {
        AnswerType.Text => JsonSerializer.Deserialize<TextAnswerViewModel>(doc.RootElement.GetRawText(), options),
        AnswerType.TrueFalse => JsonSerializer.Deserialize<TrueFalseAnswerViewModel>(doc.RootElement.GetRawText(), options),
        // ... existing cases ...
        AnswerType.DatePicker => JsonSerializer.Deserialize<DatePickerAnswerViewModel>(doc.RootElement.GetRawText(), options), // NEW
        _ => throw new NotSupportedException($"Unsupported AnswerType: {answerType}")
    };
}
```

### Step 8: Database Migration
Create and run a migration to update the database schema:

```bash
dotnet ef migrations add AddDatePickerQuestionType
dotnet ef database update
```

### Step 9: Create Unit Tests
Test your new handler:

```csharp
// JwtIdentity.Tests/Services/AnswerHandlers/DatePickerAnswerHandlerTests.cs
[TestFixture]
public class DatePickerAnswerHandlerTests
{
    private DatePickerAnswerHandler _handler;

    [SetUp]
    public void Setup()
    {
        _handler = new DatePickerAnswerHandler();
    }

    [Test]
    public void SupportedType_ShouldReturnDatePicker()
    {
        Assert.That(_handler.SupportedType, Is.EqualTo(AnswerType.DatePicker));
    }

    [Test]
    public void HasChanged_WhenDateIsDifferent_ShouldReturnTrue()
    {
        var newAnswer = new DatePickerAnswer { SelectedDate = DateTime.Today, Complete = true };
        var existingAnswer = new DatePickerAnswer { SelectedDate = DateTime.Today.AddDays(-1), Complete = true };

        var result = _handler.HasChanged(newAnswer, existingAnswer);

        Assert.That(result, Is.True);
    }

    // Add more tests as needed...
}
```

## That's It!

With the new Handler Pattern, adding a new question/answer type requires:

1. **Enum entries** (2 lines)
2. **Model classes** (1 question + 1 answer class)  
3. **ViewModels** (2 classes)
4. **Handler implementation** (1 class with 4 methods)
5. **DI registration** (1 line)
6. **AutoMapper config** (4 lines)
7. **JSON converter** (1 line)
8. **Migration** (1 command)
9. **Unit tests** (1 test class)

The **AnswerController requires no changes** - it automatically uses the new handler through dependency injection!

## Before vs After

### Before (Old Switch-Based Approach)
- ❌ 50+ line switch statement in controller
- ❌ Repeated type checking and casting
- ❌ Controller knows about all answer types
- ❌ Hard to test individual type logic  
- ❌ Easy to forget updating all places

### After (Handler Pattern)
- ✅ Clean, single-responsibility handlers
- ✅ Controller is type-agnostic
- ✅ Easy to unit test each type
- ✅ Compile-time safety with DI
- ✅ Clear separation of concerns

This approach scales much better as the application grows and makes the codebase significantly more maintainable.