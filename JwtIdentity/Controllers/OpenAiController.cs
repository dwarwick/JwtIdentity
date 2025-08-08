using JwtIdentity.Common.ViewModels;
using JwtIdentity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JwtIdentity.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OpenAiController : ControllerBase
    {
        private readonly IOpenAi _openAiService;
        private readonly ILogger<OpenAiController> _logger;

        public OpenAiController(IOpenAi openAiService, ILogger<OpenAiController> logger)
        {
            _openAiService = openAiService;
            _logger = logger;
        }

        [HttpPost("generate")]
        public async Task<ActionResult<SurveyViewModel>> GenerateSurvey([FromBody] OpenAiSurveyRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Description))
            {
                return BadRequest();
            }

            try
            {
                var survey = await _openAiService.GenerateSurveyAsync(request.Description);
                if (survey == null)
                {
                    return StatusCode(StatusCodes.Status502BadGateway, "Unable to generate survey");
                }

                return Ok(survey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating survey from OpenAI");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error generating survey");
            }
        }
    }
}
