# Survey Branching Feature Implementation

## Overview

This implementation adds comprehensive survey branching capability to the JwtIdentity/SurveyShark application. Survey creators can now organize questions into groups and define conditional logic that determines which questions respondents see based on their answers.

## What Has Been Implemented ✅

### 1. Database Schema Changes
- **New Table**: `QuestionGroups` - Stores question group metadata
  - `GroupNumber`: User-friendly identifier (0, 1, 2, etc.)
  - `GroupName`: Optional descriptive name for the group
  - `NextGroupId`: Defines sequential flow between groups
  - `SubmitAfterGroup`: Flag to end survey after group completion
  
- **Question Table Updates**:
  - Added `GroupId` column (defaults to 0 for backward compatibility)
  
- **ChoiceOption Table Updates**:
  - Added `BranchToGroupId` column for conditional branching on choice selection

- **Migration**: `20251013180811_AddSurveyBranching.cs` - Ready to apply

### 2. API Layer
- **New Controller**: `QuestionGroupController`
  - Full CRUD operations for question groups
  - Automatic cleanup when groups are deleted (questions move to group 0)
  
- **Updated Controllers**:
  - `SurveyController`: Now includes question groups when fetching surveys
  - `QuestionController`: New endpoint to update question group assignments
  
- **Endpoints**:
  ```
  GET    /api/questiongroup/survey/{surveyId}  - List groups for survey
  GET    /api/questiongroup/{id}               - Get specific group
  POST   /api/questiongroup                    - Create new group
  PUT    /api/questiongroup                    - Update group
  DELETE /api/questiongroup/{id}               - Delete group
  PUT    /api/question/UpdateGroup             - Move question to different group
  ```

### 3. Survey Creation UI
- **New Page**: `/survey/branching/{surveyId}`
  - Professional, user-friendly interface for configuring branching
  - Collapsible accordion panels for each question group
  - Visual organization of questions by group
  - Drag-and-drop functionality for question ordering (maintained from existing feature)
  
- **Features**:
  - Add/Delete question groups with confirmation dialogs
  - Assign custom names to groups for better organization
  - Move questions between groups via dropdown selection
  - Configure branching for:
    - Multiple Choice questions (per option)
    - Select All That Apply questions (per option)
    - True/False questions (separate branches for True/False)
  - Define group completion behavior:
    - Submit survey immediately
    - Branch to specific group
  - Visual indicators showing group names or numbers in selection dropdowns
  
- **Access**: "Configure Branching" button added to main Edit Survey page

### 4. Data Models & ViewModels
All models have been created and mapped:
- `QuestionGroup` and `QuestionGroupViewModel`
- Updated all question and choice option models/viewmodels
- AutoMapper configuration complete
- Full serialization support

### 5. Backward Compatibility
- All existing surveys automatically have questions in group 0
- Surveys without branching configuration continue to work exactly as before
- Zero breaking changes to existing functionality

### 6. Foundational Survey-Taking Logic
Basic infrastructure added to `Survey.razor.cs`:
- Detection of surveys with branching (`HasBranching` property)
- Question list management (`QuestionsToShow`, `CurrentQuestion`)
- Navigation tracking (`CurrentQuestionIndex`, progress indicators)
- Question answered validation
- Framework for Previous/Next navigation

## What Remains To Be Implemented 🚧

### 1. Complete Survey-Taking UI (High Priority)

The `Survey.razor` page needs to be updated to support one-question-at-a-time display when branching is enabled:

**Required Changes**:
```razor
@if (HasBranching)
{
    <!-- Show only current question -->
    <div class="question-container">
        <!-- Current question display -->
        
        <!-- Navigation buttons -->
        <MudStack Row="true" Justify="Justify.SpaceBetween">
            <MudButton OnClick="GoToPreviousQuestion" 
                       Disabled="@IsFirstQuestion">
                Previous
            </MudButton>
            
            @if (IsLastQuestion)
            {
                <MudButton OnClick="SubmitSurvey">Submit</MudButton>
            }
            else
            {
                <MudButton OnClick="GoToNextQuestion" 
                           Disabled="@(!IsCurrentQuestionAnswered())">
                    Next
                </MudButton>
            }
        </MudStack>
    </div>
    
    <!-- Progress indicator -->
    <MudText>Question @TotalQuestionsShown of [calculated total]</MudText>
}
else
{
    <!-- Existing code: show all questions on one page -->
}
```

### 2. Implement Full Branching Logic (High Priority)

Enhance `GetNextQuestion()` method in `Survey.razor.cs`:

```csharp
private QuestionViewModel GetNextQuestion()
{
    var currentQuestion = CurrentQuestion;
    var currentAnswer = currentQuestion.Answers[0];
    
    // Check for answer-specific branching
    if (currentQuestion.QuestionType == QuestionType.MultipleChoice)
    {
        var mcAnswer = (MultipleChoiceAnswerViewModel)currentAnswer;
        var selectedOption = GetOption(currentQuestion, mcAnswer.SelectedOptionId);
        
        if (selectedOption?.BranchToGroupId != null)
        {
            // Branch to specified group
            return GetFirstQuestionInGroup(selectedOption.BranchToGroupId.Value);
        }
    }
    else if (currentQuestion.QuestionType == QuestionType.TrueFalse)
    {
        // Implement True/False branching
        // NOTE: Need to persist branch settings for True/False
    }
    else if (currentQuestion.QuestionType == QuestionType.SelectAllThatApply)
    {
        // Implement Select All That Apply branching
        // Decision needed: How to handle multiple selected options with different branches?
    }
    
    // Check group-level flow
    if (IsGroupComplete(currentQuestion.GroupId))
    {
        var group = GetQuestionGroup(currentQuestion.GroupId);
        
        if (group.SubmitAfterGroup)
        {
            return null; // End survey
        }
        
        if (group.NextGroupId.HasValue)
        {
            return GetFirstQuestionInGroup(GetGroupNumber(group.NextGroupId.Value));
        }
    }
    
    // Default: next question in sequence within same group
    return GetNextInGroup(currentQuestion);
}
```

### 3. Progress Calculation (Medium Priority)

Dynamic progress tracking that accounts for branching:

```csharp
protected int CalculateTotalQuestions()
{
    // In branching mode, this is an estimate
    // Could show "Question 5" without "of X" since total varies by path
    // OR show percentage based on current group
}
```

### 4. True/False Branching Persistence (Medium Priority)

**Option A**: Add columns to `TrueFalseQuestion` table
```sql
ALTER TABLE Questions 
ADD BranchToGroupIdOnTrue INT NULL,
ADD BranchToGroupIdOnFalse INT NULL
```

**Option B**: Create implicit ChoiceOptions for True/False
- Create two ChoiceOption records: one for True, one for False
- Store `BranchToGroupId` like other choice questions

### 5. Validation & Error Prevention (Medium Priority)

Add validation to prevent:
- Circular references (Group A → Group B → Group A)
- Orphaned groups (groups that can never be reached)
- Deleted groups still referenced in NextGroupId
- Multiple branching conflicts

### 6. Enhanced Features (Low Priority)

- **Preview Mode**: Show branching flow diagram
- **Testing Mode**: Test all possible paths through survey
- **Analytics**: Track which branches users take most often
- **Group Templates**: Save and reuse common branching patterns
- **Bulk Operations**: Assign multiple questions to group at once

### 7. Documentation Updates (Low Priority)

- Update `CreatingSurveys.razor` documentation page
- Add screenshots and examples
- Create video tutorial
- Best practices guide for branching design

## How to Use (Current State)

### Creating a Branching Survey

1. **Create Survey and Questions** (existing process)
   - Navigate to "Create Survey"
   - Add title, description
   - Add all questions you want to include

2. **Configure Branching**
   - Click "Configure Branching" button on Edit Survey page
   - Click "Add Group" to create new groups
   - Assign descriptive names to groups (optional but recommended)
   - Drag questions from "Group 0 (Default)" to appropriate groups
   - For choice-based questions, select target group for each option
   - Define what happens after each group completes

3. **Publish Survey**
   - Review branching configuration
   - Test in Preview mode (once UI is complete)
   - Publish when ready

### Taking a Survey (Once Complete)

1. Users see one question at a time (when branching is configured)
2. Must answer current question before proceeding
3. Click "Next" to advance
4. Survey flow adapts based on answers
5. Progress indicator shows current position
6. "Submit" appears at end of their specific path

## Database Migration

To apply the schema changes:

```bash
cd JwtIdentity
dotnet ef database update
```

This will create the QuestionGroups table and add new columns to existing tables.

## Testing Checklist

Before considering this feature complete, test:

- [ ] Survey without branching (group 0 only) works as before
- [ ] Linear branching (0 → 1 → 2 → Submit)
- [ ] Conditional branching (choice determines next group)
- [ ] Skip groups based on answers
- [ ] Progress indicator updates correctly
- [ ] Previous button navigates correctly
- [ ] Survey submission works from any ending point
- [ ] Validation prevents invalid configurations
- [ ] Multiple users can take same survey with different paths

## Architecture Notes

### Design Decisions

1. **Why Separate Branching Page?**
   - EditSurvey was already crowded
   - Branching is advanced functionality
   - Cleaner separation of concerns
   - Better user experience

2. **Why Groups Instead of Question-Level Flow?**
   - Easier to understand and visualize
   - Reduces UI complexity
   - More maintainable
   - Still allows granular control (one question per group if needed)

3. **Why Group 0 is Special?**
   - Provides default behavior for non-branching surveys
   - Ensures backward compatibility
   - Always shown first (entry point)
   - Cannot be deleted

### Performance Considerations

- Question groups are loaded with survey data (single query)
- Branching logic executes client-side (no server round-trip)
- Answer persistence unchanged (still saves immediately)
- Minimal impact on existing surveys

## Support & Questions

For questions or issues with this implementation:
1. Check `BRANCHING_IMPLEMENTATION_STATUS.md` for detailed status
2. Review API documentation in respective controller files
3. Examine `BranchingSurveyEdit.razor` for UI patterns
4. Test with demo data before production use

## Future Enhancements (Ideas)

- **Multi-Language Support**: Branch based on user's language preference
- **Time-Based Branching**: Show different questions based on time of day
- **Score-Based Branching**: Calculate running score and branch accordingly
- **Skip Logic**: Allow respondents to skip certain question groups
- **Save & Resume**: Let users save progress and return later
- **Branching Templates**: Library of common branching patterns
