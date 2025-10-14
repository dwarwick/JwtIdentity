using Microsoft.AspNetCore.Authorization;
using JwtIdentity.Interfaces;

namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionGroupController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<QuestionGroupController> _logger;
        private readonly IApiAuthService _authService;

        public QuestionGroupController(ApplicationDbContext context, IMapper mapper, ILogger<QuestionGroupController> logger, IApiAuthService authService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _authService = authService;
        }

        // GET: api/QuestionGroup/Survey/{surveyId}
        [HttpGet("Survey/{surveyId}")]
        public async Task<ActionResult<IEnumerable<QuestionGroupViewModel>>> GetQuestionGroupsForSurvey(int surveyId)
        {
            try
            {
                _logger.LogInformation("Fetching question groups for survey {SurveyId}", surveyId);

                var groups = await _context.QuestionGroups
                    .Where(qg => qg.SurveyId == surveyId)
                    .OrderBy(qg => qg.GroupNumber)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} question groups for survey {SurveyId}", groups.Count, surveyId);
                return Ok(_mapper.Map<List<QuestionGroupViewModel>>(groups));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving question groups for survey {SurveyId}", surveyId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "An error occurred while retrieving question groups");
            }
        }

        // POST: api/QuestionGroup
        [HttpPost]
        [Authorize(Policy = Permissions.CreateSurvey)]
        public async Task<ActionResult<QuestionGroupViewModel>> CreateQuestionGroup(QuestionGroupViewModel groupViewModel)
        {
            try
            {
                if (groupViewModel == null)
                {
                    _logger.LogWarning("Bad request: Null question group data");
                    return BadRequest("Question group data is required");
                }

                _logger.LogInformation("Creating new question group for survey {SurveyId}", groupViewModel.SurveyId);

                var group = _mapper.Map<QuestionGroup>(groupViewModel);
                _context.QuestionGroups.Add(group);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created question group {GroupId} for survey {SurveyId}", 
                    group.Id, group.SurveyId);

                return CreatedAtAction(nameof(GetQuestionGroup), new { id = group.Id }, 
                    _mapper.Map<QuestionGroupViewModel>(group));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating question group");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "An error occurred while creating the question group");
            }
        }

        // GET: api/QuestionGroup/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<QuestionGroupViewModel>> GetQuestionGroup(int id)
        {
            try
            {
                _logger.LogInformation("Fetching question group {GroupId}", id);

                var group = await _context.QuestionGroups.FindAsync(id);

                if (group == null)
                {
                    _logger.LogWarning("Question group {GroupId} not found", id);
                    return NotFound($"Question group with ID {id} not found");
                }

                return Ok(_mapper.Map<QuestionGroupViewModel>(group));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving question group {GroupId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "An error occurred while retrieving the question group");
            }
        }

        // PUT: api/QuestionGroup
        [HttpPut]
        [Authorize(Policy = Permissions.CreateSurvey)]
        public async Task<IActionResult> UpdateQuestionGroup(QuestionGroupViewModel groupViewModel)
        {
            try
            {
                if (groupViewModel == null || groupViewModel.Id == 0)
                {
                    _logger.LogWarning("Bad request: Invalid question group data");
                    return BadRequest("Invalid question group data");
                }

                _logger.LogInformation("Updating question group {GroupId}", groupViewModel.Id);

                var group = await _context.QuestionGroups.FindAsync(groupViewModel.Id);
                if (group == null)
                {
                    _logger.LogWarning("Question group {GroupId} not found for update", groupViewModel.Id);
                    return NotFound($"Question group with ID {groupViewModel.Id} not found");
                }

                // Update properties
                group.GroupName = groupViewModel.GroupName;
                group.NextGroupId = groupViewModel.NextGroupId;
                group.SubmitAfterGroup = groupViewModel.SubmitAfterGroup;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated question group {GroupId}", group.Id);
                return Ok(_mapper.Map<QuestionGroupViewModel>(group));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question group {GroupId}", groupViewModel?.Id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "An error occurred while updating the question group");
            }
        }

        // DELETE: api/QuestionGroup/{id}
        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.CreateSurvey)]
        public async Task<IActionResult> DeleteQuestionGroup(int id)
        {
            try
            {
                _logger.LogInformation("Deleting question group {GroupId}", id);

                var group = await _context.QuestionGroups.FindAsync(id);
                if (group == null)
                {
                    _logger.LogWarning("Question group {GroupId} not found for deletion", id);
                    return NotFound($"Question group with ID {id} not found");
                }

                // Move all questions in this group back to group 0
                var questions = await _context.Questions
                    .Where(q => q.SurveyId == group.SurveyId && q.GroupId == group.GroupNumber)
                    .ToListAsync();

                foreach (var question in questions)
                {
                    question.GroupId = 0;
                }

                _context.QuestionGroups.Remove(group);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted question group {GroupId} and moved {QuestionCount} questions to group 0", 
                    id, questions.Count);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question group {GroupId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "An error occurred while deleting the question group");
            }
        }
    }
}
