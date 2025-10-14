# Survey Branching Implementation Status

## Completed Features ✅

### Database Layer
- ✅ Added `QuestionGroup` model with properties:
  - Id, SurveyId, GroupNumber, GroupName, NextGroupId, SubmitAfterGroup
- ✅ Added `GroupId` property to `Question` model (default 0)
- ✅ Added `BranchToGroupId` property to `ChoiceOption` model for branching logic
- ✅ Created and applied migration `20251013180811_AddSurveyBranching`

### ViewModels
- ✅ Created `QuestionGroupViewModel` in Common project
- ✅ Updated `ChoiceOptionViewModel` with `BranchToGroupId`
- ✅ Updated `QuestionViewModel` with `GroupId`
- ✅ Updated `SurveyViewModel` with `QuestionGroups` list
- ✅ Updated AutoMapper configuration for all new mappings

### API Layer
- ✅ Created `QuestionGroupController` with full CRUD operations:
  - GET api/QuestionGroup/Survey/{surveyId} - Get groups for a survey
  - GET api/QuestionGroup/{id} - Get specific group
  - POST api/QuestionGroup - Create new group
  - PUT api/QuestionGroup - Update existing group  
  - DELETE api/QuestionGroup/{id} - Delete group (moves questions to group 0)
- ✅ Added UpdateGroup endpoint to `QuestionController`
- ✅ Updated `SurveyController` to include QuestionGroups when fetching surveys
- ✅ Added `QuestionGroup` constant to `ApiEndpoints`

### UI - Survey Creation/Management
- ✅ Created `BranchingSurveyEdit.razor` page with comprehensive UI:
  - Collapsible accordion panels for each question group
  - Add/delete question groups
  - Assign custom names to groups
  - Move questions between groups
  - Configure branching for Multiple Choice, Select All That Apply, and True/False questions
  - Set group completion behavior (Submit or Branch to another group)
  - Visual indicators showing group names or numbers
- ✅ Added "Configure Branching" button to main EditSurvey page
- ✅ Full drag-and-drop support maintained for question ordering
- ✅ Validation to ensure deleted groups move questions back to group 0

## Remaining Work 🚧

### UI - Survey Taking Experience
The following enhancements are needed in `Survey.razor` and `Survey.razor.cs`:

#### 1. One-Question-at-a-Time Display
When a survey has multiple groups (more than just group 0), the interface should:
- Display only the current question instead of all questions
- Show Previous/Next buttons for navigation
- Show Submit button only at the end of the survey
- Disable Next button until current question is answered

#### 2. Progress Indicator
- Add a progress bar or counter showing "Question X of Y"
- Update dynamically as user progresses through branching paths
- Account for conditional questions that may or may not be shown

#### 3. Branching Logic Implementation
In `Survey.razor.cs`, implement:

```csharp
// Track which questions have been shown in current session
private List<int> _shownQuestions = new();
private int _currentQuestionIndex = 0;

// Determine next question based on answer and branching rules
private int? GetNextQuestion(QuestionViewModel currentQuestion, AnswerViewModel answer)
{
    // For Multiple Choice - check if selected option has branching
    if (answer is MultipleChoiceAnswerViewModel mcAnswer)
    {
        var option = GetSelectedOption(currentQuestion, mcAnswer.SelectedOptionId);
        if (option?.BranchToGroupId != null)
        {
            return GetFirstQuestionInGroup(option.BranchToGroupId.Value);
        }
    }
    
    // For Select All That Apply - determine branching based on checked options
    // (Implementation note: may need to decide on priority if multiple options selected)
    
    // For True/False - check branching configuration
    // (Note: Need to extend ChoiceOption or add separate table for True/False branching)
    
    // Check if current group is complete
    var currentGroup = GetQuestionGroup(currentQuestion.GroupId);
    if (IsGroupComplete(currentGroup))
    {
        if (currentGroup.SubmitAfterGroup)
        {
            return null; // End survey
        }
        else if (currentGroup.NextGroupId.HasValue)
        {
            return GetFirstQuestionInGroup(GetGroupNumber(currentGroup.NextGroupId.Value));
        }
    }
    
    // Default: return next question in sequence
    return GetNextQuestionInSequence(currentQuestion);
}
```

#### 4. Captcha Handling
- Ensure captcha is shown at the START of survey (already implemented)
- Do not show captcha again during branching navigation

#### 5. Answer Persistence
- Save answers immediately when user moves to next question
- Handle case where user goes back and changes an answer
- Recalculate branching path if answers change

### Data Model Considerations

#### True/False Branching
Currently, True/False questions don't have "options" like Multiple Choice. Consider:
- **Option A**: Extend True/False to have two implicit ChoiceOptions (True, False) 
- **Option B**: Add separate branching fields to TrueFalseQuestion model
- **Current Implementation**: Uses Dictionary in UI (TrueBranch/FalseBranch) but not persisted

**Recommendation**: Add BranchToGroupIdOnTrue and BranchToGroupIdOnFalse to TrueFalseQuestion model.

### Testing Requirements

1. **Single Group Survey** - Ensure existing surveys (all in group 0) continue to work exactly as before
2. **Linear Branching** - Group 0 → Group 1 → Group 2 → Submit
3. **Conditional Branching** - Multiple choice option determines which group to show next
4. **Skip Groups** - Group 0 → Group 3 (skip 1 and 2 based on answer)
5. **Return to Earlier Group** - Group 0 → Group 1 → back to Group 0 questions
6. **Dead End Detection** - Validate that all branches eventually lead to submission
7. **Select All That Apply** - Handle branching when multiple options selected

### Documentation Updates

- Update CreatingSurveys.razor docs page with branching instructions
- Add screenshots of branching UI
- Create user guide for setting up conditional logic
- Document best practices for survey flow design

## Known Limitations

1. **Select All That Apply Branching**: When multiple options are selected, need to decide on branch priority
   - Possible solutions: Take first selected option's branch, OR show union of all branched groups
   
2. **True/False Branching Persistence**: Currently only configured in UI, needs backend persistence

3. **Circular References**: No validation yet to prevent Group A → Group B → Group A loops

4. **Group Deletion**: When deleting a group that is referenced as NextGroupId by another group, 
   need to update those references

## Architecture Decisions

### Why Separate Branching Page?
- EditSurvey.razor was already feeling crowded (per requirements)
- Branching is advanced functionality - not needed for simple surveys
- Separates concerns: question content vs. flow logic
- Easier to navigate and understand for users

### Why Group Numbers vs. IDs?
- GroupNumber (0, 1, 2...) is more user-friendly than database IDs
- Allows easy reference in UI ("Go to Group 2")  
- Group 0 is special "default" group that always exists
- Actual database uses Ids for relationships, GroupNumber for display

### Why Not Question-Level Next?
- Group-based branching is clearer for users to understand and configure
- Reduces complexity of the branching UI
- Still allows fine-grained control by putting each question in its own group if needed
- More maintainable codebase

## Next Steps

Priority order for remaining implementation:

1. **High Priority**: Implement one-question-at-a-time display when groups exist
2. **High Priority**: Implement basic branching logic in Survey.razor.cs  
3. **Medium Priority**: Add progress indicator
4. **Medium Priority**: Handle True/False branching persistence
5. **Low Priority**: Add circular reference validation
6. **Low Priority**: Update documentation

## Migration Path for Existing Surveys

All existing surveys will automatically have their questions in Group 0 (default value).
They will continue to display all questions on one page unless groups are explicitly added.
This ensures backward compatibility.
