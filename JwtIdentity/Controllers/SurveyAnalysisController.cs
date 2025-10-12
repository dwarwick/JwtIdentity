using Microsoft.AspNetCore.Authorization;

namespace JwtIdentity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SurveyAnalysisController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IOpenAi _openAiService;
        private readonly ILogger<SurveyAnalysisController> _logger;
        private readonly IApiAuthService _authService;

        public SurveyAnalysisController(
            ApplicationDbContext context,
            IMapper mapper,
            IOpenAi openAiService,
            ILogger<SurveyAnalysisController> logger,
            IApiAuthService authService)
        {
            _context = context;
            _mapper = mapper;
            _openAiService = openAiService;
            _logger = logger;
            _authService = authService;
        }

        [HttpPost("generate/{surveyId}")]
        [Authorize(Policy = $"{Permissions.CreateSurvey}")]
        public async Task<ActionResult<SurveyAnalysisViewModel>> GenerateAnalysis(int surveyId)
        {
            try
            {
                var userId = _authService.GetUserId(User);
                if (userId == 0)
                {
                    return Unauthorized();
                }

                // Verify user owns the survey or is admin
                var survey = await _context.Surveys.FindAsync(surveyId);
                if (survey == null)
                {
                    return NotFound("Survey not found");
                }

                bool isAdmin = User.IsInRole("Admin");
                if (!isAdmin && survey.CreatedById != userId)
                {
                    return Forbid();
                }

                // Check if there are any responses
                var questionIds = await _context.Questions
                    .Where(q => q.SurveyId == surveyId)
                    .Select(q => q.Id)
                    .ToListAsync();

                var hasResponses = await _context.Answers
                    .AnyAsync(a => questionIds.Contains(a.QuestionId) && a.Complete);

                if (!hasResponses)
                {
                    return BadRequest("Cannot generate analysis. Survey has no responses yet.");
                }

                // Determine user's local date range (today) using client-provided timezone offset
                int offsetMinutes = 0;
                if (Request.Headers.TryGetValue("X-Timezone-Offset", out var headerValues)
                    && int.TryParse(headerValues.FirstOrDefault(), out var parsed))
                {
                    offsetMinutes = parsed;
                }

                var offset = TimeSpan.FromMinutes(offsetMinutes);
                var userNow = DateTime.UtcNow + offset;
                var userTodayLocal = userNow.Date; // start of day in user's local time
                var dayStartUtc = userTodayLocal - offset;
                var dayEndUtc = userTodayLocal.AddDays(1) - offset;

                // Enforce: only one analysis per (user local) day
                var existingAnalysisToday = await _context.SurveyAnalyses
                    .Where(sa => sa.SurveyId == surveyId && sa.CreatedDate >= dayStartUtc && sa.CreatedDate < dayEndUtc)
                    .OrderByDescending(sa => sa.CreatedDate)
                    .FirstOrDefaultAsync();

#if !DEBUG
                if (existingAnalysisToday != null)
                {
                    return BadRequest("You can only generate one analysis per day. Please try again tomorrow (based on your local time).");
                }
#endif

                // Additionally: require new responses since the last time an analysis was generated (if any)
                var latestAnalysis = await _context.SurveyAnalyses
                    .Where(sa => sa.SurveyId == surveyId)
                    .OrderByDescending(sa => sa.CreatedDate)
                    .FirstOrDefaultAsync();

                if (latestAnalysis != null)
                {
                    var latestAnalysisDate = latestAnalysis.CreatedDate;
                    var newResponsesSinceAnalysis = await _context.Answers
                        .AnyAsync(a => questionIds.Contains(a.QuestionId) && a.Complete && a.CreatedDate > latestAnalysisDate);

#if !DEBUG
                    if (!newResponsesSinceAnalysis)
                    {
                        return BadRequest("No new responses since the last analysis. Please collect new responses before generating another analysis.");
                    }
#endif
                }

                // Generate the analysis using OpenAI
                var analysisResponse = await _openAiService.AnalyzeSurveyAsync(surveyId);
                if (analysisResponse == null)
                {
                    return StatusCode(StatusCodes.Status502BadGateway, "Unable to generate analysis");
                }

                // Format the analysis for storage
                var formattedAnalysis = FormatAnalysisForStorage(analysisResponse);

                // Save the analysis to the database
                var surveyAnalysis = new SurveyAnalysis
                {
                    SurveyId = surveyId,
                    Analysis = formattedAnalysis,
                    CreatedById = userId
                };

                _context.SurveyAnalyses.Add(surveyAnalysis);
                await _context.SaveChangesAsync();

                return Ok(_mapper.Map<SurveyAnalysisViewModel>(surveyAnalysis));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating survey analysis for survey {SurveyId}", surveyId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error generating analysis");
            }
        }

        [HttpGet("list/{surveyId}")]
        [Authorize(Policy = $"{Permissions.CreateSurvey}")]
        public async Task<ActionResult<List<SurveyAnalysisViewModel>>> GetAnalyses(int surveyId)
        {
            try
            {
                var userId = _authService.GetUserId(User);
                if (userId == 0)
                {
                    return Unauthorized();
                }

                // Verify user owns the survey or is admin
                var survey = await _context.Surveys.FindAsync(surveyId);
                if (survey == null)
                {
                    return NotFound("Survey not found");
                }

                bool isAdmin = User.IsInRole("Admin");
                if (!isAdmin && survey.CreatedById != userId)
                {
                    return Forbid();
                }

                var analyses = await _context.SurveyAnalyses
                    .Where(sa => sa.SurveyId == surveyId)
                    .OrderByDescending(sa => sa.CreatedDate)
                    .ToListAsync();

                return Ok(_mapper.Map<List<SurveyAnalysisViewModel>>(analyses));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analyses for survey {SurveyId}", surveyId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving analyses");
            }
        }

        [HttpGet("{id}")]
        [Authorize(Policy = $"{Permissions.CreateSurvey}")]
        public async Task<ActionResult<SurveyAnalysisViewModel>> GetAnalysis(int id)
        {
            try
            {
                var userId = _authService.GetUserId(User);
                if (userId == 0)
                {
                    return Unauthorized();
                }

                var analysis = await _context.SurveyAnalyses
                    .Include(sa => sa.Survey)
                    .FirstOrDefaultAsync(sa => sa.Id == id);

                if (analysis == null)
                {
                    return NotFound("Analysis not found");
                }

                bool isAdmin = User.IsInRole("Admin");
                if (!isAdmin && analysis.Survey.CreatedById != userId)
                {
                    return Forbid();
                }

                return Ok(_mapper.Map<SurveyAnalysisViewModel>(analysis));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analysis {AnalysisId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving analysis");
            }
        }

        private string FormatAnalysisForStorage(SurveyAnalysisResponse response)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("=== OVERALL ANALYSIS ===");
            sb.AppendLine();
            sb.AppendLine(response.OverallAnalysis);
            sb.AppendLine();
            sb.AppendLine("=== QUESTION-BY-QUESTION ANALYSIS ===");
            sb.AppendLine();

            foreach (var qa in response.QuestionAnalyses.OrderBy(q => q.QuestionNumber))
            {
                sb.AppendLine($"Question {qa.QuestionNumber}: {qa.QuestionText}");
                sb.AppendLine();
                sb.AppendLine(qa.Analysis);
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
